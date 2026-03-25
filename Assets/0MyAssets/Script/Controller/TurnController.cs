using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ゲーム全体のエントリーポイント。
/// 入力イベントを受付け、ターン処理（移動→スポーン→判定）を実行し、二重処理を防ぐ。
/// </summary>
public class TurnController : MonoBehaviour
{
    /// <summary>入力管理</summary>
    [SerializeField] private InputEventController _inputController;

    /// <summary>カード台帳</summary>
    [SerializeField] private CardRegistry _cardRegistry;

    /// <summary>カード移動・マージ処理</summary>
    [SerializeField] private CardMover _cardMover;

    /// <summary>新規カード生成</summary>
    [SerializeField] private CardSpawner _cardSpawner;

    /// <summary>ゲーム状態管理</summary>
    [SerializeField] private GameStateManager _gameStateManager;

    /// <summary>UI管理</summary>
    [SerializeField] private GameUIController _gameUIController;

    /// <summary>スコア管理</summary>
    [SerializeField] private ScoreManager _scoreManager;

    /// <summary>ターン処理中の二重入力を防ぐ。finallyで必ずロックを解除。</summary>
    private bool _isProcessing = false;

    private void Start()
    {
        // ボードを初期化し、入力イベントを接続
        InitializeBoard();
        _inputController.OnSwipe += HandleSwipe;
        _inputController.OnReset += ResetGame;
        _inputController.OnDebugClear += HandleDebugClear;
    }

    private void OnDestroy()
    {
        // クリーンアップ時にイベント接続を解除
        _inputController.OnSwipe -= HandleSwipe;
        _inputController.OnReset -= ResetGame;
        _inputController.OnDebugClear -= HandleDebugClear;
    }

    /// <summary>方向イベントハンドラ。処理中やゲーム終了時は何もしない。</summary>
    private void HandleSwipe(Direction direction)
    {
        if (_isProcessing || _gameStateManager.IsGameOver || _gameStateManager.IsCleared)
            return;

        ProcessTurnAsync(direction, destroyCancellationToken).Forget();
    }

    /// <summary>
    /// ターン処理を非同期実行。
    /// 1. CardMover でカード移動・マージ
    /// 2. CardSpawner で新規カード生成
    /// 3. MoveJudger でゲームオーバー判定
    /// </summary>
    private async UniTask ProcessTurnAsync(Direction direction, CancellationToken ct)
    {
        _isProcessing = true;
        _inputController.SetInputEnabled(false);

        try
        {
            var result = await _cardMover.ExecuteTurnAsync(direction, ct);
            if (!result.HasMoved) return;

            // マージ後処理（Model層操作はController側で）
            if (result.Merges != null)
            {
                foreach (var (survivor, absorbed, newValue) in result.Merges)
                {
                    Object.Destroy(absorbed.gameObject);
                    _cardRegistry.UpdateCardValue(survivor, newValue);
                }
            }

            foreach (int value in result.MergedValues)
            {
                _scoreManager.AddScore(value);
                if (value >= 2048)
                    _gameStateManager.SetCleared();
            }

            _cardSpawner.SpawnCard();

            if (!MoveJudger.CanMove(_cardRegistry.GetGridState(), GameDatabase.Instance.GridSize))
                _gameStateManager.SetGameOver();
        }
        finally
        {
            _isProcessing = false;
            _inputController.SetInputEnabled(true);
        }
    }

    /// <summary>ボードを初期化し、初期カード2枚をスポーン。</summary>
    private void InitializeBoard()
    {
        _cardRegistry.Reset();
        _cardSpawner.SpawnCard();
        _cardSpawner.SpawnCard();
    }

    /// <summary>デバッグ用：0キーで強制クリア。</summary>
    private void HandleDebugClear()
    {
        if (_isProcessing || _gameStateManager.IsGameOver || _gameStateManager.IsCleared)
            return;

        _gameStateManager.SetCleared();
    }

    /// <summary>ゲームをリセット。全状態を初期化し、ボードを再構築。</summary>
    private void ResetGame()
    {
        if (_isProcessing) return;

        _gameStateManager.Reset();
        _scoreManager.Reset();
        _gameUIController.Reset();
        InitializeBoard();
    }
}

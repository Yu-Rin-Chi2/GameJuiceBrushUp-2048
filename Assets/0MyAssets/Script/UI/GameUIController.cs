using TMPro;
using UnityEngine;

/// <summary>
/// UIを更新する。スコア・ゲームステータス変更時にイベントを購読し、
/// スコアテキスト、ゲームオーバー・クリアパネルを表示。
/// </summary>
public class GameUIController : MonoBehaviour
{
    /// <summary>スコアを表示するTextMeshPro</summary>
    [SerializeField] private TextMeshProUGUI _scoreText;

    /// <summary>通常時に表示するメインUIパネル</summary>
    [SerializeField] private GameObject _mainPanel;

    /// <summary>ゲームオーバー時に表示するUIパネル</summary>
    [SerializeField] private GameObject _gameOverPanel;

    /// <summary>ゲームクリア時に表示するUIパネル</summary>
    [SerializeField] private GameObject _clearedPanel;

    /// <summary>スコア管理</summary>
    [SerializeField] private ScoreManager _scoreManager;

    /// <summary>ゲーム状態管理</summary>
    [SerializeField] private GameStateManager _gameStateManager;

    private void Start()
    {
        // 全パネルを非表示にして Main をアクティブ化
        HideAllPanels();
        _mainPanel.SetActive(true);

        // スコア、ゲームステータスのイベントを購読し、UI更新を登録
        _scoreManager.OnScoreChanged += UpdateScore;
        _gameStateManager.OnGameOver += ShowGameOver;
        _gameStateManager.OnGameCleared += ShowCleared;

        // 現在のスコアを表示
        UpdateScore(_scoreManager.Score);
    }


    private void OnDestroy()
    {
        // イベント購読解除
        _scoreManager.OnScoreChanged -= UpdateScore;
        _gameStateManager.OnGameOver -= ShowGameOver;
        _gameStateManager.OnGameCleared -= ShowCleared;
    }

    /// <summary>スコアをUI上で更新。ScoreManagerのイベント購読先</summary>
    /// <param name="score">更新されたスコア</param>
    private void UpdateScore(int score)
    {
        _scoreText.text = $"{score}";
    }

    /// <summary>ゲームオーバーパネルを表示。GameStateManagerのイベント購読先</summary>
    private void ShowGameOver()
    {
        HideAllPanels();
        _gameOverPanel.SetActive(true);
    }

    /// <summary>ゲームクリアパネルを表示。GameStateManagerのイベント購読先</summary>
    private void ShowCleared()
    {
        HideAllPanels();
        _clearedPanel.SetActive(true);
    }

    /// <summary>全パネルを非表示にしてゲームをリセット。Main パネルをアクティブ化。</summary>
    public void Reset()
    {
        HideAllPanels();
        _mainPanel.SetActive(true);
    }

    /// <summary>全UI パネルを非表示化。</summary>
    private void HideAllPanels()
    {
        _mainPanel.SetActive(false);
        _gameOverPanel.SetActive(false);
        _clearedPanel.SetActive(false);
    }
}

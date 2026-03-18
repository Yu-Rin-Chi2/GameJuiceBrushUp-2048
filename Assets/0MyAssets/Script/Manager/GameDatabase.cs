using UnityEngine;

/// <summary>
/// ゲーム全体で必要な設定データ(ScriptableObjects)を一元管理するシングルトン。
/// BoardSettings, CardColorTable への依存を一箇所に集中させ、
/// 各クラスは GameDatabase を通じてのみ設定データにアクセスする。
/// </summary>
public class GameDatabase : MonoBehaviour
{
    public static GameDatabase Instance { get; private set; }

    [SerializeField] private BoardSettings _boardSettings;
    [SerializeField] private CardColorTable _cardColorTable;

    private void Awake()
    {
        // シングルトン処理
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ========== BoardSettings Facade ==========

    /// <summary>グリッドのサイズ</summary>
    public int GridSize => _boardSettings.GridSize;

    /// <summary>グリッド座標から UI Canvas 上のアンカー位置を計算</summary>
    public Vector2 GetAnchoredPosition(int row, int col)
        => _boardSettings.GetAnchoredPosition(row, col);

    /// <summary>カード「2」をスポーンしてくる確率の母数</summary>
    public int SpawnValue2Weight => _boardSettings.SpawnValue2Weight;

    /// <summary>カード「4」をスポーンしてくる確率の母数</summary>
    public int SpawnValue4Weight => _boardSettings.SpawnValue4Weight;

    // ========== CardColorTable Facade ==========

    /// <summary>指定された数値に対応する色を取得</summary>
    public bool TryGetCardColors(int value, out Color bgColor, out Color textColor)
        => _cardColorTable.TryGetColors(value, out bgColor, out textColor);
}

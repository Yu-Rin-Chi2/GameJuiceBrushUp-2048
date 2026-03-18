using UnityEngine;

[CreateAssetMenu(fileName = "BoardSettings", menuName = "2048/BoardSettings")]
/// <summary>
/// グリッドの行列サイズとカード配置座標を計算するScriptableObject。
/// ゲーム各所で共有され、Editor上で設定を変更可能。
/// </summary>
public class BoardSettings : ScriptableObject
{
    [Header("Grid")]
    /// <summary>グリッドのサイズ（通常4）</summary>
    public int GridSize = 4;

    /// <summary>グリッド左上のUI上でのX絶対座標（pixel）</summary>
    public float OriginX = 90f;

    /// <summary>グリッド左上のUI上でのY絶対座標（pixel）</summary>
    public float OriginY = -90f;

    /// <summary>セル間の間隔を含む1セルの大きさ（pixel）</summary>
    public float CellSize = 140f;

    [Header("Spawn")]
    /// <summary>カード「2」のスポーン確率の重み</summary>
    public int SpawnValue2Weight = 9;

    /// <summary>カード「4」のスポーン確率の重み</summary>
    public int SpawnValue4Weight = 1;

    /// <summary>
    /// グリッド座標からUI上座標を計算。
    /// </summary>
    /// <param name="row">行番号（0以上）</param>
    /// <param name="col">列番号（0以上）</param>
    /// <returns>UI Canvas上のアンカー位置（Vector2）</returns>
    public Vector2 GetAnchoredPosition(int row, int col)
        => new Vector2(OriginX + col * CellSize, OriginY - row * CellSize);
}

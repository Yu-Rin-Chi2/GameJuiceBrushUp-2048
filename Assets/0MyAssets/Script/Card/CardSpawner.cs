using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 新しいカードをスポーンして、CardRegistryに登録。
/// 空きセルを検索し、ランダムに新カードを生成。確率はBoardSettingsで指定。
/// </summary>
public class CardSpawner : MonoBehaviour
{
    /// <summary>カード管理クラス</summary>
    [SerializeField] private CardRegistry _cardRegistry;

    /// <summary>生成するカードのPrefab</summary>
    [SerializeField] private CardView _cardPrefab;

    /// <summary>生成したカードのGameObject親</summary>
    [SerializeField] private Transform _cardParent;

    /// <summary>
    /// 空きセルを探し、ランダムに新カードを生成。
    /// 数値はBoardSettingsのを確率に基づいて決定(2が90%, 4が10%)。
    /// </summary>
    /// <returns>空きセルがあれば(row, col, value)を返し、なければnull</returns>
    public (int row, int col, int value)? SpawnCard()
    {
        var emptySpots = FindEmptySpots(_cardRegistry.GetGridState());
        // 空きセルがなければゲームオーバー
        if (emptySpots.Count == 0) return null;

        // 空きセルからランダムに一つ選択
        var (row, col) = emptySpots[Random.Range(0, emptySpots.Count)];

        // BoardSettingsの重み比率で数値（2 or 4）を決定
        int totalWeight = GameDatabase.Instance.SpawnValue2Weight + GameDatabase.Instance.SpawnValue4Weight;
        int value = Random.Range(0, totalWeight) < GameDatabase.Instance.SpawnValue2Weight ? 2 : 4;

        // Prefabを生成し、初期化し、CardRegistryに登録
        var card = Instantiate(_cardPrefab, _cardParent);
        card.Initialize(row, col, value);
        _cardRegistry.AddCard(card);

        Debug.Log($"[Spawn] ({row}, {col}) = {value}");
        return (row, col, value);
    }


    /// <summary>
    /// グリッド中の空きセル一覧を探索。
    /// </summary>
    /// <param name="grid">グリッド配列</param>
    /// <returns>空きセルの(row, col)一覧</returns>
    private List<(int, int)> FindEmptySpots(int[,] grid)
    {
        var spots = new List<(int, int)>();
        int size = GameDatabase.Instance.GridSize;
        // 全要素を走査して空きセル（0）を収集
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                if (grid[r, c] == 0)
                    spots.Add((r, c));
        return spots;
    }
}

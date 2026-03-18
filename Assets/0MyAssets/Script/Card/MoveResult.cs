using System.Collections.Generic;

/// <summary>
/// カード移動の結果を表す構造体。
/// 移動有無、マージされた数値一覧、マージ詳細を保持。
/// </summary>
public readonly struct MoveResult
{
    /// <summary>カードが移動したかどうか</summary>
    public readonly bool HasMoved;

    /// <summary>マージされた新数値の一覧（スコア加算・クリア判定用）</summary>
    public readonly List<int> MergedValues;

    /// <summary>マージ詳細（生存カード・吸収カード・新数値）。TurnControllerで後処理に使用。</summary>
    public readonly List<(CardView survivor, CardView absorbed, int newValue)> Merges;

    /// <summary>移動なしの結果</summary>
    public static readonly MoveResult None = new(false, null, null);

    public MoveResult(List<int> mergedValues, List<(CardView survivor, CardView absorbed, int newValue)> merges)
    {
        HasMoved = true;
        MergedValues = mergedValues;
        Merges = merges;
    }

    private MoveResult(bool hasMoved, List<int> mergedValues, List<(CardView, CardView, int)> merges)
    {
        HasMoved = hasMoved;
        MergedValues = mergedValues;
        Merges = merges;
    }
}

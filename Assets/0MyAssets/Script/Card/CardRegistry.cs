using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム中に存在する全カードを管理。
/// カードリストとグリッド配列を双方保持し、常に同期。
/// 他クラスからはこのクラス経由でカードを操作・削除・更新。
/// </summary>
public class CardRegistry : MonoBehaviour
{
    /// <summary>現在ゲーム中の全カードの参照リスト</summary>
    private List<CardView> _cards = new();

    /// <summary>グリッド状態。grid[row, col]にカードの数値を保持。空きセルは0</summary>
    private int[,] _grid;

    /// <summary>現在ゲーム中の全カードを取得。</summary>
    /// <returns>カードリスト</returns>
    public List<CardView> GetAllCards() => _cards;

    /// <summary>指定されたグリッド位置からカードを検索。</summary>
    /// <param name="row">行番号</param>
    /// <param name="col">列番号</param>
    /// <returns>該当カード、またはnull</returns>
    public CardView GetCard(int row, int col)
    {
        return _cards.Find(c => c.Row == row && c.Col == col);
    }

    /// <summary>新しいカードを追加し、グリッド配列を更新。</summary>
    /// <param name="card">追加するカード</param>
    public void AddCard(CardView card)
    {
        _cards.Add(card);
        _grid[card.Row, card.Col] = card.Value;
    }

    /// <summary>カードを削除し、グリッド配列を更新（0にリセット）。</summary>
    /// <param name="card">削除するカード</param>
    public void RemoveCard(CardView card)
    {
        _cards.Remove(card);
        _grid[card.Row, card.Col] = 0;
    }

    /// <summary>カード位置を更新し、グリッド配列を同期。</summary>
    /// <param name="card">更新するカード</param>
    /// <param name="newRow">新しい行番号</param>
    /// <param name="newCol">新しい列番号</param>
    public void UpdateCardPosition(CardView card, int newRow, int newCol)
    {
        _grid[card.Row, card.Col] = 0;
        card.SetGridPositionLogical(newRow, newCol);
        _grid[newRow, newCol] = card.Value;
    }

    /// <summary>カード数値を更新し、グリッド配列を同期。マージ時に生存カードの数値を更新。</summary>
    /// <param name="card">更新するカード</param>
    /// <param name="newValue">新しい数値</param>
    public void UpdateCardValue(CardView card, int newValue)
    {
        _grid[card.Row, card.Col] = newValue;
        card.SetValue(newValue);
    }

    /// <summary>現在のカード枚数を取得。</summary>
    public int CardCount => _cards.Count;

    /// <summary>空きマスの数を取得。</summary>
    public int EmptyCount
    {
        get
        {
            int size = GameDatabase.Instance.GridSize;
            return size * size - _cards.Count;
        }
    }

    /// <summary>グリッド全体のマス数を取得。</summary>
    public int TotalCells
    {
        get
        {
            int size = GameDatabase.Instance.GridSize;
            return size * size;
        }
    }

    /// <summary>現在のグリッド状態配列を取得。移動・マージ計算やゲームオーバー判定用。</summary>
    /// <returns>グリッド配列の参照</returns>
    public int[,] GetGridState() => _grid;

    /// <summary>全カードを破棄し、ゲームリセット。ゲーム開始時に呼ばれる。</summary>
    public void Reset()
    {
        // 残っている全カードをGameObjectごと破棄
        foreach (var card in _cards)
            if (card != null)
                Destroy(card.gameObject);
        _cards.Clear();
        _grid = new int[GameDatabase.Instance.GridSize, GameDatabase.Instance.GridSize];
    }
}

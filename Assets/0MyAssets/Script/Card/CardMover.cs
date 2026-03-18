using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// カード移動・マージを処理し、アニメーションを実行。
/// グリッド上を走査して移動目標・マージ先を計算し、DOTweenでアニメーション。
/// </summary>
public class CardMover : MonoBehaviour
{
    /// <summary>カード管理</summary>
    [SerializeField] private CardRegistry _cardRegistry;

    /// <summary>カード移動アニメーション設定</summary>
    [Header("Animation")]
    /// <summary>カード移動時間(1カード)</summary>
    [SerializeField] private float _moveDuration = 0.1f;
    /// <summary>カード移動のイージング</summary>
    [SerializeField] private Ease _moveEase = Ease.OutQuart;

    /// <summary>
    /// ターン開始から終了まで実行。
    /// 1. CalculateMovesで移動・マージを計算
    /// 2. AnimateMovesAsyncで全カード移動アニメ(並列)
    /// 3. AnimateMergesAsyncでマージアニメ(順次)
    /// </summary>
    /// <param name="direction">カード移動方向</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>移動結果（移動有無とマージされた数値一覧）</returns>
    public async UniTask<MoveResult> ExecuteTurnAsync(Direction direction, CancellationToken ct)
    {
        // 全カードの移動目標とマージ先を計算
        var (hasMoved, pendingMoves, merges) = CalculateMoves(direction);
        if (!hasMoved) return MoveResult.None;

        Debug.Log($"[Move Start] Direction={direction}, Cards={pendingMoves.Count}");

        // 移動アニメーションを並列実行
        await AnimateMovesAsync(pendingMoves, ct);

        // マージされた数値を記録
        var mergedValues = new List<int>(merges.Count);
        foreach (var (_, _, newValue) in merges)
            mergedValues.Add(newValue);

        Debug.Log($"[Move End] Merges={merges.Count}, MergedValues=[{string.Join(",", mergedValues)}]");

        return new MoveResult(mergedValues, merges);
    }


    /// <summary>
    /// 方向を受けて、移動・マージを計算。
    /// グリッドを方向に沿って走査し、各カードの移動目標を記録。
    /// マージ時は「生存カード」、「吸収カード」、「新数値」を記録。
    /// </summary>
    private (bool hasMoved, List<(CardView card, Vector2 targetPos)> moves, List<(CardView survivor, CardView absorbed, int newValue)> merges)
        CalculateMoves(Direction direction)
    {
        int size = GameDatabase.Instance.GridSize;
        // 方向からグリッド位置変化（行列デルタ）を取得
        var (dr, dc) = GetDelta(direction);
        // 方向に応じて走査順序を反転（移動先側のカードを優先処理）
        bool reverseRow = (direction == Direction.Down);
        bool reverseCol = (direction == Direction.Right);

        // 既にマージした位置を記録して、重複マージを防ぐ
        var mergedPositions = new HashSet<(int, int)>();
        var moves = new List<(CardView, Vector2)>();
        var merges = new List<(CardView, CardView, int)>();
        bool hasMoved = false;

        // 方向に応じた順序でグリッドを走査
        for (int row = reverseRow ? size - 1 : 0; reverseRow ? row >= 0 : row < size; row += reverseRow ? -1 : 1)
        {
            for (int col = reverseCol ? size - 1 : 0; reverseCol ? col >= 0 : col < size; col += reverseCol ? -1 : 1)
            {
                // 現在位置でカードを取得
                var card = _cardRegistry.GetCard(row, col);
                if (card == null) continue;

                int curRow = row, curCol = col;
                CardView mergeTarget = null;

                // 方向へ進める最遠の位置・マージ先を探索
                while (true)
                {
                    int nextRow = curRow + dr;
                    int nextCol = curCol + dc;

                    // 端に到達した場合は探索終了
                    if (nextRow < 0 || nextRow >= size || nextCol < 0 || nextCol >= size)
                        break;

                    var next = _cardRegistry.GetCard(nextRow, nextCol);
                    if (next == null)
                    {
                        // 空きセルなら一つ先に進む
                        curRow = nextRow;
                        curCol = nextCol;
                    }
                    else if (next.Value == card.Value && !mergedPositions.Contains((nextRow, nextCol)))
                    {
                        // 同値かつ未マージならマージ先を記録し探索終了
                        mergeTarget = next;
                        curRow = nextRow;
                        curCol = nextCol;
                        break;
                    }
                    else
                    {
                        // 異なる値のカードがある場合は探索終了
                        break;
                    }
                }

                // マージ先がある場合、吸収カードを削除し、生存カードの位置を記録
                if (mergeTarget != null)
                {
                    int newValue = card.Value * 2;
                    merges.Add((mergeTarget, card, newValue));
                    mergedPositions.Add((curRow, curCol));

                    // 吸収カードをグリッドから削除
                    _cardRegistry.RemoveCard(card);
                    moves.Add((card, GameDatabase.Instance.GetAnchoredPosition(curRow, curCol)));
                    hasMoved = true;
                }
                else if (curRow != row || curCol != col)
                {
                    // マージなし・移動のみの場合、位置を更新
                    moves.Add((card, GameDatabase.Instance.GetAnchoredPosition(curRow, curCol)));
                    _cardRegistry.UpdateCardPosition(card, curRow, curCol);
                    hasMoved = true;
                }
            }
        }

        return (hasMoved, moves, merges);
    }

    /// <summary>
    /// 移動アニメーションを並列実行。DOTweenを使用して全カードを同時に移動。
    /// </summary>
    private async UniTask AnimateMovesAsync(List<(CardView card, Vector2 pos)> moves, CancellationToken ct)
    {
        // 全カードのDOTweenタスクを作成
        var tasks = new List<UniTask>();
        foreach (var (card, pos) in moves)
        {
            // 各カードを目標位置へ移動
            tasks.Add(card.RectTransform
                .DOAnchorPos(pos, _moveDuration)
                .SetEase(_moveEase)
                .ToUniTask());
        }
        // 全タスクを待機
        await UniTask.WhenAll(tasks);
    }

    /// <summary>
    /// 方向からグリッド位置変化（行列デルタ）を計算。
    /// （行値、列値）を返す。
    /// </summary>
    private (int dr, int dc) GetDelta(Direction d) => d switch
    {
        // 左: 列を減らす
        Direction.Left  => (0, -1),
        // 右: 列を増やす
        Direction.Right => (0,  1),
        // 上: 行を減らす
        Direction.Up    => (-1, 0),
        // 下: 行を増やす
        Direction.Down  => ( 1, 0),
        _ => throw new System.ArgumentOutOfRangeException()
    };

}

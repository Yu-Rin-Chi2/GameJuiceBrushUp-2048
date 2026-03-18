/// <summary>
/// ゲーム続行可否を判定する純粋ロジッククラス。
/// 空きセルがあるか、隣接同値があるかを調べる。
/// </summary>
public static class MoveJudger
{
    /// <summary>
    /// グリッド状態から移動可能かを判定。
    /// 空きセルまたは隣接同値があればtrue。
    /// </summary>
    public static bool CanMove(int[,] grid, int size)
    {
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (grid[r, c] == 0) return true;
                if (c + 1 < size && grid[r, c] == grid[r, c + 1]) return true;
                if (r + 1 < size && grid[r, c] == grid[r + 1, c]) return true;
            }
        }

        return false;
    }
}

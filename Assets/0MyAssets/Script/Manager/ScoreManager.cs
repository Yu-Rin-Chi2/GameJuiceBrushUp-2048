using System;
using UnityEngine;

/// <summary>
/// ゲームスコアを管理し、更新時にUIへ通知する。
/// イベントを発火してスコアを伝達する。
/// </summary>
public class ScoreManager : MonoBehaviour
{
    /// <summary>現在のスコア</summary>
    public int Score { get; private set; }

    /// <summary>スコアが更新された時に発火されるイベント。購読クラスがUIを更新する。</summary>
    public event Action<int> OnScoreChanged;

    /// <summary>スコアを加算し、イベントを発火。マージ時に呼ばれる。</summary>
    /// <param name="points">加算するスコア</param>
    public void AddScore(int points)
    {
        Score += points;
        // UI更新のためにスコアを通知
        OnScoreChanged?.Invoke(Score);
    }

    /// <summary>スコアをリセットしイベントを発火。ゲーム開始時に呼ばれる。</summary>
    public void Reset()
    {
        Score = 0;
        OnScoreChanged?.Invoke(Score);
    }
}

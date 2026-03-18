using System;
using UnityEngine;

/// <summary>
/// ゲーム状態を管理し、終了時にイベントを発火。
/// UIパネル表示等を制御する。
/// </summary>
public class GameStateManager : MonoBehaviour
{
    /// <summary>ゲームオーバー状態か</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>ゲームクリア(2048を達成)したか</summary>
    public bool IsCleared { get; private set; }

    /// <summary>ゲームオーバー時に発火。UIパネル表示等を制御するクラスが購読する。</summary>
    public event Action OnGameOver;

    /// <summary>ゲームクリア時に発火。UIパネル表示等を制御するクラスが購読する。</summary>
    public event Action OnGameCleared;

    /// <summary>ゲームオーバーを設定しイベントを発火。重複発火を防ぐ。</summary>
    public void SetGameOver()
    {
        // 1回目のみ発火（重複防止）
        if (!IsGameOver)
        {
            IsGameOver = true;
            Debug.Log("[GameOver] No moves available.");
            // 購読クラスに通知
            OnGameOver?.Invoke();
        }
    }

    /// <summary>ゲームクリアを設定しイベントを発火。重複発火を防ぐ。</summary>
    public void SetCleared()
    {
        // 1回目のみ発火（重複防止）
        if (!IsCleared)
        {
            IsCleared = true;
            Debug.Log("[Clear] 2048 achieved!");
            // 購読クラスに通知
            OnGameCleared?.Invoke();
        }
    }

    /// <summary>ゲーム状態をリセット。ゲーム再開時に呼ばれる。</summary>
    public void Reset()
    {
        IsGameOver = false;
        IsCleared = false;
    }
}

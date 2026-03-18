using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CardColorTable", menuName = "2048/CardColorTable")]
/// <summary>
/// カードの数値と表示色(背景・テキスト)の対応テーブル。
/// ScriptableObjectで管理し、Editor上で色を設定可能。
/// </summary>
public class CardColorTable : ScriptableObject
{
    /// <summary>
    /// カードの数値と色を対応させるデータ構造。
    /// </summary>
    [Serializable]
    public struct CardColorEntry
    {
        /// <summary>カードの数値(2, 4, 8, 16... など)</summary>
        public int Value;

        /// <summary>カード背景色</summary>
        public Color BackgroundColor;

        /// <summary>カードのテキスト色</summary>
        public Color TextColor;
    }

    /// <summary>数値と色の対応表配列。Editor上で任意の数だけ設定可能</summary>
    public CardColorEntry[] Entries;

    /// <summary>
    /// 指定された数値に対応する色を取得。
    /// 完全一致がなければ配列の最後のエントリの色を使用。
    /// エントリが空の場合のみグレー+ホワイトをデフォルトとして返す。
    /// </summary>
    /// <param name="value">カードの数値</param>
    /// <param name="bgColor">取得した背景色(out)</param>
    /// <param name="textColor">取得したテキスト色(out)</param>
    /// <returns>色が見つかった場合true。エントリが空の場合false(デフォルト色を返す)</returns>
    public bool TryGetColors(int value, out Color bgColor, out Color textColor)
    {
        // 数値と完全一致するエントリを検索
        foreach (var entry in Entries)
        {
            if (entry.Value == value)
            {
                bgColor = entry.BackgroundColor;
                textColor = entry.TextColor;
                return true;
            }
        }
        // 一致なしの場合、配列の最後のエントリの色を使用
        if (Entries.Length > 0)
        {
            bgColor = Entries[^1].BackgroundColor;
            textColor = Entries[^1].TextColor;
            return true;
        }
        // エントリが空の場合、デフォルト色
        bgColor = Color.gray;
        textColor = Color.white;
        return false;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 一つのカードの表示（位置・数値・色）を担うViewクラス。
/// 数値を更新すると、BoardSettingsを使用してUI上の位置を計算、CardColorTableを使用して色を変更。
/// </summary>
public class CardView : MonoBehaviour
{
    /// <summary>カードの背景画像</summary>
    [SerializeField] private Image _backgroundImage;

    /// <summary>カードの数値を表示するTextMeshPro</summary>
    [SerializeField] private TextMeshProUGUI _valueText;

    /// <summary>カードの現在の数値</summary>
    public int Value { get; private set; }

    /// <summary>グリッド上の行番号</summary>
    public int Row { get; private set; }

    /// <summary>グリッド上の列番号</summary>
    public int Col { get; private set; }

    /// <summary>カードのRectTransform。DOTweenアニメーション用</summary>
    public RectTransform RectTransform { get; private set; }

    private void Awake() => RectTransform = GetComponent<RectTransform>();

    /// <summary>
    /// カードを指定位置・数値で初期化。最初にSpawnerから呼ばれる。
    /// </summary>
    /// <param name="row">グリッド行番号</param>
    /// <param name="col">グリッド列番号</param>
    /// <param name="value">カードの数値</param>
    public void Initialize(int row, int col, int value)
    {
        SetGridPosition(row, col);
        SetValue(value);
    }

    /// <summary>
    /// グリッド上の位置を更新し、UI上座標を再計算。
    /// </summary>
    /// <param name="row">新しい行番号</param>
    /// <param name="col">新しい列番号</param>
    public void SetGridPosition(int row, int col)
    {
        Row = row;
        Col = col;
        RectTransform.anchoredPosition = GameDatabase.Instance.GetAnchoredPosition(row, col);
    }

    /// <summary>
    /// グリッド論理座標のみ更新（UI座標は変えない）。アニメーション前の論理位置更新用。
    /// </summary>
    public void SetGridPositionLogical(int row, int col)
    {
        Row = row;
        Col = col;
    }

    /// <summary>
    /// カードの数値を更新し、表示を変更。CardColorTableを使用して背景色・テキスト色を更新。
    /// </summary>
    /// <param name="value">新しい数値</param>
    public void SetValue(int value)
    {
        Value = value;
        _valueText.text = value.ToString();
        // CardColorTableから数値に対応した色を取得して適用
        if (GameDatabase.Instance.TryGetCardColors(value, out var bgColor, out var textColor))
        {
            _backgroundImage.color = bgColor;
            _valueText.color = textColor;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// キーボード入力およびスワイプ（マウスドラッグ/タッチ）を受け付け、Directionイベントを発火。
/// 二重入力を防ぐ仕組みを提供。
/// スワイプ検出はInput SystemのPointerデバイスを使用し、WebGL上でもPC・モバイル両対応。
/// </summary>
public class InputEventController : MonoBehaviour
{
    /// <summary>方向キー押下時に発火されるイベント。Direction値を伝える。</summary>
    public event Action<Direction> OnSwipe;

    /// <summary>リセットキー(R)押下時に発火されるイベント。</summary>
    public event Action OnReset;

    /// <summary>デバッグクリアキー(0)押下時に発火されるイベント。</summary>
    public event Action OnDebugClear;

    /// <summary>入力システムを管理</summary>
    private InputSystem_Actions _inputActions;

    /// <summary>入力が有効かどうか。ターン処理中の二重入力を防ぐためTurnControllerから制御される。</summary>
    private bool _inputEnabled = true;

    /// <summary>スワイプ判定の最小距離（スクリーンピクセル）。</summary>
    [SerializeField] private float _swipeThreshold = 50f;

    /// <summary>スワイプ操作中かどうか。</summary>
    private bool _isSwiping;

    /// <summary>スワイプ開始位置（スクリーン座標）。</summary>
    private Vector2 _swipeStartPos;

    private void Awake()
    {
        _inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.MoveUp.performed += OnMoveUp;
        _inputActions.Player.MoveDown.performed += OnMoveDown;
        _inputActions.Player.MoveLeft.performed += OnMoveLeft;
        _inputActions.Player.MoveRight.performed += OnMoveRight;
        _inputActions.Player.Reset.performed += OnResetPerformed;
        _inputActions.Player.DebugClear.performed += OnDebugClearPerformed;
    }

    private void OnDisable()
    {
        _inputActions.Player.MoveUp.performed -= OnMoveUp;
        _inputActions.Player.MoveDown.performed -= OnMoveDown;
        _inputActions.Player.MoveLeft.performed -= OnMoveLeft;
        _inputActions.Player.MoveRight.performed -= OnMoveRight;
        _inputActions.Player.Reset.performed -= OnResetPerformed;
        _inputActions.Player.DebugClear.performed -= OnDebugClearPerformed;
        _inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        _inputActions?.Dispose();
    }

    private void OnMoveUp(InputAction.CallbackContext _) => FireSwipe(Direction.Up);
    private void OnMoveDown(InputAction.CallbackContext _) => FireSwipe(Direction.Down);
    private void OnMoveLeft(InputAction.CallbackContext _) => FireSwipe(Direction.Left);
    private void OnMoveRight(InputAction.CallbackContext _) => FireSwipe(Direction.Right);
    private void OnResetPerformed(InputAction.CallbackContext _) => OnReset?.Invoke();
    private void OnDebugClearPerformed(InputAction.CallbackContext _) => OnDebugClear?.Invoke();

    /// <summary>入力の有効無効を切替。TurnControllerからターン処理中の二重入力防止、ゲーム終了時の入力無効化に使用。</summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (!enabled) _isSwiping = false;
    }

    /// <summary>
    /// スワイプ検出。Pointer（Mouse/Touchscreen共通）のプレス開始・終了を監視する。
    /// ブラウザはタッチをPointerイベントに変換するため、WebGLでもそのまま動作する。
    /// </summary>
    private void Update()
    {
        if (!_inputEnabled) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        if (pointer.press.wasPressedThisFrame)
        {
            _isSwiping = true;
            _swipeStartPos = pointer.position.ReadValue();
        }
        else if (pointer.press.wasReleasedThisFrame && _isSwiping)
        {
            _isSwiping = false;
            TryDetectSwipe(pointer.position.ReadValue());
        }
    }

    /// <summary>開始位置と終了位置の差分から方向を判定。閾値未満のタップは無視する。</summary>
    private void TryDetectSwipe(Vector2 endPos)
    {
        Vector2 delta = endPos - _swipeStartPos;
        if (delta.magnitude < _swipeThreshold) return;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            FireSwipe(delta.x > 0 ? Direction.Right : Direction.Left);
        else
            FireSwipe(delta.y > 0 ? Direction.Up : Direction.Down);
    }

    private void FireSwipe(Direction dir)
    {
        if (!_inputEnabled) return;
        OnSwipe?.Invoke(dir);
    }
}

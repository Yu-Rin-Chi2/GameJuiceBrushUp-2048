using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// キーボード入力を受け付け、Directionイベントを発火。
/// 二重入力を防ぐ仕組みを提供。
/// </summary>
public class InputEventController : MonoBehaviour
{
    /// <summary>方向キー押下時に発火されるイベント。Direction値を伝える。</summary>
    public event Action<Direction> OnSwipe;

    /// <summary>リセットキー(R)押下時に発火されるイベント。</summary>
    public event Action OnReset;

    /// <summary>入力システムを管理</summary>
    private InputSystem_Actions _inputActions;

    /// <summary>入力が有効かどうか。ターン処理中の二重入力を防ぐためTurnControllerから制御される。</summary>
    private bool _inputEnabled = true;

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
    }

    private void OnDisable()
    {
        _inputActions.Player.MoveUp.performed -= OnMoveUp;
        _inputActions.Player.MoveDown.performed -= OnMoveDown;
        _inputActions.Player.MoveLeft.performed -= OnMoveLeft;
        _inputActions.Player.MoveRight.performed -= OnMoveRight;
        _inputActions.Player.Reset.performed -= OnResetPerformed;
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

    /// <summary>入力の有効無効を切替。TurnControllerからターン処理中の二重入力防止、ゲーム終了時の入力無効化に使用。</summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    private void FireSwipe(Direction dir)
    {
        if (!_inputEnabled) return;
        OnSwipe?.Invoke(dir);
    }
}

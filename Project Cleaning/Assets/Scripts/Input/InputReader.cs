using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, InputSystem.IInputActions
{
    public bool IsDragging { get; private set; }
    public Vector2 ScreenPosition { get; private set; }

    public event Action<Vector2> TapEvent;
    public event Action<Vector2> HoldEvent;
    public event Action<Vector2> DragStartEvent;
    public event Action<Vector2> DragEvent;
    public event Action<Vector2> DragEndEvent;

    private InputSystem controls;
    private float pressStartTime;
    private readonly float holdThreshold = 0.3f;
    private bool isHolding;

    private void Awake()
    {
        controls = new InputSystem();
        controls.Input.AddCallbacks(this);
    }

    private void OnEnable()
    {
        controls.Input.Enable();
    }

    private void OnDisable()
    {
        controls.Input.Disable();
    }

    public void OnTap(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        TapEvent?.Invoke(ScreenPosition);
    }

    public void OnHold(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            pressStartTime = Time.time;
            isHolding = false;
        }
        else if (context.performed)
        {
            if (!isHolding && Time.time - pressStartTime >= holdThreshold)
            {
                isHolding = true;
                HoldEvent?.Invoke(ScreenPosition);
            }
        }
        else if (context.canceled)
        {
            isHolding = false;
        }
    }

    public void OnDrag(InputAction.CallbackContext context)
    {
        ScreenPosition = context.ReadValue<Vector2>();

        if (context.started)
        {
            IsDragging = true;
            DragStartEvent?.Invoke(ScreenPosition);
        }
        else if (context.performed)
        {
            DragEvent?.Invoke(ScreenPosition);
        }
        else if (context.canceled)
        {
            IsDragging = false;
            DragEndEvent?.Invoke(ScreenPosition);
        }
    }
}

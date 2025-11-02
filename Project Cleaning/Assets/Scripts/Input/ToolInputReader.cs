using System;
using UnityEngine;

public class ToolInputReader : MonoBehaviour
{
    public event Action<Vector2> OnTouchStart;
    public event Action<Vector2> OnTouchMove;
    public event Action<Vector2> OnTouchEnd;
    public event Action<Vector2> OnTouchTap;

    private float tapThreshold = 0.2f; // detik
    private float touchStartTime;
    private Vector2 startPos;

    private void Update()
    {
#if UNITY_EDITOR
        HandleMouseInput(); // biar bisa test di Editor
#else
        HandleTouchInput();
#endif
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            touchStartTime = Time.time;
            OnTouchStart?.Invoke(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0))
        {
            OnTouchMove?.Invoke(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            OnTouchEnd?.Invoke(Input.mousePosition);

            if (Time.time - touchStartTime <= tapThreshold)
                OnTouchTap?.Invoke(Input.mousePosition);
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        Vector2 pos = touch.position;

        switch (touch.phase)
        {
            case TouchPhase.Began:
                startPos = pos;
                touchStartTime = Time.time;
                OnTouchStart?.Invoke(pos);
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                OnTouchMove?.Invoke(pos);
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                OnTouchEnd?.Invoke(pos);
                if (Time.time - touchStartTime <= tapThreshold)
                    OnTouchTap?.Invoke(pos);
                break;
        }
    }
}

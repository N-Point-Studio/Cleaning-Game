using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ClickableObject : MonoBehaviour
{

    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;

    [Header("Scene Navigation")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool canChangeScene = false;

    // Public accessors for AdvancedInputManager
    public AudioClip ClickSound => clickSound;

    [Header("Events")]
    public UnityEvent OnObjectClicked;

    private AudioSource audioSource;
    private bool isFocused = false;

    private void Awake()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Called when object is clicked
    /// </summary>
    public void OnClick()
    {
        if (TopDownCameraController.Instance.IsTransitioning()) return;

        // Audio feedback
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        // Focus camera on this object
        TopDownCameraController.Instance.SetFocusTarget(transform);
        TopDownCameraController.Instance.SwitchState(TopDownCameraController.Instance.focusState);

        // Mark as focused
        isFocused = true;

        // Trigger custom events
        OnObjectClicked?.Invoke();

        Debug.Log($"Clicked on: {gameObject.name}");
    }


    /// <summary>
    /// Called when camera returns to overview (object loses focus)
    /// </summary>
    public void OnLoseFocus()
    {
        isFocused = false;
    }


    /// <summary>
    /// Check if this object is currently camera-focused
    /// </summary>
    public bool IsFocused()
    {
        return isFocused;
    }

    /// <summary>
    /// Force focus state (useful for external control)
    /// </summary>
    public void SetFocusState(bool focused)
    {
        isFocused = focused;
        if (!focused)
        {
            OnLoseFocus();
        }
    }

    /// <summary>
    /// Get the target scene name for this clickable object
    /// </summary>
    public string GetSceneName()
    {
        return targetSceneName;
    }

    /// <summary>
    /// Check if this object can trigger a scene change
    /// </summary>
    public bool CanChangeScene()
    {
        return canChangeScene && !string.IsNullOrEmpty(targetSceneName);
    }

    // NOTE: Unity's built-in mouse events are disabled to prevent conflicts with AdvancedInputManager
    // The AdvancedInputManager handles all input and calls OnClick() directly when needed

    // Optional: Unity Event triggers for Inspector setup (DISABLED - using AdvancedInputManager instead)
    /*
    private void OnMouseEnter()
    {
        if (enableHoverEffect)
        {
            OnHover();
        }
    }

    private void OnMouseExit()
    {
        if (enableHoverEffect)
        {
            OnUnhover();
        }
    }

    private void OnMouseDown()
    {
        OnClick();
    }
    */
}
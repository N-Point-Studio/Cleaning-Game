using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class ClickableObject : MonoBehaviour
{

    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;

    [Header("Scene Navigation")]
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private bool canChangeScene = false;
    [SerializeField] private bool useTransitionAnimation = true;
    [SerializeField] private EasyTransition.TransitionSettings transitionSettings;

    [Header("Image Popup")]
    [SerializeField] private GameObject popupImageGameObject;
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private bool autoHideAfterSeconds = true;
    [SerializeField] private float autoHideDelay = 3f;
    [SerializeField] private bool onlyShowInExplorationMode = true;

    // Public accessors for AdvancedInputManager
    public AudioClip ClickSound => clickSound;

    [Header("Events")]
    public UnityEvent OnObjectClicked;

    private AudioSource audioSource;
    private bool isFocused = false;

    // Image popup components
    private CanvasGroup popupCanvasGroup;
    private Vector3 originalPopupScale;
    private bool isPopupVisible = false;

    private void Awake()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();

        // Setup popup image if assigned
        SetupPopupImage();
    }

    private void SetupPopupImage()
    {
        if (popupImageGameObject != null)
        {
            Debug.Log($"Setting up popup for: {popupImageGameObject.name}");

            // Try to get or add CanvasGroup
            popupCanvasGroup = popupImageGameObject.GetComponent<CanvasGroup>();
            if (popupCanvasGroup == null)
            {
                Debug.Log("No CanvasGroup found, adding one");
                popupCanvasGroup = popupImageGameObject.AddComponent<CanvasGroup>();
            }

            originalPopupScale = popupImageGameObject.transform.localScale;
            Debug.Log($"Original scale saved: {originalPopupScale}");

            HidePopupImmediate();
        }
        else
        {
            Debug.LogWarning("popupImageGameObject is null in SetupPopupImage!");
        }
    }

    /// <summary>
    /// Called when object is clicked
    /// </summary>
    public void OnClick()
    {
        // Trigger the Unity Event first
        OnObjectClicked?.Invoke();

        // Show popup image (allow even during camera transitions)
        ShowPopupImage();
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

    /// <summary>
    /// Check if transition animation should be used
    /// </summary>
    public bool UseTransitionAnimation()
    {
        return useTransitionAnimation;
    }

    /// <summary>
    /// Get the Easy Transition settings for scene change
    /// </summary>
    public EasyTransition.TransitionSettings GetTransitionSettings()
    {
        return transitionSettings;
    }

    /// <summary>
    /// Show popup image with smooth animation
    /// </summary>
    public void ShowPopupImage()
    {
        if (popupImageGameObject == null)
        {
            Debug.LogWarning($"{gameObject.name}: Popup Image Game Object is not assigned!");
            return;
        }

        if (isPopupVisible)
        {
            return;
        }

        // Check exploration mode restriction
        if (onlyShowInExplorationMode && AdvancedInputManager.Instance != null)
        {
            if (!AdvancedInputManager.Instance.IsInExplorationMode())
            {
                return;
            }
        }

        isPopupVisible = true;
        popupImageGameObject.SetActive(true);

        // Start animation from zero scale and alpha
        popupImageGameObject.transform.localScale = Vector3.zero;
        popupCanvasGroup.alpha = 0f;

        // Create smooth animation sequence
        var sequence = DOTween.Sequence();

        // Scale up with bounce effect
        sequence.Append(popupImageGameObject.transform.DOScale(originalPopupScale, animationDuration)
                       .SetEase(Ease.OutBack));

        // Fade in
        sequence.Join(popupCanvasGroup.DOFade(1f, animationDuration * 0.8f)
                     .SetEase(Ease.OutSine));

        // Auto hide if enabled
        if (autoHideAfterSeconds)
        {
            sequence.AppendInterval(autoHideDelay);
            sequence.AppendCallback(() =>
            {
                HidePopupImage();
            });
        }
    }

    /// <summary>
    /// Hide popup image with smooth animation
    /// </summary>
    public void HidePopupImage()
    {
        if (popupImageGameObject == null || !isPopupVisible) return;

        var sequence = DOTween.Sequence();

        // Scale down
        sequence.Append(popupImageGameObject.transform.DOScale(Vector3.zero, animationDuration * 0.7f)
                       .SetEase(Ease.InBack));

        // Fade out
        sequence.Join(popupCanvasGroup.DOFade(0f, animationDuration * 0.5f)
                     .SetEase(Ease.InSine));

        // Hide when complete
        sequence.OnComplete(HidePopupImmediate);
    }

    private void HidePopupImmediate()
    {
        if (popupImageGameObject != null)
        {
            isPopupVisible = false;
            popupImageGameObject.transform.localScale = Vector3.zero;
            popupCanvasGroup.alpha = 0f;
            popupImageGameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle popup visibility
    /// </summary>
    public void TogglePopupImage()
    {
        if (isPopupVisible)
            HidePopupImage();
        else
            ShowPopupImage();
    }

    /// <summary>
    /// Test popup manually (for debugging)
    /// </summary>
    [System.Obsolete("For debugging only")]
    public void TestPopupManual()
    {
        Debug.Log("=== MANUAL TEST POPUP ===");
        Debug.Log($"Popup GameObject: {(popupImageGameObject != null ? popupImageGameObject.name : "NULL")}");
        Debug.Log($"Is Popup Visible: {isPopupVisible}");
        Debug.Log($"Canvas Group: {(popupCanvasGroup != null ? "Found" : "NULL")}");

        if (popupImageGameObject != null)
        {
            Debug.Log($"GameObject Active: {popupImageGameObject.activeInHierarchy}");
            Debug.Log($"Has Image Component: {popupImageGameObject.GetComponent<Image>() != null}");
        }

        ShowPopupImage();
    }

    private void OnDestroy()
    {
        // Clean up DOTween animations
        if (popupImageGameObject != null)
        {
            DOTween.Kill(popupImageGameObject.transform);
        }
        if (popupCanvasGroup != null)
        {
            DOTween.Kill(popupCanvasGroup);
        }
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
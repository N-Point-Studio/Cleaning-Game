using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

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

    [Header("Text Popup")]
    [SerializeField] private GameObject popupTextGameObject;
    [SerializeField] private TextMeshProUGUI textMeshPro;
    [SerializeField] private float characterAnimationSpeed = 0.05f;
    [SerializeField] private float scaleAnimationDuration = 0.6f;
    [SerializeField] private bool autoHideAfterSeconds = true;
    [SerializeField] private float autoHideDelay = 3f;
    [SerializeField] private bool onlyShowInExplorationMode = true;

    [Header("Shake Animation")]
    [SerializeField] private bool enableShakeAnimation = true;
    [SerializeField] private float shakeIntensity = 0.05f;
    [SerializeField] private float shakeDuration = 3f;
    [SerializeField] private bool shakeOnlyWhenFocused = true;

    [Header("Inspectable Object")]
    [SerializeField] private bool isInspectable = true;

    // Public accessors for AdvancedInputManager
    public AudioClip ClickSound => clickSound;

    [Header("Events")]
    public UnityEvent OnObjectClicked;

    private AudioSource audioSource;
    private bool isFocused = false;

    // Text popup components
    private Vector3 originalPopupScale;
    private bool isPopupVisible = false;
    private string fullTextContent;
    private Sequence currentTextSequence;

    // Shake animation components
    private Vector3 originalPosition;
    private Sequence currentShakeSequence;

    // Inspectable components
    private InspectableJar inspectableJar;

    private void Awake()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();

        // Setup popup image if assigned
        SetupPopupImage();

        // Setup shake animation
        SetupShakeAnimation();

        // Setup inspectable functionality
        SetupInspectableObject();
    }

    private void SetupPopupImage()
    {
        if (popupTextGameObject != null)
        {
            // Auto-find TextMeshPro component if not assigned
            if (textMeshPro == null)
            {
                textMeshPro = popupTextGameObject.GetComponent<TextMeshProUGUI>();
                if (textMeshPro == null)
                {
                    textMeshPro = popupTextGameObject.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (textMeshPro != null)
            {
                // Store the full text content
                fullTextContent = textMeshPro.text;
                originalPopupScale = popupTextGameObject.transform.localScale;

                // Hide initially
                popupTextGameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("No TextMeshProUGUI component found in popup GameObject!");
            }
        }
        else
        {
            Debug.LogWarning("popupTextGameObject is null!");
        }
    }

    private void SetupShakeAnimation()
    {
        // Store the original position
        originalPosition = transform.localPosition;

        // Don't start shake animation automatically - only when focused/clicked
    }

    private void SetupInspectableObject()
    {
        if (isInspectable)
        {
            // Get or add InspectableJar component
            inspectableJar = GetComponent<InspectableJar>();
            if (inspectableJar == null)
            {
                inspectableJar = gameObject.AddComponent<InspectableJar>();
            }

            // Disable inspectable functionality initially - only enable when focused in zoom mode
            inspectableJar.enabled = false;
        }
    }

    /// <summary>
    /// Called when object is clicked
    /// </summary>
    public void OnClick()
    {
        // Trigger the Unity Event first
        OnObjectClicked?.Invoke();

        // Show popup text (allow even during camera transitions)
        ShowPopupText();

        // Set focus state and start shake animation when clicked (if in exploration mode and enabled)
        if (enableShakeAnimation && shakeOnlyWhenFocused)
        {
            if (AdvancedInputManager.Instance != null && AdvancedInputManager.Instance.IsInExplorationMode())
            {
                isFocused = true;
                StartShakeAnimation();
            }
        }
    }


    /// <summary>
    /// Called when camera returns to overview (object loses focus)
    /// </summary>
    public void OnLoseFocus()
    {
        isFocused = false;

        // Stop shake animation when losing focus
        if (enableShakeAnimation && shakeOnlyWhenFocused)
        {
            StopShakeAnimation();
        }

        // Disable inspectable functionality when losing focus
        if (isInspectable && inspectableJar != null)
        {
            inspectableJar.enabled = false;
        }
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
        else
        {
            // Start shake animation when gaining focus (if enabled and in exploration mode)
            if (enableShakeAnimation && shakeOnlyWhenFocused)
            {
                if (AdvancedInputManager.Instance != null && AdvancedInputManager.Instance.IsInExplorationMode())
                {
                    StartShakeAnimation();
                }
            }

            // Enable inspectable functionality when entering zoom mode
            if (isInspectable && inspectableJar != null && AdvancedInputManager.Instance != null)
            {
                if (AdvancedInputManager.Instance.IsInZoomMode())
                {
                    inspectableJar.enabled = true;
                }
            }
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
    /// Check if this object can be inspected with pinch/zoom gestures
    /// </summary>
    public bool IsInspectable()
    {
        return isInspectable;
    }

    /// <summary>
    /// Enable or disable inspectable functionality
    /// </summary>
    public void SetInspectableEnabled(bool enabled)
    {
        if (isInspectable && inspectableJar != null)
        {
            inspectableJar.enabled = enabled;
        }
    }

    /// <summary>
    /// Show popup text with character-by-character animation (Fixed version)
    /// </summary>
    public void ShowPopupText()
    {
        // Auto-find components if not set
        if (textMeshPro == null)
        {
            textMeshPro = popupTextGameObject?.GetComponent<TextMeshProUGUI>();
            if (textMeshPro == null)
                textMeshPro = popupTextGameObject?.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (popupTextGameObject == null || textMeshPro == null)
        {
            Debug.LogError($"TextMeshPro setup failed! GameObject: {popupTextGameObject != null}, TextMeshPro: {textMeshPro != null}");
            return;
        }

        if (string.IsNullOrEmpty(fullTextContent))
            fullTextContent = textMeshPro.text;

        if (string.IsNullOrEmpty(fullTextContent))
        {
            Debug.LogError("No text content to display!");
            return;
        }

        if (isPopupVisible) return;

        // Check exploration mode
        if (onlyShowInExplorationMode && AdvancedInputManager.Instance != null)
        {
            if (!AdvancedInputManager.Instance.IsInExplorationMode())
                return;
        }

        Debug.Log($"Starting text animation: '{fullTextContent}'");

        isPopupVisible = true;
        popupTextGameObject.SetActive(true);

        // Kill existing animation
        if (currentTextSequence != null && currentTextSequence.IsActive())
            currentTextSequence.Kill();

        // Set full text, start with no visible characters
        textMeshPro.text = fullTextContent;
        textMeshPro.maxVisibleCharacters = 0;

        // Character-by-character animation
        currentTextSequence = DOTween.Sequence();
        currentTextSequence.Append(DOTween.To(() => textMeshPro.maxVisibleCharacters,
                                             x => textMeshPro.maxVisibleCharacters = x,
                                             fullTextContent.Length,
                                             fullTextContent.Length * characterAnimationSpeed)
                                           .SetEase(Ease.Linear));

        // Auto hide (commented out to keep text visible)
        // if (autoHideAfterSeconds)
        // {
        //     currentTextSequence.AppendInterval(autoHideDelay);
        //     currentTextSequence.AppendCallback(HidePopupText);
        // }

        Debug.Log("Text animation started!");
    }

    /// <summary>
    /// Hide popup text (Simple version)
    /// </summary>
    public void HidePopupText()
    {
        if (popupTextGameObject == null || !isPopupVisible) return;

        Debug.Log("Hiding text popup");

        // Kill any existing animation
        if (currentTextSequence != null && currentTextSequence.IsActive())
        {
            currentTextSequence.Kill();
        }

        // Simple hide
        isPopupVisible = false;
        textMeshPro.maxVisibleCharacters = 0;
        popupTextGameObject.SetActive(false);
    }

    /// <summary>
    /// Toggle popup visibility
    /// </summary>
    public void TogglePopupText()
    {
        if (isPopupVisible)
            HidePopupText();
        else
            ShowPopupText();
    }

    /// <summary>
    /// Test popup manually (for debugging)
    /// </summary>
    [System.Obsolete("For debugging only")]
    public void TestPopupManual()
    {
        Debug.Log("=== MANUAL TEST TEXT POPUP ===");
        Debug.Log($"Popup GameObject: {(popupTextGameObject != null ? popupTextGameObject.name : "NULL")}");
        Debug.Log($"TextMeshPro: {(textMeshPro != null ? "Found" : "NULL")}");
        Debug.Log($"Text Content: '{fullTextContent}'");
        Debug.Log($"Is Popup Visible: {isPopupVisible}");

        ShowPopupText();
    }

    /// <summary>
    /// Start the slow shake animation for the clickable object
    /// </summary>
    public void StartShakeAnimation()
    {
        if (!enableShakeAnimation) return;

        // Only start shake if focused and in exploration mode (when shakeOnlyWhenFocused is enabled)
        if (shakeOnlyWhenFocused)
        {
            if (!isFocused) return;
            if (AdvancedInputManager.Instance != null && !AdvancedInputManager.Instance.IsInExplorationMode()) return;
        }

        // Kill existing shake animation
        if (currentShakeSequence != null && currentShakeSequence.IsActive())
            currentShakeSequence.Kill();

        // Create a smooth continuous shake animation using a single tween
        // This creates a seamless up-down motion without any stops
        var shakeTween = transform.DOLocalMoveY(originalPosition.y + shakeIntensity, shakeDuration / 2)
                                 .SetEase(Ease.InOutSine)
                                 .SetLoops(-1, LoopType.Yoyo);

        // Wrap in sequence for proper cleanup
        currentShakeSequence = DOTween.Sequence();
        currentShakeSequence.Append(shakeTween);
    }

    /// <summary>
    /// Stop the shake animation and return to original position
    /// </summary>
    public void StopShakeAnimation()
    {
        if (currentShakeSequence != null && currentShakeSequence.IsActive())
        {
            currentShakeSequence.Kill();
        }

        // Return to original position
        transform.DOLocalMove(originalPosition, 0.2f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Toggle shake animation on/off
    /// </summary>
    public void ToggleShakeAnimation()
    {
        if (currentShakeSequence != null && currentShakeSequence.IsActive())
            StopShakeAnimation();
        else
            StartShakeAnimation();
    }

    private void OnDestroy()
    {
        // Clean up DOTween animations
        if (currentTextSequence != null && currentTextSequence.IsActive())
        {
            currentTextSequence.Kill();
        }

        if (currentShakeSequence != null && currentShakeSequence.IsActive())
        {
            currentShakeSequence.Kill();
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
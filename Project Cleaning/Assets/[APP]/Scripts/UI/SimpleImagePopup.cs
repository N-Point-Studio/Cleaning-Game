using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Simple script to attach directly to your existing Image GameObject
/// Shows smooth popup animation when objects are clicked in exploration mode
/// </summary>
public class SimpleImagePopup : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private float fadeDelay = 0.3f;
    [SerializeField] private bool autoHideAfterSeconds = true;
    [SerializeField] private float autoHideDelay = 3f;

    [Header("Object to Listen For Clicks")]
    [SerializeField] private ClickableObject[] clickableObjects;

    private Image popupImage;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private bool isVisible = false;

    private void Awake()
    {
        // Get components
        popupImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Add CanvasGroup if doesn't exist
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Store original scale
        originalScale = transform.localScale;

        // Hide initially
        HideImmediate();
    }

    private void Start()
    {
        // Subscribe to all clickable objects
        foreach (var clickable in clickableObjects)
        {
            if (clickable != null)
                clickable.OnObjectClicked.AddListener(OnObjectClicked);
        }
    }

    private void OnObjectClicked()
    {
        // Only show in exploration mode
        if (AdvancedInputManager.Instance != null &&
            AdvancedInputManager.Instance.IsInExplorationMode())
        {
            ShowPopup();
        }
    }

    public void ShowPopup()
    {
        if (isVisible) return;

        isVisible = true;
        gameObject.SetActive(true);

        // Start from zero scale and alpha
        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // Animate scale up
        transform.DOScale(originalScale, animationDuration)
                 .SetEase(Ease.OutBack);

        // Animate fade in with delay
        canvasGroup.DOFade(1f, animationDuration - fadeDelay)
                   .SetDelay(fadeDelay)
                   .SetEase(Ease.OutSine);

        // Auto hide if enabled
        if (autoHideAfterSeconds)
        {
            DOVirtual.DelayedCall(autoHideDelay, HidePopup);
        }
    }

    public void HidePopup()
    {
        if (!isVisible) return;

        // Animate scale down and fade out
        transform.DOScale(Vector3.zero, animationDuration * 0.7f)
                 .SetEase(Ease.InBack);

        canvasGroup.DOFade(0f, animationDuration * 0.5f)
                   .SetEase(Ease.InSine)
                   .OnComplete(HideImmediate);
    }

    private void HideImmediate()
    {
        isVisible = false;
        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // Call this from UI button or other scripts
    public void OnCloseButtonClick()
    {
        HidePopup();
    }

    private void OnDestroy()
    {
        // Clean up subscriptions
        foreach (var clickable in clickableObjects)
        {
            if (clickable != null)
                clickable.OnObjectClicked.RemoveListener(OnObjectClicked);
        }

        // Clean up DOTween
        DOTween.Kill(transform);
        DOTween.Kill(canvasGroup);
    }
}
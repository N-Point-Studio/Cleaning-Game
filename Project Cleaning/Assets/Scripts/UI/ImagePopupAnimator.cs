using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Simple script for smooth image popup animations when clicking objects
/// Attach this to a UI Canvas and assign images in inspector
/// </summary>
public class ImagePopupAnimator : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private Image popupImage;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private Button closeButton;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private Ease fadeEase = Ease.OutSine;

    [Header("Auto Close")]
    [SerializeField] private bool autoCloseAfterSeconds = false;
    [SerializeField] private float autoCloseDelay = 3f;

    // Singleton for easy access
    public static ImagePopupAnimator Instance { get; private set; }

    private Vector3 originalScale;
    private bool isAnimating = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SetupComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupComponents()
    {
        // Setup canvas group if not assigned
        if (popupCanvasGroup == null && popupImage != null)
            popupCanvasGroup = popupImage.GetComponent<CanvasGroup>();

        // Setup close button
        if (closeButton != null)
            closeButton.onClick.AddListener(HidePopup);

        // Store original scale and hide initially
        if (popupImage != null)
        {
            originalScale = popupImage.transform.localScale;
            HideImmediate();
        }
    }

    /// <summary>
    /// Show popup with the specified sprite
    /// </summary>
    public void ShowPopup(Sprite sprite)
    {
        if (sprite == null || popupImage == null || isAnimating) return;

        // Set the sprite
        popupImage.sprite = sprite;

        // Show and animate
        ShowPopupAnimated();
    }

    /// <summary>
    /// Show popup with current sprite (if already assigned)
    /// </summary>
    public void ShowPopup()
    {
        if (popupImage == null || isAnimating) return;
        ShowPopupAnimated();
    }

    private void ShowPopupAnimated()
    {
        isAnimating = true;

        // Enable the popup
        if (popupCanvasGroup != null)
            popupCanvasGroup.gameObject.SetActive(true);

        // Start from scale 0 and fade 0
        popupImage.transform.localScale = Vector3.zero;
        if (popupCanvasGroup != null)
            popupCanvasGroup.alpha = 0f;

        // Create animation sequence
        var sequence = DOTween.Sequence();

        // Scale animation
        sequence.Append(popupImage.transform.DOScale(originalScale, animationDuration).SetEase(scaleEase));

        // Fade animation (parallel with scale)
        if (popupCanvasGroup != null)
        {
            sequence.Join(popupCanvasGroup.DOFade(1f, fadeInDuration).SetEase(fadeEase));
        }

        // Complete callback
        sequence.OnComplete(() =>
        {
            isAnimating = false;

            // Auto close if enabled
            if (autoCloseAfterSeconds)
            {
                DOVirtual.DelayedCall(autoCloseDelay, HidePopup);
            }
        });
    }

    /// <summary>
    /// Hide popup with smooth animation
    /// </summary>
    public void HidePopup()
    {
        if (popupImage == null || isAnimating) return;

        isAnimating = true;

        var sequence = DOTween.Sequence();

        // Fade out first
        if (popupCanvasGroup != null)
        {
            sequence.Append(popupCanvasGroup.DOFade(0f, fadeInDuration * 0.7f).SetEase(Ease.InSine));
        }

        // Scale down
        sequence.Join(popupImage.transform.DOScale(Vector3.zero, animationDuration * 0.8f).SetEase(Ease.InBack));

        // Hide when done
        sequence.OnComplete(() =>
        {
            HideImmediate();
            isAnimating = false;
        });
    }

    private void HideImmediate()
    {
        if (popupImage != null)
            popupImage.transform.localScale = Vector3.zero;

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Check if popup is currently visible
    /// </summary>
    public bool IsVisible()
    {
        return popupCanvasGroup != null &&
               popupCanvasGroup.gameObject.activeInHierarchy &&
               popupCanvasGroup.alpha > 0f;
    }

    private void OnDestroy()
    {
        // Clean up DOTween
        DOTween.Kill(transform);
        if (popupImage != null)
            DOTween.Kill(popupImage.transform);
        if (popupCanvasGroup != null)
            DOTween.Kill(popupCanvasGroup);
    }
}
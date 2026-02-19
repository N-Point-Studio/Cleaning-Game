using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Attach this to ClickableObject to show image popup with smooth animation
/// Just drag your existing Image GameObject to the inspector
/// </summary>
public class ClickableImageDisplay : MonoBehaviour
{
    [Header("Image Display")]
    [SerializeField] private GameObject imageGameObject;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.6f;
    [SerializeField] private bool autoHideAfterSeconds = true;
    [SerializeField] private float autoHideDelay = 3f;
    [SerializeField] private bool onlyShowInExplorationMode = true;

    private Image imageComponent;
    private CanvasGroup canvasGroup;
    private ClickableObject clickableObject;
    private Vector3 originalScale;
    private bool isVisible = false;
    private Sequence currentSequence;

    private void Awake()
    {
        clickableObject = GetComponent<ClickableObject>();

        if (imageGameObject != null)
        {
            imageComponent = imageGameObject.GetComponent<Image>();
            canvasGroup = imageGameObject.GetComponent<CanvasGroup>();

            // Add CanvasGroup if doesn't exist
            if (canvasGroup == null)
                canvasGroup = imageGameObject.AddComponent<CanvasGroup>();

            // Store original scale
            originalScale = imageGameObject.transform.localScale;

            // Hide initially
            HideImmediate();
        }
    }

    private void Start()
    {
        // Subscribe to click event
        if (clickableObject != null)
        {
            clickableObject.OnObjectClicked.AddListener(OnObjectClicked);
        }
    }

    private void OnObjectClicked()
    {
        // Check if should only show in exploration mode
        if (onlyShowInExplorationMode)
        {
            if (AdvancedInputManager.Instance == null ||
                !AdvancedInputManager.Instance.IsInExplorationMode())
            {
                return;
            }
        }

        // Toggle visibility
        if (isVisible)
            HideImage();
        else
            ShowImage();
    }

    public void ShowImage()
    {
        if (imageGameObject == null || isVisible) return;

        isVisible = true;
        imageGameObject.SetActive(true);

        // Start from zero scale and alpha
        imageGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // Create smooth animation sequence
        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        // Scale up with bounce
        currentSequence.Append(imageGameObject.transform.DOScale(originalScale, animationDuration)
                       .SetEase(Ease.OutBack));

        // Fade in
        currentSequence.Join(canvasGroup.DOFade(1f, animationDuration * 0.8f)
                     .SetEase(Ease.OutSine));

        // Auto hide if enabled
        if (autoHideAfterSeconds)
        {
            currentSequence.AppendInterval(autoHideDelay);
            currentSequence.AppendCallback(HideImage);
        }
    }

    public void HideImage()
    {
        if (imageGameObject == null || !isVisible) return;

        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        // Scale down
        currentSequence.Append(imageGameObject.transform.DOScale(Vector3.zero, animationDuration * 0.7f)
                       .SetEase(Ease.InBack));

        // Fade out
        currentSequence.Join(canvasGroup.DOFade(0f, animationDuration * 0.5f)
                     .SetEase(Ease.InSine));

        // Hide when complete
        currentSequence.OnComplete(HideImmediate);
    }

    private void HideImmediate()
    {
        if (imageGameObject != null)
        {
            isVisible = false;
            imageGameObject.transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
            imageGameObject.SetActive(false);
        }
    }

    // Public methods for external use
    public void SetImageGameObject(GameObject newImageGameObject)
    {
        imageGameObject = newImageGameObject;
        Awake(); // Reinitialize
    }

    public bool IsImageVisible()
    {
        return isVisible;
    }

    // Call from close button or other UI
    public void CloseImage()
    {
        HideImage();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (clickableObject != null)
        {
            clickableObject.OnObjectClicked.RemoveListener(OnObjectClicked);
        }

        // Clean up DOTween
        currentSequence?.Kill();
    }
}
using UnityEngine;

/// <summary>
/// Simple component to show image popup when object is clicked
/// Attach this to any ClickableObject to show an image popup
/// </summary>
public class ClickableImagePopup : MonoBehaviour
{
    [Header("Popup Image")]
    [SerializeField] private Sprite popupSprite;
    [SerializeField] private bool showOnlyInExplorationMode = true;

    private ClickableObject clickableObject;

    private void Awake()
    {
        clickableObject = GetComponent<ClickableObject>();
    }

    private void Start()
    {
        // Subscribe to click event
        if (clickableObject != null)
        {
            clickableObject.OnObjectClicked.AddListener(ShowImagePopup);
        }
    }

    private void ShowImagePopup()
    {
        // Check if we should only show in exploration mode
        if (showOnlyInExplorationMode)
        {
            if (AdvancedInputManager.Instance == null ||
                !AdvancedInputManager.Instance.IsInExplorationMode())
            {
                return;
            }
        }

        // Show popup if we have a sprite and popup animator
        if (popupSprite != null && ImagePopupAnimator.Instance != null)
        {
            ImagePopupAnimator.Instance.ShowPopup(popupSprite);
        }
        else
        {
            Debug.LogWarning($"Cannot show popup for {gameObject.name}: " +
                            $"Sprite={popupSprite != null}, " +
                            $"Animator={ImagePopupAnimator.Instance != null}");
        }
    }

    /// <summary>
    /// Change the popup sprite at runtime
    /// </summary>
    public void SetPopupSprite(Sprite newSprite)
    {
        popupSprite = newSprite;
    }

    /// <summary>
    /// Manually trigger popup (for testing or external calls)
    /// </summary>
    public void TriggerPopup()
    {
        ShowImagePopup();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (clickableObject != null)
        {
            clickableObject.OnObjectClicked.RemoveListener(ShowImagePopup);
        }
    }
}
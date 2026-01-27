using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public enum ButtonAnimationType
{
    Scale,
    Fade,
    Color,
    Position,
    Rotation,
    Punch,
    Shake
}

public class ButtonScaleAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Animation Type")]
    [SerializeField] private ButtonAnimationType animationType = ButtonAnimationType.Scale;

    [Header("Scale Animation")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float clickScale = 0.95f;

    [Header("Fade Animation")]
    [SerializeField] private float normalAlpha = 1f;
    [SerializeField] private float hoverAlpha = 0.8f;
    [SerializeField] private float clickAlpha = 0.6f;

    [Header("Color Animation")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color clickColor = Color.gray;

    [Header("Position Animation")]
    [SerializeField] private Vector3 hoverOffset = new Vector3(0, 5f, 0);
    [SerializeField] private Vector3 clickOffset = new Vector3(0, -2f, 0);

    [Header("Rotation Animation")]
    [SerializeField] private Vector3 hoverRotation = new Vector3(0, 0, 5f);
    [SerializeField] private Vector3 clickRotation = new Vector3(0, 0, -5f);

    [Header("Punch Animation")]
    [SerializeField] private Vector3 punchScale = new Vector3(0.2f, 0.2f, 0);
    [SerializeField] private int punchVibrato = 5;
    [SerializeField] private float punchElasticity = 0.5f;

    [Header("Shake Animation")]
    [SerializeField] private float shakeStrength = 10f;
    [SerializeField] private int shakeVibrato = 20;

    [Header("Timing & Easing")]
    [SerializeField] private float animationDuration = 0.2f;
    [SerializeField] private Ease hoverEase = Ease.OutBack;
    [SerializeField] private Ease clickEase = Ease.OutQuart;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Vector3 originalRotation;
    private Color originalColor;
    private float originalAlpha;

    private Button button;
    private Image buttonImage;
    private CanvasGroup canvasGroup;
    private Tween currentTween;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        // Store original values
        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
        originalRotation = transform.localEulerAngles;

        // Get components
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        // Store original color and alpha
        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
            originalAlpha = originalColor.a;
        }
        else if (canvasGroup != null)
        {
            originalAlpha = canvasGroup.alpha;
        }

        Log($"ButtonScaleAnimation initialized with {animationType} animation");
    }

    #region Hover Events

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        Log("Mouse Enter");
        PlayAnimation(true, false); // hover = true, click = false
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        Log("Mouse Exit");
        PlayAnimation(false, false); // return to normal
    }

    #endregion

    #region Click Events

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        Log("Mouse Down");
        PlayAnimation(false, true); // click = true
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsInteractable()) return;

        Log("Mouse Up");
        bool isHovering = IsPointerOver();
        PlayAnimation(isHovering, false); // return to hover or normal
    }

    #endregion

    #region Animation Methods

    private void PlayAnimation(bool isHover, bool isClick)
    {
        // Kill previous tween
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        // Choose ease based on state
        Ease easing = isClick ? clickEase : hoverEase;

        // Play animation based on type
        switch (animationType)
        {
            case ButtonAnimationType.Scale:
                PlayScaleAnimation(isHover, isClick, easing);
                break;
            case ButtonAnimationType.Fade:
                PlayFadeAnimation(isHover, isClick, easing);
                break;
            case ButtonAnimationType.Color:
                PlayColorAnimation(isHover, isClick, easing);
                break;
            case ButtonAnimationType.Position:
                PlayPositionAnimation(isHover, isClick, easing);
                break;
            case ButtonAnimationType.Rotation:
                PlayRotationAnimation(isHover, isClick, easing);
                break;
            case ButtonAnimationType.Punch:
                PlayPunchAnimation(isClick);
                break;
            case ButtonAnimationType.Shake:
                PlayShakeAnimation(isClick);
                break;
        }
    }

    private void PlayScaleAnimation(bool isHover, bool isClick, Ease easing)
    {
        Vector3 targetScale = originalScale;

        if (isClick)
            targetScale *= clickScale;
        else if (isHover)
            targetScale *= hoverScale;

        currentTween = transform.DOScale(targetScale, animationDuration)
            .SetEase(easing)
            .SetUpdate(true);
    }

    private void PlayFadeAnimation(bool isHover, bool isClick, Ease easing)
    {
        float targetAlpha = normalAlpha;

        if (isClick)
            targetAlpha = clickAlpha;
        else if (isHover)
            targetAlpha = hoverAlpha;

        if (buttonImage != null)
        {
            currentTween = buttonImage.DOFade(targetAlpha, animationDuration)
                .SetEase(easing)
                .SetUpdate(true);
        }
        else if (canvasGroup != null)
        {
            currentTween = canvasGroup.DOFade(targetAlpha, animationDuration)
                .SetEase(easing)
                .SetUpdate(true);
        }
    }

    private void PlayColorAnimation(bool isHover, bool isClick, Ease easing)
    {
        if (buttonImage == null) return;

        Color targetColor = normalColor;

        if (isClick)
            targetColor = clickColor;
        else if (isHover)
            targetColor = hoverColor;

        currentTween = buttonImage.DOColor(targetColor, animationDuration)
            .SetEase(easing)
            .SetUpdate(true);
    }

    private void PlayPositionAnimation(bool isHover, bool isClick, Ease easing)
    {
        Vector3 targetPosition = originalPosition;

        if (isClick)
            targetPosition += clickOffset;
        else if (isHover)
            targetPosition += hoverOffset;

        currentTween = transform.DOLocalMove(targetPosition, animationDuration)
            .SetEase(easing)
            .SetUpdate(true);
    }

    private void PlayRotationAnimation(bool isHover, bool isClick, Ease easing)
    {
        Vector3 targetRotation = originalRotation;

        if (isClick)
            targetRotation += clickRotation;
        else if (isHover)
            targetRotation += hoverRotation;

        currentTween = transform.DOLocalRotate(targetRotation, animationDuration)
            .SetEase(easing)
            .SetUpdate(true);
    }

    private void PlayPunchAnimation(bool isClick)
    {
        if (!isClick) return; // Punch only on click

        currentTween = transform.DOPunchScale(punchScale, animationDuration, punchVibrato, punchElasticity)
            .SetUpdate(true);
    }

    private void PlayShakeAnimation(bool isClick)
    {
        if (!isClick) return; // Shake only on click

        currentTween = transform.DOShakePosition(animationDuration, shakeStrength, shakeVibrato)
            .SetUpdate(true);
    }

    #endregion

    #region Helper Methods

    private bool IsInteractable()
    {
        return button == null || button.interactable;
    }

    private bool IsPointerOver()
    {
        return EventSystem.current != null &&
               EventSystem.current.currentSelectedGameObject == gameObject;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[ButtonScaleAnimation] {gameObject.name}: {message}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reset to original state instantly
    /// </summary>
    public void ResetToOriginal()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        transform.localEulerAngles = originalRotation;

        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = originalAlpha;
        }
    }

    /// <summary>
    /// Manually trigger hover animation
    /// </summary>
    public void TriggerHoverAnimation()
    {
        PlayAnimation(true, false);
    }

    /// <summary>
    /// Manually trigger click animation
    /// </summary>
    public void TriggerClickAnimation()
    {
        PlayAnimation(false, true);
    }

    /// <summary>
    /// Change animation type at runtime
    /// </summary>
    public void ChangeAnimationType(ButtonAnimationType newType)
    {
        animationType = newType;
        ResetToOriginal();
    }

    #endregion

    #region Unity Events

    void OnDestroy()
    {
        // Clean up tween
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }
    }

    void OnDisable()
    {
        // Reset to original state when disabled
        ResetToOriginal();
    }

    #endregion
}
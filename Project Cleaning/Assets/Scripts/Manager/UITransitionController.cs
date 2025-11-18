using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Handles UI transitions and animations for the input manager
/// </summary>
public class UITransitionController : MonoBehaviour
{
    [Header("UI Controls")]
    [SerializeField] private Image startExplorationImage;
    [SerializeField] private Image additionalImage1;
    [SerializeField] private Image additionalImage2;

    [Header("UI Transition Settings")]
    [SerializeField] private float buttonFadeDuration = 0.5f;
    [SerializeField] private float buttonScaleDuration = 0.3f;
    [SerializeField] private float cameraDelayAfterButton = 0.2f;

    // Singleton
    public static UITransitionController Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupUI();
    }
    #endregion

    #region UI Setup
    public void SetupUI()
    {
        if (startExplorationImage != null)
            SetupImageClickDetection(startExplorationImage, StartExplorationMode);

        if (additionalImage1 != null)
            additionalImage1.raycastTarget = false;

        if (additionalImage2 != null)
            additionalImage2.raycastTarget = false;
    }

    private void SetupImageClickDetection(Image targetImage, System.Action onClickAction)
    {
        if (!targetImage.TryGetComponent<EventTrigger>(out var eventTrigger))
            eventTrigger = targetImage.gameObject.AddComponent<EventTrigger>();

        var clickEvent = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEvent.callback.AddListener((data) => onClickAction?.Invoke());
        eventTrigger.triggers.Add(clickEvent);
        targetImage.raycastTarget = true;
    }

    public void SetImageClickable(Image targetImage, bool clickable)
    {
        if (targetImage == null) return;

        targetImage.raycastTarget = clickable;
        if (targetImage.TryGetComponent<EventTrigger>(out var eventTrigger))
            eventTrigger.enabled = clickable;
    }
    #endregion

    #region UI Transitions
    public void StartExplorationTransition()
    {
        SetImageClickable(startExplorationImage, false);
        if (additionalImage1 != null) additionalImage1.raycastTarget = false;
        if (additionalImage2 != null) additionalImage2.raycastTarget = false;

        StartCoroutine(ElegantButtonTransition());
    }

    private IEnumerator ElegantButtonTransition()
    {
        var imagesToAnimate = new List<Image>();
        if (startExplorationImage != null) imagesToAnimate.Add(startExplorationImage);
        if (additionalImage1 != null) imagesToAnimate.Add(additionalImage1);
        if (additionalImage2 != null) imagesToAnimate.Add(additionalImage2);

        var masterSequence = DOTween.Sequence();

        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            var image = imagesToAnimate[i];
            var canvasGroup = image.GetComponent<CanvasGroup>() ?? image.gameObject.AddComponent<CanvasGroup>();
            var transform = image.transform;
            var originalScale = transform.localScale;

            var imageSequence = DOTween.Sequence();
            imageSequence.Append(transform.DOScale(originalScale * 0.85f, buttonScaleDuration * 0.6f).SetEase(Ease.OutBack));
            imageSequence.Join(canvasGroup.DOFade(0.4f, buttonScaleDuration * 0.6f).SetEase(Ease.OutSine));
            imageSequence.Append(transform.DOScale(0f, buttonFadeDuration * 1.2f).SetEase(Ease.InSine));
            imageSequence.Join(canvasGroup.DOFade(0f, buttonFadeDuration * 1.2f).SetEase(Ease.InSine));

            masterSequence.Insert(i * 0.15f, imageSequence);
        }

        yield return masterSequence.WaitForCompletion();

        foreach (var image in imagesToAnimate) image.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        // Reset the slide position to the beginning
        CameraAnimationController.Instance?.ResetSlideIndex();

        // Notify that button transition is complete
        GameModeManager.Instance?.StartExplorationMode();

        // Explicitly start the camera animation for the initial transition
        CameraAnimationController.Instance?.BeginExplorationMode();
    }

    public IEnumerator ShowStartButtonElegantly(float returnTransitionDuration)
    {
        yield return new WaitForSeconds(returnTransitionDuration * 0.7f);

        var imagesToAnimate = new List<Image>();
        if (startExplorationImage != null) imagesToAnimate.Add(startExplorationImage);
        if (additionalImage1 != null) imagesToAnimate.Add(additionalImage1);
        if (additionalImage2 != null) imagesToAnimate.Add(additionalImage2);

        foreach (var image in imagesToAnimate)
        {
            image.gameObject.SetActive(true);
            var canvasGroup = image.GetComponent<CanvasGroup>() ?? image.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            image.transform.localScale = Vector3.zero;
        }

        if (startExplorationImage != null) SetImageClickable(startExplorationImage, true);

        var masterSequence = DOTween.Sequence();
        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            var image = imagesToAnimate[i];
            var canvasGroup = image.GetComponent<CanvasGroup>();
            var transform = image.transform;

            var imageSequence = DOTween.Sequence();
            imageSequence.Append(transform.DOScale(Vector3.one * 1.05f, buttonFadeDuration * 0.7f).SetEase(Ease.OutBack));
            imageSequence.Join(canvasGroup.DOFade(1f, buttonFadeDuration * 0.7f).SetEase(Ease.OutSine));
            imageSequence.Append(transform.DOScale(Vector3.one, buttonFadeDuration * 0.3f).SetEase(Ease.OutSine));

            masterSequence.Insert(i * 0.2f, imageSequence);
        }

        yield return masterSequence.WaitForCompletion();
    }

    private void StartExplorationMode()
    {
        // Trigger the exploration mode start through GameModeManager
        StartExplorationTransition();
    }

    public float GetCameraDelayAfterButton() => cameraDelayAfterButton;
    #endregion
}
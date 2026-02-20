using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UITransitionController : MonoBehaviour
{
    [Header("UI Controls")]
    [SerializeField] private Image startExplorationImage;
    [SerializeField] private Image additionalImage1;
    [SerializeField] private Image additionalImage2;
    public System.Action OnStartExploration;

    [Header("UI Transition Settings")]
    [SerializeField] private float buttonFadeDuration = 0.5f;
    [SerializeField] private float buttonScaleDuration = 0.3f;
    [SerializeField] private float cameraDelayAfterButton = 0.2f;

    private Sequence currentTransitionSequence;

    // Singleton
    public static UITransitionController Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (GetComponent<RectTransform>() == null)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupUI();

        bool returningFromGameplay = CameraAnimationController.Instance != null &&
                                     CameraAnimationController.Instance.IsReturningFromGameplay();
        bool introAlreadyShownThisSession = SaveSystem.Instance != null &&
                                            SaveSystem.Instance.IsIntroTransitionShown();

        if (!returningFromGameplay && !introAlreadyShownThisSession)
        {
            ForceUIVisible();
        }
        else
        {
            ForceUIHidden();
        }
    }

    private void ForceUIVisible()
    {
        SetImageState(startExplorationImage, true, 1f);
        SetImageState(additionalImage1, true, 1f);
        SetImageState(additionalImage2, true, 1f);
        SetImageClickable(startExplorationImage, true);
    }

    private void ForceUIHidden()
    {
        SetImageState(startExplorationImage, false, 0f);
        SetImageState(additionalImage1, false, 0f);
        SetImageState(additionalImage2, false, 0f);
        SetImageClickable(startExplorationImage, false);
    }

    private void SetImageState(Image img, bool active, float scaleAndAlpha)
    {
        if (img == null) return;
        img.gameObject.SetActive(active);
        img.transform.localScale = Vector3.one * scaleAndAlpha;

        var canvasGroup = img.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = img.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = scaleAndAlpha;
    }

    private void OnDestroy()
    {
        currentTransitionSequence?.Kill();
        if (Instance == this) Instance = null;
    }
    #endregion

    #region UI Setup
    public void SetupUI()
    {
        if (startExplorationImage != null)
            SetupImageClickDetection(startExplorationImage, StartExplorationMode);

        if (additionalImage1 != null) additionalImage1.raycastTarget = false;
        if (additionalImage2 != null) additionalImage2.raycastTarget = false;
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
        StartCoroutine(ElegantButtonTransition());
        OnStartExploration?.Invoke();
    }

    // ANIMASI DOTWEEN SAAT DI-KLIK (Menghilang)
    private IEnumerator ElegantButtonTransition()
    {
        var imagesToAnimate = new List<Image> { startExplorationImage, additionalImage1, additionalImage2 };

        currentTransitionSequence?.Kill();
        currentTransitionSequence = DOTween.Sequence();

        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            var image = imagesToAnimate[i];
            if (image == null) continue;

            var canvasGroup = image.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
            var transform = image.transform;
            var originalScale = Vector3.one;

            var imageSequence = DOTween.Sequence();
            imageSequence.Append(transform.DOScale(originalScale * 0.85f, buttonScaleDuration * 0.6f).SetEase(Ease.OutBack));
            imageSequence.Join(canvasGroup.DOFade(0.4f, buttonScaleDuration * 0.6f).SetEase(Ease.OutSine));
            imageSequence.Append(transform.DOScale(0f, buttonFadeDuration * 1.2f).SetEase(Ease.InSine));
            imageSequence.Join(canvasGroup.DOFade(0f, buttonFadeDuration * 1.2f).SetEase(Ease.InSine));

            currentTransitionSequence.Insert(i * 0.15f, imageSequence);
        }

        yield return currentTransitionSequence.WaitForCompletion();

        ForceUIHidden(); // Safeguard agar pasti hilang
        yield return new WaitForSeconds(0.3f);

        CameraAnimationController.Instance?.ResetSlideIndex();
        GameModeManager.Instance?.StartExplorationMode();
        CameraAnimationController.Instance?.BeginExplorationMode();
    }

    // ANIMASI DOTWEEN SAAT DI-RESET (Membesar/Memantul)
    public IEnumerator ShowStartButtonElegantly(float returnTransitionDuration)
    {
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsIntroTransitionShown())
        {
            ForceUIVisible();
            yield break;
        }

        yield return new WaitForSeconds(returnTransitionDuration * 0.7f);

        var imagesToAnimate = new List<Image> { startExplorationImage, additionalImage1, additionalImage2 };

        foreach (var image in imagesToAnimate)
        {
            if (image == null) continue;
            image.gameObject.SetActive(true);
            var canvasGroup = image.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = image.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            image.transform.localScale = Vector3.zero;
        }

        if (startExplorationImage != null) SetImageClickable(startExplorationImage, true);

        currentTransitionSequence?.Kill();
        currentTransitionSequence = DOTween.Sequence();

        for (int i = 0; i < imagesToAnimate.Count; i++)
        {
            var image = imagesToAnimate[i];
            if (image == null) continue;

            var canvasGroup = image.GetComponent<CanvasGroup>();
            var transform = image.transform;

            var imageSequence = DOTween.Sequence();
            imageSequence.Append(transform.DOScale(Vector3.one * 1.05f, buttonFadeDuration * 0.7f).SetEase(Ease.OutBack));
            imageSequence.Join(canvasGroup.DOFade(1f, buttonFadeDuration * 0.7f).SetEase(Ease.OutSine));
            imageSequence.Append(transform.DOScale(Vector3.one, buttonFadeDuration * 0.3f).SetEase(Ease.OutSine));

            currentTransitionSequence.Insert(i * 0.2f, imageSequence);
        }

        yield return currentTransitionSequence.WaitForCompletion();

        ForceUIVisible(); // Safeguard agar pasti muncul 100%

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetIntroTransitionShown(true);
        }
    }

    private void StartExplorationMode()
    {
        StartExplorationTransition();
    }

    public float GetCameraDelayAfterButton() => cameraDelayAfterButton;
    #endregion
}
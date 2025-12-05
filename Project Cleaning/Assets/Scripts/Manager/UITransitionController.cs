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
    public System.Action OnStartExploration;

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
            Debug.Log($"🚨 UITransitionController Instance set on GameObject: {gameObject.name}");
        }
        else
        {
            Debug.Log($"🚨 Destroying duplicate UITransitionController on GameObject: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupUI();

        // Show intro controls only when entering from menu fresh, not when returning from gameplay.
        bool returningFromGameplay = CameraAnimationController.Instance != null &&
                                     CameraAnimationController.Instance.IsReturningFromGameplay();
        bool introAlreadyShownThisSession = CameraAnimationController.Instance != null &&
                                            CameraAnimationController.Instance.HasShownStartupUI();

        bool shouldShowIntro = !returningFromGameplay && !introAlreadyShownThisSession;

        Debug.Log($"🚨 UITransitionController Start - returningFromGameplay={returningFromGameplay}, introAlreadyShownThisSession={introAlreadyShownThisSession}, shouldShowIntro={shouldShowIntro}");

        if (shouldShowIntro)
        {
            if (startExplorationImage != null) startExplorationImage.gameObject.SetActive(true);
            if (additionalImage1 != null) additionalImage1.gameObject.SetActive(true);
            if (additionalImage2 != null) additionalImage2.gameObject.SetActive(true);
        }
        else
        {
            if (startExplorationImage != null) startExplorationImage.gameObject.SetActive(false);
            if (additionalImage1 != null) additionalImage1.gameObject.SetActive(false);
            if (additionalImage2 != null) additionalImage2.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Kill all DOTween animations to prevent cleanup warnings
        DOTween.Kill(this);

        // Also kill any animations on our UI images
        if (startExplorationImage != null) DOTween.Kill(startExplorationImage.transform);
        if (additionalImage1 != null) DOTween.Kill(additionalImage1.transform);
        if (additionalImage2 != null) DOTween.Kill(additionalImage2.transform);

        // Clear singleton reference
        if (Instance == this)
        {
            Instance = null;
        }

        Debug.Log($"🚨 UITransitionController cleanup completed on {gameObject.name}");
    }
    #endregion

    #region UI Setup
    public void SetupUI()
    {
        Debug.Log($"🚨 UITransitionController.SetupUI() called on GameObject: {gameObject.name}");
        Debug.Log($"🚨 Image references - startExplorationImage: {(startExplorationImage != null ? startExplorationImage.name : "NULL")}");
        Debug.Log($"🚨 Image references - additionalImage1: {(additionalImage1 != null ? additionalImage1.name : "NULL")}");
        Debug.Log($"🚨 Image references - additionalImage2: {(additionalImage2 != null ? additionalImage2.name : "NULL")}");

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

        // Notify listeners (e.g., SaveSystem) that exploration has started
        OnStartExploration?.Invoke();
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
        float timestamp = Time.time;
        Debug.Log($"🚨🚨 UITransitionController.ShowStartButtonElegantly CALLED at {timestamp}");
        Debug.Log($"🚨🚨 returnTransitionDuration: {returnTransitionDuration}");

        // Check if intro transition has already been shown
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsIntroTransitionShown())
        {
            Debug.Log($"🚨🚨 Intro transition already shown - skipping animation");
            yield break; // Exit the coroutine without showing the transition
        }

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

        // Mark intro transition as shown
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetIntroTransitionShown(true);
            Debug.Log($"🚨🚨 Intro transition marked as shown in save system");
        }

        Debug.Log($"🚨🚨 UITransitionController.ShowStartButtonElegantly FINISHED at {Time.time}");
        Debug.Log($"🚨🚨 UI startup images should now be visible!");
    }

    private void StartExplorationMode()
    {
        // Trigger the exploration mode start through GameModeManager
        StartExplorationTransition();
    }

    /// <summary>
    /// Check if intro transition has been shown and hide intro images if needed
    /// </summary>
    private IEnumerator CheckAndHideIntroImagesIfNeeded()
    {
        // Wait a frame to ensure SaveSystem is ready
        yield return null;

        // Check if intro transition has already been shown
        if (SaveSystem.Instance != null && SaveSystem.Instance.IsIntroTransitionShown())
        {
            Debug.Log($"🚨 Intro transition already shown - searching for and hiding intro images");
            HideIntroImagesGlobally();
        }
        else
        {
            Debug.Log($"🚨 Intro transition not yet shown - keeping intro images visible");
        }
    }

    /// <summary>
    /// Search for and hide intro images globally (in case they're active by default)
    /// </summary>
    private void HideIntroImagesGlobally()
    {
        int hiddenCount = 0;

        // Search for common intro image names
        string[] possibleImageNames = {
            "Start Exploration Image", "StartExplorationImage", "start exploration image",
            "Additional Image 1", "AdditionalImage1", "additional image 1",
            "Additional Image 2", "AdditionalImage2", "additional image 2",
            "StartExploration", "StartExplorationButton", "StartButton"
        };

        foreach (string imageName in possibleImageNames)
        {
            GameObject imageObj = GameObject.Find(imageName);
            if (imageObj != null && imageObj.activeInHierarchy)
            {
                Debug.Log($"🚨 Found and hiding intro image: {imageName}");
                imageObj.SetActive(false);
                hiddenCount++;
            }
        }

        // Also search for Image components that might be intro images
        UnityEngine.UI.Image[] allImages = FindObjectsOfType<UnityEngine.UI.Image>(true);
        foreach (var image in allImages)
        {
            string imageName = image.gameObject.name.ToLower();
            if (imageName.Contains("start") && imageName.Contains("exploration") ||
                imageName.Contains("additional") && imageName.Contains("image") ||
                imageName.Contains("intro") && imageName.Contains("transition"))
            {
                if (image.gameObject.activeInHierarchy)
                {
                    Debug.Log($"🚨 Found and hiding intro image by component search: {image.gameObject.name}");
                    image.gameObject.SetActive(false);
                    hiddenCount++;
                }
            }
        }

        Debug.Log($"🚨 Total intro images hidden: {hiddenCount}");
    }

    public float GetCameraDelayAfterButton() => cameraDelayAfterButton;
    #endregion
}

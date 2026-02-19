using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PaginationUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Toggle dotIndicatorPrefab;
    [SerializeField] private ToggleGroup dotIndicatorToggleGroup;
    [SerializeField] private Transform paginationContainer;
    [SerializeField] private Button buttonNext;
    [SerializeField] private Button buttonPrevious;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float delayOnEnterExploration = 1.5f;

    private Tween fadeTween;
    private GameModeManager.GameMode lastKnownMode = GameModeManager.GameMode.Initial;

    private void Awake()
    {
        if (buttonNext != null) buttonNext.onClick.AddListener(OnClickNext);
        if (buttonPrevious != null) buttonPrevious.onClick.AddListener(OnClickPrevious);

        if (canvasGroup == null && rootCanvas != null)
        {
            canvasGroup = rootCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = rootCanvas.AddComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (rootCanvas != null) rootCanvas.SetActive(false);
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => CameraAnimationController.Instance != null);

        SetupDots(CameraAnimationController.Instance.GetTotalPages());

        if (GameModeManager.Instance != null)
        {
            lastKnownMode = GameModeManager.Instance.GetCurrentMode();
            GameModeManager.Instance.OnModeChanged += HandleModeChanged;

            if (lastKnownMode == GameModeManager.GameMode.Exploration)
            {
                ShowAnimated(0f);
            }
        }

        MainMenuEvents.OnPageChanged += UpdatePaginationUI;
        UpdatePaginationUI(CameraAnimationController.Instance.GetCurrentPositionIndex());
    }

    private void OnDestroy()
    {
        if (buttonNext != null) buttonNext.onClick.RemoveListener(OnClickNext);
        if (buttonPrevious != null) buttonPrevious.onClick.RemoveListener(OnClickPrevious);

        if (GameModeManager.Instance != null)
            GameModeManager.Instance.OnModeChanged -= HandleModeChanged;

        MainMenuEvents.OnPageChanged -= UpdatePaginationUI;
        fadeTween?.Kill();
    }

    private void HandleModeChanged(GameModeManager.GameMode mode)
    {
        if (mode == GameModeManager.GameMode.Exploration)
        {
            if (lastKnownMode == GameModeManager.GameMode.Initial)
                ShowAnimated(delayOnEnterExploration);
            else if (lastKnownMode == GameModeManager.GameMode.Zoom)
                ShowAnimated(0f);
        }
        else
        {
            HideAnimated();
        }
        lastKnownMode = mode;
    }

    private void ShowAnimated(float delay)
    {
        if (rootCanvas != null) rootCanvas.SetActive(true);
        if (canvasGroup == null) return;

        fadeTween?.Kill();
        rootCanvas.transform.localScale = Vector3.one * 0.85f;
        rootCanvas.transform.DOScale(1f, fadeDuration).SetDelay(delay).SetEase(Ease.OutBack);

        fadeTween = canvasGroup.DOFade(1f, fadeDuration).SetDelay(delay).OnComplete(() =>
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        });
    }

    private void HideAnimated()
    {
        if (canvasGroup == null)
        {
            if (rootCanvas != null) rootCanvas.SetActive(false);
            return;
        }

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        fadeTween?.Kill();
        rootCanvas.transform.DOScale(0.85f, fadeDuration * 0.5f).SetEase(Ease.InBack);

        fadeTween = canvasGroup.DOFade(0f, fadeDuration * 0.5f).OnComplete(() =>
        {
            if (rootCanvas != null) rootCanvas.SetActive(false);
        });
    }

    private void UpdatePaginationUI(int currentIndex)
    {
        if (paginationContainer != null && paginationContainer.childCount > currentIndex)
        {
            Toggle toggle = paginationContainer.GetChild(currentIndex).GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.SetIsOnWithoutNotify(true);
            }
        }

        if (CameraAnimationController.Instance != null)
        {
            int totalPages = CameraAnimationController.Instance.GetTotalPages();
            if (buttonPrevious != null) buttonPrevious.interactable = (currentIndex > 0);
            if (buttonNext != null) buttonNext.interactable = (currentIndex < totalPages - 1);
        }
    }

    private void SetupDots(int totalPages)
    {
        ClearAll();
        for (int i = 0; i < totalPages; i++)
        {
            AddIndicator();
        }
    }

    public void AddIndicator()
    {
        if (dotIndicatorPrefab == null || paginationContainer == null) return;

        int dotIndex = paginationContainer.childCount;

        Toggle toggle = Instantiate(dotIndicatorPrefab, paginationContainer);
        toggle.group = dotIndicatorToggleGroup;
        toggle.transform.localScale = Vector3.one;
        toggle.name = $"Dot_{dotIndex}";

        toggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                OnDotClicked(dotIndex);
            }
        });
    }

    private void OnDotClicked(int targetIndex)
    {
        if (CameraAnimationController.Instance != null &&
            CameraAnimationController.Instance.GetCurrentPositionIndex() != targetIndex)
        {
            MenuSfxManager.Instance?.PlaySwipe();

            MainMenuEvents.OnGoToPage?.Invoke(targetIndex);
        }
    }

    private void OnClickNext()
    {
        if (buttonNext != null && buttonNext.interactable)
        {
            MenuSfxManager.Instance?.PlaySwipe();
            MainMenuEvents.OnNextPage?.Invoke();
        }
    }

    private void OnClickPrevious()
    {
        if (buttonPrevious != null && buttonPrevious.interactable)
        {
            MenuSfxManager.Instance?.PlaySwipe();
            MainMenuEvents.OnPreviousPage?.Invoke();
        }
    }

    public void ClearAll()
    {
        foreach (Transform child in paginationContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
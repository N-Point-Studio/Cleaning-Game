using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MenuTutorialNudge : MonoBehaviour
{
    [Header("Target & UI")]
    [SerializeField] private ClickableObject targetCoin;
    [SerializeField] private GameObject pointerUI;

    [Header("Animation Settings")]
    [SerializeField] private float bounceDistance = 30f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private float delayBeforeShow = 1.5f;

    private Tween bounceTween;
    private GameModeManager.GameMode lastKnownMode = GameModeManager.GameMode.Initial;
    private Coroutine showCoroutine;

    private void Start()
    {
        if (pointerUI != null) pointerUI.SetActive(false);

        if (GameModeManager.Instance != null)
        {
            lastKnownMode = GameModeManager.Instance.GetCurrentMode();
            GameModeManager.Instance.OnModeChanged += HandleModeChanged;

            if (lastKnownMode == GameModeManager.GameMode.Exploration)
            {
                HandleModeChanged(lastKnownMode);
            }
        }
    }

    private void HandleModeChanged(GameModeManager.GameMode mode)
    {
        if (!IsFirstTimePlayer())
        {
            HideTutorial();
            return;
        }

        if (mode == GameModeManager.GameMode.Exploration)
        {
            float delay = (lastKnownMode == GameModeManager.GameMode.Initial) ? delayBeforeShow : 0.2f;

            if (showCoroutine != null) StopCoroutine(showCoroutine);
            showCoroutine = StartCoroutine(ShowTutorialAfterDelay(delay));
        }
        else
        {
            HideTutorial();
        }

        lastKnownMode = mode;
    }

    private IEnumerator ShowTutorialAfterDelay(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (GameModeManager.Instance != null && GameModeManager.Instance.GetCurrentMode() != GameModeManager.GameMode.Exploration)
            yield break;

        ShowAndAnimatePointer();

        if (targetCoin != null)
        {
            targetCoin.ForceStartShakeAnimation();
        }
    }

    private bool IsFirstTimePlayer()
    {
        if (SaveSystem.Instance == null) return true;
        return SaveSystem.Instance.GetSaveData().GetCompletedCount() == 0;
    }

    private void ShowAndAnimatePointer()
    {
        if (pointerUI == null) return;

        pointerUI.SetActive(true);

        bounceTween?.Kill();

        bounceTween = pointerUI.transform.DOLocalMoveY(pointerUI.transform.localPosition.y + bounceDistance, bounceDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void HideTutorial()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        if (pointerUI != null)
        {
            bounceTween?.Kill();
            pointerUI.SetActive(false);
        }

        if (targetCoin != null)
        {
            targetCoin.StopShakeAnimation();
        }
    }

    private void OnDestroy()
    {
        bounceTween?.Kill();

        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnModeChanged -= HandleModeChanged;
        }
    }
}
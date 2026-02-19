using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ScrollTargetZoomController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform scrollSnapRect;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.InOutQuart;

    [Header("Tweak")]
    [SerializeField] private Vector3 tweakPositionOffset;

    private Vector2 initialAnchoredPos;
    private Vector3 initialScale;
    private bool isZoomed = false;

    private void Awake()
    {
        if (scrollSnapRect)
        {
            initialAnchoredPos = scrollSnapRect.anchoredPosition;
            initialScale = scrollSnapRect.localScale;
        }

        if (!scrollRect && scrollSnapRect) scrollRect = scrollSnapRect.GetComponentInChildren<ScrollRect>();
    }

    public void ZoomToMatchTarget(RectTransform clickedItem, RectTransform targetRef, System.Action onComplete = null)
    {
        if (isZoomed || scrollSnapRect == null) return;
        isZoomed = true;

        if (scrollRect)
        {
            scrollRect.velocity = Vector2.zero;
            scrollRect.StopMovement();
            scrollRect.enabled = false;
        }

        Canvas.ForceUpdateCanvases();
        float widthRatio = targetRef.rect.width / clickedItem.rect.width;
        float heightRatio = targetRef.rect.height / clickedItem.rect.height;

        float targetScale = Mathf.Min(widthRatio, heightRatio) * scrollSnapRect.localScale.x;

        Vector3 itemWorldCenter = GetWorldCenter(clickedItem);
        Vector3 parentWorldPos = scrollSnapRect.position;

        Vector3 offsetVector = itemWorldCenter - parentWorldPos;

        float currentScale = scrollSnapRect.localScale.x;
        Vector3 predictedOffset = offsetVector * (targetScale / currentScale);

        Vector3 targetWorldCenter = GetWorldCenter(targetRef);
        Vector3 finalPos = targetWorldCenter - predictedOffset;

        finalPos += tweakPositionOffset;

        Sequence seq = DOTween.Sequence();

        seq.Join(scrollSnapRect.DOMove(finalPos, duration).SetEase(easeType));
        seq.Join(scrollSnapRect.DOScale(Vector3.one * targetScale, duration).SetEase(easeType));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void ZoomOut(System.Action onComplete = null)
    {
        if (!isZoomed || scrollSnapRect == null) return;
        isZoomed = false;

        Sequence seq = DOTween.Sequence();

        seq.Join(scrollSnapRect.DOAnchorPos(initialAnchoredPos, duration).SetEase(easeType));
        seq.Join(scrollSnapRect.DOScale(initialScale, duration).SetEase(easeType));

        seq.OnComplete(() =>
        {
            if (scrollRect) scrollRect.enabled = true;

            onComplete?.Invoke();
        });
    }

    private Vector3 GetWorldCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0] + corners[2]) / 2f;
    }
}
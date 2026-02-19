using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArtefactDetailController : MonoBehaviour
{
    [Header("Main References")]
    [SerializeField] private GameObject root;
    [SerializeField] private CanvasGroup canvasGroupRoot;
    [SerializeField] private Sprite defaultBgSprite;
    [SerializeField] private Image imageBg;
    [SerializeField] private Sprite[] backgroundSprites;

    [Header("Icon Target")]
    [SerializeField] private Image imageIcon;
    [SerializeField] private RectTransform imageIconRect;

    [Header("Item Details")]
    [SerializeField] private string uncompletedLabel = "Unknown Artefact";
    [SerializeField] private TMP_Text textTitle;
    [SerializeField] private TMP_Text textDescription;
    [SerializeField] private TMP_Text textFooter;

    [Header("Buttons")]
    [SerializeField] private Button buttonBack;
    [SerializeField] private Button buttonClean;

    [Header("Settings")]
    [SerializeField] private float fadeDuration;

    private ArtefactData currentArtefactData;

    public RectTransform TargetIconRect => imageIconRect;

    private void Awake()
    {
        buttonBack.onClick.AddListener(OnClickBack);
        buttonClean.onClick.AddListener(OnClickClean);
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (buttonBack) buttonBack.onClick.RemoveListener(OnClickBack);
        if (buttonClean) buttonClean.onClick.RemoveListener(OnClickClean);
        transform.DOKill();
    }

    public void OpenDetail(ArtefactData artefactData, ChapterType chapterType, bool isCompleted)
    {
        currentArtefactData = artefactData;

        if (chapterType == ChapterType.China)
        {
            imageBg.sprite = backgroundSprites[0];
        }
        else if (chapterType == ChapterType.Indonesia)
        {
            imageBg.sprite = backgroundSprites[1];
        }
        else if (chapterType == ChapterType.Mesir)
        {
            imageBg.sprite = backgroundSprites[2];
        }
        else
        {
            imageBg.sprite = defaultBgSprite;
        }
        if (currentArtefactData == null) return;

        imageIcon.sprite = isCompleted ? currentArtefactData.CompletedIcon : currentArtefactData.BaseData.ItemIcon;
        textTitle.text = isCompleted ? currentArtefactData.BaseData.ItemName : uncompletedLabel;

        textDescription.gameObject.SetActive(isCompleted);
        textFooter.gameObject.SetActive(isCompleted);

        if (isCompleted)
        {
            textDescription.text = currentArtefactData.BaseData.ItemDescription;
            textFooter.text = $"Code: {currentArtefactData.BaseData.Id}\nConserved by Nova";
        }

        HideContentInstant();

        root.SetActive(true);
    }

    public void CloseDetail()
    {
        if (canvasGroupRoot != null)
        {
            canvasGroupRoot.DOFade(0f, 0.3f).OnComplete(() =>
            {
                root.SetActive(false);
                canvasGroupRoot.alpha = 1f;
            });
        }
        else
        {
            root.SetActive(false);
        }
    }

    public void HideContentInstant()
    {
        if (canvasGroupRoot)
        {
            canvasGroupRoot.alpha = 0f;
            canvasGroupRoot.blocksRaycasts = false;
        }

        if (imageBg) SetAlpha(imageBg, 0f);
        if (imageIcon) SetAlpha(imageIcon, 0f);
    }

    public void ShowContentFadeIn()
    {
        Sequence seq = DOTween.Sequence();

        if (canvasGroupRoot) canvasGroupRoot.blocksRaycasts = true;

        if (imageBg) seq.Join(imageBg.DOFade(1f, fadeDuration));
        if (imageIcon) seq.Join(imageIcon.DOFade(1f, fadeDuration));
        if (canvasGroupRoot) seq.Join(canvasGroupRoot.DOFade(1f, fadeDuration));
    }

    private void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null) return;
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }

    private void OnClickBack()
    {
        MainMenuEvents.OnCloseArtefactDetail?.Invoke();
        // TODO: zoom out pake manager
    }

    private void OnClickClean()
    {
        if (currentArtefactData == null) return;
        MainMenuEvents.OnRequestArtefactPlay?.Invoke(currentArtefactData);
        // TODO: kasih tau ke manager untuk mulai ke gameplay scene
    }
}
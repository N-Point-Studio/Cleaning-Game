using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class ContentSwitcher : MonoBehaviour
{
    [Header("Button Trigger")]
    [SerializeField] private Button triggerButton;

    [Header("Content GameObjects")]
    [SerializeField] private GameObject initialText;           // First text (shows initially)
    [SerializeField] private GameObject text1;                 // Second text (shows after button)
    [SerializeField] private GameObject text2;                 // Third text (shows after text1)
    [SerializeField] private GameObject text3;                 // Fourth text (shows after text2)
    [SerializeField] private GameObject beforeImage;           // First image (shows initially)
    [SerializeField] private GameObject afterImage;            // Second image (shows after button)

    [Header("Animation Settings")]
    [SerializeField] private float fadeTransitionDuration = 0.5f;
    [SerializeField] private float delayBetweenTexts = 0.3f;
    [SerializeField] private float characterAnimationSpeed = 0.05f;

    private bool isContentRevealed = false;
    private bool isAnimating = false;

    private void Start()
    {
        SetupInitialState();
        SetupButtonListener();
    }

    private void SetupInitialState()
    {
        // Show initial content
        if (initialText != null)
            initialText.SetActive(true);

        if (beforeImage != null)
            beforeImage.SetActive(true);

        // Hide additional content
        if (text1 != null)
            text1.SetActive(false);

        if (text2 != null)
            text2.SetActive(false);

        if (text3 != null)
            text3.SetActive(false);

        if (afterImage != null)
            afterImage.SetActive(false);
    }

    private void SetupButtonListener()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.AddListener(OnButtonClicked);
        }
    }

    public void OnButtonClicked()
    {
        if (isAnimating) return; // Prevent multiple clicks during animation

        if (!isContentRevealed)
        {
            StartCoroutine(RevealContentWithAnimation());
        }
        else
        {
            StartCoroutine(HideContentWithAnimation());
        }
    }

    private IEnumerator RevealContentWithAnimation()
    {
        isAnimating = true;

        // Fade out initial content
        yield return StartCoroutine(FadeOutGameObject(initialText));
        yield return StartCoroutine(FadeOutGameObject(beforeImage));

        // Hide initial content
        if (initialText != null)
            initialText.SetActive(false);
        if (beforeImage != null)
            beforeImage.SetActive(false);

        // Show and fade in after image
        if (afterImage != null)
        {
            afterImage.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(afterImage));
        }

        // Show texts sequentially with natural writing animation
        yield return StartCoroutine(ShowTextsSequentially());

        isContentRevealed = true;
        isAnimating = false;
    }

    private IEnumerator HideContentWithAnimation()
    {
        isAnimating = true;

        // Fade out all additional content
        yield return StartCoroutine(FadeOutGameObject(text1));
        yield return StartCoroutine(FadeOutGameObject(text2));
        yield return StartCoroutine(FadeOutGameObject(text3));
        yield return StartCoroutine(FadeOutGameObject(afterImage));

        // Hide additional content
        if (text1 != null)
            text1.SetActive(false);
        if (text2 != null)
            text2.SetActive(false);
        if (text3 != null)
            text3.SetActive(false);
        if (afterImage != null)
            afterImage.SetActive(false);

        // Show and fade in initial content
        if (initialText != null)
        {
            initialText.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(initialText));
        }
        if (beforeImage != null)
        {
            beforeImage.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(beforeImage));
        }

        isContentRevealed = false;
        isAnimating = false;
    }

    private IEnumerator ShowTextsSequentially()
    {
        GameObject[] textObjects = { text1, text2, text3 };

        for (int i = 0; i < textObjects.Length; i++)
        {
            if (textObjects[i] != null)
            {
                // Show the GameObject
                textObjects[i].SetActive(true);

                // Prepare text components for typewriter effect (hide all characters)
                PrepareTextForTypewriter(textObjects[i]);

                // Make sure text is fully visible (no fade, just direct visibility)
                yield return StartCoroutine(SetGameObjectAlpha(textObjects[i], 1f));

                // Start typewriter animation
                yield return StartCoroutine(StartTypewriterAnimationCoroutine(textObjects[i]));

                // Wait before showing next text
                yield return new WaitForSeconds(delayBetweenTexts);
            }
        }
    }

    private void PrepareTextForTypewriter(GameObject textObject)
    {
        // Find all TextMeshProUGUI components and set maxVisibleCharacters to 0
        TextMeshProUGUI[] textComponents = textObject.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent != null)
            {
                textComponent.maxVisibleCharacters = 0;
            }
        }
    }

    private IEnumerator StartTypewriterAnimationCoroutine(GameObject textObject)
    {
        // Find all TextMeshProUGUI components in this GameObject
        TextMeshProUGUI[] textComponents = textObject.GetComponentsInChildren<TextMeshProUGUI>();

        float longestAnimationDuration = 0f;

        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent != null)
            {
                // Store the full text content
                string fullText = textComponent.text;

                if (!string.IsNullOrEmpty(fullText))
                {
                    // Create typewriter animation using DOTween (same as ClickableObject)
                    float animationDuration = fullText.Length * characterAnimationSpeed;
                    longestAnimationDuration = Mathf.Max(longestAnimationDuration, animationDuration);

                    DOTween.To(() => textComponent.maxVisibleCharacters,
                               x => textComponent.maxVisibleCharacters = x,
                               fullText.Length,
                               animationDuration)
                           .SetEase(Ease.Linear);
                }
            }
        }

        // Wait for the longest typewriter animation to complete
        if (longestAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(longestAnimationDuration);
        }
    }

    private IEnumerator FadeInGameObject(GameObject obj)
    {
        if (obj == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeTransitionDuration);
            yield return StartCoroutine(SetGameObjectAlpha(obj, alpha));
            yield return null;
        }
        yield return StartCoroutine(SetGameObjectAlpha(obj, 1f));
    }

    private IEnumerator FadeOutGameObject(GameObject obj)
    {
        if (obj == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < fadeTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (elapsedTime / fadeTransitionDuration));
            yield return StartCoroutine(SetGameObjectAlpha(obj, alpha));
            yield return null;
        }
        yield return StartCoroutine(SetGameObjectAlpha(obj, 0f));
    }

    private IEnumerator SetGameObjectAlpha(GameObject obj, float alpha)
    {
        if (obj == null) yield break;

        // Set alpha for all Image components
        Image[] images = obj.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            Color color = img.color;
            color.a = alpha;
            img.color = color;
        }

        // Set alpha for all TextMeshProUGUI components
        TextMeshProUGUI[] texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in texts)
        {
            Color color = text.color;
            color.a = alpha;
            text.color = color;
        }

        yield return null;
    }

    // Public methods for external control
    public void ResetToInitialState()
    {
        if (isAnimating)
            return;

        isContentRevealed = false;
        SetupInitialState();
    }

    private void OnDestroy()
    {
        if (triggerButton != null)
            triggerButton.onClick.RemoveListener(OnButtonClicked);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

[System.Serializable]
public enum ChapterType
{
    China,
    Indonesia,
    Mesir
}

[System.Serializable]
public enum ObjectType
{
    ChinaCoin,
    ChinaJar,
    ChinaHorse,
    IndonesiaKendin,
    MesirWingedScared
}

/// <summary>
/// ContentSwitcher handles artifact information display and transition to next artifact preview.
/// Supports different chapter types with customizable behavior for each.
///
/// Chapter Type Configuration:
/// Set the chapterType field in the inspector to specify which chapter this ContentSwitcher represents:
/// - China: For Chinese artifacts and transitions
/// - Indonesia: For Indonesian artifacts and transitions
/// - Mesir: For Egyptian (Mesir) artifacts and transitions
///
/// Each chapter type can have different animation behaviors by modifying the respective methods:
/// - ShowChinaImageTransition(): Custom behavior for China chapter
/// - ShowIndonesiaImageTransition(): Custom behavior for Indonesia chapter
/// - ShowMesirImageTransition(): Custom behavior for Mesir chapter
///
/// Animation Flow:
/// 1. Initial state: initialText + nextArtifactImage1 visible
/// 2. Button clicked: initialText fades out, nextArtifactImage1 remains visible, afterImage + text1, text2, text3 (sequential typewriter)
/// 3. After text3: nextArtifactImage1 fades out, then nextArtifactImage2 appears (chapter-specific transition)
/// 4. Button clicked again: everything fades out, returns to initial state (initialText + nextArtifactImage1)
/// </summary>
public class ContentSwitcher : MonoBehaviour
{
    [Header("Chapter Configuration")]
    [SerializeField] private ChapterType chapterType = ChapterType.China;
    [SerializeField] private ObjectType objectType = ObjectType.ChinaCoin;

    [Header("Testing Configuration")]
    [SerializeField] private ChapterType currentTestingChapter = ChapterType.China;
    [SerializeField] private bool enableTestingMode = true;

    [Header("Button Trigger")]
    [SerializeField] private Button triggerButton;

    [Header("Content GameObjects")]
    [SerializeField] private GameObject initialText;           // First text (shows initially)
    [SerializeField] private GameObject text1;                 // Second text (shows after button)
    [SerializeField] private GameObject text2;                 // Third text (shows after text1)
    [SerializeField] private GameObject text3;                 // Fourth text (shows after text2)

    [Header("Current Artifact Images")]
    [SerializeField] private GameObject afterImage;            // Current artifact processed image (shows after button)

    [Header("Next Artifact Preview Images")]
    [SerializeField] private GameObject nextArtifactImage1;    // Next artifact preview (appears after text3, then disappears)
    [SerializeField] private GameObject nextArtifactImage2;    // Next artifact final preview (appears after nextArtifactImage1 disappears)

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
        {
            initialText.SetActive(true);
            // Make sure initial text starts with all characters hidden (like ClickableObject)
            PrepareTextForTypewriter(initialText);
        }

        // Hide additional content
        if (text1 != null)
            text1.SetActive(false);

        if (text2 != null)
            text2.SetActive(false);

        if (text3 != null)
            text3.SetActive(false);

        if (afterImage != null)
            afterImage.SetActive(false);

        if (nextArtifactImage1 != null)
            nextArtifactImage1.SetActive(true);

        if (nextArtifactImage2 != null)
            nextArtifactImage2.SetActive(false);
    }

    private void SetupButtonListener()
    {
        if (triggerButton != null)
        {
            // Remove any existing listeners to prevent conflicts
            triggerButton.onClick.RemoveAllListeners();
            triggerButton.onClick.AddListener(OnButtonClicked);
            Debug.Log($"[ContentSwitcher-{gameObject.name}] Button listener set for {chapterType} chapter");
        }
        else
        {
            Debug.LogWarning($"[ContentSwitcher-{gameObject.name}] No trigger button assigned for {chapterType} chapter!");
        }
    }

    public void OnButtonClicked()
    {
        if (isAnimating) return; // Prevent multiple clicks during animation

        // Check if testing mode is enabled and if this chapter should respond
        if (enableTestingMode)
        {
            if (chapterType != currentTestingChapter)
            {
                Debug.Log($"[ContentSwitcher-{gameObject.name}] Ignoring button click - Testing {currentTestingChapter}, but this is {chapterType}");
                return;
            }
            Debug.Log($"[ContentSwitcher-{gameObject.name}] === TESTING MODE: {chapterType.ToString().ToUpper()} CHAPTER ===");
        }

        // Only this specific chapter type should respond
        string instanceName = gameObject.name;
        Debug.Log($"[ContentSwitcher-{instanceName}] Button clicked for {chapterType} chapter. Current state: {(isContentRevealed ? "Revealed" : "Initial")}");

        if (!isContentRevealed)
        {
            Debug.Log($"[ContentSwitcher-{instanceName}] === STARTING {chapterType.ToString().ToUpper()} CHAPTER REVEAL ===");
            StartCoroutine(RevealContentWithAnimation());
        }
        else
        {
            Debug.Log($"[ContentSwitcher-{instanceName}] === HIDING {chapterType.ToString().ToUpper()} CHAPTER CONTENT ===");
            StartCoroutine(HideContentWithAnimation());
        }
    }

    private IEnumerator RevealContentWithAnimation()
    {
        isAnimating = true;
        Debug.Log($"[ContentSwitcher] RevealContentWithAnimation started for {chapterType} chapter");

        // Fade out initial content only (keep nextArtifactImage1 visible)
        Debug.Log("[ContentSwitcher] Fading out initial content (keeping nextArtifactImage1 visible)");
        yield return StartCoroutine(FadeOutGameObject(initialText));

        // Hide initial content only
        if (initialText != null)
        {
            initialText.SetActive(false);
            Debug.Log("[ContentSwitcher] Initial text hidden");
        }
        Debug.Log("[ContentSwitcher] NextArtifactImage1 remains visible during text animations");

        // Show and fade in after image
        if (afterImage != null)
        {
            afterImage.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(afterImage));
        }

        // Show texts sequentially with natural writing animation
        yield return StartCoroutine(ShowTextsSequentially());

        // After text animations are done, show image transition based on chapter type
        Debug.Log($"[ContentSwitcher] Starting chapter-specific image transition for {chapterType}");
        yield return StartCoroutine(ShowImageTransitionByChapter());

        isContentRevealed = true;
        isAnimating = false;
        Debug.Log($"[ContentSwitcher] RevealContentWithAnimation completed for {chapterType} chapter");
    }

    private IEnumerator RevealChinaChapter()
    {
        isAnimating = true;
        Debug.Log("[CHINA CHAPTER] Starting reveal animation specifically for China testing");

        // Step 1: Fade out initial China content
        Debug.Log("[CHINA CHAPTER] Step 1: Hiding initial China content");
        yield return StartCoroutine(FadeOutGameObject(initialText));
        yield return StartCoroutine(FadeOutGameObject(nextArtifactImage1));

        if (initialText != null)
        {
            initialText.SetActive(false);
            Debug.Log("[CHINA CHAPTER] Initial China text hidden");
        }
        if (nextArtifactImage1 != null)
        {
            nextArtifactImage1.SetActive(false);
            Debug.Log("[CHINA CHAPTER] China artifact preview (nextArtifactImage1) hidden");
        }

        // Step 2: Show China processed image
        Debug.Log("[CHINA CHAPTER] Step 2: Showing China processed artifact");
        if (afterImage != null)
        {
            afterImage.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(afterImage));
            Debug.Log("[CHINA CHAPTER] China processed artifact (afterImage) shown");
        }

        // Step 3: Show China text sequence
        Debug.Log("[CHINA CHAPTER] Step 3: Starting China text sequence");
        yield return StartCoroutine(ShowTextsSequentially());

        // Step 4: Show China final artifact preview
        Debug.Log("[CHINA CHAPTER] Step 4: Showing China final artifact preview");
        yield return StartCoroutine(ShowChinaImageTransition());

        isContentRevealed = true;
        isAnimating = false;
        Debug.Log("[CHINA CHAPTER] === CHINA CHAPTER REVEAL COMPLETED ===");
    }

    private IEnumerator HideContentWithAnimation()
    {
        isAnimating = true;

        // Fade out all additional content (but not nextArtifactImage1 - we want to restore it)
        yield return StartCoroutine(FadeOutGameObject(text1));
        yield return StartCoroutine(FadeOutGameObject(text2));
        yield return StartCoroutine(FadeOutGameObject(text3));
        yield return StartCoroutine(FadeOutGameObject(afterImage));
        yield return StartCoroutine(FadeOutGameObject(nextArtifactImage2));

        // Hide additional content
        if (text1 != null)
            text1.SetActive(false);
        if (text2 != null)
            text2.SetActive(false);
        if (text3 != null)
            text3.SetActive(false);
        if (afterImage != null)
            afterImage.SetActive(false);
        if (nextArtifactImage2 != null)
            nextArtifactImage2.SetActive(false);

        // Show and fade in initial content
        if (initialText != null)
        {
            initialText.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(initialText));
        }
        if (nextArtifactImage1 != null)
        {
            nextArtifactImage1.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(nextArtifactImage1));
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

    private IEnumerator ShowImageTransitionByChapter()
    {
        // Handle image transitions based on chapter type
        switch (chapterType)
        {
            case ChapterType.China:
                yield return StartCoroutine(ShowChinaImageTransition());
                break;
            case ChapterType.Indonesia:
                yield return StartCoroutine(ShowIndonesiaImageTransition());
                break;
            case ChapterType.Mesir:
                yield return StartCoroutine(ShowMesirImageTransition());
                break;
        }
    }

    private IEnumerator ShowChinaImageTransition()
    {
        // China specific image transition - Hide nextArtifactImage1 and show nextArtifactImage2
        Debug.Log("[ContentSwitcher] Starting China chapter image transition");

        // First, fade out nextArtifactImage1
        if (nextArtifactImage1 != null)
        {
            Debug.Log("[ContentSwitcher] Hiding nextArtifactImage1 (China preview)");
            yield return StartCoroutine(FadeOutGameObject(nextArtifactImage1));
            nextArtifactImage1.SetActive(false);
        }

        // Then, show nextArtifactImage2
        if (nextArtifactImage2 != null)
        {
            Debug.Log("[ContentSwitcher] Showing nextArtifactImage2 for China chapter");
            nextArtifactImage2.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(nextArtifactImage2));
            Debug.Log("[ContentSwitcher] China chapter image transition completed");
        }
        else
        {
            Debug.LogWarning("[ContentSwitcher] nextArtifactImage2 is null for China chapter");
        }
    }

    private IEnumerator ShowIndonesiaImageTransition()
    {
        // Indonesia specific image transition - Hide nextArtifactImage1 and show nextArtifactImage2
        Debug.Log("[ContentSwitcher] Starting Indonesia chapter image transition");

        // First, fade out nextArtifactImage1
        if (nextArtifactImage1 != null)
        {
            Debug.Log("[ContentSwitcher] Hiding nextArtifactImage1 (Indonesia preview)");
            yield return StartCoroutine(FadeOutGameObject(nextArtifactImage1));
            nextArtifactImage1.SetActive(false);
        }

        // Then, show nextArtifactImage2
        if (nextArtifactImage2 != null)
        {
            Debug.Log("[ContentSwitcher] Showing nextArtifactImage2 for Indonesia chapter");
            nextArtifactImage2.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(nextArtifactImage2));
            Debug.Log("[ContentSwitcher] Indonesia chapter image transition completed");
        }
    }

    private IEnumerator ShowMesirImageTransition()
    {
        // Mesir specific image transition - Hide nextArtifactImage1 and show nextArtifactImage2
        Debug.Log("[ContentSwitcher] Starting Mesir chapter image transition");

        // First, fade out nextArtifactImage1
        if (nextArtifactImage1 != null)
        {
            Debug.Log("[ContentSwitcher] Hiding nextArtifactImage1 (Mesir preview)");
            yield return StartCoroutine(FadeOutGameObject(nextArtifactImage1));
            nextArtifactImage1.SetActive(false);
        }

        // Then, show nextArtifactImage2
        if (nextArtifactImage2 != null)
        {
            Debug.Log("[ContentSwitcher] Showing nextArtifactImage2 for Mesir chapter");
            nextArtifactImage2.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(nextArtifactImage2));
            Debug.Log("[ContentSwitcher] Mesir chapter image transition completed");
        }
    }

    private IEnumerator ShowImageTransition()
    {
        // Legacy method - now calls chapter-specific method
        yield return StartCoroutine(ShowImageTransitionByChapter());
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

    // Method to trigger initial text animation (can be called from ClickableObject)
    public void ShowInitialText()
    {
        if (initialText != null)
        {
            StartCoroutine(StartTypewriterAnimationCoroutine(initialText));
        }
    }

    // Public method to get current chapter type
    public ChapterType GetChapterType()
    {
        return chapterType;
    }

    // Public method to set chapter type (useful for runtime configuration)
    public void SetChapterType(ChapterType newChapterType)
    {
        chapterType = newChapterType;
    }

    // Public method to get current object type
    public ObjectType GetObjectType()
    {
        return objectType;
    }

    // Public method to set object type (useful for runtime configuration)
    public void SetObjectType(ObjectType newObjectType)
    {
        objectType = newObjectType;
    }

    // Public method to get object info as string
    public string GetObjectInfo()
    {
        return $"Chapter: {chapterType}, Object: {objectType}";
    }

    // Testing methods
    public void SetTestingChapter(ChapterType testChapter)
    {
        currentTestingChapter = testChapter;
        Debug.Log($"[Testing] Current testing chapter set to: {testChapter}");
    }

    public void EnableTestingMode(bool enabled)
    {
        enableTestingMode = enabled;
        Debug.Log($"[Testing] Testing mode: {(enabled ? "ENABLED" : "DISABLED")}");
    }

    // Quick testing methods
    [System.Obsolete("For testing only")]
    public void TestChina()
    {
        SetTestingChapter(ChapterType.China);
    }

    [System.Obsolete("For testing only")]
    public void TestIndonesia()
    {
        SetTestingChapter(ChapterType.Indonesia);
    }

    [System.Obsolete("For testing only")]
    public void TestMesir()
    {
        SetTestingChapter(ChapterType.Mesir);
    }

    private void OnDestroy()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(OnButtonClicked);
            Debug.Log($"[ContentSwitcher-{gameObject.name}] Button listener removed for {chapterType} chapter");
        }
    }
}

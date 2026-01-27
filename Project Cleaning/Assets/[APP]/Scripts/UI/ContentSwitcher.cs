using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
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

    // ✅ NEW: Modern state management system
    public enum ContentSwitcherState
    {
        Initial,              // Default state - showing initial content
        PermanentCompleted    // Permanently completed from gameplay - cannot be changed
    }

    [Header("State Management")]
    [SerializeField] private ContentSwitcherState currentState = ContentSwitcherState.Initial;
    private bool isAnimating = false;

    private void Start()
    {
        Debug.Log($"[ContentSwitcher] Start() called for {chapterType} {objectType} - Current state: {currentState}");

        // ✅ CRITICAL FIX: Check if we're already completed before doing anything
        bool wasAlreadyCompleted = CheckIfThisObjectCompleted();

        if (wasAlreadyCompleted)
        {
            Debug.Log($"[ContentSwitcher] {chapterType} {objectType} is already completed - setting permanent state immediately");
            currentState = ContentSwitcherState.PermanentCompleted;
            ApplyPermanentCompletionVisualState();

            // ✅ SKIP SetupInitialState() if already completed - it will be ignored anyway
            // SetupInitialState() - SKIP THIS!
        }
        else
        {
            Debug.Log($"[ContentSwitcher] {chapterType} {objectType} not completed - setting up initial state");
            SetupInitialState();
        }

        SetupButtonListener();

        // Apply save data logic after a small delay to allow initial animation
        StartCoroutine(ApplySaveDataAfterInitialSetup());
    }

    private void SetupInitialState()
    {
        // ✅ CRITICAL FIX: DO NOT RESET UI IF ALREADY PERMANENTLY COMPLETED
        if (currentState == ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher] SetupInitialState() called but {chapterType} {objectType} is permanently completed - IGNORING to prevent reset!");
            return;
        }

        Debug.Log($"[ContentSwitcher] Setting up initial state for {chapterType} {objectType}");

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

    /// <summary>
    /// ✅ REFACTOR: Entry point for gameplay completion trigger (SceneTransitionManager calls this)
    /// </summary>
    public void OnButtonClicked()
    {
        TriggerCompletionFromGameplay();
    }

    /// <summary>
    /// ✅ NEW: Main entry point for gameplay completion
    /// </summary>
    public void TriggerCompletionFromGameplay()
    {
        if (isAnimating)
        {
            Debug.LogWarning($"[ContentSwitcher-{gameObject.name}] Animation in progress - ignoring completion trigger");
            return;
        }

        if (currentState == ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher-{gameObject.name}] Already permanently completed - ignoring trigger");
            return;
        }

        // Check if testing mode is enabled and if this chapter should respond
        if (enableTestingMode)
        {
            if (chapterType != currentTestingChapter)
            {
                Debug.Log($"[ContentSwitcher-{gameObject.name}] Ignoring trigger - Testing {currentTestingChapter}, but this is {chapterType}");
                return;
            }
            Debug.Log($"[ContentSwitcher-{gameObject.name}] === TESTING MODE: {chapterType.ToString().ToUpper()} CHAPTER ===");
        }

        string instanceName = gameObject.name;
        Debug.Log($"[ContentSwitcher-{instanceName}] Triggered completion for {chapterType} chapter. Current state: {currentState}");

        // Check if we have completed objects for this specific object/chapter
        bool hasCompletedObjects = CheckIfThisObjectCompleted();

        if (hasCompletedObjects)
        {
            Debug.Log($"[ContentSwitcher-{instanceName}] === PERMANENT COMPLETION: {chapterType.ToString().ToUpper()} CHAPTER ===");
            StartCoroutine(ShowPermanentCompletionAnimation());
        }
        else
        {
            Debug.Log($"[ContentSwitcher-{instanceName}] No completion for this object - staying in initial state");
        }
    }

    /// <summary>
    /// ✅ NEW: Show permanent completion animation (cannot be reversed)
    /// </summary>
    private IEnumerator ShowPermanentCompletionAnimation()
    {
        isAnimating = true;
        Debug.Log($"[ContentSwitcher] Starting permanent completion animation for {chapterType} chapter");

        // Fade out initial content
        yield return StartCoroutine(FadeOutGameObject(initialText));

        // Hide initial content
        if (initialText != null)
        {
            initialText.SetActive(false);
        }

        // Show and fade in after image (completed artifact)
        if (afterImage != null)
        {
            afterImage.SetActive(true);
            yield return StartCoroutine(FadeInGameObject(afterImage));
        }

        // Show texts sequentially with typewriter animation
        yield return StartCoroutine(ShowTextsSequentially());

        // Show image transition based on chapter type
        yield return StartCoroutine(ShowImageTransitionByChapter());

        // Update next artifact preview based on current save data
        UpdateNextArtifactPreview();

        // ✅ CRITICAL: Set permanent state (cannot be changed)
        currentState = ContentSwitcherState.PermanentCompleted;
        ApplyPermanentCompletionVisualState();

        isAnimating = false;
        Debug.Log($"[ContentSwitcher] Permanent completion animation completed for {chapterType} chapter - STATE: {currentState}");
    }

    /// <summary>
    /// Ensure completed state visuals persist (hide before image, show after image)
    /// </summary>
    private void ApplyPermanentCompletionVisualState()
    {
        bool hasAfterImage = afterImage != null;

        // Hide any "before" visuals that share the same ObjectType under this ContentSwitcher
        if (hasAfterImage)
        {
            var clickables = GetComponentsInChildren<ClickableObject>(true);
            foreach (var clickable in clickables)
            {
                if (clickable.gameObject == afterImage) continue;
                if (clickable.GetObjectType() != objectType) continue;

                clickable.gameObject.SetActive(false);
            }
        }

        if (initialText != null) initialText.SetActive(false);
        HideIfSameObjectType(nextArtifactImage1);
        HideIfSameObjectType(nextArtifactImage2);

        if (hasAfterImage) afterImage.SetActive(true);
    }

    private void HideIfSameObjectType(GameObject target)
    {
        if (target == null) return;

        var clickable = target.GetComponent<ClickableObject>();
        if (clickable == null)
        {
            // Non-clickable preview can be hidden safely
            target.SetActive(false);
            return;
        }

        // Only hide if it belongs to the same object type (before/after pair for this item)
        if (clickable.GetObjectType() == objectType)
        {
            target.SetActive(false);
        }
    }

    /// <summary>
    /// ✅ DEPRECATED: Legacy animation method
    /// </summary>
    [System.Obsolete("Use ShowPermanentCompletionAnimation instead")]
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

        // ✅ DEPRECATED: This method is obsolete
        Debug.LogWarning("[ContentSwitcher] RevealContentWithAnimation is deprecated - use ShowPermanentCompletionAnimation");

        isAnimating = false;
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

        // ✅ DEPRECATED: This method is obsolete
        Debug.LogWarning("[ContentSwitcher] RevealChinaChapter is deprecated and should not be used");

        isAnimating = false;
    }

    /// <summary>
    /// ✅ DEPRECATED: No longer needed with permanent completion system
    /// </summary>
    [System.Obsolete("Manual hiding removed - permanent completion only")]
    private IEnumerator HideContentWithAnimation()
    {
        Debug.LogWarning("[ContentSwitcher] HideContentWithAnimation is deprecated - permanent completion cannot be hidden");
        yield break;
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

    /// <summary>
    /// ✅ CRITICAL FIX: Completely block resetting of permanently completed ContentSwitcher
    /// </summary>
    [System.Obsolete("Cannot reset permanently completed ContentSwitcher")]
    public void ResetToInitialState()
    {
        Debug.Log($"[ContentSwitcher] ResetToInitialState() called for {chapterType} {objectType} - Current state: {currentState}");

        if (currentState == ContentSwitcherState.PermanentCompleted)
        {
            Debug.LogError($"[ContentSwitcher] ❌ CRITICAL: Attempted to reset permanently completed {chapterType} {objectType} - BLOCKED!");
            return;
        }

        if (isAnimating)
        {
            Debug.LogWarning($"[ContentSwitcher] Cannot reset while animating for {chapterType} {objectType}");
            return;
        }

        Debug.Log($"[ContentSwitcher] Resetting {chapterType} {objectType} to initial state");
        currentState = ContentSwitcherState.Initial;
        SetupInitialState();
    }

    /// <summary>
    /// Method to trigger initial text animation (can be called from ClickableObject)
    /// ✅ PROTECTED: Won't run if permanently completed
    /// </summary>
    public void ShowInitialText()
    {
        if (currentState == ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher] ShowInitialText() ignored for permanently completed {chapterType} {objectType}");
            return;
        }

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

    #region Multi-Object State Management

    /// <summary>
    /// ✅ DEPRECATED: Use ApplySaveDataStateIfAvailable() instead
    /// Check save data to determine what content should be shown based on chapter completion
    /// </summary>
    [System.Obsolete("Use ApplySaveDataStateIfAvailable() instead to preserve animations")]
    private void DetermineContentStateFromSaveData()
    {
        // Redirect to new method that preserves animations
        ApplySaveDataStateIfAvailable();
    }

    /// <summary>
    /// Get the most recently completed object from a list
    /// </summary>
    private CompletedObject GetLatestCompletedObject(List<CompletedObject> completedObjects)
    {
        CompletedObject latest = completedObjects[0];

        foreach (var obj in completedObjects)
        {
            try
            {
                if (System.DateTime.Parse(obj.completedTime) > System.DateTime.Parse(latest.completedTime))
                {
                    latest = obj;
                }
            }
            catch (System.Exception)
            {
                // If timestamp parsing fails, use first object
                continue;
            }
        }

        return latest;
    }

    /// <summary>
    /// Show content for a completed object immediately (without animation)
    /// </summary>
    private void ShowContentForCompletedObject(CompletedObject completedObj)
    {
        Debug.Log($"[ContentSwitcher] Showing content for completed object: {completedObj.objectName} ({completedObj.objectType})");

        // Show the completion state immediately (no animation)
        if (initialText != null) initialText.SetActive(false);
        if (afterImage != null) afterImage.SetActive(true);
        if (text1 != null) text1.SetActive(true);
        if (text2 != null) text2.SetActive(true);
        if (text3 != null) text3.SetActive(true);

        // Ensure text is fully visible
        PrepareTextFullyVisible(text1);
        PrepareTextFullyVisible(text2);
        PrepareTextFullyVisible(text3);

        // Show appropriate next artifact preview based on chapter progress
        ShowNextArtifactForChapter();

        // ✅ DEPRECATED: This method is obsolete - no longer using isContentRevealed
        Debug.LogWarning("[ContentSwitcher] ShowContentForCompletedObject is deprecated");
    }

    /// <summary>
    /// Make text fully visible without typewriter effect
    /// </summary>
    private void PrepareTextFullyVisible(GameObject textObject)
    {
        if (textObject == null) return;

        TextMeshProUGUI[] textComponents = textObject.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            if (textComponent != null)
            {
                textComponent.maxVisibleCharacters = textComponent.text.Length;
            }
        }
    }

    /// <summary>
    /// ✅ DISABLED: Update next artifact preview (REMOVED TO PREVENT RESET ISSUE)
    /// This function was causing completed objects to reset when new objects are completed
    /// </summary>
    private void UpdateNextArtifactPreview()
    {
        Debug.Log($"[ContentSwitcher] UpdateNextArtifactPreview DISABLED to prevent reset issue for {chapterType} {objectType}");

        // ✅ FIX: DO NOT update next artifact preview automatically
        // This was causing completed ContentSwitchers to reset their UI
        // Next artifact updates should only happen during initial completion animation

        return; // Completely disabled to prevent reset issue
    }

    /// <summary>
    /// Show next artifact preview based on chapter completion progress
    /// </summary>
    private void ShowNextArtifactForChapter()
    {
        switch (chapterType)
        {
            case ChapterType.China:
                ShowNextChinaArtifact();
                break;
            case ChapterType.Indonesia:
                ShowNextIndonesiaArtifact();
                break;
            case ChapterType.Mesir:
                ShowNextMesirArtifact();
                break;
        }
    }

    /// <summary>
    /// Handle next artifact display for China chapter
    /// </summary>
    private void ShowNextChinaArtifact()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[ContentSwitcher] SaveSystem not available - cannot update China artifact preview");
            return;
        }

        var saveData = SaveSystem.Instance.GetSaveData();
        var chinaObjects = saveData.GetCompletedObjectsByChapter(ChapterType.China);

        Debug.Log($"[ContentSwitcher] China chapter has {chinaObjects.Count} completed objects");

        // ✅ FIX: Only update next artifact images if permanently completed
        if (currentState != ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher] China not permanently completed yet - skipping next artifact update");
            return;
        }

        if (chinaObjects.Count >= 3)
        {
            // All China objects completed - show Indonesia preview
            if (nextArtifactImage1 != null) nextArtifactImage1.SetActive(false);
            if (nextArtifactImage2 != null) nextArtifactImage2.SetActive(true); // Indonesia preview
            Debug.Log("[ContentSwitcher] All China objects completed - showing Indonesia preview");
        }
        else
        {
            // Show next China artifact
            if (nextArtifactImage1 != null) nextArtifactImage1.SetActive(true);
            if (nextArtifactImage2 != null) nextArtifactImage2.SetActive(false);
            Debug.Log($"[ContentSwitcher] China progress: {chinaObjects.Count}/3 - showing next China artifact");
        }
    }

    /// <summary>
    /// Handle next artifact display for Indonesia chapter
    /// </summary>
    private void ShowNextIndonesiaArtifact()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.LogWarning("[ContentSwitcher] SaveSystem not available - cannot update Indonesia artifact preview");
            return;
        }

        var saveData = SaveSystem.Instance.GetSaveData();
        var indonesiaObjects = saveData.GetCompletedObjectsByChapter(ChapterType.Indonesia);

        Debug.Log($"[ContentSwitcher] Indonesia chapter has {indonesiaObjects.Count} completed objects");

        // ✅ FIX: Only update next artifact images if permanently completed
        if (currentState != ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher] Indonesia not permanently completed yet - skipping next artifact update");
            return;
        }

        if (indonesiaObjects.Count >= 1)
        {
            // Indonesia completed - show Mesir preview
            if (nextArtifactImage1 != null) nextArtifactImage1.SetActive(false);
            if (nextArtifactImage2 != null) nextArtifactImage2.SetActive(true); // Mesir preview
            Debug.Log("[ContentSwitcher] Indonesia completed - showing Mesir preview");
        }
        else
        {
            // Show Indonesia artifact
            if (nextArtifactImage1 != null) nextArtifactImage1.SetActive(true);
            if (nextArtifactImage2 != null) nextArtifactImage2.SetActive(false);
            Debug.Log("[ContentSwitcher] Showing Indonesia artifact");
        }
    }

    /// <summary>
    /// Handle next artifact display for Mesir chapter
    /// </summary>
    private void ShowNextMesirArtifact()
    {
        // ✅ FIX: Only update next artifact images if permanently completed
        if (currentState != ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher] Mesir not permanently completed yet - skipping next artifact update");
            return;
        }

        // Mesir is the last chapter - show completion or credits
        if (nextArtifactImage1 != null) nextArtifactImage1.SetActive(false);
        if (nextArtifactImage2 != null) nextArtifactImage2.SetActive(true); // Completion/Credits
        Debug.Log("[ContentSwitcher] Mesir chapter - showing final completion state");
    }

    /// <summary>
    /// ✅ FIX: Apply save data after initial setup to maintain animation flow
    /// </summary>
    private System.Collections.IEnumerator ApplySaveDataAfterInitialSetup()
    {
        // ✅ CHANGE: Don't auto-trigger - wait for SceneTransitionManager to trigger us
        // SceneTransitionManager will call OnButtonClicked() when ready
        Debug.Log($"[ContentSwitcher] {chapterType} waiting for SceneTransitionManager trigger instead of auto-applying save data");

        // Don't apply save data automatically - let SceneTransitionManager handle the timing
        yield break;
    }

    /// <summary>
    /// ✅ NEW: Check if THIS SPECIFIC object is completed
    /// </summary>
    private bool CheckIfThisObjectCompleted()
    {
        if (SaveSystem.Instance == null)
        {
            return false;
        }

        var saveData = SaveSystem.Instance.GetSaveData();

        // Check if this specific object type is completed
        bool isCompleted = saveData.IsObjectCompleted(objectType.ToString(), objectType);

        Debug.Log($"[ContentSwitcher] Checking {objectType} completion - Result: {isCompleted}");
        return isCompleted;
    }

    /// <summary>
    /// ✅ DEPRECATED: Use CheckIfThisObjectCompleted instead
    /// </summary>
    [System.Obsolete("Use CheckIfThisObjectCompleted for specific object checking")]
    private bool CheckIfChapterHasCompletedObjects()
    {
        return CheckIfThisObjectCompleted();
    }

    /// <summary>
    /// Apply save data state but preserve animation experience
    /// </summary>
    private void ApplySaveDataStateIfAvailable()
    {
        if (SaveSystem.Instance == null)
        {
            Debug.Log($"[ContentSwitcher] SaveSystem not available - keeping initial state for {chapterType}");
            return;
        }

        // Get all completed objects in this chapter
        var saveData = SaveSystem.Instance.GetSaveData();
        var completedInChapter = saveData.GetCompletedObjectsByChapter(chapterType);

        Debug.Log($"[ContentSwitcher] Checking completion for {chapterType} chapter - Found {completedInChapter.Count} completed objects");

        if (completedInChapter.Count > 0)
        {
            // Show completion animation instead of static state
            Debug.Log($"[ContentSwitcher] Found completed objects in {chapterType} - triggering completion animation");

            // Trigger the button click animation to show completion
            // ✅ DEPRECATED: This method is obsolete - use TriggerCompletionFromGameplay
            Debug.LogWarning("[ContentSwitcher] ApplySaveDataStateIfAvailable is deprecated");
            TriggerCompletionFromGameplay();
        }
        else
        {
            Debug.Log($"[ContentSwitcher] No completed objects in {chapterType} - keeping initial state");
        }
    }

    #endregion

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

    #region Event-Driven Updates

    /// <summary>
    /// ✅ FIX: Subscribe to save system events when enabled
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to save system events
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.OnDataLoaded += OnSaveDataChanged;
            SaveSystem.Instance.OnDataSaved += OnSaveDataChanged;
            Debug.Log($"[ContentSwitcher] Subscribed to SaveSystem events for {chapterType} chapter");
        }
    }

    /// <summary>
    /// ✅ FIX: Unsubscribe from save system events when disabled
    /// </summary>
    private void OnDisable()
    {
        // Unsubscribe from save system events
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.OnDataLoaded -= OnSaveDataChanged;
            SaveSystem.Instance.OnDataSaved -= OnSaveDataChanged;
            Debug.Log($"[ContentSwitcher] Unsubscribed from SaveSystem events for {chapterType} chapter");
        }
    }

    /// <summary>
    /// ✅ FIXED: Handle save data changes and update content state (PREVENT RESET ISSUE)
    /// </summary>
    private void OnSaveDataChanged(SaveData saveData)
    {
        Debug.Log($"[ContentSwitcher] Save data changed - checking if update needed for {chapterType} object {objectType}");

        // ✅ CRITICAL FIX: Only respond to save data changes if we're in initial state
        // DO NOT modify permanently completed ContentSwitchers!
        if (currentState == ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher] {chapterType} {objectType} permanently completed - IGNORING save data change to prevent reset");
            return;
        }

        // Small delay to ensure save is fully processed
        StartCoroutine(UpdateContentStateAfterDelay());
    }

    /// <summary>
    /// Update content state with small delay to ensure save is processed (RESET-SAFE VERSION)
    /// </summary>
    private System.Collections.IEnumerator UpdateContentStateAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);

        // ✅ DOUBLE-CHECK: Make sure we haven't been marked as completed during delay
        if (currentState == ContentSwitcherState.PermanentCompleted)
        {
            Debug.Log($"[ContentSwitcher] {chapterType} {objectType} became completed during delay - aborting update");
            yield break;
        }

        // Check if this specific object should now be completed
        if (CheckIfThisObjectCompleted())
        {
            Debug.Log($"[ContentSwitcher] {chapterType} {objectType} now completed - triggering permanent completion");
            TriggerCompletionFromGameplay();
        }
        else
        {
            Debug.Log($"[ContentSwitcher] {chapterType} {objectType} still not completed - no state change needed");
        }
    }

    #endregion

    private void OnDestroy()
    {
        // Cleanup event subscriptions
        OnDisable();

        if (triggerButton != null)
        {
            triggerButton.onClick.RemoveListener(OnButtonClicked);
            Debug.Log($"[ContentSwitcher-{gameObject.name}] Button listener removed for {chapterType} chapter");
        }
    }
}

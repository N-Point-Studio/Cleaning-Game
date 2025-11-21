using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// Manages data persistence across scene transitions and triggers ContentSwitcher
/// Singleton that survives scene loads to pass ObjectType data between scenes
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Configuration")]
    [SerializeField] private string targetSceneName = "New Start Game Sandy";
    [SerializeField] private float sceneTransitionDelay = 0.5f;
    [SerializeField] private bool useEasyTransition = true;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    // Data to persist across scenes
    private ObjectType currentObjectType;
    private ChapterType currentChapterType;
    private bool shouldTriggerContentSwitcher = false;
    private bool isTransitionInProgress = false;

    // NEW: Track which specific object was clicked
    private string clickedObjectName = "";
    private Vector3 clickedObjectPosition = Vector3.zero;
    private bool shouldUpdateClickedObject = false;

    // Events for notification
    public System.Action<ObjectType> OnObjectTypeSet;
    public System.Action OnContentSwitcherTriggered;

    // Cached WaitForSeconds to avoid garbage collection
    private WaitForSeconds cachedSceneTransitionDelay;
    private readonly WaitForEndOfFrame cachedWaitForEndOfFrame = new WaitForEndOfFrame();
    private readonly WaitForSeconds cachedSmallDelay = new WaitForSeconds(0.1f);

    private void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Subscribe to scene loaded events
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Initialize cached WaitForSeconds
            cachedSceneTransitionDelay = new WaitForSeconds(sceneTransitionDelay);

            if (enableDebugLogs)
            {
                Debug.Log("=== SceneTransitionManager initialized and persisted ===");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    /// <summary>
    /// Set the ObjectType that will be passed to ContentSwitcher in the next scene
    /// </summary>
    public void SetObjectTypeForTransition(ObjectType objectType)
    {
        currentObjectType = objectType;
        currentChapterType = GetChapterFromObjectType(objectType);
        shouldTriggerContentSwitcher = true;

        if (enableDebugLogs)
        {
            Debug.Log($"=== SCENE TRANSITION MANAGER ===");
            Debug.Log($"Object Type Set: {objectType}");
            Debug.Log($"Chapter Type: {currentChapterType}");
            Debug.Log($"Will trigger ContentSwitcher: {shouldTriggerContentSwitcher}");
            Debug.Log($"===============================");
        }

        // Notify listeners
        OnObjectTypeSet?.Invoke(objectType);
    }

    /// <summary>
    /// Set the ObjectType that will be passed to ContentSwitcher AND remember which object was clicked
    /// </summary>
    public void SetObjectTypeForTransitionWithClickedObject(ObjectType objectType, string objectName, Vector3 objectPosition)
    {
        currentObjectType = objectType;
        currentChapterType = GetChapterFromObjectType(objectType);
        shouldTriggerContentSwitcher = true;

        // NEW: Remember which object was clicked
        clickedObjectName = objectName;
        clickedObjectPosition = objectPosition;
        shouldUpdateClickedObject = true;

        if (enableDebugLogs)
        {
            Debug.Log($"=== SCENE TRANSITION MANAGER (WITH CLICKED OBJECT) ===");
            Debug.Log($"Object Type Set: {objectType}");
            Debug.Log($"Chapter Type: {currentChapterType}");
            Debug.Log($"Clicked Object Name: {objectName}");
            Debug.Log($"Clicked Object Position: {objectPosition}");
            Debug.Log($"Will trigger ContentSwitcher + Update clicked object");
            Debug.Log($"==================================================");
        }

        OnObjectTypeSet?.Invoke(objectType);
    }

    /// <summary>
    /// Start scene transition to target scene with ContentSwitcher trigger
    /// </summary>
    public void TransitionToMainSceneWithContentSwitcher(ObjectType objectType)
    {
        if (isTransitionInProgress)
        {
            Debug.LogWarning("Scene transition already in progress!");
            return;
        }

        SetObjectTypeForTransition(objectType);
        StartCoroutine(PerformSceneTransition());
    }

    /// <summary>
    /// Perform the actual scene transition
    /// </summary>
    private IEnumerator PerformSceneTransition()
    {
        isTransitionInProgress = true;

        if (enableDebugLogs)
        {
            Debug.Log($"Starting scene transition to: {targetSceneName}");
        }

        // Optional delay before transition
        if (sceneTransitionDelay > 0)
        {
            yield return cachedSceneTransitionDelay;
        }

        // Load the target scene
        if (useEasyTransition)
        {
            bool transitionSuccessful = TryEasyTransition();
            if (!transitionSuccessful)
            {
                // Fallback to standard scene loading
                if (enableDebugLogs)
                {
                    Debug.Log("EasyTransition not available, using standard scene loading");
                }
                SceneManager.LoadScene(targetSceneName);
            }
        }
        else
        {
            // Standard Unity scene loading
            SceneManager.LoadScene(targetSceneName);
        }
    }

    /// <summary>
    /// Try to use EasyTransition for scene loading
    /// For now, just use standard Unity scene loading as fallback
    /// TODO: Implement proper EasyTransition API when available
    /// </summary>
    private bool TryEasyTransition()
    {
        try
        {
            // Check if EasyTransition is available in the project
            var easyTransitionType = System.Type.GetType("EasyTransition.TransitionManager");
            if (easyTransitionType != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("EasyTransition detected but API not implemented yet. Using standard scene loading.");
                }

                // For now, use standard Unity scene loading
                // This can be updated later when EasyTransition API is properly integrated
                SceneManager.LoadScene(targetSceneName);
                return true;
            }
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"EasyTransition check failed: {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// Update the clicked object after scene is loaded (make it look completed/changed)
    /// </summary>
    private IEnumerator UpdateClickedObjectAfterDelay()
    {
        // Wait for ContentSwitcher to finish first
        yield return new WaitForSeconds(1f);

        if (enableDebugLogs)
        {
            Debug.Log("=== UPDATING CLICKED OBJECT ===");
            Debug.Log($"Looking for object: {clickedObjectName}");
            Debug.Log($"At position: {clickedObjectPosition}");
        }

        // Find the clicked object in the scene
        ClickableObject targetObject = FindClickedObjectInScene();

        if (targetObject != null)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Found clicked object: {targetObject.name}");
            }

            // NEW: Use ClickableObject's built-in ContentSwitcher change system
            if (targetObject.HasValidContentSwitcher())
            {
                targetObject.ApplyContentSwitcherChanges();
                if (enableDebugLogs)
                {
                    Debug.Log($"Applied ContentSwitcher changes using ClickableObject system for: {targetObject.name}");
                }
            }
            else
            {
                // Fallback: Use legacy update system
                UpdateObjectToCompletedState(targetObject);
                if (enableDebugLogs)
                {
                    Debug.Log($"Using legacy update system for: {targetObject.name}");
                }
            }

            // Reset flag
            shouldUpdateClickedObject = false;

            if (enableDebugLogs)
            {
                Debug.Log("=== CLICKED OBJECT UPDATED SUCCESSFULLY ===");
            }
        }
        else
        {
            Debug.LogWarning($"Could not find clicked object: {clickedObjectName}");
        }
    }

    /// <summary>
    /// Find the clicked object in the current scene
    /// </summary>
    private ClickableObject FindClickedObjectInScene()
    {
        // Method 1: Find by exact name match
        if (!string.IsNullOrEmpty(clickedObjectName))
        {
            GameObject foundObj = GameObject.Find(clickedObjectName);
            if (foundObj != null)
            {
                ClickableObject clickable = foundObj.GetComponent<ClickableObject>();
                if (clickable != null)
                {
                    return clickable;
                }
            }
        }

        // Method 2: Find by ObjectType match
        ClickableObject[] allClickables = FindObjectsOfType<ClickableObject>();
        foreach (ClickableObject clickable in allClickables)
        {
            if (clickable.GetObjectType() == currentObjectType)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Found object by ObjectType match: {clickable.name}");
                }
                return clickable;
            }
        }

        // Method 3: Find by position (if close enough)
        if (clickedObjectPosition != Vector3.zero)
        {
            foreach (ClickableObject clickable in allClickables)
            {
                float distance = Vector3.Distance(clickable.transform.position, clickedObjectPosition);
                if (distance < 2f) // Within 2 units
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Found object by position match: {clickable.name} (distance: {distance:F2})");
                    }
                    return clickable;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Update the object to show it's completed (visual changes)
    /// </summary>
    private void UpdateObjectToCompletedState(ClickableObject targetObject)
    {
        if (targetObject == null) return;

        if (enableDebugLogs)
        {
            Debug.Log($"Updating object to completed state: {targetObject.name}");
        }

        // Method 1: Add a visual completion effect (particle, glow, etc.)
        AddCompletionEffect(targetObject);

        // Method 2: Change object material/color
        ChangeObjectAppearance(targetObject);

        // Method 3: Add completion indicator (checkmark, star, etc.)
        AddCompletionIndicator(targetObject);

        // Method 4: Trigger any completion animations
        TriggerCompletionAnimation(targetObject);
    }

    /// <summary>
    /// Add completion effect (particles, glow, etc.)
    /// </summary>
    private void AddCompletionEffect(ClickableObject targetObject)
    {
        // Try to find particle system on the object
        ParticleSystem particles = targetObject.GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            particles.Play();
            if (enableDebugLogs)
            {
                Debug.Log($"Playing particle effect on {targetObject.name}");
            }
        }

        // Try to add a simple glow effect
        Renderer objectRenderer = targetObject.GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            // Add glow by changing emission
            Material material = objectRenderer.material;
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.yellow * 0.5f);
                material.EnableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// Change object appearance (color, material, etc.)
    /// </summary>
    private void ChangeObjectAppearance(ClickableObject targetObject)
    {
        // Find renderer and change color slightly
        Renderer objectRenderer = targetObject.GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            // Tint object slightly to show completion
            Color originalColor = objectRenderer.material.color;
            Color completedColor = new Color(originalColor.r * 1.2f, originalColor.g * 1.2f, originalColor.b * 1.2f, originalColor.a);
            objectRenderer.material.color = completedColor;

            if (enableDebugLogs)
            {
                Debug.Log($"Changed appearance of {targetObject.name}");
            }
        }
    }

    /// <summary>
    /// Add completion indicator (checkmark, star, etc.)
    /// </summary>
    private void AddCompletionIndicator(ClickableObject targetObject)
    {
        // Try to find and activate a completion indicator child object
        Transform indicator = targetObject.transform.Find("CompletionIndicator");
        if (indicator != null)
        {
            indicator.gameObject.SetActive(true);
            if (enableDebugLogs)
            {
                Debug.Log($"Activated completion indicator on {targetObject.name}");
            }
        }
    }

    /// <summary>
    /// Trigger completion animation
    /// </summary>
    private void TriggerCompletionAnimation(ClickableObject targetObject)
    {
        // Try to find and trigger animator
        Animator animator = targetObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Completed");
            if (enableDebugLogs)
            {
                Debug.Log($"Triggered completion animation on {targetObject.name}");
            }
        }

        // Alternative: Simple scale animation using DOTween
        targetObject.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f, 5);
    }

    /// <summary>
    /// Called when a new scene is loaded
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Scene loaded: {scene.name}");
        }

        isTransitionInProgress = false;

        // Check if we need to trigger ContentSwitcher
        if (shouldTriggerContentSwitcher && scene.name == targetSceneName)
        {
            StartCoroutine(TriggerContentSwitcherAfterDelay());
        }

        // Check if we need to update clicked object
        if (shouldUpdateClickedObject && scene.name == targetSceneName)
        {
            StartCoroutine(UpdateClickedObjectAfterDelay());
        }
    }

    /// <summary>
    /// Trigger ContentSwitcher after scene is fully loaded
    /// </summary>
    private IEnumerator TriggerContentSwitcherAfterDelay()
    {
        // Wait a frame for scene objects to initialize
        yield return cachedWaitForEndOfFrame;

        // Additional small delay to ensure everything is ready
        yield return cachedSmallDelay;

        if (enableDebugLogs)
        {
            Debug.Log("=== TRIGGERING CONTENT SWITCHER ===");
            Debug.Log($"Looking for ContentSwitcher with ObjectType: {currentObjectType}");
        }

        // Find and trigger the appropriate ContentSwitcher
        bool contentSwitcherTriggered = TriggerContentSwitcher();

        if (contentSwitcherTriggered)
        {
            // Reset the flag after successful trigger
            shouldTriggerContentSwitcher = false;
            OnContentSwitcherTriggered?.Invoke();

            if (enableDebugLogs)
            {
                Debug.Log("=== CONTENT SWITCHER TRIGGERED SUCCESSFULLY ===");
            }
        }
        else
        {
            Debug.LogError("Failed to find or trigger ContentSwitcher!");
        }
    }

    /// <summary>
    /// Find and trigger ONLY the ContentSwitcher that was assigned to the clicked ClickableObject
    /// NO auto-assignment - only use specifically assigned ContentSwitcher
    /// </summary>
    private bool TriggerContentSwitcher()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== LOOKING FOR ASSIGNED CONTENT SWITCHER ===");
            Debug.Log($"Current ObjectType: {currentObjectType}");
            Debug.Log($"Clicked Object: {clickedObjectName}");
        }

        // Step 1: Find the specific ClickableObject that was clicked
        ClickableObject clickedObject = FindClickedObjectInScene();

        if (clickedObject == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError("❌ Could not find the clicked ClickableObject!");
                Debug.LogError("Cannot trigger ContentSwitcher without knowing which object was clicked.");
            }
            return false;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"✅ Found clicked object: {clickedObject.name}");
        }

        // Step 2: Check if this ClickableObject has a valid ContentSwitcher assigned
        if (!clickedObject.HasValidContentSwitcher())
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"❌ ClickableObject '{clickedObject.name}' does NOT have a valid ContentSwitcher assigned!");
                Debug.LogError($"Please assign a ContentSwitcher to this object in the Inspector.");
                Debug.LogError($"ContentSwitcher will NOT be triggered.");
            }
            return false;
        }

        // Step 3: Get the assigned ContentSwitcher from the ClickableObject
        ContentSwitcher assignedContentSwitcher = clickedObject.GetLinkedContentSwitcher();

        if (assignedContentSwitcher == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"❌ ClickableObject '{clickedObject.name}' linked ContentSwitcher is NULL!");
            }
            return false;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"✅ Found assigned ContentSwitcher: {assignedContentSwitcher.name}");
            Debug.Log($"   ContentSwitcher ChapterType: {assignedContentSwitcher.GetChapterType()}");
            Debug.Log($"   ContentSwitcher ObjectType: {assignedContentSwitcher.GetObjectType()}");
            Debug.Log($"   Required ChapterType: {currentChapterType}");
            Debug.Log($"   Required ObjectType: {currentObjectType}");
        }

        // Step 4: Validate that the assigned ContentSwitcher can handle our ObjectType
        if (assignedContentSwitcher.GetChapterType() != currentChapterType)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ Chapter type mismatch!");
                Debug.LogWarning($"   ContentSwitcher ChapterType: {assignedContentSwitcher.GetChapterType()}");
                Debug.LogWarning($"   Required ChapterType: {currentChapterType}");
                Debug.LogWarning($"   Will set ContentSwitcher to match required chapter.");
            }

            // Set the chapter to match (but warn about it)
            assignedContentSwitcher.SetChapterType(currentChapterType);
        }

        // Step 5: Configure the ContentSwitcher for the specific ObjectType
        assignedContentSwitcher.SetObjectType(currentObjectType);
        assignedContentSwitcher.SetTestingChapter(currentChapterType);

        if (enableDebugLogs)
        {
            Debug.Log($"🔧 Configured assigned ContentSwitcher:");
            Debug.Log($"   Name: {assignedContentSwitcher.name}");
            Debug.Log($"   Set ChapterType to: {currentChapterType}");
            Debug.Log($"   Set ObjectType to: {currentObjectType}");
            Debug.Log($"   Set TestingChapter to: {currentChapterType}");
        }

        // Step 6: Trigger the specifically assigned ContentSwitcher
        assignedContentSwitcher.OnButtonClicked();

        if (enableDebugLogs)
        {
            Debug.Log($"🚀 TRIGGERED ASSIGNED CONTENT SWITCHER: {assignedContentSwitcher.name}");
            Debug.Log($"   Chapter: {currentChapterType}");
            Debug.Log($"   Object: {currentObjectType}");
            Debug.Log("=== ASSIGNED-ONLY MODE - NO AUTO-ASSIGNMENT USED ===");
        }

        return true;
    }

    /// <summary>
    /// Get chapter type from object type
    /// </summary>
    private ChapterType GetChapterFromObjectType(ObjectType objectType)
    {
        switch (objectType)
        {
            case ObjectType.ChinaCoin:
            case ObjectType.ChinaJar:
                return ChapterType.China;
            case ObjectType.IndonesiaKendin:
                return ChapterType.Indonesia;
            case ObjectType.MesirWingedScared:
                return ChapterType.Mesir;
            default:
                return ChapterType.China;
        }
    }

    // Public getters for current data
    public ObjectType GetCurrentObjectType() => currentObjectType;
    public ChapterType GetCurrentChapterType() => currentChapterType;
    public bool ShouldTriggerContentSwitcher() => shouldTriggerContentSwitcher;
    public bool IsTransitionInProgress() => isTransitionInProgress;

    /// <summary>
    /// Manual trigger for testing
    /// </summary>
    [System.Obsolete("For testing only")]
    public void TestTransitionWithObjectType(ObjectType testObjectType)
    {
        Debug.Log($"=== TESTING SCENE TRANSITION ===");
        Debug.Log($"Test Object Type: {testObjectType}");
        TransitionToMainSceneWithContentSwitcher(testObjectType);
    }

    /// <summary>
    /// Reset all transition data
    /// </summary>
    public void ResetTransitionData()
    {
        currentObjectType = ObjectType.ChinaCoin;
        currentChapterType = ChapterType.China;
        shouldTriggerContentSwitcher = false;
        isTransitionInProgress = false;

        if (enableDebugLogs)
        {
            Debug.Log("SceneTransitionManager data reset");
        }
    }
}
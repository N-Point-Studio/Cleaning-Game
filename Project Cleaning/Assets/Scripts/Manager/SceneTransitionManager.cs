using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Manages data persistence across scene transitions and triggers ContentSwitcher
/// Singleton that survives scene loads to pass ObjectType data between scenes
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Configuration")]
    [SerializeField] private float sceneTransitionDelay = 0.5f;
    [SerializeField] private bool useEasyTransition = true;
    [SerializeField] private EasyTransition.TransitionSettings defaultTransitionSettings;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Re-enable to debug remaining issue

    // Data to persist across scenes
    private ObjectType currentObjectType;
    private ChapterType currentChapterType;
    private string targetSceneName = ""; // Dynamic scene name from clicked object
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

        Debug.Log($"🔧 SetObjectTypeForTransition called:");
        Debug.Log($"   Object Type Set: {objectType}");
        Debug.Log($"   Chapter Type: {currentChapterType}");
        Debug.Log($"   shouldTriggerContentSwitcher SET TO: {shouldTriggerContentSwitcher}");

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
    /// Simple and clean version - gets scene name from clicked object
    /// </summary>
    public void TransitionToMainSceneWithContentSwitcher(ObjectType objectType, string sceneName, string objectName = "", Vector3 objectPosition = default)
    {
        if (isTransitionInProgress)
        {
            Debug.LogWarning("Scene transition already in progress!");
            return;
        }

        // Validate scene name
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"❌ Scene name is empty! Cannot transition. Make sure ClickableObject has targetSceneName set.");
            return;
        }

        // Store transition data
        targetSceneName = sceneName;
        clickedObjectName = objectName;
        clickedObjectPosition = objectPosition;
        shouldUpdateClickedObject = !string.IsNullOrEmpty(objectName);

        SetObjectTypeForTransition(objectType);

        Debug.Log($"=== SCENE TRANSITION STARTED ===");
        Debug.Log($"Clicked Object: {objectName}");
        Debug.Log($"Object Type: {objectType}");
        Debug.Log($"Target Scene: {sceneName}");
        Debug.Log($"==============================");

        StartCoroutine(PerformSceneTransition());
    }

    /// <summary>
    /// Perform the actual scene transition with proper cleanup
    /// </summary>
    private IEnumerator PerformSceneTransition()
    {
        isTransitionInProgress = true;

        if (enableDebugLogs)
        {
            Debug.Log($"Starting scene transition to: {targetSceneName}");
        }

        // STEP 1: Optional delay before transition (for animation setup time)
        if (sceneTransitionDelay > 0)
        {
            yield return cachedSceneTransitionDelay;
        }

        // STEP 2: Load the target scene (BEFORE cleanup to preserve animations)
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

                // CLEANUP: Only cleanup if fallback to standard loading
                CleanupCurrentSceneForTransition();
                yield return new WaitForSeconds(0.1f); // Brief delay for cleanup
                SceneManager.LoadScene(targetSceneName);
            }
            // If Easy Transition successful, it handles the scene loading internally
        }
        else
        {
            // Standard Unity scene loading with cleanup
            CleanupCurrentSceneForTransition();
            yield return new WaitForSeconds(0.1f); // Brief delay for cleanup
            SceneManager.LoadScene(targetSceneName);
        }
    }

    /// <summary>
    /// Try to use EasyTransition for scene loading with proper animation support
    /// </summary>
    private bool TryEasyTransition()
    {
        try
        {
            // METHOD 1: Try to find EasyTransition.TransitionManager in scene
            var transitionManager = FindObjectOfType<EasyTransition.TransitionManager>();
            if (transitionManager != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("=== USING EASY TRANSITION ANIMATION ===");
                    Debug.Log($"Found TransitionManager: {transitionManager.name}");
                }

                // Use EasyTransition with proper animation
                // Try different method signatures
                try
                {
                    if (enableDebugLogs)
                    {
                       
                    }

                  
                    // Try to find ANY TransitionSettings in the project
                    EasyTransition.TransitionSettings[] allTransitionSettings = Resources.FindObjectsOfTypeAll<EasyTransition.TransitionSettings>();
                    for (int i = 0; i < allTransitionSettings.Length; i++)
                    {
                    }

                    // Method A: Check if defaultTransitionSettings is properly assigned
                    if (defaultTransitionSettings != null)
                    {

                        // DEEP INSPECT: Check ALL fields in TransitionSettings to see what's NULL
                        var settingsType = defaultTransitionSettings.GetType();
                        var allFields = settingsType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        foreach (var field in allFields)
                        {
                            try
                            {
                                var value = field.GetValue(defaultTransitionSettings);
                                string status = value == null ? "❌ NULL" : "✅ HAS VALUE";
                                if (value != null && value.ToString() != "")
                                {
                                }
                            }
                            catch (System.Exception ex)
                            {
                            }
                        }

                        // FIXED: Use transitionTime from TransitionSettings instead of hardcoded duration
                        try
                        {
                            // Get the transition time from TransitionSettings object
                            var transitionTimeField = defaultTransitionSettings.GetType().GetField("transitionTime");
                            float transitionTime = 1f; // default fallback
                            if (transitionTimeField != null)
                            {
                                transitionTime = (float)transitionTimeField.GetValue(defaultTransitionSettings);
                                if (enableDebugLogs)
                                {
                                    Debug.Log($"Using TransitionSettings transitionTime: {transitionTime}s");
                                }
                            }

                            // CRITICAL FIX: Reset runningTransition state before calling transition
                            var runningTransitionField = transitionManager.GetType().GetField("runningTransition",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (runningTransitionField != null)
                            {
                                bool currentRunningState = (bool)runningTransitionField.GetValue(transitionManager);

                                if (currentRunningState)
                                {
                                    runningTransitionField.SetValue(transitionManager, false);
                                }
                            }

                            // Call EasyTransition with proper duration from settings
                            transitionManager.Transition(targetSceneName, defaultTransitionSettings, transitionTime);

                            if (enableDebugLogs)
                            {
                                Debug.Log("✅ EasyTransition called successfully with proper settings!");
                                Debug.Log($"   Scene: {targetSceneName}");
                                Debug.Log($"   Transition: {defaultTransitionSettings.name}");
                                Debug.Log($"   Duration: {transitionTime}s");
                            }
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"❌ EasyTransition call failed: {ex.Message}");
                            Debug.LogError($"   Exception Type: {ex.GetType().Name}");
                            Debug.LogError($"   Will try fallback methods...");
                            throw; // Continue to reflection methods
                        }
                    }
                    else
                    {
                        Debug.LogWarning("❌ Default TransitionSettings is null!");
                        Debug.LogWarning("Trying to find alternative TransitionSettings...");

                        // EMERGENCY FALLBACK 1: Use any TransitionSettings found in project
                        if (allTransitionSettings.Length > 0)
                        {
                            EasyTransition.TransitionSettings emergencySettings = allTransitionSettings[0];
                            Debug.LogError($"🚨 EMERGENCY: Using first available TransitionSettings: {emergencySettings.name}");

                            // Try using the emergency settings
                            var emergencyTransitionTimeField = emergencySettings.GetType().GetField("transitionTime");
                            float emergencyTransitionTime = 1f;
                            if (emergencyTransitionTimeField != null)
                            {
                                emergencyTransitionTime = (float)emergencyTransitionTimeField.GetValue(emergencySettings);
                            }

                            transitionManager.Transition(targetSceneName, emergencySettings, emergencyTransitionTime);
                            Debug.LogError("🚨 Emergency transition call successful!");
                            return true;
                        }

                        // FALLBACK 2: Try to find TransitionSettings from ClickableObjects in scene
                        EasyTransition.TransitionSettings fallbackSettings = FindTransitionSettingsInScene();
                        if (fallbackSettings != null)
                        {
                            Debug.Log($"✅ Found fallback TransitionSettings: {fallbackSettings.name}");

                            // FIXED: Use transitionTime from fallback settings too (not 0f)
                            var fallbackTransitionTimeField = fallbackSettings.GetType().GetField("transitionTime");
                            float fallbackTransitionTime = 1f; // default fallback
                            if (fallbackTransitionTimeField != null)
                            {
                                fallbackTransitionTime = (float)fallbackTransitionTimeField.GetValue(fallbackSettings);
                                Debug.Log($"Using fallback TransitionSettings transitionTime: {fallbackTransitionTime}s");
                            }

                            transitionManager.Transition(targetSceneName, fallbackSettings, fallbackTransitionTime);
                            Debug.Log("✅ Fallback transition call successful!");
                            return true;
                        }

                        // Last resort: Try to create a default fade transition
                        Debug.LogError("No TransitionSettings found anywhere! Will try reflection...");
                        throw new System.Exception("No TransitionSettings available");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ EasyTransition direct call FAILED: {ex.Message}");
                    Debug.LogError($"Exception type: {ex.GetType()}");
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                    Debug.LogWarning("Trying reflection fallback methods...");

                    // Method B: Try different reflection approaches
                    if (TryReflectionTransition(transitionManager))
                    {
                        Debug.Log("✅ Reflection fallback succeeded!");
                        return true;
                    }
                    else
                    {
                        Debug.LogError("❌ All fallback methods failed!");
                    }
                }
            }

            // METHOD 2: Try to access EasyTransition.TransitionManager.Instance()
            var easyTransitionType = System.Type.GetType("EasyTransition.TransitionManager");
            if (easyTransitionType != null)
            {
                // Use reflection to call Instance().Transition()
                var instanceMethod = easyTransitionType.GetMethod("Instance");
                if (instanceMethod != null)
                {
                    var instance = instanceMethod.Invoke(null, null);
                    if (instance != null)
                    {
                        // Try different method signatures
                        System.Reflection.MethodInfo transitionMethod = null;

                        // Try Method A: Transition(string, TransitionSettings, float)
                        transitionMethod = easyTransitionType.GetMethod("Transition",
                            new[] { typeof(string), typeof(UnityEngine.Object), typeof(float) });

                        // Try Method B: Transition(string, float)
                        if (transitionMethod == null)
                        {
                            transitionMethod = easyTransitionType.GetMethod("Transition",
                                new[] { typeof(string), typeof(float) });
                        }

                        // Try Method C: Transition(string)
                        if (transitionMethod == null)
                        {
                            transitionMethod = easyTransitionType.GetMethod("Transition",
                                new[] { typeof(string) });
                        }

                        if (transitionMethod != null)
                        {
                            if (enableDebugLogs)
                            {
                                Debug.Log("=== USING EASY TRANSITION VIA REFLECTION ===");
                                Debug.Log($"Using method: {transitionMethod.Name} with {transitionMethod.GetParameters().Length} parameters");
                            }

                            // Call with appropriate parameters based on method signature
                            var parameters = transitionMethod.GetParameters();
                            if (parameters.Length == 3)
                            {
                                // Use 1f instead of 0f for reflection calls too
                                transitionMethod.Invoke(instance, new object[] { targetSceneName, null, 1f });
                            }
                            else if (parameters.Length == 2)
                            {
                                // Use 1f instead of 0f for reflection calls too
                                transitionMethod.Invoke(instance, new object[] { targetSceneName, 1f });
                            }
                            else
                            {
                                transitionMethod.Invoke(instance, new object[] { targetSceneName });
                            }
                            return true;
                        }
                    }
                }
            }

            if (enableDebugLogs)
            {
                Debug.LogWarning("EasyTransition not found - will use standard scene loading");
            }
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"EasyTransition attempt failed: {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// Try to find TransitionSettings from ClickableObjects or other sources in scene
    /// </summary>
    private EasyTransition.TransitionSettings FindTransitionSettingsInScene()
    {
        Debug.Log("Searching for TransitionSettings in scene...");

        // Method 1: Check all ClickableObjects for TransitionSettings
        ClickableObject[] clickableObjects = FindObjectsOfType<ClickableObject>();
        foreach (var clickable in clickableObjects)
        {
            var transitionSettings = clickable.GetTransitionSettings();
            if (transitionSettings != null)
            {
                Debug.Log($"Found TransitionSettings in ClickableObject: {clickable.name}");
                return transitionSettings;
            }
        }

        // Method 2: Try to find any TransitionSettings assets in scene
        EasyTransition.TransitionSettings[] allSettings = FindObjectsOfType<EasyTransition.TransitionSettings>();
        if (allSettings.Length > 0)
        {
            Debug.Log($"Found TransitionSettings asset in scene: {allSettings[0].name}");
            return allSettings[0];
        }

        Debug.LogWarning("No TransitionSettings found in scene");
        return null;
    }

    /// <summary>
    /// Try reflection-based transition methods as fallback
    /// </summary>
    private bool TryReflectionTransition(EasyTransition.TransitionManager transitionManager)
    {
        try
        {
            // Method 1: Try simple string-only transition
            var stringMethod = transitionManager.GetType().GetMethod("Transition", new[] { typeof(string) });
            if (stringMethod != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("Using reflection: Transition(string)");
                }
                stringMethod.Invoke(transitionManager, new object[] { targetSceneName });
                return true;
            }

            // Method 2: Try LoadLevel method if available
            var loadLevelMethod = transitionManager.GetType().GetMethod("LoadLevel", new[] { typeof(string) });
            if (loadLevelMethod != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("Using reflection: LoadLevel(string)");
                }
                loadLevelMethod.Invoke(transitionManager, new object[] { targetSceneName });
                return true;
            }

            // Method 3: Try any public methods that take string parameter
            var allMethods = transitionManager.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var method in allMethods)
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string) &&
                    (method.Name.Contains("Transition") || method.Name.Contains("Load")))
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Using reflection: {method.Name}(string)");
                    }
                    method.Invoke(transitionManager, new object[] { targetSceneName });
                    return true;
                }
            }
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"Reflection transition failed: {ex.Message}");
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

            // SAVE PROGRESS: Mark object as completed
            if (SaveSystem.Instance != null)
            {
                SaveSystem.Instance.MarkObjectCompleted(
                    targetObject.name,
                    currentObjectType,
                    currentChapterType,
                    targetObject.transform.position
                );

                if (enableDebugLogs)
                {
                    Debug.Log($"Saved completion progress for: {targetObject.name}");
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
        if (enableDebugLogs)
        {
            Debug.Log("=== FINDING CLICKED OBJECT IN SCENE ===");
            Debug.Log($"Looking for: {clickedObjectName}");
            Debug.Log($"ObjectType: {currentObjectType}");
            Debug.Log($"Position: {clickedObjectPosition}");
        }

        // Method 1: Find by exact name match
        if (!string.IsNullOrEmpty(clickedObjectName))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"=== METHOD 1: Searching by exact name '{clickedObjectName}' ===");
            }

            GameObject foundObj = GameObject.Find(clickedObjectName);
            if (foundObj != null)
            {
                ClickableObject clickable = foundObj.GetComponent<ClickableObject>();
                if (clickable != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"✅ METHOD 1 SUCCESS: Found exact match '{clickable.name}'");
                    }
                    return clickable;
                }
                else
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"⚠️ Found object '{foundObj.name}' but no ClickableObject component");
                    }
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"⚠️ METHOD 1 FAILED: No object named '{clickedObjectName}' found");
                }
            }
        }

        // Method 2: Find by ObjectType match
        if (enableDebugLogs)
        {
            Debug.Log($"=== METHOD 2: Searching by ObjectType '{currentObjectType}' ===");
        }

        ClickableObject[] allClickables = FindObjectsOfType<ClickableObject>();

        if (enableDebugLogs)
        {
            Debug.Log($"Found {allClickables.Length} ClickableObjects in scene:");
            for (int i = 0; i < allClickables.Length; i++)
            {
                ClickableObject obj = allClickables[i];
                Debug.Log($"  {i+1}. {obj.name} - ObjectType: {obj.GetObjectType()} - HasValidContentSwitcher: {obj.HasValidContentSwitcher()}");
            }
        }

        foreach (ClickableObject clickable in allClickables)
        {
            if (clickable.GetObjectType() == currentObjectType)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"✅ METHOD 2 SUCCESS: Found ObjectType match '{clickable.name}'");
                    Debug.Log($"   ObjectType: {clickable.GetObjectType()}");
                    Debug.Log($"   HasValidContentSwitcher: {clickable.HasValidContentSwitcher()}");
                }
                return clickable;
            }
        }

        if (enableDebugLogs)
        {
            Debug.LogWarning($"⚠️ METHOD 2 FAILED: No object with ObjectType '{currentObjectType}' found");
        }

        // Method 3: Find by position (if close enough)
        if (clickedObjectPosition != Vector3.zero)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"=== METHOD 3: Searching by position {clickedObjectPosition} ===");
            }

            foreach (ClickableObject clickable in allClickables)
            {
                float distance = Vector3.Distance(clickable.transform.position, clickedObjectPosition);
                if (enableDebugLogs)
                {
                    Debug.Log($"  {clickable.name}: distance = {distance:F2}");
                }

                if (distance < 2f) // Within 2 units
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"✅ METHOD 3 SUCCESS: Found position match '{clickable.name}' (distance: {distance:F2})");
                    }
                    return clickable;
                }
            }

            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ METHOD 3 FAILED: No object within 2 units of position {clickedObjectPosition}");
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("⚠️ METHOD 3 SKIPPED: clickedObjectPosition is zero");
            }
        }

        // Method 4: Ultimate fallback - find ANY object with valid ContentSwitcher for same Chapter
        if (enableDebugLogs)
        {
            Debug.Log($"=== METHOD 4: Ultimate fallback - searching by ChapterType '{currentChapterType}' ===");
        }

        foreach (ClickableObject clickable in allClickables)
        {
            if (clickable.HasValidContentSwitcher())
            {
                var linkedCS = clickable.GetLinkedContentSwitcher();
                if (linkedCS != null && linkedCS.GetChapterType() == currentChapterType)
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"⚠️ METHOD 4 SUCCESS: Using fallback object '{clickable.name}' with matching chapter");
                        Debug.Log($"   Object ObjectType: {clickable.GetObjectType()}");
                        Debug.Log($"   Target ObjectType: {currentObjectType}");
                        Debug.Log($"   ContentSwitcher Chapter: {linkedCS.GetChapterType()}");
                    }

                    // Auto-configure the ContentSwitcher to match our requirements
                    linkedCS.SetObjectType(currentObjectType);
                    linkedCS.SetTestingChapter(currentChapterType);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"🔧 Auto-configured ContentSwitcher to match ObjectType: {currentObjectType}");
                    }

                    return clickable;
                }
            }
        }

        // Method 5: Last resort - find ANY object that can be auto-configured
        if (enableDebugLogs)
        {
            Debug.Log("=== METHOD 5: Last resort - find any ClickableObject to auto-configure ===");
        }

        foreach (ClickableObject clickable in allClickables)
        {
            // Try to assign a ContentSwitcher from scene if object doesn't have one
            if (!clickable.HasValidContentSwitcher())
            {
                ContentSwitcher[] availableCS = FindObjectsOfType<ContentSwitcher>();
                foreach (ContentSwitcher cs in availableCS)
                {
                    if (cs.GetChapterType() == currentChapterType)
                    {
                        if (enableDebugLogs)
                        {
                            Debug.LogWarning($"⚠️ METHOD 5: Auto-assigning ContentSwitcher '{cs.name}' to '{clickable.name}'");
                        }

                        // Auto-assign ContentSwitcher
                        clickable.SetContentSwitcherObject(cs.gameObject);

                        // Configure it
                        cs.SetObjectType(currentObjectType);
                        cs.SetTestingChapter(currentChapterType);

                        if (enableDebugLogs)
                        {
                            Debug.Log($"🔧 Auto-configured new ContentSwitcher assignment");
                        }

                        return clickable;
                    }
                }
            }
        }

        if (enableDebugLogs)
        {
            Debug.LogError("❌ ALL METHODS FAILED: Could not find or configure any suitable object");
            Debug.LogError("🔧 POSSIBLE SOLUTIONS:");
            Debug.LogError("   1. Ensure 'JarUndoneImage' object has a ContentSwitcher assigned in Inspector");
            Debug.LogError("   2. Ensure 'JarUndoneImage' ObjectType is set to 'China Jar'");
            Debug.LogError("   3. Ensure ContentSwitcher in scene is configured for 'China' chapter");
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

        // CRITICAL FIX: Re-enable TouchManager with proper timing
        // Wait for UI system to be fully initialized before enabling touch
        StartCoroutine(ReenableTouchAfterUIReady());

        // RESET: Clear clicked object data when returning to menu scene
        if (scene.name.Contains("New Start Game Sandy") || scene.name.Contains("Menu") || scene.name.Contains("Main"))
        {
            Debug.Log("🔄 RETURNING TO MENU - Clearing clicked object data but keeping ContentSwitcher trigger");

            // Clear clicked object data (prevent searching for old objects)
            clickedObjectName = "";
            clickedObjectPosition = Vector3.zero;
            shouldUpdateClickedObject = false;

            // FIXED: Notify CameraAnimationController that we're returning from gameplay
            // This prevents startup UI (UITapToPlay, NPoint) from showing again
            if (CameraAnimationController.Instance != null)
            {
                Debug.Log("🎬 BEFORE NotifyReturningFromGameplay - about to notify CameraAnimationController");
                CameraAnimationController.Instance.NotifyReturningFromGameplay();
                Debug.Log("🎬 AFTER NotifyReturningFromGameplay - notification complete");
            }
            else
            {
                Debug.LogWarning("⚠️ CameraAnimationController.Instance is NULL! Cannot notify!");
            }

            // Keep shouldTriggerContentSwitcher = true so menu ContentSwitcher still works
            // Don't call ResetTransitionData() here as it would disable ContentSwitcher in menu
        }

        // Check if we need to trigger ContentSwitcher
        Debug.Log($"=== SCENE LOADED DEBUG ===");
        Debug.Log($"Scene name: {scene.name}");
        Debug.Log($"Target scene name: {targetSceneName}");
        Debug.Log($"shouldTriggerContentSwitcher: {shouldTriggerContentSwitcher}");
        Debug.Log($"currentObjectType: {currentObjectType}");

        // FIXED: Always trigger ContentSwitcher if shouldTriggerContentSwitcher is true
        // This allows menu ContentSwitcher to work after returning from gameplay
        if (shouldTriggerContentSwitcher)
        {
            Debug.Log("✅ Starting ContentSwitcher trigger...");
            StartCoroutine(TriggerContentSwitcherAfterDelay());
        }
        else
        {
            Debug.LogWarning("❌ ContentSwitcher NOT triggered:");
            Debug.LogWarning($"   shouldTriggerContentSwitcher: {shouldTriggerContentSwitcher}");
            Debug.LogWarning($"   Scene: {scene.name}");
            Debug.LogWarning($"   Target Scene: {targetSceneName}");
        }

        // SIMPLIFIED: Skip complex clicked object update system for now
        // The ContentSwitcher trigger above should handle the visual changes
        shouldUpdateClickedObject = false;

        // LOAD SAVED PROGRESS: Apply completion status to objects that were previously completed
        if (scene.name == targetSceneName)
        {
            StartCoroutine(LoadAndApplySavedProgressAfterDelay());
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
            // FOR MENU SCENES: Keep shouldTriggerContentSwitcher true for multiple object updates
            // FOR GAMEPLAY SCENES: Can reset it since we only trigger once
            bool isMenuScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("New Start Game Sandy") ||
                              UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Menu") ||
                              UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Main");

            if (!isMenuScene)
            {
                // Reset only for gameplay scenes
                shouldTriggerContentSwitcher = false;
                Debug.Log("🔄 Reset shouldTriggerContentSwitcher for gameplay scene");
            }
            else
            {
                Debug.Log("🔧 Keeping shouldTriggerContentSwitcher TRUE for menu scene");
            }

            OnContentSwitcherTriggered?.Invoke();

            Debug.Log("✅ ContentSwitcher triggered successfully!");

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
    /// Smart ContentSwitcher trigger - find the RIGHT ContentSwitcher based on clicked object
    /// </summary>
    private bool TriggerContentSwitcher()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== SMART CONTENT SWITCHER TRIGGER ===");
            Debug.Log($"Current ObjectType: {currentObjectType}");
            Debug.Log($"Current ChapterType: {currentChapterType}");
            Debug.Log($"Clicked Object Name: {clickedObjectName}");
        }

        // Find ALL ClickableObjects in scene
        ClickableObject[] clickableObjects = FindObjectsOfType<ClickableObject>();
        ClickableObject targetClickableObject = null;

        // METHOD 1: Find by exact ObjectType match
        foreach (ClickableObject clickable in clickableObjects)
        {
            if (clickable.GetObjectType() == currentObjectType)
            {
                targetClickableObject = clickable;
                if (enableDebugLogs)
                {
                    Debug.Log($"✅ Found matching ClickableObject: {clickable.name} (ObjectType: {clickable.GetObjectType()})");
                }
                break;
            }
        }

        // METHOD 2: If no exact match, find by name similarity
        if (targetClickableObject == null && !string.IsNullOrEmpty(clickedObjectName))
        {
            foreach (ClickableObject clickable in clickableObjects)
            {
                if (clickable.name.ToLower().Contains(clickedObjectName.ToLower()) ||
                    clickedObjectName.ToLower().Contains(clickable.name.ToLower()))
                {
                    targetClickableObject = clickable;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"✅ Found similar name ClickableObject: {clickable.name} (for clicked: {clickedObjectName})");
                    }
                    break;
                }
            }
        }

        if (targetClickableObject == null)
        {
            Debug.LogWarning($"⚠️ Could not find ClickableObject for ObjectType {currentObjectType}, using fallback ContentSwitcher trigger");
            return TriggerFallbackContentSwitcher();
        }

        // Check if target object has ContentSwitcher assigned
        if (targetClickableObject.HasValidContentSwitcher())
        {
            ContentSwitcher assignedContentSwitcher = targetClickableObject.GetLinkedContentSwitcher();

            if (enableDebugLogs)
            {
                Debug.Log($"🎯 Using ASSIGNED ContentSwitcher from {targetClickableObject.name}");
                Debug.Log($"   ContentSwitcher: {assignedContentSwitcher.name}");
            }

            // Configure and trigger the assigned ContentSwitcher
            assignedContentSwitcher.SetObjectType(currentObjectType);
            assignedContentSwitcher.SetTestingChapter(currentChapterType);
            assignedContentSwitcher.OnButtonClicked();

            if (enableDebugLogs)
            {
                Debug.Log($"🚀 TRIGGERED ASSIGNED CONTENT SWITCHER: {assignedContentSwitcher.name}");
                Debug.Log($"   For Object: {targetClickableObject.name}");
                Debug.Log($"   ObjectType: {currentObjectType}");
            }
            return true;
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ Target object {targetClickableObject.name} has no ContentSwitcher assigned, using fallback");
            }
            return TriggerFallbackContentSwitcher();
        }
    }

    /// <summary>
    /// Fallback: Find ANY ContentSwitcher and configure it
    /// </summary>
    private bool TriggerFallbackContentSwitcher()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== FALLBACK CONTENT SWITCHER TRIGGER ===");
        }

        // Find ANY ContentSwitcher in the scene
        ContentSwitcher[] contentSwitchers = FindObjectsOfType<ContentSwitcher>();

        if (contentSwitchers.Length == 0)
        {
            Debug.LogError("❌ No ContentSwitcher found in scene!");
            return false;
        }

        // Use the first ContentSwitcher found
        ContentSwitcher targetSwitcher = contentSwitchers[0];

        if (enableDebugLogs)
        {
            Debug.Log($"✅ Using fallback ContentSwitcher: {targetSwitcher.name}");
        }

        // Configure the ContentSwitcher to match our requirements
        targetSwitcher.SetObjectType(currentObjectType);
        targetSwitcher.SetTestingChapter(currentChapterType);
        targetSwitcher.OnButtonClicked();

        if (enableDebugLogs)
        {
            Debug.Log($"🚀 TRIGGERED FALLBACK CONTENT SWITCHER: {targetSwitcher.name}");
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
        TransitionToMainSceneWithContentSwitcher(testObjectType, "New Start Game Sandy", "TestObject");
    }

    /// <summary>
    /// Load saved progress and apply completion status to objects in scene
    /// </summary>
    private IEnumerator LoadAndApplySavedProgressAfterDelay()
    {
        // Wait for scene to fully initialize
        yield return new WaitForSeconds(0.5f);

        if (SaveSystem.Instance == null)
        {
            if (enableDebugLogs)
            {
                Debug.Log("SaveSystem not found, skipping saved progress loading");
            }
            yield break;
        }

        SaveData saveData = SaveSystem.Instance.GetSaveData();
        if (saveData.completedObjects.Count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("No saved progress found");
            }
            yield break;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"=== LOADING SAVED PROGRESS ===");
            Debug.Log($"Found {saveData.completedObjects.Count} completed objects");
        }

        // Find all ClickableObjects in scene
        ClickableObject[] allClickableObjects = FindObjectsOfType<ClickableObject>();

        // Apply completion status to matching objects
        foreach (var completedObj in saveData.completedObjects)
        {
            foreach (var clickableObj in allClickableObjects)
            {
                // Match by name and ObjectType
                if (clickableObj.name == completedObj.objectName &&
                    clickableObj.GetObjectType() == completedObj.objectType)
                {
                    // Apply completion changes
                    clickableObj.ApplyContentSwitcherChanges();

                    if (enableDebugLogs)
                    {
                        Debug.Log($"Applied saved completion to: {clickableObj.name}");
                    }
                    break;
                }
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log("=== SAVED PROGRESS LOADED ===");
        }
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

    /// <summary>
    /// Clean up current scene before transition to free memory and prevent conflicts
    /// GENTLE CLEANUP - Preserves transition animations
    /// </summary>
    private void CleanupCurrentSceneForTransition()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== GENTLE CLEANUP FOR TRANSITION ===");
        }

        try
        {
            // 1. Disable TouchManager to prevent input conflicts during transition
            if (TouchManager.Instance != null)
            {
                TouchManager.Instance.DisableAllTouch(true);
                if (enableDebugLogs)
                {
                    Debug.Log("TouchManager disabled for transition");
                }
            }

            // 2. GENTLE cleanup - Don't kill all animations immediately
            // Only stop non-essential systems
            CleanupSceneManagersGently();

            // 3. Stop only non-transition related coroutines
            CleanupNonTransitionCoroutines();

            if (enableDebugLogs)
            {
                Debug.Log("Gentle scene cleanup for transition completed successfully");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during scene cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Gentle cleanup of scene managers - preserves transition animations
    /// </summary>
    private void CleanupSceneManagersGently()
    {
        // Clean up GamePlayManager (but not its transition coroutines)
        if (GamePlayManager.Instance != null)
        {
            // Don't stop ALL coroutines - GamePlayManager might still need transition coroutines
            // Instead, just mark it as scene unloading
            var gamePlayManager = GamePlayManager.Instance;
            var isSceneUnloadingField = gamePlayManager.GetType().GetField("isSceneUnloading",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isSceneUnloadingField != null)
            {
                isSceneUnloadingField.SetValue(gamePlayManager, true);
            }
        }

        // Clean up UIManager (but preserve finish button functionality)
        if (UIManager.Instance != null)
        {
            // Don't stop ALL coroutines during transition
            var uiManager = UIManager.Instance;
            var isSceneUnloadingField = uiManager.GetType().GetField("isSceneUnloading",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (isSceneUnloadingField != null)
            {
                isSceneUnloadingField.SetValue(uiManager, true);
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log("Scene managers gently prepared for transition");
        }
    }

    /// <summary>
    /// Stop only non-transition related coroutines
    /// </summary>
    private void CleanupNonTransitionCoroutines()
    {
        // Find objects that are NOT related to transitions
        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            // Skip transition-related components
            if (mb == this ||
                mb is EasyTransition.TransitionManager ||
                mb.name.Contains("Transition") ||
                mb.name.Contains("Animation"))
            {
                continue; // Don't stop transition-related coroutines
            }

            // Stop coroutines for non-essential systems
            if (mb is ClickableObject ||
                mb.name.Contains("UI") && !mb.name.Contains("Finish"))
            {
                mb.StopAllCoroutines();
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log("Non-transition coroutines cleaned up");
        }
    }

    /// <summary>
    /// Clean up specific scene managers
    /// </summary>
    private void CleanupSceneManagers()
    {
        // Clean up GamePlayManager if it exists
        if (GamePlayManager.Instance != null)
        {
            GamePlayManager.Instance.StopAllCoroutines();
        }

        // Clean up UIManager if it exists
        if (UIManager.Instance != null)
        {
            UIManager.Instance.StopAllCoroutines();
        }

        // Clean up camera controllers
        CameraAnimationController[] cameraControllers = FindObjectsOfType<CameraAnimationController>();
        foreach (CameraAnimationController controller in cameraControllers)
        {
            controller.StopAllCoroutines();
        }

        if (enableDebugLogs)
        {
            Debug.Log("Scene managers cleaned up");
        }
    }

    /// <summary>
    /// Clean up UI elements to prevent memory leaks
    /// </summary>
    private void CleanupUIElements()
    {
        // Stop all UI animations
        DOTween.Kill("UI");

        // Find and clean up UI canvases
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            // Don't destroy DontDestroyOnLoad canvases
            if (canvas.gameObject.scene.name != "DontDestroyOnLoad")
            {
                // Stop animations on UI elements
                Animator[] animators = canvas.GetComponentsInChildren<Animator>();
                foreach (Animator animator in animators)
                {
                    if (animator != null)
                    {
                        animator.enabled = false;
                    }
                }
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log("UI elements cleaned up");
        }
    }

    /// <summary>
    /// Re-enable TouchManager after UI system is fully ready
    /// This prevents the stuck UI issue by ensuring proper initialization order
    /// </summary>
    private IEnumerator ReenableTouchAfterUIReady()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== WAITING FOR UI SYSTEM TO BE READY ===");
        }

        // Wait for at least 2 frames to ensure Unity's UI system is initialized
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Wait for EventSystem to be active
        int maxWaitFrames = 30; // Maximum 30 frames (about 0.5 seconds at 60fps)
        int frameCount = 0;

        while (frameCount < maxWaitFrames)
        {
            // Check if EventSystem exists and is active
            if (EventSystem.current != null &&
                EventSystem.current.gameObject.activeInHierarchy)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"✅ EventSystem ready after {frameCount} frames");
                }
                break;
            }

            yield return null;
            frameCount++;
        }

        if (frameCount >= maxWaitFrames)
        {
            Debug.LogWarning("⚠️ EventSystem not found after maximum wait time - proceeding anyway");
        }

        // Additional small delay to ensure Canvas and GraphicRaycaster components are ready
        yield return new WaitForSeconds(0.1f);

        // Now it's safe to re-enable TouchManager
        if (TouchManager.Instance != null)
        {
            // Ensure TouchManager is properly reset before enabling
            TouchManager.Instance.DisableAllTouch(true);
            yield return null; // Wait one frame

            TouchManager.Instance.DisableAllTouch(false);

            if (enableDebugLogs)
            {
                Debug.Log("✅ TouchManager re-enabled after UI system ready");
                Debug.Log("✅ UI STUCK ISSUE SHOULD BE FIXED");
            }
        }
        else
        {
            Debug.LogWarning("TouchManager.Instance is null - cannot re-enable touch input");
        }

        // Verify UI components are responsive and fix if necessary
        yield return StartCoroutine(VerifyAndFixUIResponsiveness());
    }

    /// <summary>
    /// Verify that UI components are responsive and fix issues if found
    /// </summary>
    private IEnumerator VerifyAndFixUIResponsiveness()
    {
        yield return new WaitForSeconds(0.2f);

        if (enableDebugLogs)
        {
            Debug.Log("=== COMPREHENSIVE UI DIAGNOSIS & FIX ===");
        }

        bool foundIssue = false;

        // 1. Check EventSystem
        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            Debug.LogError("❌ CRITICAL: No EventSystem found! Creating one...");
            CreateEventSystem();
            foundIssue = true;
            yield return null; // Wait one frame for EventSystem to initialize
            eventSystem = EventSystem.current;
        }

        if (eventSystem != null)
        {
            Debug.Log($"✅ EventSystem active: {eventSystem.name}");

            // Check if EventSystem is enabled
            if (!eventSystem.enabled)
            {
                Debug.LogWarning("❌ EventSystem is disabled! Enabling...");
                eventSystem.enabled = true;
                foundIssue = true;
            }

            // Check current selected object
            if (eventSystem.currentSelectedGameObject != null)
            {
                Debug.Log($"⚠️ EventSystem has selected object: {eventSystem.currentSelectedGameObject.name}");
                Debug.Log("Clearing selected object to prevent UI blocking...");
                eventSystem.SetSelectedGameObject(null);
                foundIssue = true;
            }
        }

        // 2. Check Canvas and GraphicRaycaster
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        int activeCanvasCount = 0;
        int raycasterCount = 0;
        int enabledRaycasterCount = 0;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.activeInHierarchy)
            {
                activeCanvasCount++;

                // Check GraphicRaycaster
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null)
                {
                    raycasterCount++;

                    if (!raycaster.enabled)
                    {
                        Debug.LogWarning($"❌ GraphicRaycaster disabled on {canvas.name}! Enabling...");
                        raycaster.enabled = true;
                        foundIssue = true;
                    }
                    else
                    {
                        enabledRaycasterCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"❌ No GraphicRaycaster on Canvas: {canvas.name}");
                    // Add GraphicRaycaster if missing on active Canvas
                    if (canvas.isRootCanvas)
                    {
                        raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                        Debug.Log($"✅ Added GraphicRaycaster to {canvas.name}");
                        raycasterCount++;
                        enabledRaycasterCount++;
                        foundIssue = true;
                    }
                }

                // Check Canvas properties
                if (canvas.enabled == false)
                {
                    Debug.LogWarning($"❌ Canvas disabled: {canvas.name}");
                    foundIssue = true;
                }
            }
        }

        Debug.Log($"Canvas Report: {activeCanvasCount} active, {raycasterCount} total raycasters, {enabledRaycasterCount} enabled");

        // 3. Test UI Raycast functionality
        yield return StartCoroutine(TestUIRaycast());

        // 4. If issues found, force refresh UI system
        if (foundIssue)
        {
            Debug.LogWarning("🔧 UI issues detected - forcing system refresh...");
            yield return StartCoroutine(ForceRefreshUISystem());
        }

        if (enableDebugLogs)
        {
            Debug.Log($"=== UI DIAGNOSIS COMPLETE - Issues Found: {foundIssue} ===");

            // Final status
            bool uiSystemHealthy = (EventSystem.current != null &&
                                  EventSystem.current.enabled &&
                                  enabledRaycasterCount > 0);

            if (uiSystemHealthy)
            {
                Debug.Log("🎉 UI SYSTEM IS NOW HEALTHY AND RESPONSIVE!");
            }
            else
            {
                Debug.LogError("❌ UI SYSTEM STILL HAS ISSUES - Manual intervention needed");
            }
        }
    }

    /// <summary>
    /// Create EventSystem if missing
    /// </summary>
    private void CreateEventSystem()
    {
        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();
        Debug.Log("✅ Created new EventSystem");
    }

    /// <summary>
    /// Test UI raycast to see if it's working
    /// </summary>
    private IEnumerator TestUIRaycast()
    {
        yield return null;

        if (enableDebugLogs)
        {
            Debug.Log("=== TESTING UI RAYCAST ===");
        }

        // Get screen center position for testing
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Create PointerEventData
        var eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            PointerEventData pointerData = new PointerEventData(eventSystem)
            {
                position = screenCenter
            };

            // Perform raycast
            List<RaycastResult> results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            Debug.Log($"UI Raycast Test at screen center ({screenCenter.x}, {screenCenter.y}):");
            Debug.Log($"Found {results.Count} UI elements");

            if (results.Count > 0)
            {
                foreach (var result in results)
                {
                    Debug.Log($"  - {result.gameObject.name} (Canvas: {result.module?.transform.name})");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No UI elements found in raycast - this could indicate the problem");
            }
        }
    }

    /// <summary>
    /// Force refresh the entire UI system
    /// </summary>
    private IEnumerator ForceRefreshUISystem()
    {
        Debug.Log("🔧 FORCING UI SYSTEM REFRESH...");

        // 1. Disable all Canvas briefly
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.enabled)
            {
                canvas.enabled = false;
            }
        }

        yield return null; // Wait one frame

        // 2. Re-enable all Canvas
        foreach (Canvas canvas in allCanvases)
        {
            canvas.enabled = true;
        }

        // 3. Reset EventSystem
        var eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.enabled = false;
            yield return null;
            eventSystem.enabled = true;
            eventSystem.SetSelectedGameObject(null); // Clear any selected object
        }

        // 4. Force TouchManager reset
        if (TouchManager.Instance != null)
        {
            TouchManager.Instance.DisableAllTouch(true);
            yield return null;
            TouchManager.Instance.DisableAllTouch(false);
        }

        Debug.Log("✅ UI System force refresh completed");
    }

    /// <summary>
    /// Reset all saved progress (for testing)
    /// </summary>
    [ContextMenu("Reset All Saved Progress")]
    public void ResetAllSavedProgress()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ResetAllProgress();
            Debug.Log("All saved progress has been reset!");

            // Reload scene to refresh object states
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogWarning("SaveSystem not found!");
        }
    }
}
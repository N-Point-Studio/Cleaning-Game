using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// Removed DG.Tweening - minimal usage replaced with manual cleanup

/// <summary>
/// Manages data persistence across scene transitions and triggers ContentSwitcher
/// Singleton that survives scene loads to pass ObjectType data between scenes
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Scene Configuration")]
    [SerializeField] private float sceneTransitionDelay = 0.5f;
    [SerializeField] private bool useEasyTransition = false; // ✅ DISABLED: Use TransitionScreen instead
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Re-enable to debug remaining issue
    [Header("Fallback Transition")]
    [Tooltip("Assign a default TransitionSettings asset here to be used when no other settings are found.")]
    [SerializeField] private EasyTransition.TransitionSettings fallbackTransitionSettings;

    // Data to persist across scenes
    private ObjectType currentObjectType;
    private ChapterType currentChapterType;
    private string targetSceneName = ""; // Dynamic scene name from clicked object
    private bool shouldTriggerContentSwitcher = false;
    private bool isTransitionInProgress = false;
    private bool isReturningFromGameplay = false; // mark when coming back from gameplay
    private bool useBackToMenuVisuals = false; // flag for TransitionScreenController
    private enum TransitionDirection { Unknown, ToGameplay, ToMenu }
    private TransitionDirection currentTransitionDirection = TransitionDirection.Unknown;

    // NEW: Track which specific object was clicked
    private string clickedObjectName = "";
    private Vector3 clickedObjectPosition = Vector3.zero;

    // Events for notification
    public System.Action<ObjectType> OnObjectTypeSet;
    public System.Action OnContentSwitcherTriggered;

    // Cached WaitForSeconds to avoid garbage collection
    private WaitForSeconds cachedSceneTransitionDelay;
    private readonly WaitForEndOfFrame cachedWaitForEndOfFrame = new WaitForEndOfFrame();
    private readonly WaitForSeconds cachedSmallDelay = new WaitForSeconds(0.1f);

    // Staged transition data
    private string stagedFinalDestinationScene;
    private float stagedDelay;
    private bool isStagedTransition = false;
    private string intermediarySceneName;
    private EasyTransition.TransitionSettings stagedFinalTransitionSettings; // Renamed for clarity

    private void Awake()
    {
        // If this component lives alongside other managers, spawn a dedicated persistent GameObject to avoid dragging them across scenes.
        if (Instance == null && HasOtherManagersOnGameObject())
        {
            var persistentGO = new GameObject("SceneTransitionManager");
            var newSTM = persistentGO.AddComponent<SceneTransitionManager>();
            newSTM.CopyConfigFrom(this);
            Destroy(this);
            return;
        }

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

        // Store transition data FIRST
        targetSceneName = sceneName;
        clickedObjectName = objectName;
        clickedObjectPosition = objectPosition;

        SetObjectTypeForTransition(objectType);

        Debug.Log($"=== SCENE TRANSITION STARTED ===");
        Debug.Log($"Clicked Object: {objectName}");
        Debug.Log($"Object Type: {objectType}");
        Debug.Log($"Target Scene: {sceneName}");
        Debug.Log($"==============================");

        // CRITICAL: Save camera state IMMEDIATELY before any transition begins
        // This must happen while TopDownCameraController is still valid
        SaveCameraStateForRestore();

        // ONLY start transition AFTER saving state
        StartCoroutine(PerformSceneTransition());
    }

    private bool forceEnteringTransitionVisual = false;

    public void StartStagedTransition(string intermediaryScene, string finalDestinationScene, float delay, EasyTransition.TransitionSettings settings, bool forceEnteringVisual = false, bool backToMenu = false)
    {
        if (isTransitionInProgress)
        {
            Debug.LogWarning("Staged scene transition already in progress!");
            return;
        }

        forceEnteringTransitionVisual = forceEnteringVisual;
        useBackToMenuVisuals = backToMenu;

        // Mark direction based on destination (gameplay vs menu)
        currentTransitionDirection = finalDestinationScene.ToLower().Contains("gameplay")
            ? TransitionDirection.ToGameplay
            : TransitionDirection.ToMenu;

        stagedFinalDestinationScene = finalDestinationScene;
        stagedDelay = delay;
        isStagedTransition = true;
        intermediarySceneName = intermediaryScene;
        targetSceneName = intermediaryScene;
        stagedFinalTransitionSettings = settings; // Store for the final leg

        if (enableDebugLogs)
        {
            Debug.Log($"=== STAGED TRANSITION STARTED ===\nIntermediary: {intermediaryScene}\nFinal: {finalDestinationScene}");
        }

        SaveCameraStateForRestore();
        StartCoroutine(PerformSceneTransition(settings)); // Use settings for the first leg
    }

    /// <summary>
    /// Mark that the upcoming transition is a return from gameplay to menu.
    /// </summary>
    public void MarkReturningFromGameplay()
    {
        isReturningFromGameplay = true;
    }


    /// <summary>
    /// SIMPLE APPROACH: Use SimpleCameraFocusRestore system - removes complex dependencies
    /// </summary>
    private void SaveCameraStateForRestore()
    {
        Debug.Log("=== SIMPLE CAMERA STATE SAVING ===");

        try
        {
            // Use SimpleCameraFocusRestore system for clean, dependency-free saving
            var simpleCameraRestore = FindObjectOfType<SimpleCameraFocusRestore>();
            if (simpleCameraRestore != null)
            {
                simpleCameraRestore.SaveCurrentFocus();
                Debug.Log("✅ Camera state saved via SimpleCameraFocusRestore");
                return;
            }
            else
            {
                Debug.LogWarning("⚠️ SimpleCameraFocusRestore not found - creating instance");

                // Create SimpleCameraFocusRestore if not found
                GameObject simpleCameraGO = new GameObject("SimpleCameraFocusRestore");
                simpleCameraRestore = simpleCameraGO.AddComponent<SimpleCameraFocusRestore>();

                // Try saving with newly created instance
                if (simpleCameraRestore != null)
                {
                    simpleCameraRestore.SaveCurrentFocus();
                    Debug.Log("✅ Camera state saved via newly created SimpleCameraFocusRestore");
                    return;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ SimpleCameraFocusRestore save failed: {ex.Message}");
        }

        // SIMPLE BACKUP: Direct state capture without complex systems
        try
        {
            var topDownCamera = TopDownCameraController.Instance;
            if (topDownCamera != null && topDownCamera.gameObject != null)
            {
                var currentFocus = topDownCamera.GetCurrentFocus();
                if (currentFocus != null)
                {
                    clickedObjectName = currentFocus.name;
                    clickedObjectPosition = currentFocus.position;
                    Debug.Log($"✅ Backup save: {clickedObjectName} at {clickedObjectPosition}");
                }
                else
                {
                    Debug.Log("📝 No focus target to save");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Backup save failed: {ex.Message}");
        }

        Debug.Log("=== SIMPLE SAVE COMPLETED ===");
    }

    /// <summary>
    /// Perform the actual scene transition with proper cleanup
    /// </summary>
    private IEnumerator PerformSceneTransition(EasyTransition.TransitionSettings settings = null)
    {
        isTransitionInProgress = true;

        if (enableDebugLogs)
        {
            Debug.Log($"Starting scene transition to: {targetSceneName}");
        }

        if (sceneTransitionDelay > 0)
        {
            yield return cachedSceneTransitionDelay;
        }

        if (useEasyTransition)
        {
            if (!TryEasyTransition(settings))
            {
                if (enableDebugLogs) Debug.Log("EasyTransition failed, using standard scene loading");
                CleanupCurrentSceneForTransition();
                SceneManager.LoadScene(targetSceneName);
            }
        }
        else
        {
            // ✅ NEW SYSTEM: Using TransitionScreenController instead of EasyTransition
            if (enableDebugLogs)
            {
                Debug.Log("=== USING NEW TRANSITION SYSTEM ===");
                Debug.Log($"Loading scene: {targetSceneName}");
                Debug.Log($"TransitionScreen will automatically detect direction via TransitionScreenController");
                Debug.Log("====================================");
            }

            CleanupCurrentSceneForTransition();
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private IEnumerator ContinueStagedTransition()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"...continuing staged transition, waiting {stagedDelay}s...");
        }

        if (stagedDelay > 0)
        {
            yield return new WaitForSeconds(stagedDelay);
        }

        targetSceneName = stagedFinalDestinationScene;
        EasyTransition.TransitionSettings finalSettings = stagedFinalTransitionSettings;

        // Reset staged data before starting next transition
        isStagedTransition = false;
        stagedFinalDestinationScene = null;
        stagedDelay = 0;
        intermediarySceneName = null;
        stagedFinalTransitionSettings = null;
        forceEnteringTransitionVisual = false; // reset override after intermediary leg

        if (enableDebugLogs)
        {
            Debug.Log($"...wait over, transitioning to final scene: {targetSceneName}");
        }
        StartCoroutine(PerformSceneTransition(finalSettings));
    }

    private bool TryEasyTransition(EasyTransition.TransitionSettings settings)
    {
        try
        {
            // METHOD 1: Try to find EasyTransition.TransitionManager in scene
            var transitionManager = FindObjectOfType<EasyTransition.TransitionManager>();
            if (transitionManager == null)
            {
                if (enableDebugLogs) Debug.LogWarning("EasyTransition.TransitionManager not found. Using standard scene loading.");
                return false;
            }

            // CRITICAL FIX: ALWAYS reset runningTransition flag BEFORE attempting transition
            // This fixes the issue where 2nd and 3rd gameplay transitions fail
            var runningTransitionField = transitionManager.GetType().GetField("runningTransition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (runningTransitionField != null)
            {
                bool wasRunning = (bool)runningTransitionField.GetValue(transitionManager);
                if (wasRunning)
                {
                    if (enableDebugLogs) Debug.LogWarning("⚠️ Found stuck runningTransition=true flag! Resetting...");
                }
                runningTransitionField.SetValue(transitionManager, false);
                if (enableDebugLogs) Debug.Log("✅ Reset runningTransition flag to false");
            }

            if (enableDebugLogs)
            {
                Debug.Log("=== USING EASY TRANSITION ANIMATION ===");
                Debug.Log($"Found TransitionManager: {transitionManager.name}");
            }

            EasyTransition.TransitionSettings transitionToUse = settings;

            // If no settings are provided via parameters, try to find them from the specific clicked object
            if (transitionToUse == null && !string.IsNullOrEmpty(clickedObjectName))
            {
                Debug.Log($"Attempting to find settings from clicked object: {clickedObjectName}");
                GameObject clickedGO = GameObject.Find(clickedObjectName);
                if (clickedGO != null)
                {
                    ClickableObject clickable = clickedGO.GetComponent<ClickableObject>();
                    if (clickable != null)
                    {
                        transitionToUse = clickable.GetTransitionSettings();
                        if (transitionToUse != null)
                        {
                            Debug.Log($"✅ Found TransitionSettings '{transitionToUse.name}' on clicked object '{clickedObjectName}'.");
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ Clicked object '{clickedObjectName}' found, but it has no TransitionSettings assigned.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ Could not find GameObject for clicked object name: '{clickedObjectName}'.");
                }
            }

            // If still no settings, use the broader search fallbacks
            if (transitionToUse == null)
            {
                if (enableDebugLogs) Debug.Log("No specific transition settings found, searching for fallbacks in scene...");
                transitionToUse = FindTransitionSettingsInScene(); // Broader search in scene
            }

            // If still null, try the inspector-assigned fallback on this manager
            if (transitionToUse == null)
            {
                if (fallbackTransitionSettings != null)
                {
                    if (enableDebugLogs) Debug.LogWarning("Using fallback transition settings assigned directly on SceneTransitionManager.");
                    transitionToUse = fallbackTransitionSettings;
                }
            }

            // Final attempt: search all loaded resources
            if (transitionToUse == null)
            {
                EasyTransition.TransitionSettings[] allTransitionSettings = Resources.FindObjectsOfTypeAll<EasyTransition.TransitionSettings>();
                if (allTransitionSettings.Length > 0)
                {
                    transitionToUse = allTransitionSettings[0];
                    if (enableDebugLogs) Debug.LogWarning($"Using first available TransitionSettings found in project assets: {transitionToUse.name}");
                }
            }

            if (transitionToUse != null)
            {
                try
                {
                    // Get the transition time from TransitionSettings object using reflection
                    var transitionTimeField = transitionToUse.GetType().GetField("transitionTime");
                    float transitionTime = 1f; // default fallback
                    if (transitionTimeField != null)
                    {
                        transitionTime = (float)transitionTimeField.GetValue(transitionToUse);
                        if (enableDebugLogs) Debug.Log($"Using TransitionSettings transitionTime: {transitionTime}s");
                    }

                    // Call EasyTransition with proper duration from settings
                    transitionManager.Transition(targetSceneName, transitionToUse, transitionTime);

                    if (enableDebugLogs)
                    {
                        Debug.Log($"✅ EasyTransition called successfully with proper settings: Scene '{targetSceneName}', Transition '{transitionToUse.name}', Duration '{transitionTime}s'");
                    }

                    return true;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ EasyTransition direct call FAILED: {ex.Message}. Stack Trace: {ex.StackTrace}");
                    Debug.LogWarning("Trying reflection fallback methods...");
                }
            }
            else
            {
                Debug.LogError("❌ No TransitionSettings found anywhere! Cannot perform animated transition. Trying reflection fallback...");
            }

            // Fallback to reflection if settings-based approach failed or wasn't possible
            if (TryReflectionTransition(transitionManager))
            {
                if (enableDebugLogs) Debug.Log("✅ Reflection fallback for transition succeeded!");
                return true;
            }

            Debug.LogError("❌ All EasyTransition methods failed!");
            return false;
        }
        catch (System.Exception ex)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"EasyTransition attempt failed globally: {ex.Message}");
            }
            return false;
        }
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
                Debug.Log($"  {i + 1}. {obj.name} - ObjectType: {obj.GetObjectType()} - HasValidContentSwitcher: {obj.HasValidContentSwitcher()}");
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

        // Alternative: Simple scale animation using coroutine (replaced DOTween)
        StartCoroutine(PunchScaleAnimation(targetObject.transform, Vector3.one * 0.1f, 0.5f));
    }


    /// <summary>
    /// Simple scale animation coroutine to replace DOTween.DOPunchScale
    /// </summary>
    private IEnumerator PunchScaleAnimation(Transform target, Vector3 punch, float duration)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale + punch;

        float elapsed = 0f;
        float halfDuration = duration * 0.5f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(originalScale, targetScale, progress);
            yield return null;
        }

        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(targetScale, originalScale, progress);
            yield return null;
        }

        // Ensure exact original scale
        target.localScale = originalScale;
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

        // ADDITIONAL SAFEGUARD: Reset EasyTransition's runningTransition flag when scene loads
        // This ensures transitions work properly on 2nd, 3rd gameplay entries
        var transitionManager = FindObjectOfType<EasyTransition.TransitionManager>();
        if (transitionManager != null)
        {
            var runningTransitionField = transitionManager.GetType().GetField("runningTransition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (runningTransitionField != null)
            {
                runningTransitionField.SetValue(transitionManager, false);
                if (enableDebugLogs) Debug.Log("✅ OnSceneLoaded: Reset EasyTransition runningTransition flag");
            }
        }

        if (isStagedTransition && scene.name == intermediarySceneName)
        {
            StartCoroutine(ContinueStagedTransition());
            return;
        }

        // --- CAMERA AND CONTENT SWITCHER LOGIC ---
        bool isMenuScene = scene.name.Contains("New Start Game Sandy") || scene.name.Contains("Menu") || scene.name.Contains("Main");
        bool hasObjectContext = !string.IsNullOrEmpty(clickedObjectName);

        if (isMenuScene && hasObjectContext)
        {
            // This runs synchronously BEFORE the first frame is rendered.
            SetupInstantFocus();
        }
        else if (isMenuScene && (shouldTriggerContentSwitcher || isReturningFromGameplay))
        {
            StartCoroutine(ForceHardcodedCameraView());
        }
        else if (shouldTriggerContentSwitcher)
        {
            StartCoroutine(TriggerContentSwitcherAfterDelay());
        }

        // If we just returned from gameplay, prevent intro UI from reappearing.
        if (isMenuScene && isReturningFromGameplay)
        {
            CameraAnimationController.Instance?.NotifyReturningFromGameplay();
            isReturningFromGameplay = false;
            Debug.Log("🔄 Returned from gameplay - intro UI will stay hidden.");
        }

        // Re-enable touch input and load progress
        if (isMenuScene)
        {
            StartCoroutine(ReenableTouchAfterUIReady());
            StartCoroutine(LoadAndApplySavedProgressAfterDelay());

            // ✅ CRITICAL FIX: Force refresh unlock visuals after returning from gameplay
            // This ensures Kendi and other objects show correct lock/unlock state
            if (isReturningFromGameplay || shouldTriggerContentSwitcher)
            {
                StartCoroutine(RefreshUnlockVisualsAfterDelay());
            }
        }
    }

    private void SetupInstantFocus()
    {
        Debug.Log("[InstantFocus] Setting up camera position synchronously in OnSceneLoaded.");

        var cameraController = TopDownCameraController.Instance;
        if (cameraController == null)
        {
            Debug.LogError("[InstantFocus] Camera controller not found! Cannot perform instant focus.");
            return;
        }

        cameraController.enabled = false;

        ClickableObject targetObject = FindClickedObjectInScene();
        if (targetObject != null)
        {
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.ForceEnterZoomMode();
            }

            cameraController.FocusOnObjectImmediate(targetObject.transform);

            cameraController.enabled = true;
            Debug.Log("[InstantFocus] Camera setup complete. Controller re-enabled.");

            shouldTriggerContentSwitcher = true;
            StartCoroutine(TriggerContentSwitcherAfterDelay());
            StartCoroutine(EnsureZoomInputReadyAfterAutoFocus());
            StartCoroutine(EnsureZoomInputReadyAfterAutoFocus()); // double guarantee
        }
        else
        {
            cameraController.enabled = true; // Always re-enable
            Debug.LogWarning("[InstantFocus] Could not find the last clicked object. Defaulting to hardcoded view.");
            StartCoroutine(ForceHardcodedCameraView());
        }
    }

    /// <summary>
    /// NEW: Forces the camera to a hardcoded position, then triggers the content switcher.
    /// </summary>
    private IEnumerator ForceHardcodedCameraView()
    {
        Debug.Log("🎯 [HARDCODE] Starting ForceHardcodedCameraView coroutine.");

        // Try to grab the camera controller immediately so the camera is already in place on the first frame after the transition.
        TopDownCameraController controller = TopDownCameraController.Instance;
        int attempts = 0;
        while (controller == null && attempts < 50)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
            controller = TopDownCameraController.Instance;
        }

        if (controller == null)
        {
            Debug.LogError("[HARDCODE] Failed to find TopDownCameraController instance. Aborting.");
            yield break;
        }

        Debug.Log("📷 [HARDCODE] STEP 1: Forcing camera position.");

        // 2. Define the hardcoded position and rotation.
        Vector3 hardcodedPosition = new Vector3(-4.634338f, 2f, -0.2877529f);
        Quaternion hardcodedRotation = Quaternion.Euler(90f, 0f, 0f);

        // 3. Call the new method on the camera controller.
        controller.SetHardcodedFocusPosition(hardcodedPosition, hardcodedRotation);

        // Keep input logic in sync with the forced camera view so double-tap knows we're already in zoom.
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ForceEnterZoomMode();
            Debug.Log("[HARDCODE] GameMode set to Zoom to match forced camera view.");
        }

        // 4. Wait for the camera to settle.
        yield return new WaitForSeconds(0.5f); // Wait for half a second after forcing position.
        Debug.Log("✅ [HARDCODE] Camera position has been set.");

        // 5. NOW TRIGGER CONTENT SWITCHER
        Debug.Log("🎨 [HARDCODE] STEP 2: Now triggering ContentSwitcher.");
        shouldTriggerContentSwitcher = true;
        StartCoroutine(EnsureZoomInputReadyAfterAutoFocus());
        StartCoroutine(EnsureZoomInputReadyAfterAutoFocus()); // double guarantee
        bool contentSwitcherSuccess = TriggerContentSwitcher();

        if (contentSwitcherSuccess)
        {
            Debug.Log("✅ [HARDCODE] ContentSwitcher triggered successfully after forcing camera view.");
            OnContentSwitcherTriggered?.Invoke();
        }
        else
        {
            Debug.LogError("❌ [HARDCODE] ContentSwitcher failed after forcing camera view.");
        }

        Debug.Log("🎯 [HARDCODE] Coroutine completed.");
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

                // Make sure auto-zoom leaves controls unlocked and in zoom mode
                StartCoroutine(EnsureZoomInputReadyAfterTransition());
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
            Debug.LogWarning("Failed to find or trigger ContentSwitcher!");
        }
    }

    /// <summary>
    /// Re-enable input after auto-zoom (stay in zoom mode; only unlock controls and mode).
    /// </summary>
    private IEnumerator EnsureZoomInputReadyAfterTransition()
    {
        // Small delay to let ContentSwitcher finish setting up the zoom view
        yield return new WaitForSeconds(0.15f);

        // Force zoom mode so drag/swipe uses zoom branch
        GameModeManager.Instance?.ForceEnterZoomMode();

        // Ensure camera controller is active (it might be disabled during instant focus)
        var camController = TopDownCameraController.Instance;
        if (camController != null && !camController.enabled)
        {
            camController.enabled = true;
        }

        // Kill any leftover transition lock
        AdvancedInputManager.EndTransitionLock();

        // Explicitly re-enable gesture input systems
        if (AdvancedInputManager.Instance != null)
        {
            AdvancedInputManager.Instance.UnblockInput();
            AdvancedInputManager.Instance.ResetGestureSystems();
        }

        // Unlock input
        TouchManager.Instance?.DisableAllTouch(false);
    }

    /// <summary>
    /// Ensure zoom input is ready after automatic focus/zoom (gameplay or menu).
    /// </summary>
    private IEnumerator EnsureZoomInputReadyAfterAutoFocus()
    {
        yield return new WaitForSeconds(0.1f);

        GameModeManager.Instance?.ForceEnterZoomMode();

        var camController = TopDownCameraController.Instance;
        if (camController != null && !camController.enabled)
        {
            camController.enabled = true;
        }

        AdvancedInputManager.EndTransitionLock();
        AdvancedInputManager.Instance?.UnblockInput();
        TouchManager.Instance?.DisableAllTouch(false);
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
            Debug.LogWarning("⚠️ No ContentSwitcher found in scene - skipping content switch.");
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
            case ObjectType.ChinaHorse:
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
    public bool IsReturningFromGameplayFlag() => isReturningFromGameplay;
    public string GetTransitionDirection() => currentTransitionDirection.ToString();
    public bool ShouldForceEnteringTransitionVisual() => forceEnteringTransitionVisual;
    public void ClearReturningFromGameplayFlag() => isReturningFromGameplay = false;
    public void SetBackToMenuFlag(bool value) => useBackToMenuVisuals = value;
    public bool GetBackToMenuFlag() => useBackToMenuVisuals;

    public void SetTransitionDirectionToGameplay()
    {
        currentTransitionDirection = TransitionDirection.ToGameplay;
        useBackToMenuVisuals = false; // gameplay transitions should not use back-to-menu visuals
    }

    public void SetTransitionDirectionToMenu()
    {
        currentTransitionDirection = TransitionDirection.ToMenu;
    }

    /// <summary>
    /// Direct transition to a target scene without triggering ContentSwitcher (for exit/back flows).
    /// </summary>
    public void TransitionToSceneDirect(string sceneName)
    {
        if (isTransitionInProgress)
        {
            Debug.LogWarning("Scene transition already in progress!");
            return;
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("❌ Scene name is empty! Cannot transition.");
            return;
        }

        targetSceneName = sceneName;

        // Disable content switcher flow for this transition
        shouldTriggerContentSwitcher = false;
        clickedObjectName = string.Empty;
        clickedObjectPosition = Vector3.zero;
        isReturningFromGameplay = false; // ensure menu loads in exploration view

        // Preserve camera state if available
        SaveCameraStateForRestore();

        // Go
        StartCoroutine(PerformSceneTransition());
    }

    /// <summary>
    /// Hard fallback: force load a scene immediately, bypassing transition guards (use for Exit button if other flows are blocked).
    /// </summary>
    public void ForceTransitionImmediate(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("❌ Scene name is empty! Cannot force transition.");
            return;
        }

        // Cancel any running transitions and clear flags
        StopAllCoroutines();
        isTransitionInProgress = false;
        shouldTriggerContentSwitcher = false;
        isStagedTransition = false;
        clickedObjectName = string.Empty;
        clickedObjectPosition = Vector3.zero;
        stagedFinalDestinationScene = null;
        stagedFinalTransitionSettings = null;

        Debug.LogWarning($"[SceneTransitionManager] Force loading scene immediately: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Utility: detect if this GO hosts other managers that should not be marked DontDestroyOnLoad.
    /// </summary>
    private bool HasOtherManagersOnGameObject()
    {
        var components = GetComponents<Component>();
        int extra = 0;
        foreach (var comp in components)
        {
            if (comp == null || comp is Transform || comp == this) continue;
            extra++;
        }
        return extra > 0;
    }

    /// <summary>
    /// Copy serialized config to a new instance when migrating to a dedicated GO.
    /// </summary>
    private void CopyConfigFrom(SceneTransitionManager other)
    {
        sceneTransitionDelay = other.sceneTransitionDelay;
        useEasyTransition = other.useEasyTransition;
        enableDebugLogs = other.enableDebugLogs;
        fallbackTransitionSettings = other.fallbackTransitionSettings;
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

        // ✅ FIXED: Apply completion status to matching objects (PREVENT RESET ISSUE)
        foreach (var completedObj in saveData.completedObjects)
        {
            foreach (var clickableObj in allClickableObjects)
            {
                // Match by name and ObjectType
                if (clickableObj.name == completedObj.objectName &&
                    clickableObj.GetObjectType() == completedObj.objectType)
                {
                    // ✅ FIX: DO NOT call ApplyContentSwitcherChanges automatically!
                    // This was causing visual changes that bypass ContentSwitcher state management
                    // Instead, let ContentSwitcher handle its own state through event system

                    if (enableDebugLogs)
                    {
                        Debug.Log($"Found completed object: {clickableObj.name} - letting ContentSwitcher handle state");
                    }

                    // ✅ COMMENTED OUT: This line was causing the reset issue
                    // clickableObj.ApplyContentSwitcherChanges();

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
        currentTransitionDirection = TransitionDirection.Unknown;
        isReturningFromGameplay = false;
        useBackToMenuVisuals = false;
        forceEnteringTransitionVisual = false;

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
    /// Helper method for fallback to exploration mode
    /// </summary>
    private void RestoreToExplorationModeFallback(TopDownCameraController cameraController)
    {
        Debug.Log("🔧 FALLBACK: Restoring to exploration mode");

        // Set GameModeManager to exploration mode
        if (GameModeManager.Instance != null)
        {
            Debug.Log("🔄 Setting GameModeManager to exploration mode");
            GameModeManager.Instance.ReturnToExplorationMode();
        }

        // Transition camera to overview/exploration state
        Debug.Log("🔄 Transitioning camera to exploration overview state");
        cameraController.TransitionToOverview();
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
        // UI cleanup removed - not needed without DOTween

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
    /// Force refresh unlock visuals for all ClickableObjects in scene
    /// This fixes the issue where Kendi stays locked after Coin completion
    /// </summary>
    private IEnumerator RefreshUnlockVisualsAfterDelay()
    {
        // Wait for scene to be fully loaded and SaveSystem to be ready
        yield return new WaitForSeconds(0.7f);

        if (enableDebugLogs)
        {
            Debug.Log("=== REFRESHING UNLOCK VISUALS ===");
        }

        // Find all ClickableObjects in scene
        ClickableObject[] allClickableObjects = FindObjectsOfType<ClickableObject>();

        if (enableDebugLogs)
        {
            Debug.Log($"Found {allClickableObjects.Length} ClickableObjects to refresh");
        }

        // Force each object to update its lock/unlock visual
        foreach (var clickable in allClickableObjects)
        {
            if (clickable != null)
            {
                // Force re-enable to trigger UpdateLockVisual in OnEnable
                clickable.gameObject.SetActive(false);
                yield return null; // Wait one frame
                clickable.gameObject.SetActive(true);

                if (enableDebugLogs)
                {
                    Debug.Log($"Refreshed: {clickable.name}");
                }
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log("=== UNLOCK VISUALS REFRESH COMPLETE ===");
        }
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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    public static GamePlayManager Instance;

    [SerializeField] GameObject Environment;
    [SerializeField] Transform ClearInspect;
    [SerializeField] Camera ToolCamera;
    [SerializeField] ParticleSystem GlitterParticle;

    [Header("Object Type Configuration")]
    [SerializeField] private ObjectType currentGameplayObjectType = ObjectType.ChinaCoin;
    [SerializeField] private bool autoDetectObjectType = true;

    [Header("Scene Transition")]
    [SerializeField] private bool enableSceneTransition = true;
    [SerializeField] private float transitionDelayAfterFinish = 2f;
    [SerializeField] private float environmentAnimationDuration = 3f; // Time to complete environment animation

    public bool isGameFinished = false;
    private bool transitionTriggered = false;
    private bool isSceneUnloading = false;
    private bool isEnvironmentAnimating = false; // Track environment animation state

    private readonly Vector3 FinishedPosition = new Vector3(0.0f, -5.5f, -9f);

    private void Awake()
    {
        Instance = this;
        ToolCamera.enabled = true;

        // Auto-detect ObjectType if enabled
        if (autoDetectObjectType)
        {
            DetectObjectTypeFromScene();
        }
    }

    void Start()
    {
        Debug.Log($"=== GAMEPLAY MANAGER INITIALIZED ===");
        Debug.Log($"Current Object Type: {currentGameplayObjectType}");
        Debug.Log($"Scene Transition Enabled: {enableSceneTransition}");

        // Enable touch input safely after TouchManager is ready
        StartCoroutine(EnableTouchInputDelayed());
    }

    private IEnumerator EnableTouchInputDelayed()
    {
        // Wait a frame to ensure TouchManager is fully initialized
        yield return null;

        // Check if TouchManager instance exists before calling
        int retryCount = 0;
        while (TouchManager.Instance == null && retryCount < 10)
        {
            yield return new WaitForSeconds(0.1f);
            retryCount++;
        }

        if (TouchManager.Instance != null)
        {
            TouchManager.Instance.DisableAllTouch(false);
            Debug.Log("=== TOUCH INPUT ENABLED IN GAMEPLAY ===");
        }
        else
        {
            Debug.LogError("=== TouchManager Instance not found after retries! ===");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // SAFETY CHECK: Stop general processing if scene is unloading, but allow environment animation to continue
        if (isSceneUnloading && !isEnvironmentAnimating)
            return;

        // SAFETY CHECK: Allow environment animation even if UI is destroyed
        if (UIManager.Instance == null && !isEnvironmentAnimating)
            return;

        // Handle environment animation continuation even during scene unloading
        if (isEnvironmentAnimating)
        {
            ContinueEnvironmentAnimation();
            return; // Only do environment animation during this phase
        }

        // Normal game processing
        try
        {
            float progressValue = UIManager.Instance.GetAllProgressValue();
            Debug.Log("Value: " + progressValue);
            FinishedGame();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"GamePlayManager Update failed (likely scene unloading): {ex.Message}");
            if (!isEnvironmentAnimating)
            {
                isSceneUnloading = true; // Stop further processing only if not animating environment
            }
        }
    }

    private void FinishedGame()
    {
        // SAFETY CHECK: Stop if scene is unloading or already finished
        if (isSceneUnloading || isGameFinished)
            return;

        // SAFETY CHECK: Verify UIManager is still valid
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager destroyed during FinishedGame check");
            isSceneUnloading = true;
            return;
        }

        if (UIManager.Instance.GetAllProgressValue() >= 100)
        {
            // Safely disable touch if TouchManager exists
            if (TouchManager.Instance != null)
            {
                TouchManager.Instance.DisableAllTouch(true);
            }
            UIManager.Instance.ShowFinishUI(true);
            UIManager.Instance.ShowFinishBackground(true);
            GlitterParticle.gameObject.SetActive(true);
            ToolCamera.enabled = false;

            // Start environment animation
            if (!isEnvironmentAnimating)
            {
                isEnvironmentAnimating = true;
                StartCoroutine(HandleEnvironmentAnimationThenTransition());
            }

            // Continue environment and cluster animations in Update
            ContinueEnvironmentAnimation();
        }
    }

    /// <summary>
    /// Continue environment and cluster animations - called from Update
    /// </summary>
    private void ContinueEnvironmentAnimation()
    {
        if (!isEnvironmentAnimating) return;

        // Environment animation - your original code preserved
        Environment.transform.position = Vector3.Lerp(
            Environment.transform.position,
            FinishedPosition,
            Time.deltaTime * 2f
        );

        // Cluster animation - your original code preserved
        ClusterStateMachine cluster = AssembleManager.Instance.CurrentClusterInspected;
        if (cluster != null)
        {
            if (!isGameFinished) // Only log once
            {
                Debug.Log("Cluster Finished: " + cluster.name);
                cluster.SwitchState(new ClusterFinishState(cluster));
                isGameFinished = true;
            }

            cluster.transform.position = Vector3.Lerp(cluster.transform.position, ClearInspect.position, Time.deltaTime * 2);

            cluster.transform.rotation = Quaternion.Slerp(
                cluster.transform.rotation,
                Quaternion.Euler(90f, 0f, 0f),
                Time.deltaTime * 2f
            );

            cluster.transform.SetParent(ClearInspect);
        }
    }

    /// <summary>
    /// Handle environment animation duration then trigger scene transition
    /// </summary>
    private IEnumerator HandleEnvironmentAnimationThenTransition()
    {
        Debug.Log($"=== STARTING ENVIRONMENT ANIMATION ({environmentAnimationDuration}s) ===");

        // Wait for environment animation to complete
        yield return new WaitForSeconds(environmentAnimationDuration);

        Debug.Log("=== ENVIRONMENT ANIMATION COMPLETED ===");
        isEnvironmentAnimating = false;

        // Now trigger scene transition
        if (enableSceneTransition && !transitionTriggered)
        {
            transitionTriggered = true;
            StartCoroutine(TriggerSceneTransitionAfterDelay());
        }
    }

    public bool IsGameFinished()
    {
        return isGameFinished;
    }

    /// <summary>
    /// Auto-detect ObjectType based on scene name or other indicators
    /// </summary>
    private void DetectObjectTypeFromScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();

        // Try to detect object type from scene name
        if (sceneName.Contains("coin") || sceneName.Contains("china coin"))
        {
            currentGameplayObjectType = ObjectType.ChinaCoin;
        }
        else if (sceneName.Contains("jar") || sceneName.Contains("china jar"))
        {
            currentGameplayObjectType = ObjectType.ChinaJar;
        }
        else if (sceneName.Contains("kendin") || sceneName.Contains("indonesia"))
        {
            currentGameplayObjectType = ObjectType.IndonesiaKendin;
        }
        else if (sceneName.Contains("winged") || sceneName.Contains("mesir"))
        {
            currentGameplayObjectType = ObjectType.MesirWingedScared;
        }

        Debug.Log($"Auto-detected ObjectType: {currentGameplayObjectType} from scene: {sceneName}");
    }

    /// <summary>
    /// Trigger scene transition after a delay when game is finished
    /// </summary>
    private IEnumerator TriggerSceneTransitionAfterDelay()
    {
        Debug.Log($"=== GAME FINISHED - Starting transition in {transitionDelayAfterFinish} seconds ===");

        // Wait for the specified delay
        yield return new WaitForSeconds(transitionDelayAfterFinish);

        // Ensure SceneTransitionManager exists
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager not found! Creating one...");
            GameObject stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        Debug.Log($"=== TRIGGERING SCENE TRANSITION ===");
        Debug.Log($"Object Type: {currentGameplayObjectType}");

        // Get the target scene from SceneTransitionManager data (from originally clicked object)
        string targetScene = GetTargetSceneForReturn();
        Debug.Log($"Target Scene: {targetScene}");

        // Trigger the scene transition with ContentSwitcher
        SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(currentGameplayObjectType, targetScene);

        // CLEANUP: Prepare for scene unload AFTER transition starts
        // Give transition animation time to start properly
        yield return new WaitForSeconds(0.5f);
        PrepareForSceneTransition();
    }

    /// <summary>
    /// Prepare GamePlayManager for scene unload - stop all processing
    /// </summary>
    private void PrepareForSceneTransition()
    {
        Debug.Log("=== PREPARING GAMEPLAY MANAGER FOR SCENE TRANSITION ===");

        // Stop all Update() processing (but environment animation can continue if still running)
        isSceneUnloading = true;

        // CLEANUP: Prepare UIManager for scene transition
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PrepareForSceneTransition();
        }

        // Stop all coroutines EXCEPT environment animation
        StopNonEssentialCoroutines();

        // Clear instance reference to prevent cross-scene access
        if (Instance == this)
        {
            Instance = null;
        }

        Debug.Log("GamePlayManager prepared for scene unload");
    }

    /// <summary>
    /// Stop coroutines but preserve environment animation
    /// </summary>
    private void StopNonEssentialCoroutines()
    {
        // If environment is still animating, don't stop all coroutines yet
        if (isEnvironmentAnimating)
        {
            Debug.Log("Environment still animating - preserving animation coroutines");
            // Only stop non-essential coroutines here
            // The environment animation coroutine will naturally finish
            return;
        }

        // If environment animation is done, safe to stop all coroutines
        StopAllCoroutines();
        Debug.Log("All coroutines stopped - environment animation completed");
    }

    /// <summary>
    /// Manual method to set ObjectType (can be called from UI or other scripts)
    /// </summary>
    public void SetObjectType(ObjectType objectType)
    {
        currentGameplayObjectType = objectType;
        Debug.Log($"ObjectType manually set to: {objectType}");
    }

    /// <summary>
    /// Get current ObjectType
    /// </summary>
    public ObjectType GetCurrentObjectType()
    {
        return currentGameplayObjectType;
    }

    /// <summary>
    /// Manual trigger for finish button (can be called from UI)
    /// </summary>
    public void TriggerFinishButton()
    {
        Debug.Log("=== FINISH BUTTON TRIGGERED MANUALLY ===");

        if (enableSceneTransition && !transitionTriggered)
        {
            transitionTriggered = true;

            // Trigger transition immediately when button clicked (no delay)
            StartCoroutine(TriggerButtonTransition());
        }
        else
        {
            Debug.Log("Scene transition disabled or already triggered");
        }
    }

    /// <summary>
    /// Immediate transition when button clicked - preserves transition animation
    /// </summary>
    private IEnumerator TriggerButtonTransition()
    {
        Debug.Log("=== BUTTON TRANSITION - Starting immediately ===");

        // Ensure SceneTransitionManager exists
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager not found! Creating one...");
            GameObject stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        Debug.Log($"=== TRIGGERING SCENE TRANSITION FROM BUTTON ===");
        Debug.Log($"Object Type: {currentGameplayObjectType}");

        // Get the target scene from SceneTransitionManager data (from originally clicked object)
        string targetScene = GetTargetSceneForReturn();
        Debug.Log($"Target Scene: {targetScene}");

        // Trigger the scene transition with ContentSwitcher (preserves your transition animation)
        SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(currentGameplayObjectType, targetScene);

        // CLEANUP: Prepare for scene unload AFTER transition animation has time to start
        // Give transition animation more time to properly initialize
        yield return new WaitForSeconds(1.0f);
        PrepareForSceneTransition();
    }

    /// <summary>
    /// Immediate scene transition (for testing or immediate transition)
    /// </summary>
    public void TriggerImmediateTransition()
    {
        Debug.Log("=== IMMEDIATE SCENE TRANSITION TRIGGERED ===");

        // Ensure SceneTransitionManager exists
        if (SceneTransitionManager.Instance == null)
        {
            GameObject stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        string targetScene = GetTargetSceneForReturn();
        SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(currentGameplayObjectType, targetScene);
    }

    /// <summary>
    /// Get the target scene to return to (usually the main menu scene)
    /// Simple logic: return to the scene we came from, or default main scene
    /// </summary>
    private string GetTargetSceneForReturn()
    {
        // Try to get the scene from SceneTransitionManager (if it remembers where we came from)
        if (SceneTransitionManager.Instance != null)
        {
            // For now, return to main menu scene - this could be made more sophisticated
            // by tracking the "source scene" in SceneTransitionManager
            return "New Start Game Sandy"; // Default main menu scene
        }

        // Fallback
        return "New Start Game Sandy";
    }

    /// <summary>
    /// Cleanup when GamePlayManager is destroyed
    /// </summary>
    private void OnDestroy()
    {
        // Clear instance when destroyed
        if (Instance == this)
        {
            Instance = null;
        }

        // Stop all coroutines
        StopAllCoroutines();
    }
}

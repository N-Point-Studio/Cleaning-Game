using UnityEngine;
using System.Collections;

/// <summary>
/// Specific test for debugging "destroyed object" errors during scene transitions
/// Simulates the exact scenario that was causing the error
/// </summary>
public class DestroyedObjectErrorTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private Transform testFocusObject;
    [SerializeField] private string testTargetScene = "TestGameplayScene";
    [SerializeField] private float sceneTransitionDelay = 2f;

    [Header("Error Simulation")]
    [SerializeField] private bool simulateDestroyedController = false;
    [SerializeField] private bool enableDetailedLogging = true;

    private bool isTestRunning = false;

    [ContextMenu("Test: Simulate Destroyed Object Error")]
    public void TestSimulateDestroyedObjectError()
    {
        if (isTestRunning)
        {
            Debug.LogWarning("Test already running!");
            return;
        }

        StartCoroutine(SimulateDestroyedObjectScenario());
    }

    [ContextMenu("Test: Safe Scene Transition")]
    public void TestSafeSceneTransition()
    {
        if (isTestRunning)
        {
            Debug.LogWarning("Test already running!");
            return;
        }

        StartCoroutine(SafeSceneTransitionTest());
    }

    private IEnumerator SimulateDestroyedObjectScenario()
    {
        isTestRunning = true;
        Log("🧪 === TESTING DESTROYED OBJECT ERROR SCENARIO ===");

        // Step 1: Setup focus target
        var topDownCamera = TopDownCameraController.Instance;
        if (topDownCamera != null && testFocusObject != null)
        {
            topDownCamera.SetFocusTarget(testFocusObject);
            Log($"✅ Focus target set to: {testFocusObject.name}");
            yield return new WaitForSeconds(0.5f);
        }

        // Step 2: Try to save state BEFORE transition
        Log("🔧 Testing state save before transition...");
        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        if (cameraStateManager != null)
        {
            try
            {
                cameraStateManager.SaveCurrentCameraState();
                Log("✅ Pre-transition state save successful");
            }
            catch (System.Exception ex)
            {
                Log($"❌ Pre-transition state save failed: {ex.Message}");
            }
        }

        // Step 3: Simulate what happens during scene transition
        Log("⚠️ Simulating scene transition conditions...");

        if (simulateDestroyedController)
        {
            // Simulate destroyed controller scenario
            if (topDownCamera != null)
            {
                Log("🔥 Simulating controller destruction...");
                topDownCamera.gameObject.SetActive(false);
                yield return new WaitForSeconds(0.1f);

                // Try to access destroyed controller (this would cause the error)
                Log("💀 Attempting to access destroyed controller...");
                try
                {
                    var focus = topDownCamera.GetCurrentFocus();
                    Log($"Focus target: {(focus != null ? focus.name : "NULL")}");
                }
                catch (System.Exception ex)
                {
                    Log($"❌ CAUGHT DESTROYED OBJECT ERROR: {ex.Message}");
                }

                // Restore controller
                topDownCamera.gameObject.SetActive(true);
            }
        }

        // Step 4: Test safe access methods
        Log("🛡️ Testing safe access methods...");
        yield return StartCoroutine(TestSafeAccessMethods());

        isTestRunning = false;
        Log("🧪 === DESTROYED OBJECT ERROR TEST COMPLETED ===");
    }

    private IEnumerator SafeSceneTransitionTest()
    {
        isTestRunning = true;
        Log("🧪 === TESTING SAFE SCENE TRANSITION ===");

        // Step 1: Setup focus
        var topDownCamera = TopDownCameraController.Instance;
        if (topDownCamera != null && testFocusObject != null)
        {
            topDownCamera.SetFocusTarget(testFocusObject);
            Log($"✅ Focus target set to: {testFocusObject.name}");
            yield return new WaitForSeconds(0.5f);
        }

        // Step 2: Test SceneTransitionManager save method
        Log("🔧 Testing SceneTransitionManager save method...");
        var sceneTransitionManager = SceneTransitionManager.Instance;
        if (sceneTransitionManager != null)
        {
            try
            {
                // Call the private SaveCameraStateForRestore method via reflection
                var saveMethod = typeof(SceneTransitionManager).GetMethod("SaveCameraStateForRestore",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (saveMethod != null)
                {
                    saveMethod.Invoke(sceneTransitionManager, null);
                    Log("✅ SceneTransitionManager save method executed successfully");
                }
                else
                {
                    Log("❌ Could not find SaveCameraStateForRestore method");
                }
            }
            catch (System.Exception ex)
            {
                Log($"❌ SceneTransitionManager save failed: {ex.Message}");
            }
        }

        // Step 3: Test CameraStateManager independently
        Log("🔧 Testing CameraStateManager independently...");
        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        if (cameraStateManager != null)
        {
            bool saveSuccess = false;
            try
            {
                cameraStateManager.SaveCurrentCameraState();

                var savedState = cameraStateManager.GetSavedState();
                Log($"✅ CameraStateManager save successful: {savedState}");
                saveSuccess = true;
            }
            catch (System.Exception ex)
            {
                Log($"❌ CameraStateManager save failed: {ex.Message}");
            }

            // Test restoration (outside try-catch to avoid yield error)
            if (saveSuccess)
            {
                yield return new WaitForSeconds(1f);

                try
                {
                    cameraStateManager.RestoreCameraState();
                    Log("✅ CameraStateManager restore triggered");
                }
                catch (System.Exception ex)
                {
                    Log($"❌ CameraStateManager restore failed: {ex.Message}");
                }
            }
        }

        // Step 4: Simulate actual scene transition
        if (!string.IsNullOrEmpty(testTargetScene))
        {
            Log($"🎬 Testing actual scene transition to: {testTargetScene}");
            yield return new WaitForSeconds(sceneTransitionDelay);

            // Note: Uncomment the line below to test actual scene loading
            // UnityEngine.SceneManagement.SceneManager.LoadScene(testTargetScene);
            Log("⚠️ Actual scene transition commented out for safety");
        }

        isTestRunning = false;
        Log("🧪 === SAFE SCENE TRANSITION TEST COMPLETED ===");
    }

    private IEnumerator TestSafeAccessMethods()
    {
        Log("🛡️ Testing all safe access methods...");

        // Test 1: Safe TopDownCameraController access
        bool controllerAccessSuccess = false;
        try
        {
            var controller = TopDownCameraController.Instance;
            if (controller != null && controller.gameObject != null)
            {
                var focus = controller.GetCurrentFocus();
                Log($"✅ Safe controller access: Focus = {(focus != null ? focus.name : "NULL")}");
                controllerAccessSuccess = true;
            }
            else
            {
                Log("⚠️ Controller not available for safe access test");
            }
        }
        catch (System.Exception ex)
        {
            Log($"❌ Safe controller access failed: {ex.Message}");
        }

        yield return new WaitForSeconds(0.1f);

        // Test 2: Safe CameraStateManager access
        bool stateManagerAccessSuccess = false;
        try
        {
            var stateManager = FindObjectOfType<CameraStateManager>();
            if (stateManager != null)
            {
                stateManager.SaveCurrentCameraState();
                Log("✅ Safe CameraStateManager access successful");
                stateManagerAccessSuccess = true;
            }
            else
            {
                Log("⚠️ CameraStateManager not found");
            }
        }
        catch (System.Exception ex)
        {
            Log($"❌ Safe CameraStateManager access failed: {ex.Message}");
        }

        yield return new WaitForSeconds(0.1f);

        // Test 3: Safe SceneTransitionManager access
        bool transitionManagerAccessSuccess = false;
        try
        {
            var transitionManager = SceneTransitionManager.Instance;
            if (transitionManager != null)
            {
                bool isTransitioning = transitionManager.IsTransitionInProgress();
                Log($"✅ Safe SceneTransitionManager access: IsTransitioning = {isTransitioning}");
                transitionManagerAccessSuccess = true;
            }
            else
            {
                Log("⚠️ SceneTransitionManager not found");
            }
        }
        catch (System.Exception ex)
        {
            Log($"❌ Safe SceneTransitionManager access failed: {ex.Message}");
        }

        Log($"🛡️ Safe access methods testing completed");
        Log($"   Controller: {(controllerAccessSuccess ? "✅" : "❌")}");
        Log($"   StateManager: {(stateManagerAccessSuccess ? "✅" : "❌")}");
        Log($"   TransitionManager: {(transitionManagerAccessSuccess ? "✅" : "❌")}");

        yield return null;
    }

    [ContextMenu("Debug: Print Current System State")]
    public void DebugPrintSystemState()
    {
        Log("🔍 === CURRENT SYSTEM STATE DEBUG ===");

        // TopDownCameraController state
        var controller = TopDownCameraController.Instance;
        Log($"TopDownCameraController: {(controller != null ? "FOUND" : "NULL")}");
        if (controller != null)
        {
            try
            {
                Log($"  GameObject Active: {controller.gameObject.activeInHierarchy}");
                Log($"  Component Enabled: {controller.enabled}");
                var focus = controller.GetCurrentFocus();
                Log($"  Current Focus: {(focus != null ? focus.name : "NULL")}");
            }
            catch (System.Exception ex)
            {
                Log($"  ERROR accessing controller: {ex.Message}");
            }
        }

        // CameraStateManager state
        var stateManager = FindObjectOfType<CameraStateManager>();
        Log($"CameraStateManager: {(stateManager != null ? "FOUND" : "NULL")}");
        if (stateManager != null)
        {
            try
            {
                var savedState = stateManager.GetSavedState();
                Log($"  Saved State: {savedState}");
                Log($"  Has Valid State: {stateManager.HasValidStateToRestore()}");
            }
            catch (System.Exception ex)
            {
                Log($"  ERROR accessing state manager: {ex.Message}");
            }
        }

        // SceneTransitionManager state
        var transitionManager = SceneTransitionManager.Instance;
        Log($"SceneTransitionManager: {(transitionManager != null ? "FOUND" : "NULL")}");
        if (transitionManager != null)
        {
            try
            {
                Log($"  Is Transitioning: {transitionManager.IsTransitionInProgress()}");
                Log($"  Current Object Type: {transitionManager.GetCurrentObjectType()}");
            }
            catch (System.Exception ex)
            {
                Log($"  ERROR accessing transition manager: {ex.Message}");
            }
        }

        Log("🔍 === SYSTEM STATE DEBUG COMPLETED ===");
    }

    private void Log(string message)
    {
        if (enableDetailedLogging)
        {
            Debug.Log($"[DestroyedObjectErrorTest] {message}");
        }
    }

    private void Update()
    {
        // Quick test hotkeys
        if (Input.GetKeyDown(KeyCode.F5))
        {
            TestSimulateDestroyedObjectError();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            TestSafeSceneTransition();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            DebugPrintSystemState();
        }
    }

    private void OnGUI()
    {
        if (isTestRunning)
        {
            GUI.Box(new Rect(10, Screen.height - 50, 300, 40), "DESTROYED OBJECT ERROR TEST RUNNING...");
        }

        GUI.BeginGroup(new Rect(10, Screen.height - 150, 400, 100));
        GUI.Box(new Rect(0, 0, 400, 100), "Destroyed Object Error Test");

        if (GUI.Button(new Rect(10, 25, 120, 20), "Simulate Error (F5)"))
        {
            TestSimulateDestroyedObjectError();
        }

        if (GUI.Button(new Rect(140, 25, 120, 20), "Safe Transition (F6)"))
        {
            TestSafeSceneTransition();
        }

        if (GUI.Button(new Rect(270, 25, 120, 20), "Debug State (F7)"))
        {
            DebugPrintSystemState();
        }

        GUI.Label(new Rect(10, 50, 380, 40), $"Focus Object: {(testFocusObject != null ? testFocusObject.name : "Not Set")}");

        GUI.EndGroup();
    }
}
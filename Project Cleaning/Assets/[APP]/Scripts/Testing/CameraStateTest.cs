using UnityEngine;

/// <summary>
/// Test script untuk memverifikasi Camera State Restoration functionality
/// Gunakan untuk testing dan debugging camera state system
/// </summary>
public class CameraStateTest : MonoBehaviour
{
    [Header("Test Objects")]
    [SerializeField] private Transform testFocusTarget;
    [SerializeField] private string testSceneName = "TestScene";

    [Header("Test Controls")]
    [SerializeField] private KeyCode saveStateKey = KeyCode.F1;
    [SerializeField] private KeyCode restoreStateKey = KeyCode.F2;
    [SerializeField] private KeyCode clearStateKey = KeyCode.F3;
    [SerializeField] private KeyCode simulateSceneTransitionKey = KeyCode.F4;

    private void Update()
    {
        // Test controls untuk debugging
        if (Input.GetKeyDown(saveStateKey))
        {
            TestSaveCameraState();
        }

        if (Input.GetKeyDown(restoreStateKey))
        {
            TestRestoreCameraState();
        }

        if (Input.GetKeyDown(clearStateKey))
        {
            TestClearCameraState();
        }

        if (Input.GetKeyDown(simulateSceneTransitionKey))
        {
            TestSimulateSceneTransition();
        }
    }

    [ContextMenu("Test: Save Camera State")]
    public void TestSaveCameraState()
    {
        Debug.Log("🧪 === TESTING CAMERA STATE SAVE ===");

        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        if (cameraStateManager != null)
        {
            // Set a test focus target if available
            if (testFocusTarget != null)
            {
                var topDownCamera = TopDownCameraController.Instance;
                if (topDownCamera != null)
                {
                    topDownCamera.SetFocusTarget(testFocusTarget);
                    Debug.Log($"✅ Test focus target set: {testFocusTarget.name}");
                }
            }

            cameraStateManager.SaveCurrentCameraState();
            var savedState = cameraStateManager.GetSavedState();
            Debug.Log($"✅ Saved state: {savedState}");
        }
        else
        {
            Debug.LogError("❌ CameraStateManager not found!");
        }

        Debug.Log("🧪 === TEST SAVE COMPLETED ===");
    }

    [ContextMenu("Test: Restore Camera State")]
    public void TestRestoreCameraState()
    {
        Debug.Log("🧪 === TESTING CAMERA STATE RESTORE ===");

        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        if (cameraStateManager != null)
        {
            var savedState = cameraStateManager.GetSavedState();
            Debug.Log($"📝 Current saved state: {savedState}");

            if (cameraStateManager.HasValidStateToRestore())
            {
                cameraStateManager.RestoreCameraState();
                Debug.Log("✅ Restoration triggered");
            }
            else
            {
                Debug.LogWarning("⚠️ No valid state to restore");
            }
        }
        else
        {
            Debug.LogError("❌ CameraStateManager not found!");
        }

        Debug.Log("🧪 === TEST RESTORE COMPLETED ===");
    }

    [ContextMenu("Test: Clear Camera State")]
    public void TestClearCameraState()
    {
        Debug.Log("🧪 === TESTING CAMERA STATE CLEAR ===");

        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        if (cameraStateManager != null)
        {
            cameraStateManager.ClearSavedState();
            Debug.Log("✅ Camera state cleared");
        }
        else
        {
            Debug.LogError("❌ CameraStateManager not found!");
        }

        Debug.Log("🧪 === TEST CLEAR COMPLETED ===");
    }

    [ContextMenu("Test: Simulate Scene Transition")]
    public void TestSimulateSceneTransition()
    {
        Debug.Log("🧪 === TESTING SCENE TRANSITION SIMULATION ===");

        // Step 1: Save current state
        TestSaveCameraState();

        // Step 2: Clear current focus (simulate scene unload)
        var topDownCamera = TopDownCameraController.Instance;
        if (topDownCamera != null)
        {
            topDownCamera.SetFocusTarget(null);
            topDownCamera.TransitionToOverview();
            Debug.Log("✅ Simulated scene unload (cleared focus, returned to overview)");
        }

        // Step 3: Wait and restore (simulate scene reload)
        StartCoroutine(SimulateSceneReload());

        Debug.Log("🧪 === SCENE TRANSITION SIMULATION STARTED ===");
    }

    private System.Collections.IEnumerator SimulateSceneReload()
    {
        Debug.Log("⏳ Simulating scene reload delay...");
        yield return new WaitForSeconds(2f);

        Debug.Log("🔄 Simulating scene loaded - triggering restoration");
        TestRestoreCameraState();

        Debug.Log("✅ Scene transition simulation completed");
    }

    [ContextMenu("Test: Print System Status")]
    public void TestPrintSystemStatus()
    {
        Debug.Log("🧪 === CAMERA STATE SYSTEM STATUS ===");

        // Check CameraStateManager
        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        Debug.Log($"CameraStateManager: {(cameraStateManager != null ? "FOUND" : "NOT FOUND")}");

        if (cameraStateManager != null)
        {
            var savedState = cameraStateManager.GetSavedState();
            Debug.Log($"Saved State: {savedState}");
            Debug.Log($"Has Valid State: {cameraStateManager.HasValidStateToRestore()}");
        }

        // Check TopDownCameraController
        var topDownCamera = TopDownCameraController.Instance;
        Debug.Log($"TopDownCameraController: {(topDownCamera != null ? "FOUND" : "NOT FOUND")}");

        if (topDownCamera != null)
        {
            var currentFocus = topDownCamera.GetCurrentFocus();
            Debug.Log($"Current Focus: {(currentFocus != null ? currentFocus.name : "NULL")}");
        }

        // Check SceneTransitionManager
        var sceneTransitionManager = SceneTransitionManager.Instance;
        Debug.Log($"SceneTransitionManager: {(sceneTransitionManager != null ? "FOUND" : "NOT FOUND")}");

        // Check GameModeManager
        var gameModeManager = GameModeManager.Instance;
        Debug.Log($"GameModeManager: {(gameModeManager != null ? "FOUND" : "NOT FOUND")}");
        if (gameModeManager != null)
        {
            Debug.Log($"Current Mode: {gameModeManager.GetCurrentMode()}");
        }

        Debug.Log("🧪 === STATUS CHECK COMPLETED ===");
    }

    private void OnGUI()
    {
        // Simple on-screen test controls
        GUI.BeginGroup(new Rect(10, 10, 300, 200));
        GUI.Box(new Rect(0, 0, 300, 200), "Camera State Test Controls");

        if (GUI.Button(new Rect(10, 30, 120, 25), $"Save State ({saveStateKey})"))
        {
            TestSaveCameraState();
        }

        if (GUI.Button(new Rect(140, 30, 150, 25), $"Restore State ({restoreStateKey})"))
        {
            TestRestoreCameraState();
        }

        if (GUI.Button(new Rect(10, 60, 120, 25), $"Clear State ({clearStateKey})"))
        {
            TestClearCameraState();
        }

        if (GUI.Button(new Rect(140, 60, 150, 25), $"Simulate Transition ({simulateSceneTransitionKey})"))
        {
            TestSimulateSceneTransition();
        }

        if (GUI.Button(new Rect(10, 90, 280, 25), "Print System Status"))
        {
            TestPrintSystemStatus();
        }

        // Status display
        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        bool hasValidState = cameraStateManager != null && cameraStateManager.HasValidStateToRestore();

        GUI.Label(new Rect(10, 125, 280, 20), $"CameraStateManager: {(cameraStateManager != null ? "✅" : "❌")}");
        GUI.Label(new Rect(10, 145, 280, 20), $"Has Valid State: {(hasValidState ? "✅" : "❌")}");

        var topDownCamera = TopDownCameraController.Instance;
        var currentFocus = topDownCamera?.GetCurrentFocus();
        GUI.Label(new Rect(10, 165, 280, 20), $"Current Focus: {(currentFocus != null ? currentFocus.name : "None")}");

        GUI.EndGroup();
    }
}
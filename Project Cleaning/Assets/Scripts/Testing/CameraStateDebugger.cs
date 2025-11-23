using UnityEngine;

/// <summary>
/// Debug script untuk memverifikasi CameraStateManager setup
/// Add script ini ke GameObject di scene untuk debugging
/// </summary>
public class CameraStateDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private KeyCode debugKey = KeyCode.F8;
    [SerializeField] private KeyCode saveTestKey = KeyCode.F9;
    [SerializeField] private KeyCode restoreTestKey = KeyCode.F10;

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            DebugCurrentSetup();
        }

        if (Input.GetKeyDown(saveTestKey))
        {
            TestSaveState();
        }

        if (Input.GetKeyDown(restoreTestKey))
        {
            TestRestoreState();
        }
    }

    [ContextMenu("Debug Current Setup")]
    public void DebugCurrentSetup()
    {
        Debug.Log("=== CAMERA STATE SYSTEM DEBUG ===");

        // Check 1: CameraStateManager Instance
        var cameraStateManager = CameraStateManager.Instance;
        Debug.Log($"1. CameraStateManager.Instance: {(cameraStateManager != null ? "✅ FOUND" : "❌ NULL")}");

        if (cameraStateManager != null)
        {
            Debug.Log($"   GameObject: {cameraStateManager.gameObject.name}");
            Debug.Log($"   Active: {cameraStateManager.gameObject.activeInHierarchy}");
            Debug.Log($"   Enabled: {cameraStateManager.enabled}");
            Debug.Log($"   Has Valid State: {cameraStateManager.HasValidStateToRestore()}");

            var savedState = cameraStateManager.GetSavedState();
            Debug.Log($"   Saved State: {savedState}");
        }

        // Check 2: Alternative search
        var allCameraStateManagers = FindObjectsOfType<CameraStateManager>();
        Debug.Log($"2. Total CameraStateManagers in scene: {allCameraStateManagers.Length}");
        for (int i = 0; i < allCameraStateManagers.Length; i++)
        {
            var csm = allCameraStateManagers[i];
            Debug.Log($"   [{i}] {csm.gameObject.name} - Active: {csm.gameObject.activeInHierarchy}");
        }

        // Check 3: TopDownCameraController
        var topDownCamera = TopDownCameraController.Instance;
        Debug.Log($"3. TopDownCameraController.Instance: {(topDownCamera != null ? "✅ FOUND" : "❌ NULL")}");

        if (topDownCamera != null)
        {
            try
            {
                var currentFocus = topDownCamera.GetCurrentFocus();
                Debug.Log($"   Current Focus: {(currentFocus != null ? currentFocus.name : "NULL")}");
                Debug.Log($"   GameObject: {topDownCamera.gameObject.name}");
                Debug.Log($"   Active: {topDownCamera.gameObject.activeInHierarchy}");
                Debug.Log($"   Enabled: {topDownCamera.enabled}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"   ERROR accessing TopDownCameraController: {ex.Message}");
            }
        }

        // Check 4: SceneTransitionManager
        var sceneTransitionManager = SceneTransitionManager.Instance;
        Debug.Log($"4. SceneTransitionManager.Instance: {(sceneTransitionManager != null ? "✅ FOUND" : "❌ NULL")}");

        // Check 5: GameModeManager
        var gameModeManager = GameModeManager.Instance;
        Debug.Log($"5. GameModeManager.Instance: {(gameModeManager != null ? "✅ FOUND" : "❌ NULL")}");
        if (gameModeManager != null)
        {
            Debug.Log($"   Current Mode: {gameModeManager.GetCurrentMode()}");
        }

        // Check 6: CameraAnimationController
        var cameraAnimationController = CameraAnimationController.Instance;
        Debug.Log($"6. CameraAnimationController.Instance: {(cameraAnimationController != null ? "✅ FOUND" : "❌ NULL")}");

        Debug.Log("=== END DEBUG ===");
    }

    [ContextMenu("Test Save State")]
    public void TestSaveState()
    {
        Debug.Log("=== TESTING CAMERA STATE SAVE ===");

        var cameraStateManager = CameraStateManager.Instance;
        if (cameraStateManager != null)
        {
            // First, check current camera focus
            var topDownCamera = TopDownCameraController.Instance;
            if (topDownCamera != null)
            {
                var currentFocus = topDownCamera.GetCurrentFocus();
                Debug.Log($"Current Camera Focus Before Save: {(currentFocus != null ? currentFocus.name : "NULL")}");
            }

            // Try to save state
            try
            {
                cameraStateManager.SaveCurrentCameraState();
                Debug.Log("✅ Save state called successfully");

                var savedState = cameraStateManager.GetSavedState();
                Debug.Log($"Saved State Result: {savedState}");
                Debug.Log($"Is Valid: {savedState.IsValid()}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Save state failed: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("❌ CameraStateManager.Instance is null! Setup required.");
        }

        Debug.Log("=== END SAVE TEST ===");
    }

    [ContextMenu("Test Restore State")]
    public void TestRestoreState()
    {
        Debug.Log("=== TESTING CAMERA STATE RESTORE ===");

        var cameraStateManager = CameraStateManager.Instance;
        if (cameraStateManager != null)
        {
            var savedState = cameraStateManager.GetSavedState();
            Debug.Log($"Current Saved State: {savedState}");

            if (savedState.IsValid())
            {
                try
                {
                    cameraStateManager.RestoreCameraState();
                    Debug.Log("✅ Restore state called successfully");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Restore state failed: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No valid saved state to restore");
            }
        }
        else
        {
            Debug.LogError("❌ CameraStateManager.Instance is null! Setup required.");
        }

        Debug.Log("=== END RESTORE TEST ===");
    }

    [ContextMenu("Test Manual Scene Workflow")]
    public void TestManualSceneWorkflow()
    {
        Debug.Log("=== TESTING MANUAL SCENE WORKFLOW ===");

        // Step 1: Focus on a test object
        var topDownCamera = TopDownCameraController.Instance;
        if (topDownCamera != null)
        {
            // Find any ClickableObject in scene
            var clickableObjects = FindObjectsOfType<ClickableObject>();
            if (clickableObjects.Length > 0)
            {
                var testObject = clickableObjects[0];
                Debug.Log($"Step 1: Setting focus to test object: {testObject.name}");

                try
                {
                    topDownCamera.SetFocusTarget(testObject.transform);
                    topDownCamera.TransitionToFocus();
                    Debug.Log("✅ Focus set successfully");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ Failed to set focus: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No ClickableObjects found in scene for testing");
            }
        }

        // Step 2: Save state (simulating scene transition)
        Debug.Log("Step 2: Saving camera state (simulating scene transition)");
        TestSaveState();

        // Step 3: Clear focus (simulating scene unload)
        if (topDownCamera != null)
        {
            Debug.Log("Step 3: Clearing focus (simulating scene unload)");
            topDownCamera.SetFocusTarget(null);
            topDownCamera.TransitionToOverview();
        }

        // Step 4: Wait and restore (simulating scene reload)
        Debug.Log("Step 4: Restoring camera state (simulating scene reload)");
        Invoke(nameof(TestRestoreState), 1f);

        Debug.Log("=== MANUAL WORKFLOW TEST INITIATED ===");
    }

    private void OnGUI()
    {
        GUI.BeginGroup(new Rect(10, 10, 350, 160));
        GUI.Box(new Rect(0, 0, 350, 160), "Camera State Debugger");

        if (GUI.Button(new Rect(10, 30, 150, 25), $"Debug Setup ({debugKey})"))
        {
            DebugCurrentSetup();
        }

        if (GUI.Button(new Rect(170, 30, 150, 25), $"Test Save ({saveTestKey})"))
        {
            TestSaveState();
        }

        if (GUI.Button(new Rect(10, 60, 150, 25), $"Test Restore ({restoreTestKey})"))
        {
            TestRestoreState();
        }

        if (GUI.Button(new Rect(170, 60, 150, 25), "Manual Workflow"))
        {
            TestManualSceneWorkflow();
        }

        // Status display
        var cameraStateManager = CameraStateManager.Instance;
        var statusText = cameraStateManager != null ? "✅ Ready" : "❌ Not Setup";
        GUI.Label(new Rect(10, 90, 330, 20), $"CameraStateManager: {statusText}");

        if (cameraStateManager != null)
        {
            var hasValidState = cameraStateManager.HasValidStateToRestore();
            GUI.Label(new Rect(10, 110, 330, 20), $"Has Valid State: {(hasValidState ? "✅ Yes" : "❌ No")}");

            var savedState = cameraStateManager.GetSavedState();
            GUI.Label(new Rect(10, 130, 330, 20), $"Saved Object: {(savedState.IsValid() ? savedState.focusedObjectName : "None")}");
        }
        else
        {
            GUI.Label(new Rect(10, 110, 330, 40), "❌ CameraStateManager not found!\nCreate GameObject with CameraStateManager script");
        }

        GUI.EndGroup();
    }
}
using UnityEngine;

/// <summary>
/// Setup script to automatically create CameraStateManager in the scene
/// Add this to a persistent object in your main menu scene
/// </summary>
public class CameraStateSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateCameraStateManager = true;
    [SerializeField] private bool enableDebugMode = true;

    private void Awake()
    {
        if (autoCreateCameraStateManager)
        {
            SetupCameraStateManager();
        }
    }

    private void SetupCameraStateManager()
    {
        // Check if CameraStateManager already exists
        CameraStateManager existingManager = FindObjectOfType<CameraStateManager>();

        if (existingManager == null)
        {
            // Create new CameraStateManager
            GameObject cameraStateManagerGO = new GameObject("CameraStateManager");
            CameraStateManager manager = cameraStateManagerGO.AddComponent<CameraStateManager>();

            // Configure debug mode
            var debugField = typeof(CameraStateManager).GetField("enableDebugLogs",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (debugField != null)
            {
                debugField.SetValue(manager, enableDebugMode);
            }

            Debug.Log("✅ CameraStateManager created automatically by CameraStateSetup");
        }
        else
        {
            Debug.Log("✅ CameraStateManager already exists in scene");
        }
    }

    [ContextMenu("Manual Setup CameraStateManager")]
    public void ManualSetupCameraStateManager()
    {
        SetupCameraStateManager();
    }

    [ContextMenu("Test Camera State Save")]
    public void TestCameraStateSave()
    {
        var manager = FindObjectOfType<CameraStateManager>();
        if (manager != null)
        {
            manager.SaveCurrentCameraState();
            Debug.Log("🧪 Test: Camera state saved");
        }
        else
        {
            Debug.LogError("❌ CameraStateManager not found for test");
        }
    }

    [ContextMenu("Test Camera State Restore")]
    public void TestCameraStateRestore()
    {
        var manager = FindObjectOfType<CameraStateManager>();
        if (manager != null)
        {
            manager.RestoreCameraState();
            Debug.Log("🧪 Test: Camera state restoration triggered");
        }
        else
        {
            Debug.LogError("❌ CameraStateManager not found for test");
        }
    }
}
using UnityEngine;
using Modules;

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

            AppLogger.Log("✅ CameraStateManager created automatically by CameraStateSetup");
        }
        else
        {
            AppLogger.Log("✅ CameraStateManager already exists in scene");
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
            AppLogger.Log("🧪 Test: Camera state saved");
        }
        else
        {
            AppLogger.LogError("❌ CameraStateManager not found for test");
        }
    }

    [ContextMenu("Test Camera State Restore")]
    public void TestCameraStateRestore()
    {
        var manager = FindObjectOfType<CameraStateManager>();
        if (manager != null)
        {
            manager.RestoreCameraState();
            AppLogger.Log("🧪 Test: Camera state restoration triggered");
        }
        else
        {
            AppLogger.LogError("❌ CameraStateManager not found for test");
        }
    }
}


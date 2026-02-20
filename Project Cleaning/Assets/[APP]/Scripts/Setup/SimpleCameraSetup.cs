using UnityEngine;
using Modules;

/// <summary>
/// AUTO-SETUP: Simple camera restoration system
/// Just attach this script - no manual setup required!
/// </summary>
public class SimpleCameraSetup : MonoBehaviour
{
    private void Awake()
    {
        // AUTO-SETUP: Create SimpleCameraFocusRestore if not exists
        var existing = FindObjectOfType<SimpleCameraFocusRestore>();
        if (existing == null)
        {
            // Create it automatically - no user intervention needed
            GameObject simpleCameraGO = new GameObject("SimpleCameraFocusRestore");
            simpleCameraGO.AddComponent<SimpleCameraFocusRestore>();

            AppLogger.Log("✅ AUTO-SETUP: SimpleCameraFocusRestore created automatically");
        }

        // Disable complex systems to prevent conflicts
        var cameraStateManager = FindObjectOfType<CameraStateManager>();
        if (cameraStateManager != null)
        {
            cameraStateManager.gameObject.SetActive(false);
            AppLogger.Log("🔧 AUTO-SETUP: Disabled conflicting CameraStateManager");
        }

        AppLogger.Log("🎯 SIMPLE CAMERA SYSTEM READY - No manual setup required!");

        // Destroy this setup script after auto-setup is complete
        Destroy(this);
    }
}


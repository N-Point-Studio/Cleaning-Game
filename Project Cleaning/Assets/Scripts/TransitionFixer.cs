using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple script to fix Easy Transitions scaling to match your screen
/// Just attach this to any GameObject in your scene
/// </summary>
public class TransitionFixer : MonoBehaviour
{
    [Header("Auto Fix Settings")]
    [SerializeField] private bool fixOnStart = true;
    [SerializeField] private float delayBeforeFix = 0.5f;

    [Header("Force Resolution Settings")]
    [SerializeField] private bool useForceResolution = true;
    [SerializeField] private Vector2 forceResolution = new Vector2(1920, 1080);

    private void Start()
    {
        if (fixOnStart)
        {
            Invoke(nameof(FixTransitionScaling), delayBeforeFix);
        }

        // Subscribe to transition events to fix new transitions
        SubscribeToTransitionEvents();

        // Start monitoring for new canvases being created
        StartCoroutine(MonitorForNewCanvases());
    }

    /// <summary>
    /// Continuously monitors for new transition canvases being created
    /// </summary>
    private System.Collections.IEnumerator MonitorForNewCanvases()
    {
        while (true)
        {
            // Check every 0.1 seconds for new transition canvases
            yield return new WaitForSeconds(0.1f);

            // Find all canvases and fix any new transitions
            CanvasScaler[] currentScalers = FindObjectsOfType<CanvasScaler>();

            foreach (CanvasScaler scaler in currentScalers)
            {
                if (IsTransitionCanvas(scaler) && NeedsScalingFix(scaler))
                {
                    Debug.Log($"Detected NEW transition canvas: {scaler.name} - Fixing immediately!");
                    FixSingleCanvas(scaler);
                }
            }
        }
    }

    /// <summary>
    /// Check if a canvas needs scaling fix
    /// </summary>
    private bool NeedsScalingFix(CanvasScaler scaler)
    {
        if (scaler == null) return false;

        Vector2 targetResolution = useForceResolution ? forceResolution : new Vector2(Screen.width, Screen.height);
        Vector2 currentResolution = scaler.referenceResolution;

        // Check if resolution is different from our target
        bool needsFix = Vector2.Distance(currentResolution, targetResolution) > 10f;

        return needsFix;
    }

    /// <summary>
    /// Fix a single canvas scaler
    /// </summary>
    private void FixSingleCanvas(CanvasScaler scaler)
    {
        Vector2 targetResolution = useForceResolution ? forceResolution : new Vector2(Screen.width, Screen.height);
        Vector2 oldResolution = scaler.referenceResolution;

        // Update the canvas scaler settings
        scaler.referenceResolution = targetResolution;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.matchWidthOrHeight = 0f; // Favor width scaling
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        Debug.Log($"FIXED CANVAS: {scaler.name} - Changed from {oldResolution} to {targetResolution}");

        // Force canvas update
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Main method to fix transition scaling issues
    /// </summary>
    public void FixTransitionScaling()
    {
        // Choose resolution based on settings
        Vector2 targetResolution;
        if (useForceResolution)
        {
            targetResolution = forceResolution;
        }
        else
        {
            targetResolution = new Vector2(Screen.width, Screen.height);
        }

        Debug.Log($"Fixing transition scaling...");
        Debug.Log($"Screen Resolution: {Screen.width}x{Screen.height}");
        Debug.Log($"Target Resolution: {targetResolution.x}x{targetResolution.y}");

        // Find all CanvasScaler components in the scene
        CanvasScaler[] allScalers = FindObjectsOfType<CanvasScaler>();
        int fixedCount = 0;

        foreach (CanvasScaler scaler in allScalers)
        {
            Canvas canvas = scaler.GetComponent<Canvas>();

            // Debug each canvas we find
            Debug.Log($"Found canvas: {scaler.name} - SortOrder: {canvas?.sortingOrder}, RenderMode: {canvas?.renderMode}, CurrentResolution: {scaler.referenceResolution}");

            if (IsTransitionCanvas(scaler))
            {
                Debug.Log($"Identified as TRANSITION canvas: {scaler.name}");

                // Store old settings
                Vector2 oldResolution = scaler.referenceResolution;

                // Use the single canvas fix method
                FixSingleCanvas(scaler);
                fixedCount++;
            }
        }

        Debug.Log($"=== SUMMARY: Fixed {fixedCount} transition canvases ===");
    }

    /// <summary>
    /// Determines if a CanvasScaler belongs to a transition
    /// </summary>
    private bool IsTransitionCanvas(CanvasScaler scaler)
    {
        if (scaler == null) return false;

        Canvas canvas = scaler.GetComponent<Canvas>();
        if (canvas == null) return false;

        // Check canvas properties that indicate it's a transition
        bool isOverlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;

        // Be more aggressive in detecting transition canvases
        bool hasHighSortingOrder = canvas.sortingOrder >= 100; // Lower threshold

        // Check naming convention (broader search)
        string name = scaler.name.ToLower();
        bool hasTransitionName = name.Contains("transition") ||
                                name.Contains("canvas") ||
                                name.Contains("easy") ||
                                name.Contains("template") ||
                                name.Contains("clone");

        // If it's overlay AND has either high sort order OR transition name, consider it a transition
        bool isLikelyTransition = isOverlay && (hasHighSortingOrder || hasTransitionName);

        // Also check if it has very specific resolution that indicates it's a transition asset
        Vector2 currentRes = scaler.referenceResolution;
        bool hasTransitionResolution = (currentRes.x == 960 && currentRes.y == 540) ||
                                     (currentRes.x == 1920 && currentRes.y == 1080);

        return isLikelyTransition || (isOverlay && hasTransitionResolution);
    }

    /// <summary>
    /// Subscribe to Easy Transitions events to auto-fix new transitions
    /// </summary>
    private void SubscribeToTransitionEvents()
    {
        try
        {
            if (EasyTransition.TransitionManager.Instance() != null)
            {
                EasyTransition.TransitionManager.Instance().onTransitionBegin += OnTransitionStart;
                Debug.Log("Subscribed to Easy Transitions events");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not subscribe to transition events: {e.Message}");
        }
    }

    /// <summary>
    /// Called when a new transition begins
    /// </summary>
    private void OnTransitionStart()
    {
        // Fix scaling after a short delay to ensure transition canvas is created
        Invoke(nameof(FixTransitionScaling), 0.1f);
    }

    /// <summary>
    /// Manual fix button for Inspector (right-click component)
    /// </summary>
    [ContextMenu("Fix Scaling Now")]
    public void FixScalingManual()
    {
        FixTransitionScaling();
    }

    /// <summary>
    /// Debug info about current screen and canvases
    /// </summary>
    [ContextMenu("Show Debug Info")]
    public void ShowDebugInfo()
    {
        Debug.Log($"=== TRANSITION FIXER DEBUG ===");
        Debug.Log($"Screen Resolution: {Screen.width}x{Screen.height}");

        CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>();
        Debug.Log($"Found {scalers.Length} CanvasScalers in scene:");

        foreach (CanvasScaler scaler in scalers)
        {
            Canvas canvas = scaler.GetComponent<Canvas>();
            bool isTransition = IsTransitionCanvas(scaler);

            Debug.Log($"- {scaler.name}: " +
                     $"Resolution={scaler.referenceResolution}, " +
                     $"SortOrder={canvas?.sortingOrder}, " +
                     $"RenderMode={canvas?.renderMode}, " +
                     $"IsTransition={isTransition}");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        try
        {
            if (EasyTransition.TransitionManager.Instance() != null)
            {
                EasyTransition.TransitionManager.Instance().onTransitionBegin -= OnTransitionStart;
            }
        }
        catch (System.Exception)
        {
            // Ignore errors during cleanup
        }
    }
}
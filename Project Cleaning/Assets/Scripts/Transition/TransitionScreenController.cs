using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple controller to swap transition visuals between "entering gameplay" and "exiting gameplay".
/// Attach to the Transition scene and assign two images + optional text.
/// </summary>
public class TransitionScreenController : MonoBehaviour
{
    public enum TransitionVisualMode
    {
        EnteringGameplay,
        ExitingGameplay
    }

    [Header("Visuals")]
    [SerializeField] private GameObject enteringRoot;   // Root container for entering visuals (image/anim)
    [SerializeField] private GameObject exitingRoot;    // Root container for exiting visuals (image/anim)
    [SerializeField] private TextMeshProUGUI enteringMessageText;
    [SerializeField] private TextMeshProUGUI exitingMessageText;

    [Header("Text")]
    [SerializeField] private string enteringText = "Preparing your journey...";
    [SerializeField] private string exitingText = "Returning to the museum...";

    [Header("Manual Override (optional)")]
    [SerializeField] private bool forceOverride = false;
    [SerializeField] private TransitionVisualMode forcedMode = TransitionVisualMode.EnteringGameplay;

    private void Start()
    {
        var mode = DetermineMode();
        ApplyMode(mode);
    }

    /// <summary>
    /// Decide mode based on SceneTransitionManager flag unless forced in inspector.
    /// </summary>
    private TransitionVisualMode DetermineMode()
    {
        if (forceOverride)
        {
            return forcedMode;
        }

        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.ShouldForceEnteringTransitionVisual())
        {
            return TransitionVisualMode.EnteringGameplay;
        }

        bool isExiting = false;
        if (SceneTransitionManager.Instance != null)
        {
            // Prefer explicit transition direction if available
            var direction = SceneTransitionManager.Instance.GetTransitionDirection();
            if (!string.IsNullOrEmpty(direction) && direction.Contains("ToMenu"))
            {
                isExiting = true;
            }
            else if (SceneTransitionManager.Instance.IsReturningFromGameplayFlag())
            {
                isExiting = true;
            }
        }

        return isExiting ? TransitionVisualMode.ExitingGameplay : TransitionVisualMode.EnteringGameplay;
    }

    /// <summary>
    /// Show/hide visuals and update text.
    /// </summary>
    private void ApplyMode(TransitionVisualMode mode)
    {
        bool entering = mode == TransitionVisualMode.EnteringGameplay;

        if (enteringRoot != null) enteringRoot.SetActive(entering);
        if (exitingRoot != null) exitingRoot.SetActive(!entering);

        if (enteringMessageText != null)
        {
            enteringMessageText.text = enteringText;
            enteringMessageText.gameObject.SetActive(entering);
        }

        if (exitingMessageText != null)
        {
            exitingMessageText.text = exitingText;
            exitingMessageText.gameObject.SetActive(!entering);
        }
    }

    // Convenience methods for testing from inspector context menu
    [ContextMenu("Set Entering Mode")]
    private void SetEnteringMode()
    {
        ApplyMode(TransitionVisualMode.EnteringGameplay);
    }

    [ContextMenu("Set Exiting Mode")]
    private void SetExitingMode()
    {
        ApplyMode(TransitionVisualMode.ExitingGameplay);
    }
}

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
    [SerializeField] private GameObject backToMenuRoot; // Optional root for back-to-menu visuals
    [SerializeField] private TextMeshProUGUI enteringMessageText;
    [SerializeField] private TextMeshProUGUI exitingMessageText;
    [SerializeField] private TextMeshProUGUI backToMenuMessageText;

    [Header("Text")]
    [SerializeField] private string enteringText = "Preparing your journey...";
    [SerializeField] private string exitingText = "Returning to the museum...";
    [SerializeField] private string backToMenuText = "Going back to the menu...";

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
        bool isBackToMenu = false;
        if (SceneTransitionManager.Instance != null)
        {
            isBackToMenu = SceneTransitionManager.Instance.GetBackToMenuFlag();

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

        // Back-to-menu uses exiting visuals; controller can choose which root to show
        return isExiting || isBackToMenu ? TransitionVisualMode.ExitingGameplay : TransitionVisualMode.EnteringGameplay;
    }

    /// <summary>
    /// Show/hide visuals and update text.
    /// </summary>
    private void ApplyMode(TransitionVisualMode mode)
    {
        bool entering = mode == TransitionVisualMode.EnteringGameplay;

        bool useBackToMenu = SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.GetBackToMenuFlag();
        bool showExiting = !entering && !useBackToMenu;

        if (enteringRoot != null) enteringRoot.SetActive(entering);
        if (exitingRoot != null) exitingRoot.SetActive(showExiting);
        if (backToMenuRoot != null) backToMenuRoot.SetActive(useBackToMenu);

        if (enteringMessageText != null)
        {
            enteringMessageText.text = enteringText;
            enteringMessageText.gameObject.SetActive(entering);
        }

        if (exitingMessageText != null)
        {
            exitingMessageText.text = exitingText;
            exitingMessageText.gameObject.SetActive(showExiting);
        }

        if (backToMenuMessageText != null)
        {
            backToMenuMessageText.text = backToMenuText;
            backToMenuMessageText.gameObject.SetActive(useBackToMenu);
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

using System;
using UnityEngine;

/// <summary>
/// Manages game mode transitions between Initial, Exploration, and Zoom modes
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public enum GameMode { Initial, Exploration, Zoom }

    [Header("Game Mode Settings")]
    [SerializeField] private GameMode currentGameMode = GameMode.Initial;

    // Events for mode changes
    public event Action<GameMode> OnModeChanged;
    public event Action OnEnterExplorationMode;
    public event Action OnEnterZoomMode;
    public event Action OnEnterInitialMode;

    // Singleton
    public static GameModeManager Instance { get; private set; }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Public Interface
    public GameMode GetCurrentMode() => currentGameMode;
    public bool IsInZoomMode() => currentGameMode == GameMode.Zoom;
    public bool IsInExplorationMode() => currentGameMode == GameMode.Exploration;
    public bool IsInInitialMode() => currentGameMode == GameMode.Initial;
    #endregion

    #region Mode Transitions
    public void StartExplorationMode()
    {
        if (currentGameMode != GameMode.Initial) return;

        ChangeMode(GameMode.Exploration);
        OnEnterExplorationMode?.Invoke();
    }

    public void EnterZoomMode()
    {
        Debug.Log($"=== ENTER ZOOM MODE CALLED: Current mode = {currentGameMode} ===");

        if (currentGameMode != GameMode.Exploration)
        {
            Debug.LogWarning($"=== ZOOM MODE BLOCKED: Can only enter zoom from Exploration mode, currently in {currentGameMode} ===");
            return;
        }

        Debug.Log("=== ZOOM MODE TRANSITION: Exploration -> Zoom ===");
        ChangeMode(GameMode.Zoom);
        OnEnterZoomMode?.Invoke();
    }

    public void ForceEnterZoomMode()
    {
        Debug.Log($"=== FORCE ENTER ZOOM MODE: Current mode = {currentGameMode} ===");
        ChangeMode(GameMode.Zoom);
        OnEnterZoomMode?.Invoke();
    }

    public void ReturnToExplorationMode()
    {
        Debug.Log($"=== RETURN TO EXPLORATION MODE CALLED: Current mode = {currentGameMode} ===");

        if (currentGameMode != GameMode.Zoom)
        {
            Debug.LogWarning($"=== EXPLORATION MODE BLOCKED: Can only return to exploration from Zoom mode, currently in {currentGameMode} ===");
            return;
        }

        Debug.Log("=== EXPLORATION MODE TRANSITION: Zoom -> Exploration ===");
        ChangeMode(GameMode.Exploration);
        OnEnterExplorationMode?.Invoke();
    }

    public void ExitToInitialMode()
    {
        ChangeMode(GameMode.Initial);
        OnEnterInitialMode?.Invoke();
    }

    private void ChangeMode(GameMode newMode)
    {
        GameMode previousMode = currentGameMode;
        currentGameMode = newMode;

        Debug.Log($"=== MODE CHANGED: {previousMode} -> {newMode} ===");
        Debug.LogWarning($"=== MODE CHANGE STACK TRACE ===\n{System.Environment.StackTrace}");

        OnModeChanged?.Invoke(newMode);
    }
    #endregion
}
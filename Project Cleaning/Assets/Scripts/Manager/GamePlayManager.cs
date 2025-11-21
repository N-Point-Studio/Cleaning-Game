using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    public static GamePlayManager Instance;

    [SerializeField] GameObject Environment;
    [SerializeField] Transform ClearInspect;
    [SerializeField] Camera ToolCamera;
    [SerializeField] ParticleSystem GlitterParticle;

    [Header("Object Type Configuration")]
    [SerializeField] private ObjectType currentGameplayObjectType = ObjectType.ChinaCoin;
    [SerializeField] private bool autoDetectObjectType = true;

    [Header("Scene Transition")]
    [SerializeField] private bool enableSceneTransition = true;
    [SerializeField] private float transitionDelayAfterFinish = 2f;

    public bool isGameFinished = false;
    private bool transitionTriggered = false;

    private readonly Vector3 FinishedPosition = new Vector3(0.0f, -5.5f, -9f);

    private void Awake()
    {
        Instance = this;
        TouchManager.Instance.DisableAllTouch(false);
        ToolCamera.enabled = true;

        // Auto-detect ObjectType if enabled
        if (autoDetectObjectType)
        {
            DetectObjectTypeFromScene();
        }
    }

    void Start()
    {
        Debug.Log($"=== GAMEPLAY MANAGER INITIALIZED ===");
        Debug.Log($"Current Object Type: {currentGameplayObjectType}");
        Debug.Log($"Scene Transition Enabled: {enableSceneTransition}");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Value: " + UIManager.Instance.GetAllProgressValue());
        FinishedGame();
    }

    private void FinishedGame()
    {
        if (UIManager.Instance.GetAllProgressValue() >= 100)
        {
            TouchManager.Instance.DisableAllTouch(true);
            UIManager.Instance.ShowFinishUI(true);
            UIManager.Instance.ShowFinishBackground(true);
            GlitterParticle.gameObject.SetActive(true);
            ToolCamera.enabled = false;


            Environment.transform.position = Vector3.Lerp(
                Environment.transform.position,
                FinishedPosition,
                Time.deltaTime * 2f
            );

            ClusterStateMachine cluster = AssembleManager.Instance.CurrentClusterInspected;
            Debug.Log("Cluster Finished: " + cluster.name);
            cluster.SwitchState(new ClusterFinishState(cluster));
            cluster.transform.position = Vector3.Lerp(cluster.transform.position, ClearInspect.position, Time.deltaTime * 2);

            cluster.transform.rotation = Quaternion.Slerp(
                cluster.transform.rotation,
                Quaternion.Euler(90f, 0f, 0f),
                Time.deltaTime * 2f
            );

            cluster.transform.SetParent(ClearInspect);
            isGameFinished = true;

            // Trigger scene transition after game is finished
            if (enableSceneTransition && !transitionTriggered)
            {
                transitionTriggered = true;
                StartCoroutine(TriggerSceneTransitionAfterDelay());
            }
        }
    }

    public bool IsGameFinished()
    {
        return isGameFinished;
    }

    /// <summary>
    /// Auto-detect ObjectType based on scene name or other indicators
    /// </summary>
    private void DetectObjectTypeFromScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();

        // Try to detect object type from scene name
        if (sceneName.Contains("coin") || sceneName.Contains("china coin"))
        {
            currentGameplayObjectType = ObjectType.ChinaCoin;
        }
        else if (sceneName.Contains("jar") || sceneName.Contains("china jar"))
        {
            currentGameplayObjectType = ObjectType.ChinaJar;
        }
        else if (sceneName.Contains("kendin") || sceneName.Contains("indonesia"))
        {
            currentGameplayObjectType = ObjectType.IndonesiaKendin;
        }
        else if (sceneName.Contains("winged") || sceneName.Contains("mesir"))
        {
            currentGameplayObjectType = ObjectType.MesirWingedScared;
        }

        Debug.Log($"Auto-detected ObjectType: {currentGameplayObjectType} from scene: {sceneName}");
    }

    /// <summary>
    /// Trigger scene transition after a delay when game is finished
    /// </summary>
    private IEnumerator TriggerSceneTransitionAfterDelay()
    {
        Debug.Log($"=== GAME FINISHED - Starting transition in {transitionDelayAfterFinish} seconds ===");

        // Wait for the specified delay
        yield return new WaitForSeconds(transitionDelayAfterFinish);

        // Ensure SceneTransitionManager exists
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("SceneTransitionManager not found! Creating one...");
            GameObject stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        Debug.Log($"=== TRIGGERING SCENE TRANSITION ===");
        Debug.Log($"Object Type: {currentGameplayObjectType}");
        Debug.Log($"Target Scene: New Start Game Sandy");

        // Trigger the scene transition with ContentSwitcher
        SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(currentGameplayObjectType);
    }

    /// <summary>
    /// Manual method to set ObjectType (can be called from UI or other scripts)
    /// </summary>
    public void SetObjectType(ObjectType objectType)
    {
        currentGameplayObjectType = objectType;
        Debug.Log($"ObjectType manually set to: {objectType}");
    }

    /// <summary>
    /// Get current ObjectType
    /// </summary>
    public ObjectType GetCurrentObjectType()
    {
        return currentGameplayObjectType;
    }

    /// <summary>
    /// Manual trigger for finish button (can be called from UI)
    /// </summary>
    public void TriggerFinishButton()
    {
        Debug.Log("=== FINISH BUTTON TRIGGERED MANUALLY ===");

        if (enableSceneTransition && !transitionTriggered)
        {
            transitionTriggered = true;
            StartCoroutine(TriggerSceneTransitionAfterDelay());
        }
        else
        {
            Debug.Log("Scene transition disabled or already triggered");
        }
    }

    /// <summary>
    /// Immediate scene transition (for testing or immediate transition)
    /// </summary>
    public void TriggerImmediateTransition()
    {
        Debug.Log("=== IMMEDIATE SCENE TRANSITION TRIGGERED ===");

        // Ensure SceneTransitionManager exists
        if (SceneTransitionManager.Instance == null)
        {
            GameObject stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(currentGameplayObjectType);
    }
}

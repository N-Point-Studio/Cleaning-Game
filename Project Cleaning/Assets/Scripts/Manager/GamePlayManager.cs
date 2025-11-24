using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyTransition;

public class GamePlayManager : MonoBehaviour
{
    public static GamePlayManager Instance;

    [SerializeField] GameObject Environment;
    [SerializeField] Transform ClearInspect;
    [SerializeField] Camera ToolCamera;
    [SerializeField] ParticleSystem GlitterParticle;

    [Header("Return Transition")]
    [SerializeField] private bool useStagedReturnTransition = true;
    [SerializeField] private string returnIntermediaryScene = "TransitionScreen";
    [SerializeField] private float returnIntermediaryDelay = 0.5f;
    [SerializeField] private TransitionSettings returnTransitionSettings;

    public bool isGameFinished = false;

    private Transform initialTransfromEnvironment;
    private Vector3 FinishedPosition = new Vector3(0.0f, -5.5f, -9f);


    private Quaternion finishedRotation = Quaternion.Euler(0f, 0f, 0f);
    private Vector3 finishedPosition = new Vector3(0f, 0f, 0f);

    private void Awake()
    {
        Instance = this;
        initialTransfromEnvironment = Environment.transform;
        TouchManager.Instance.DisableAllTouch(false);
        ToolCamera.enabled = true;
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Value: " + UIManager.Instance.GetAllProgressValue());
        FinishedGame();
    }

    private void FinishedGame()
    {
        Debug.Log("Check Finish Game " + AssembleManager.Instance.assemblyTargets.Count);
        if (UIManager.Instance.GetAllProgressValue() >= 100)
        {
            if (AssembleManager.Instance.assemblyTargets.Count > 0)
            {
                Debug.Log("ASSEMBLE A");
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

                cluster.transform.SetPositionAndRotation(
                    Vector3.Lerp(cluster.transform.position, ClearInspect.position, Time.deltaTime * 2),
                    Quaternion.Slerp(cluster.transform.rotation, Quaternion.Euler(90f, 0f, 0f), Time.deltaTime * 2f
                ));

                cluster.transform.SetParent(ClearInspect);
                isGameFinished = true;
            }
            else
            {
                Debug.Log("ASSEMBLE B");
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

                FragmentStateMachine cluster = AssembleManager.Instance.CurrentFragmentInspected;
                Debug.Log("Cluster Finished: " + cluster.name);
                cluster.SwitchState(new FragmentFinishState(cluster));
                // cluster.transform.position = Vector3.Lerp(cluster.transform.position, ClearInspect.position, Time.deltaTime * 2);

                // cluster.transform.rotation = Quaternion.Slerp(
                //     cluster.transform.rotation,
                //     Quaternion.Euler(-90f, -90f, -90f),
                //     Time.deltaTime * 2f
                // );

                cluster.transform.SetPositionAndRotation(
                    Vector3.Lerp(cluster.transform.position, ClearInspect.position, Time.deltaTime * 2),
                    Quaternion.Slerp(cluster.transform.rotation, Quaternion.Euler(-90f, -90f, -90f), Time.deltaTime * 2f)
                );

                cluster.transform.SetParent(ClearInspect);
                isGameFinished = true;
            }
        }
    }

    public bool IsGameFinished()
    {
        return isGameFinished;
    }

    /// <summary>
    /// Public entry point for finish button to re-run finish logic safely.
    /// </summary>
    public void TriggerFinishButton()
    {
        Debug.Log("=== FINISH BUTTON TRIGGERED FROM UI ===");

        // Pastikan state selesai di-set
        FinishedGame();

        // Lanjutkan transition ke menu
        StartSceneTransition();
    }

    private void StartSceneTransition()
    {
        // Pastikan SceneTransitionManager ada
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("SceneTransitionManager not found, creating one...");
            var stmGO = new GameObject("SceneTransitionManager");
            stmGO.AddComponent<SceneTransitionManager>();
        }

        // Deteksi ObjectType dari nama scene sebagai fallback
        ObjectType objectType = DetectObjectTypeFromScene();
        string targetScene = "New Start Game Sandy"; // main menu default

        Debug.Log($"Triggering transition to '{targetScene}' with ObjectType '{objectType}'");

        if (useStagedReturnTransition)
        {
            Debug.Log($"Using staged return transition via '{returnIntermediaryScene}' (delay {returnIntermediaryDelay}s)");
            SceneTransitionManager.Instance.MarkReturningFromGameplay();
            SceneTransitionManager.Instance.StartStagedTransition(
                returnIntermediaryScene,
                targetScene,
                returnIntermediaryDelay,
                returnTransitionSettings
            );
        }
        else
        {
            SceneTransitionManager.Instance.MarkReturningFromGameplay();
            SceneTransitionManager.Instance.TransitionToMainSceneWithContentSwitcher(objectType, targetScene, "GamePlayManager");
        }
    }

    private ObjectType DetectObjectTypeFromScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();

        if (sceneName.Contains("coin") || sceneName.Contains("china coin"))
        {
            return ObjectType.ChinaCoin;
        }
        else if (sceneName.Contains("jar") || sceneName.Contains("china jar"))
        {
            return ObjectType.ChinaJar;
        }
        else if (sceneName.Contains("kendin") || sceneName.Contains("indonesia"))
        {
            return ObjectType.IndonesiaKendin;
        }
        else if (sceneName.Contains("winged") || sceneName.Contains("mesir"))
        {
            return ObjectType.MesirWingedScared;
        }

        // Default fallback
        return ObjectType.ChinaCoin;
    }
}

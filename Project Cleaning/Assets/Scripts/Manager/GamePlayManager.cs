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
}

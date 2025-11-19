using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Separate Progress Bars")]
    [SerializeField] private ProgressBar progressDirts;
    [SerializeField] private ProgressBar progressDusts;
    [SerializeField] private ProgressBar progressAssemble;
    [SerializeField] private GameObject settingCanvas;
    [SerializeField] private Button ExitButton;
    [SerializeField] private Button ResumeButton;
    [SerializeField] private GameObject FinishUI;
    [SerializeField] private GameObject FinishBackground;
    private bool isSettingShown = false;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ExitButton.onClick.AddListener(ExitButtonInteract);
        ResumeButton.onClick.AddListener(ResumeButtonInteract);
    }

    private void Update()
    {
        ProgressUpdate();
    }

    private void ProgressUpdate()
    {
        float dustProgress = CleanManager.Instance.GetDustProgress();
        progressDusts.SetValue(dustProgress);

        float mudProgress = CleanManager.Instance.GetMudProgress();
        progressDirts.SetValue(mudProgress);

        float attachProgress = AssembleManager.Instance.GetAttachProgress();
        progressAssemble.SetValue(attachProgress);
    }

    public void ShowSetting(bool isShown)
    {
        settingCanvas.SetActive(isShown);
        TouchManager.Instance.TouchUsed(isShown);
    }

    public void ShowFinishUI(bool isShown)
    {
        FinishUI.SetActive(isShown);
    }

    public void ShowFinishBackground(bool isShown)
    {
        FinishBackground.SetActive(isShown);
    }

    public void ExitButtonInteract()
    {
        Debug.Log("Exit level");
    }

    public void ResumeButtonInteract()
    {
        Debug.Log("Exit level");
        ShowSetting(false);
    }

    public float GetAllProgressValue()
    {
        return (progressDirts.GetValue() + progressAssemble.GetValue() + progressDusts.GetValue()) / 3;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("References")]
    [SerializeField] private ProgressBar progressBar;

    private void Update()
    {
        float progress = (CleanManager.Instance.GetOverallProgress() + AssembleManager.Instance.GetAttachProgress()) / 3;
        Debug.Log("progress bar " + progress);
        // Debug.Log("progress assemble " + AssembleManager.Instance.GetTotalClusterProgress());
        progressBar.SetValue(progress);
    }


    private void Awake()
    {
        Instance = this;
    }

    public void SetProgressMax(int max)
    {
        progressBar.maximum = max;
        if (progressBar.current > max)
            progressBar.current = max;
    }

    public void SetProgressValue(int value)
    {
        progressBar.current = Mathf.Clamp(value, progressBar.minimum, progressBar.maximum);
    }

    public void AddProgress(int value)
    {
        SetProgressValue(progressBar.current + value);
    }

    public void SetProgressPercent(float percent)
    {
        percent = Mathf.Clamp01(percent);
        progressBar.current = Mathf.RoundToInt(progressBar.maximum * percent);
    }

    public void ResetProgress()
    {
        progressBar.current = progressBar.minimum;
    }

    public float GetProgressPercent()
    {
        if (progressBar.maximum <= 0) return 0f;
        return (float)progressBar.current / progressBar.maximum;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Separate Progress Bars")]
    [SerializeField] private ProgressBar progressDirts;   // For Mud
    [SerializeField] private ProgressBar progressDusts;   // For Texture/Dust cleaning
    [SerializeField] private ProgressBar progressAssemble; // For Assemble

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        // CLEANING (TEXTURE)
        float dustProgress = CleanManager.Instance.GetDustProgress();
        progressDusts.SetValue(dustProgress);

        // CLEANING (MUD / CLEANMESH)
        float mudProgress = CleanManager.Instance.GetMudProgress();
        progressDirts.SetValue(mudProgress);

        // ASSEMBLE PROGRESS
        float attachProgress = AssembleManager.Instance.GetAttachProgress();
        progressAssemble.SetValue(attachProgress);
    }
}

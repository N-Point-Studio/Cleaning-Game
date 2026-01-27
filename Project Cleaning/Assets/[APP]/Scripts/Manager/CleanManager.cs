using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanManager : MonoBehaviour
{
    public static CleanManager Instance;

    public List<Clean> allCleans = new List<Clean>();
    public List<CleanMesh> allCleanMud = new List<CleanMesh>();

    [Header("Progress (0 = kotor, 1 = bersih)")]
    [Range(0, 1f)]
    [SerializeField] private float progressClean = 0f;

    [Header("Progress (0 = kotor, 1 = bersih)")]
    [Range(0, 1f)]
    [SerializeField] private float progressCleanMud = 0f;

    public int totalTexture;
    public int totalMud;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;
    }

    private void Start()
    {
        totalTexture = allCleans.Count;
        totalMud = allCleanMud.Count;
    }
    private void Update()
    {
        allCleans.RemoveAll(item => item == null);
        allCleanMud.RemoveAll(item => item == null);
    }

    public void Register(Clean clean)
    {
        if (!allCleans.Contains(clean))
        {
            allCleans.Add(clean);
            totalTexture += 1;
        }
    }

    public void RegisterMud(CleanMesh clean, bool isRemove)
    {
        if (!allCleanMud.Contains(clean) && !isRemove)
        {
            allCleanMud.Add(clean);
            totalMud += 1;
        }
        else if (allCleanMud.Contains(clean))
        {
            allCleanMud.Remove(clean);
        }
    }

    public float GetOverallProgress()
    {
        int totalObjects = totalTexture + totalMud;
        if (totalObjects == 0) return 0f; // avoid instantly finishing when nothing is registered

        float cleanTotal = 0;

        foreach (var clean in allCleans)
        {
            cleanTotal += clean.GetDirtAmount();
        }

        if (totalTexture <= 0)
        {
            progressClean = 0f;
        }
        else
        {
            progressClean = Mathf.Clamp01(cleanTotal / totalTexture);
        }

        if (totalMud <= 0)
        {
            progressCleanMud = 0f;
        }
        else
        {
            progressCleanMud = Mathf.Clamp01(1f - (float)allCleanMud.Count / totalMud);
        }

        // var overallProgress = (progressClean + progressCleanMud) / 2;
        var overallProgress = progressClean + progressCleanMud;
        return overallProgress;
    }
    public float GetDustProgress()
    {
        // Return zero until textures are registered to prevent premature 100% progress
        if (totalTexture <= 0)
        {
            return 0f;
        }

        float cleanTotal = 0;

        foreach (var clean in allCleans)
        {
            cleanTotal += clean.GetDirtAmount();
        }
        progressClean = Mathf.Clamp01(cleanTotal / totalTexture);
        Debug.Log($"progress clean harusnya: {progressClean}");
        return progressClean;
    }

    public float GetMudProgress()
    {
        if (totalMud <= 0)
        {
            return 0f;
        }

        progressCleanMud = Mathf.Clamp01(1f - (float)allCleanMud.Count / totalMud);
        return progressCleanMud;
    }

    public void ShowingMudProgress()
    {
        var totalMud = allCleanMud.Count;
        if (totalMud == 0)
        {
            UIManager.Instance.ShowProgress(UIManager.ProgressType.Dirt, false);
        }
    }

    public void ResetForNewSession()
    {
        allCleans.Clear();
        allCleanMud.Clear();
        totalTexture = 0;
        totalMud = 0;
        progressClean = 0f;
        progressCleanMud = 0f;
        Debug.Log("[CleanManager] Reset state for new session.");
    }

    /// <summary>
    /// Rebuild tracking lists from scene objects (call after all fragments spawned).
    /// </summary>
    public void RebuildFromScene()
    {
        allCleans.Clear();
        allCleanMud.Clear();

        allCleans.AddRange(FindObjectsOfType<Clean>());
        allCleanMud.AddRange(FindObjectsOfType<CleanMesh>());

        totalTexture = allCleans.Count;
        totalMud = allCleanMud.Count;
        progressClean = 0f;
        progressCleanMud = 0f;

        Debug.Log($"[CleanManager] Rebuilt from scene. Texture={totalTexture}, Mud={totalMud}");
    }

}

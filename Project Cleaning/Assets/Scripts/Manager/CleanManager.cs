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
        if (totalObjects == 0) return 1f;

        float cleanTotal = 0;

        foreach (var clean in allCleans)
        {
            cleanTotal += clean.GetDirtAmount();
        }

        progressClean = Mathf.Clamp01(cleanTotal / totalTexture);
        progressCleanMud = Mathf.Clamp01(1f - (float)allCleanMud.Count / totalMud);

        // var overallProgress = (progressClean + progressCleanMud) / 2;
        var overallProgress = progressClean + progressCleanMud;
        return overallProgress;
    }
    public float GetDustProgress()
    {
        int totalObjects = totalTexture + totalMud;
        if (totalObjects == 0) return 1f;

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
        int totalObjects = totalTexture + totalMud;
        if (totalObjects == 0) return 1f;
        progressCleanMud = Mathf.Clamp01(1f - (float)allCleanMud.Count / totalMud);
        return progressCleanMud;
    }

}

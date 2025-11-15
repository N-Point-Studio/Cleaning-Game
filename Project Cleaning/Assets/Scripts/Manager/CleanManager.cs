using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanManager : MonoBehaviour
{
    public static CleanManager Instance;

    private List<Clean> allCleans = new List<Clean>();

    private void Awake()
    {
        Instance = this;
    }

    public void Register(Clean clean)
    {
        if (!allCleans.Contains(clean))
            allCleans.Add(clean);
    }

    public float GetOverallProgress()
    {
        if (allCleans.Count == 0)
            return 1f;

        float sum = 0f;

        foreach (var clean in allCleans)
        {
            sum += clean.GetDirtAmount();
        }

        // rata-rata
        return sum / allCleans.Count;
    }
}

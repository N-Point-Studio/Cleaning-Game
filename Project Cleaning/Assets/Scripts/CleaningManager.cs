using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleaningManager : MonoBehaviour
{
    public static CleaningManager Instance;

    public DirtySpot[] spots;        // array to hold all dirty spot references
    public Transform toolStartPoint; // default position for tool
    public GameObject cleaningTool;  // reference to the tool
    private int totalSpots;
    private int cleanedSpots;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find all spots if not assigned
        if (spots == null || spots.Length == 0)
        {
            spots = FindObjectsOfType<DirtySpot>();
        }

        totalSpots = spots.Length;

        ResetLevel();
    }

    public void SpotCleaned()
    {
        cleanedSpots++;
        Debug.Log("Spots cleaned: " + cleanedSpots + " / " + totalSpots);

        if (cleanedSpots >= totalSpots)
        {
            AllCleaned();
        }
    }

    void AllCleaned()
    {
        Debug.Log("All spots cleaned! Well done!");
        // Additional finish logic here
    }

    public void ResetLevel()
    {
        // Reset count
        cleanedSpots = 0;
    
        // Reset each spot
        foreach (DirtySpot spot in spots)
        {
           
        }

        // Reset tool position
        if (cleaningTool != null && toolStartPoint != null)
        {
            cleaningTool.transform.position = toolStartPoint.position;
            cleaningTool.transform.rotation = toolStartPoint.rotation;
        }
    }
}

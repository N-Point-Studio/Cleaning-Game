using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirtySpotSpawner : MonoBehaviour
{
    [Header("Spot prefab and settings")]
    public GameObject dirtySpotPrefab;      // assign your dirty spot object (with DirtySpot.cs etc)
    public int numberOfSpots = 10;          // how many to spawn
    public Vector3 spawnAreaCenter;         // centre point of spawn area
    public Vector3 spawnAreaSize = new Vector3(2f,0.5f,2f);  // area size (X, Y, Z)
    
    [Header("Size settings")]
    public Vector2 sizeRange = new Vector2(0.3f, 0.8f);  // min & max scale multiplier

    void Start()
    {
        SpawnSpots();
    }

    void SpawnSpots()
    {
        for (int i = 0; i < numberOfSpots; i++)
        {
            // random position inside area
            Vector3 pos = spawnAreaCenter 
                        + new Vector3(
                            (Random.value - 0.5f) * spawnAreaSize.x,
                            (Random.value - 0.5f) * spawnAreaSize.y,
                            (Random.value - 0.5f) * spawnAreaSize.z
                          );
            GameObject spot = Instantiate(dirtySpotPrefab, pos, Quaternion.identity, transform);
            
            // random size
            float s = Random.Range(sizeRange.x, sizeRange.y);
            spot.transform.localScale = new Vector3(s, s, s);
            
            // maybe random rotation so varied look
            spot.transform.rotation = Random.rotation;
            
            // ensure the prefab has DirtySpot component
        }
    }
}

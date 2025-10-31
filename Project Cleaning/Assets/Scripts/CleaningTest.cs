using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CleaningTest : MonoBehaviour
{
    public Texture2D heightMap;
    public Vector3 size = new Vector3(100, 10, 100);
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        int x = Mathf.FloorToInt(transform.position.x / size.x * heightMap.width);
        int z = Mathf.FloorToInt(transform.position.z / size.z * heightMap.height);
        Vector3 pos = transform.position;
        pos.y = heightMap.GetPixel(x, z).grayscale * size.y;
        transform.position = pos;
    }

    void Setup()
    {
        Texture2D texture = new Texture2D(128, 128);
        GetComponent<Renderer>().material.mainTexture = texture;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color color = ((x & y) != 0 ? Color.white : Color.gray);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }
}

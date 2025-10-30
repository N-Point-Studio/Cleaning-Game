using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubeBlocky : MonoBehaviour
{
    [SerializeField] private int width = 30;
    [SerializeField] private int height = 10;
    [SerializeField] private float resolution = 1f;
    [SerializeField] private float noiseScale = 0.1f;
    [SerializeField] private float heightThreshold = 0.5f;
    [SerializeField] private bool use3DNoise = false;

    private float[,,] heights;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        Generate();
    }

    void Generate()
    {
        SetHeights();
        BuildBlocks();
        SetMesh();
    }

    void SetHeights()
    {
        heights = new float[width + 1, height + 1, width + 1];

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                for (int z = 0; z <= width; z++)
                {
                    float val;
                    if (use3DNoise)
                        val = PerlinNoise3D(x * noiseScale, y * noiseScale, z * noiseScale);
                    else
                        val = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);

                    heights[x, y, z] = val;
                }
            }
        }
    }

    void BuildBlocks()
    {
        vertices.Clear();
        triangles.Clear();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < width; z++)
                {
                    if (heights[x, y, z] > heightThreshold)
                        AddCube(new Vector3(x, y, z) * resolution);
                }
            }
        }
    }

    void AddCube(Vector3 pos)
    {
        int vertStart = vertices.Count;

        // 8 corners of cube
        Vector3[] cubeVerts = new Vector3[]
        {
            pos + new Vector3(0, 0, 0),
            pos + new Vector3(1, 0, 0),
            pos + new Vector3(1, 1, 0),
            pos + new Vector3(0, 1, 0),
            pos + new Vector3(0, 0, 1),
            pos + new Vector3(1, 0, 1),
            pos + new Vector3(1, 1, 1),
            pos + new Vector3(0, 1, 1)
        };

        vertices.AddRange(cubeVerts);

        // Triangles for each cube face (6 faces * 2 triangles)
        int[] faceTris = {
            0, 2, 1, 0, 3, 2, // front
            5, 6, 4, 6, 7, 4, // back
            3, 7, 2, 7, 6, 2, // top
            0, 1, 4, 1, 5, 4, // bottom
            4, 7, 0, 7, 3, 0, // left
            1, 2, 5, 2, 6, 5  // right
        };

        for (int i = 0; i < faceTris.Length; i++)
            triangles.Add(vertStart + faceTris[i]);
    }

    void SetMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
    }

    float PerlinNoise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);
        return (xy + xz + yz + yx + zx + zy) / 6;
    }
}

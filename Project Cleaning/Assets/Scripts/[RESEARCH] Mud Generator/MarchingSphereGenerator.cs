using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingSphereGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int size = 32;
    public float cubeSize = 1f;

    [Header("Sphere Settings")]
    public float radius = 10f;
    public float isoLevel = 0.5f;

    private float[,,] densityGrid;
    private List<Vector3> vertices;
    private List<int> triangles;

    void Start()
    {
        GenerateVoxelField();
        GenerateMesh();
    }

    void GenerateVoxelField()
    {
        densityGrid = new float[size + 1, size + 1, size + 1];
        Vector3 center = new Vector3(size / 2f, size / 2f, size / 2f);

        for (int x = 0; x <= size; x++)
        {
            for (int y = 0; y <= size; y++)
            {
                for (int z = 0; z <= size; z++)
                {
                    Vector3 pos = new Vector3(x, y, z);
                    float distance = Vector3.Distance(pos, center);
                    densityGrid[x, y, z] = radius - distance; // inside sphere = positive
                }
            }
        }
    }

    void GenerateMesh()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    MarchCube(x, y, z);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void MarchCube(int x, int y, int z)
    {
        float[] cube = new float[8];
        Vector3[] cornerPositions = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            Vector3Int corner = TableMarching.Points[i];
            Vector3 worldPos = new Vector3(x + corner.x, y + corner.y, z + corner.z);
            cube[i] = densityGrid[x + corner.x, y + corner.y, z + corner.z];
            cornerPositions[i] = worldPos * cubeSize;
        }

        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cube[i] < isoLevel)
                cubeIndex |= 1 << i;
        }

        // no intersection
        if (cubeIndex == 0 || cubeIndex == 255)
            return;

        for (int i = 0; i < 16; i += 3)
        {
            int a0 = TableMarching.TriTable[cubeIndex, i];
            if (a0 == -1)
                break;

            int a1 = TableMarching.TriTable[cubeIndex, i + 1];
            int a2 = TableMarching.TriTable[cubeIndex, i + 2];

            Vector3 v1 = InterpVerts(cornerPositions, cube, a0);
            Vector3 v2 = InterpVerts(cornerPositions, cube, a1);
            Vector3 v3 = InterpVerts(cornerPositions, cube, a2);

            int startIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            triangles.Add(startIndex);
            triangles.Add(startIndex + 1);
            triangles.Add(startIndex + 2);
        }
    }

    Vector3 InterpVerts(Vector3[] cornerPos, float[] cube, int edgeIndex)
    {
        Vector2Int edge = TableMarching.Edges[edgeIndex];
        Vector3 p1 = cornerPos[edge.x];
        Vector3 p2 = cornerPos[edge.y];
        float v1 = cube[edge.x];
        float v2 = cube[edge.y];

        float t = (isoLevel - v1) / (v2 - v1);
        return p1 + t * (p2 - p1);
    }
}

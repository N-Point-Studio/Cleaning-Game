using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class VoxelGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    public int xSize = 20;
    public int zSize = 20;

    [Header("Torus Settings")]
    public float R = 3f; // Main radius
    public float r = 1f; // Tube radius
    public int segMain = 30; // Main circle segments
    public int segTube = 20; // Tube circle segments

    [Header("Destruction Settings")]
    public float destroyRadius = 1f;
    public Camera playerCamera; // Assign your camera here

    private MeshCollider meshCollider;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();

        CreateTorus(R, r, segMain, segTube); // Or call CreateShape() for terrain
        UpdateCollider();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                DigMesh(hit.point, destroyRadius);
            }
        }
    }

    // ---------------- Terrain ----------------
    void CreateShape()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * .3f, z * .3f) * 2f;
                vertices[i] = new Vector3(x, y, z);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6];

        int vert = 0;
        int tris = 0;

        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        UpdateMesh();
    }

    // ---------------- Torus ----------------
    void CreateTorus(float R, float r, int segMain, int segTube)
    {
        vertices = new Vector3[(segMain + 1) * (segTube + 1)];
        triangles = new int[segMain * segTube * 6];

        for (int i = 0; i <= segMain; i++)
        {
            float u = i * Mathf.PI * 2 / segMain;
            for (int j = 0; j <= segTube; j++)
            {
                float v = j * Mathf.PI * 2 / segTube;
                float x = (R + r * Mathf.Cos(v)) * Mathf.Cos(u);
                float y = r * Mathf.Sin(v);
                float z = (R + r * Mathf.Cos(v)) * Mathf.Sin(u);

                vertices[i * (segTube + 1) + j] = new Vector3(x, y, z);
            }
        }

        int tris = 0;
        for (int i = 0; i < segMain; i++)
        {
            for (int j = 0; j < segTube; j++)
            {
                int current = i * (segTube + 1) + j;
                int next = (i + 1) * (segTube + 1) + j;

                // Correct winding order
                triangles[tris++] = current;
                triangles[tris++] = current + 1;
                triangles[tris++] = next;

                triangles[tris++] = current + 1;
                triangles[tris++] = next + 1;
                triangles[tris++] = next;
            }
        }

        UpdateMesh();
        UpdateCollider();
    }

    // ---------------- Dig Mesh ----------------
    void DigMesh(Vector3 point, float radius)
    {
        List<int> newTriangles = new List<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            Vector3 v0 = transform.TransformPoint(vertices[i0]);
            Vector3 v1 = transform.TransformPoint(vertices[i1]);
            Vector3 v2 = transform.TransformPoint(vertices[i2]);

            // Keep triangle only if all vertices are outside radius
            if ((v0 - point).magnitude > radius &&
                (v1 - point).magnitude > radius &&
                (v2 - point).magnitude > radius)
            {
                newTriangles.Add(i0);
                newTriangles.Add(i1);
                newTriangles.Add(i2);
            }
        }

        triangles = newTriangles.ToArray();
        UpdateMesh();
        UpdateCollider();
    }

    // ---------------- Update Mesh ----------------
    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void UpdateCollider()
    {
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

    // ---------------- Gizmos ----------------
    void OnDrawGizmos()
    {
        if (vertices == null) return;

        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.05f);
        }
    }
}

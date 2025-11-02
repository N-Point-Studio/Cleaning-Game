using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class VoxelGenerator : MonoBehaviour
{
    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;

    // [Header("Generation Settings")]
    public enum Mode { Terrain, Torus, ImportedMesh }
    public Mode generationMode = Mode.Terrain;

    [Header("Terrain Settings")]
    public int xSize = 20;
    public int zSize = 20;
    public float terrainNoiseScale = 0.3f;
    public float terrainHeight = 2f;

    [Header("Torus Settings")]
    public float R = 3f; // Main radius
    public float r = 1f; // Tube radius
    public int segMain = 30;
    public int segTube = 20;

    [Header("Imported Mesh Settings")]
    public MeshFilter importedMeshFilter; // Drag your FBX/OBJ here

    [Header("Destruction Settings")]
    public float digRadius = 1f;
    public float digStrength = 0.5f; // How much to move vertices
    public Camera playerCamera; // Assign main camera here

    private MeshCollider meshCollider;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshCollider = GetComponent<MeshCollider>();

        switch (generationMode)
        {
            case Mode.Terrain:
                CreateTerrain();
                break;
            case Mode.Torus:
                CreateTorus(R, r, segMain, segTube);
                break;
            case Mode.ImportedMesh:
                if (importedMeshFilter != null)
                    ConvertImportedMesh(importedMeshFilter.mesh);
                else
                    Debug.LogError("No imported mesh assigned!");
                break;
        }

        UpdateCollider();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                DigVertices(hit.point, digRadius, digStrength);
            }
        }
    }

    // ---------------- Terrain ----------------
    void CreateTerrain()
    {
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                float y = Mathf.PerlinNoise(x * terrainNoiseScale, z * terrainNoiseScale) * terrainHeight;
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

                // Correct outward winding
                triangles[tris++] = current;
                triangles[tris++] = current + 1;
                triangles[tris++] = next;

                triangles[tris++] = current + 1;
                triangles[tris++] = next + 1;
                triangles[tris++] = next;
            }
        }

        UpdateMesh();
    }

    // ---------------- Convert Imported Mesh ----------------
    void ConvertImportedMesh(Mesh importedMesh)
    {
        vertices = importedMesh.vertices;
        triangles = importedMesh.triangles;
        UpdateMesh();
    }

    // ---------------- Dig Vertices Smoothly ----------------
    void DigVertices(Vector3 point, float radius, float strength)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(worldVertex, point);

            if (distance < radius)
            {
                float falloff = 1 - (distance / radius); // smooth
                vertices[i] += Vector3.down * strength * falloff;
            }
        }

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

    void OnDrawGizmos()
    {
        if (vertices == null) return;

        for (int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(transform.TransformPoint(vertices[i]), 0.05f);
        }
    }
}

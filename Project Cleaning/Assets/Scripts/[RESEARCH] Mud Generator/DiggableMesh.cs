using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class DiggableMarchingCubes : MonoBehaviour
{
    [Header("Voxel Field Settings")]
    public int width = 20;
    public int height = 20;
    public int depth = 20;
    public float cubeSize = 1f;
    public float isoLevel = 0.5f; // Threshold for marching cubes

    [Header("Dig Settings")]
    public float digRadius = 2f;
    public float digStrength = 1f;

    private float[,,] voxelField;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        voxelField = new float[width + 1, height + 1, depth + 1];
        InitializeVoxelField();
        UpdateMesh();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                DiggableMarchingCubes diggable = hit.collider.GetComponent<DiggableMarchingCubes>();
                if (diggable != null)
                {
                    diggable.Dig(hit.point);
                }
            }
        }
    }


    void InitializeVoxelField()
    {
        // Fill voxels: 1 = solid, 0 = empty
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                for (int z = 0; z <= depth; z++)
                {
                    voxelField[x, y, z] = 1f; // full solid
                }
            }
        }

        // Example: put an object inside as "empty"
        for (int x = 8; x <= 12; x++)
        {
            for (int y = 8; y <= 12; y++)
            {
                for (int z = 8; z <= 12; z++)
                {
                    voxelField[x, y, z] = 0f; // hollow space
                }
            }
        }
    }

    void UpdateMesh()
    {
        MarchingCubesMeshGenerator generator = new MarchingCubesMeshGenerator();
        Mesh mesh = generator.GenerateMesh(voxelField, cubeSize, isoLevel);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void Dig(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        int radiusInVoxels = Mathf.CeilToInt(digRadius / cubeSize);

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                for (int z = 0; z <= depth; z++)
                {
                    Vector3 voxelPos = new Vector3(x, y, z) * cubeSize;
                    if (Vector3.Distance(voxelPos, localPos) <= digRadius)
                    {
                        voxelField[x, y, z] -= digStrength * Time.deltaTime;
                        voxelField[x, y, z] = Mathf.Clamp01(voxelField[x, y, z]);
                    }
                }
            }
        }

        UpdateMesh();
    }
}

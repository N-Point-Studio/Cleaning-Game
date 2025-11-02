using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class MudBlock : MonoBehaviour
{
    public int resolution = 16;
    public float size = 1f;
    public float isoLevel = 0.5f;

    private float[,,] density;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        density = new float[resolution + 1, resolution + 1, resolution + 1];
        for (int x = 0; x <= resolution; x++)
            for (int y = 0; y <= resolution; y++)
                for (int z = 0; z <= resolution; z++)
                    density[x, y, z] = 1f;

        GenerateMesh();
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider == meshCollider)
                {
                    Vector3 localPos = transform.InverseTransformPoint(hit.point);
                    RemoveMud(localPos, 0.1f);
                    GenerateMesh();
                }
            }
        }
    }

    void RemoveMud(Vector3 localPos, float radius)
    {
        int r = Mathf.CeilToInt(radius * resolution);
        for (int x = 0; x <= resolution; x++)
            for (int y = 0; y <= resolution; y++)
                for (int z = 0; z <= resolution; z++)
                {
                    Vector3 p = new Vector3(x, y, z) / resolution * size;
                    float dist = Vector3.Distance(p, localPos);
                    if (dist < radius)
                    {
                        density[x, y, z] = 0f; // carve out
                    }
                }
    }

    void GenerateMesh()
    {
        MarchingCubes mc = new MarchingCubes(isoLevel);
        mc.Generate(density, size);
        meshFilter.mesh = mc.CreateMesh();
        meshCollider.sharedMesh = meshFilter.mesh;
    }
}

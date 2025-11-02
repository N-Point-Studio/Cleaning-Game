using UnityEngine;
using System.Collections.Generic;

public class MarchingCubes
{
    private float isoLevel;
    private List<Vector3> vertices;
    private List<int> triangles;
    private float[,,] field;
    private float cubeSize;

    public MarchingCubes(float isoLevel)
    {
        this.isoLevel = isoLevel;
    }

    public void Generate(float[,,] field, float cubeSize)
    {
        this.field = field;
        this.cubeSize = cubeSize;
        vertices = new List<Vector3>();
        triangles = new List<int>();

        int resX = field.GetLength(0) - 1;
        int resY = field.GetLength(1) - 1;
        int resZ = field.GetLength(2) - 1;

        for (int x = 0; x < resX; x++)
            for (int y = 0; y < resY; y++)
                for (int z = 0; z < resZ; z++)
                    Polygonize(x, y, z);
    }

    private void Polygonize(int x, int y, int z)
    {
        // Simplified cube fill: create a cube if above isoLevel
        if (field[x, y, z] > isoLevel)
        {
            float s = cubeSize / (field.GetLength(0) - 1);
            Vector3 pos = new Vector3(x, y, z) * s;
            AddCube(pos, s);
        }
    }

    private void AddCube(Vector3 pos, float s)
    {
        Vector3[] verts = new Vector3[]
        {
            pos + new Vector3(0,0,0), pos + new Vector3(s,0,0),
            pos + new Vector3(s,s,0), pos + new Vector3(0,s,0),
            pos + new Vector3(0,0,s), pos + new Vector3(s,0,s),
            pos + new Vector3(s,s,s), pos + new Vector3(0,s,s)
        };

        int[] tris = {
            0,2,1, 0,3,2, 4,5,6, 4,6,7,
            0,1,5, 0,5,4, 2,3,7, 2,7,6,
            0,4,7, 0,7,3, 1,2,6, 1,6,5
        };

        int start = vertices.Count;
        vertices.AddRange(verts);
        foreach (int t in tris) triangles.Add(start + t);
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        return mesh;
    }
}

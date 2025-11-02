using System.Collections.Generic;
using UnityEngine;

public class MarchingCubesMeshGenerator
{
    public Mesh GenerateMesh(float[,,] field, float cubeSize, float isoLevel)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int width = field.GetLength(0) - 1;
        int height = field.GetLength(1) - 1;
        int depth = field.GetLength(2) - 1;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    MarchCube(x, y, z, field, cubeSize, isoLevel, vertices, triangles);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // for large meshes
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    void MarchCube(int x, int y, int z, float[,,] field, float cubeSize, float isoLevel, List<Vector3> vertices, List<int> triangles)
    {
        // Minimal placeholder: create a cube if voxel is solid
        if (field[x, y, z] > isoLevel)
        {
            Vector3 basePos = new Vector3(x, y, z) * cubeSize;
            int vStart = vertices.Count;

            vertices.Add(basePos);
            vertices.Add(basePos + new Vector3(cubeSize, 0, 0));
            vertices.Add(basePos + new Vector3(cubeSize, cubeSize, 0));
            vertices.Add(basePos + new Vector3(0, cubeSize, 0));
            vertices.Add(basePos + new Vector3(0, 0, cubeSize));
            vertices.Add(basePos + new Vector3(cubeSize, 0, cubeSize));
            vertices.Add(basePos + new Vector3(cubeSize, cubeSize, cubeSize));
            vertices.Add(basePos + new Vector3(0, cubeSize, cubeSize));

            // 12 triangles (two per face)
            int[] cubeTris = {
                0,2,1, 0,3,2,
                1,2,6, 6,5,1,
                4,5,6, 6,7,4,
                2,3,7, 7,6,2,
                0,7,3, 0,4,7,
                0,1,5, 0,5,4
            };
            for (int i = 0; i < cubeTris.Length; i++)
            {
                triangles.Add(vStart + cubeTris[i]);
            }
        }
    }
}

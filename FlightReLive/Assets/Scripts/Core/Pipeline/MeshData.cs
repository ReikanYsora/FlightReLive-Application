using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlightReLive.Core.Pipeline
{
    [Serializable]
    public class MeshData
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<Vector2> uvs2 = new List<Vector2>();
        public List<int> triangles = new List<int>();
        public List<Vector3> normals = new List<Vector3>();

        public Mesh ConvertToUnityMesh(MeshType meshType)
        {
            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            mesh.indexFormat = IndexFormat.UInt32;
            Vector3 center = CalculateMeshCenter(vertices);
            List<Vector3> centeredVertices = vertices.Select(v => v - center).ToList();

            switch (meshType)
            {
                default:
                case MeshType.Triangles:
                    mesh.SetVertices(centeredVertices);
                    mesh.SetTriangles(triangles, 0);
                    mesh.SetUVs(0, uvs);
                    mesh.SetUVs(1, uvs2);
                    mesh.SetNormals(normals);
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                    break;
                case MeshType.Point:

                    int[] indices = new int[centeredVertices.Count];

                    for (int i = 0; i < indices.Length; i++)
                    {
                        indices[i] = i;
                    }

                    mesh.SetVertices(centeredVertices);
                    mesh.SetIndices(indices, MeshTopology.Points, 0);
                    mesh.uv = uvs2.ToArray();
                    mesh.normals = normals.ToArray();
                    mesh.RecalculateBounds();
                    break;
            }

            return mesh;
        }

        private Vector3 CalculateMeshCenter(List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count == 0)
            {
                return Vector3.zero;
            }

            float sumX = 0f;
            float sumZ = 0f;

            foreach (var v in vertices)
            {
                sumX += v.x;
                sumZ += v.z;
            }

            float centerX = sumX / vertices.Count;
            float centerZ = sumZ / vertices.Count;

            return new Vector3(centerX, 0f, centerZ);
        }

    }

    public enum MeshType
    {
        Triangles = 0,
        Point = 1
    }
}

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

        public Mesh ConvertToUnityMesh()
        {
            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            mesh.indexFormat = IndexFormat.UInt32;
            Vector3 center = CalculateMeshCenter(vertices);
            mesh.SetVertices(vertices.Select(v => v - center).ToList());
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uvs2);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
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

            foreach (Vector3 v in vertices)
            {
                sumX += v.x;
                sumZ += v.z;
            }

            float centerX = sumX / vertices.Count;
            float centerZ = sumZ / vertices.Count;

            return new Vector3(centerX, 0f, centerZ);
        }

    }
}

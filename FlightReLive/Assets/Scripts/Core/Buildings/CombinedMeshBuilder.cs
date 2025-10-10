using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlightReLive.Core.OpenVectorTile
{
    internal sealed class CombinedMeshBuilder
    {
        #region ATTRIBUTES
        internal readonly List<Vector3> Vertices = new List<Vector3>();
        internal readonly List<Vector3> Normals = new List<Vector3>();
        internal readonly List<Vector2> UVs = new List<Vector2>();
        internal readonly List<int> Indices = new List<int>();
        #endregion

        #region PROPERTIES
        internal bool HasData => Vertices.Count > 0 && Indices.Count > 0;
        #endregion

        #region METHODS
        internal void AddUnityMesh(Mesh src, Vector3 worldOffset)
        {
            int baseIndex = Vertices.Count;

            Vector3[] vtx = src.vertices;
            Vector3[] nrm = src.normals;
            Vector2[] uv0 = src.uv;
            int[] tri = src.triangles;

            for (int i = 0; i < vtx.Length; i++)
            {
                Vertices.Add(vtx[i] + worldOffset);
                Normals.Add((nrm != null && nrm.Length == vtx.Length) ? nrm[i] : Vector3.up);
                UVs.Add((uv0 != null && uv0.Length == vtx.Length) ? uv0[i] : Vector2.zero);
            }

            for (int i = 0; i < tri.Length; i++)
            {
                Indices.Add(baseIndex + tri[i]);
            }
        }

        internal Mesh ToMesh()
        {
            Mesh m = new Mesh { indexFormat = IndexFormat.UInt32 };
            m.SetVertices(Vertices);
            m.SetNormals(Normals);
            m.SetUVs(0, UVs);
            m.SetTriangles(Indices, 0, true);
            m.RecalculateBounds();

            return m;
        }

        internal void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            UVs.Clear();
            Indices.Clear();
        }
        #endregion
    }
}

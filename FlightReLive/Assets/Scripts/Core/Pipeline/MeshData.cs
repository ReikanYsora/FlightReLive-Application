using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace FlightReLive.Core.Pipeline
{
    [Serializable]
    public class MeshData
    {
        #region ATTRIBUTES
        public NativeArray<Vector3> vertices;
        public NativeArray<Vector2> uvs;
        public NativeArray<int> triangles;
        public NativeArray<Vector3> normals;
        #endregion

        #region METHODS
        /// <summary>
        /// Builds a Unity Mesh from stored data, centered on the mesh centroid.
        /// Works directly with NativeArrays (zero copy, no GC).
        /// </summary>
        public Mesh ConvertToUnityMesh()
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;

            // Compute centroid before uploading to mesh
            Vector3 center = CalculateMeshCenter(vertices);

            // Apply offset
            NativeArray<Vector3> offsetVerts = new NativeArray<Vector3>(vertices.Length, Allocator.Temp);
            for (int i = 0; i < vertices.Length; i++)
            {
                offsetVerts[i] = vertices[i] - center;
            }

            mesh.SetVertices(offsetVerts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(triangles, 0, 0, triangles.Length);
            SubMeshDescriptor subMesh = new SubMeshDescriptor(0, triangles.Length, MeshTopology.Triangles);
            mesh.SetSubMesh(0, subMesh, MeshUpdateFlags.DontRecalculateBounds);

            // Dispose
            offsetVerts.Dispose();
            vertices.Dispose();
            normals.Dispose();
            uvs.Dispose();
            triangles.Dispose();

            return mesh;
        }

        /// <summary>
        /// Computes centroid of the mesh using a parallel reduction on X/Z.
        /// </summary>
        private Vector3 CalculateMeshCenter(NativeArray<Vector3> verts)
        {
            if (!verts.IsCreated || verts.Length == 0)
            {
                Debug.Log("CalculateMeshCenter: empty verts (0 ms)");
                return Vector3.zero;
            }

            int chunkSize = 2048;
            int chunkCount = (verts.Length + chunkSize - 1) / chunkSize;

            NativeArray<float> chunkSumsX = new NativeArray<float>(chunkCount, Allocator.TempJob);
            NativeArray<float> chunkSumsZ = new NativeArray<float>(chunkCount, Allocator.TempJob);

            SumChunksJob job = new SumChunksJob
            {
                vertices = verts,
                chunkSize = chunkSize,
                sumsX = chunkSumsX,
                sumsZ = chunkSumsZ
            };

            JobHandle handle = job.Schedule(chunkCount, 1);
            handle.Complete();

            float totalX = 0f, totalZ = 0f;
            for (int i = 0; i < chunkCount; i++)
            {
                totalX += chunkSumsX[i];
                totalZ += chunkSumsZ[i];
            }

            chunkSumsX.Dispose();
            chunkSumsZ.Dispose();

            return new Vector3(totalX / verts.Length, 0f, totalZ / verts.Length);
        }
        #endregion

        #region JOBS
        private struct SumChunksJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> vertices;
            [ReadOnly] public int chunkSize;

            [WriteOnly] public NativeArray<float> sumsX;
            [WriteOnly] public NativeArray<float> sumsZ;

            public void Execute(int chunkIndex)
            {
                int start = chunkIndex * chunkSize;
                int end = math.min(start + chunkSize, vertices.Length);
                float sx = 0f, sz = 0f;

                for (int i = start; i < end; i++)
                {
                    Vector3 v = vertices[i];
                    sx += v.x;
                    sz += v.z;
                }

                sumsX[chunkIndex] = sx;
                sumsZ[chunkIndex] = sz;
            }
        }
        #endregion
    }
}

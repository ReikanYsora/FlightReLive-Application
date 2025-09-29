using FlightReLive.Core.Settings;
using UnityEngine;

namespace FlightReLive.Core.Paths
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class PathColliderUpdater : MonoBehaviour
    {
        /// <summary>
        /// Rebuilds the MeshCollider so that it matches the displaced mesh (BaseThickness).
        /// </summary>
        public void UpdateColliderMesh()
        {
            float baseThickness = SettingsManager.CurrentSettings.Path3DThickness;

            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh original = mf != null ? mf.sharedMesh : null;

            if (original == null || original.vertexCount == 0 || original.triangles.Length == 0)
            {
                return;
            }

            Vector3[] vertices = original.vertices;
            Vector3[] normals = original.normals;
            int[] triangles = original.triangles;
            Vector2[] uvs = original.uv;

            if (vertices.Length != normals.Length)
            {
                return;
            }

            Vector3[] displaced = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 offset = normals[i].normalized * (baseThickness + 0.2f);
                displaced[i] = vertices[i] + offset;
            }

            Mesh colliderMesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = displaced,
                triangles = triangles,
                normals = normals,
                uv = uvs
            };

            colliderMesh.RecalculateBounds();
            colliderMesh.RecalculateNormals();

            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = colliderMesh;
            }
        }
    }
}

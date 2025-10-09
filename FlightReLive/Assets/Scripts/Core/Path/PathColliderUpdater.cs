using FlightReLive.Core.Settings;
using UnityEngine;

namespace FlightReLive.Core.Paths
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class PathColliderUpdater : MonoBehaviour
    {
        /// <summary>
        /// Rebuilds the MeshCollider slightly larger (≈10%) than the visible mesh
        /// to make hover and click interactions more forgiving.
        /// </summary>
        public void UpdateColliderMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null)
            {
                return;
            }

            Mesh original = mf.sharedMesh;
            if (original == null || original.vertexCount == 0 || original.triangles.Length == 0)
            {
                return;
            }

            //Base user thickness (0.1f => 1.0f) converted to actual world-space radius (0.005 m – 0.05 m)
            float baseThickness = Mathf.Clamp(SettingsManager.CurrentSettings.Path3DThickness, 0.01f, 1f) * 0.05f;

            //Collider will be 30 % larger than visual radius
            float colliderOffset = baseThickness * 0.3f;

            Vector3[] vertices = original.vertices;
            Vector3[] normals = original.normals;

            if (vertices.Length != normals.Length)
            {
                return;
            }

            Vector3[] displaced = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                //Push vertices slightly outward along normals (+10 % radius)
                displaced[i] = vertices[i] + normals[i].normalized * colliderOffset;
            }

            Mesh colliderMesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = displaced,
                triangles = original.triangles,
                normals = normals,
                uv = original.uv
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

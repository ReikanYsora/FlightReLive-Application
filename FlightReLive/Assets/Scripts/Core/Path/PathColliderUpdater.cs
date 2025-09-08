using UnityEngine;

namespace FlightReLive.Core.Paths
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
    public class PathColliderUpdater : MonoBehaviour
    {
        [Header("Displacement Settings")]
        public float baseThickness = 0.09f;
        public float cameraDistanceFactor = 1.0f;

        public void UpdateColliderMesh()
        {
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
                Vector3 offset = normals[i].normalized * (baseThickness * cameraDistanceFactor);
                displaced[i] = vertices[i] + offset;
            }

            Mesh colliderMesh = new Mesh();
            colliderMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            colliderMesh.vertices = displaced;
            colliderMesh.triangles = triangles;
            colliderMesh.normals = normals;
            colliderMesh.uv = uvs;

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

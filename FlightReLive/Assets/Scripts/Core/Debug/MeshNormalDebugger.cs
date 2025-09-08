using UnityEngine;

namespace FlightReLive.Core.RuntimeDebug
{
    [ExecuteAlways]
    public class MeshNormalDebugger : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Debug Settings")]
        [SerializeField] private float _normalLength = 0.2f;
        #endregion

        #region UNITY METHODS
        private void OnDrawGizmos()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            Mesh mesh = meshFilter.sharedMesh;
            Transform tf = meshFilter.transform;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            Gizmos.color = Color.green;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = tf.TransformPoint(vertices[i]);
                Vector3 worldNormal = tf.TransformDirection(normals[i]);

                Gizmos.DrawLine(worldPos, worldPos + worldNormal * _normalLength);
            }
        }
        #endregion
    }
}

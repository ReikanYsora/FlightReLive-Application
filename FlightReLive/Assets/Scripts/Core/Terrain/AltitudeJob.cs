using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace FlightReLive.Core.Terrain
{
    [BurstCompile]
    public struct AltitudeJob : IJob
    {
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<Vector2> uv2;
        [ReadOnly] public Matrix4x4 localToWorld;
        [ReadOnly] public Vector2 targetXZ;

        public NativeArray<float> result; // [0] = minDist, [1] = altitude

        public void Execute()
        {
            float minDist = float.MaxValue;
            float altitude = 0f;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = localToWorld.MultiplyPoint3x4(vertices[i]);
                float dx = worldPos.x - targetXZ.x;
                float dz = worldPos.z - targetXZ.y;
                float distSqr = dx * dx + dz * dz;

                if (distSqr < minDist)
                {
                    minDist = distSqr;
                    altitude = uv2[i].x;
                }
            }

            result[0] = minDist;
            result[1] = altitude;
        }
    }
}
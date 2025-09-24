using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.OpenMapTile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FlightReLive.Core.ProceduralTerrain
{
    /// <summary>
    /// Procedural scattering of trees inside LandCover zones (e.g., "forest").
    /// Trees are painted into Unity Terrains via TreeInstances.
    /// </summary>
    internal class ProceduralTreeManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [Header("Tree Prefabs (HDRP-ready, SpeedTree or Terrain-compatible)")]
        [SerializeField] private GameObject[] _treePrefabs;

        [Header("Placement Settings")]
        [Tooltip("Trees per m² (0.02 ≈ 200 trees/ha).")]
        [SerializeField] private float _treeDensity = 0.02f;

        [Tooltip("Random scale factor (multiplied by GlobalScale).")]
        [SerializeField] private Vector2 _randomScaleRange = new Vector2(0.8f, 1.3f);

        private TreePrototype[] _treePrototypes;
        #endregion

        #region METHODS
        /// <summary>
        /// Configures tree prototypes and triggers tree scattering directly.
        /// </summary>
        internal void LoadTrees(FlightData flight, List<Terrain> terrains)
        {
            if (flight == null || _treePrefabs == null || _treePrefabs.Length == 0 || terrains.Count == 0)
            {
                return;
            }

            //Configure prototypes once
            _treePrototypes = new TreePrototype[_treePrefabs.Length];

            for (int i = 0; i < _treePrefabs.Length; i++)
            {
                _treePrototypes[i] = new TreePrototype { prefab = _treePrefabs[i] };
            }

            foreach (Terrain t in terrains)
            {
                t.terrainData.treePrototypes = _treePrototypes;
                t.terrainData.treeInstances = new TreeInstance[0];
            }

            //Scatter immediately
            SpawnTrees(flight, terrains);
        }

        /// <summary>
        /// Scatters all trees with bulk assignment,
        /// ordered by tile priority for faster and more logical loading.
        /// </summary>
        private void SpawnTrees(FlightData flight, List<Terrain> terrains)
        {
            Dictionary<OpenMapTileFeature, List<List<Vector2>>> landcoverZones = OpenMapTileManager.Instance.GetZoneContours(OpenMapTileManager.OpenMapTileZone.LandCover);

            if (landcoverZones == null || landcoverZones.Count == 0)
            {
                return;
            }

            // Buffer per terrain
            Dictionary<Terrain, List<TreeInstance>> terrainTrees = new Dictionary<Terrain, List<TreeInstance>>();
            foreach (Terrain t in terrains)
            {
                terrainTrees[t] = new List<TreeInstance>(4096);
            }

            // Order features by tile priority (only priority 0 and 1)
            List<KeyValuePair<OpenMapTileFeature, List<List<Vector2>>>> orderedFeatures = landcoverZones
                    .Where(x => x.Key.TileDefinition.Priority < 2)
                    .OrderBy(x => x.Key.TileDefinition.Priority)
                    .ToList();

            int processed = 0;

            foreach (KeyValuePair<OpenMapTileFeature, List<List<Vector2>>> featureEntry in orderedFeatures)
            {
                if (featureEntry.Key is not LandcoverFeature lcf)
                {
                    continue;
                }

                string sub = (lcf.Subclass ?? string.Empty).ToLowerInvariant();
                if (!(sub.Contains("wood") || sub.Contains("forest")))
                {
                    continue;
                }

                foreach (List<Vector2> contour in featureEntry.Value)
                {
                    foreach ((Terrain terrain, TreeInstance tree) in GenerateTreesInPolygon(flight, terrains, contour))
                    {
                        terrainTrees[terrain].Add(tree);
                        processed++;
                    }
                }
            }

            //Bulk apply to each terrain
            foreach (KeyValuePair<Terrain, List<TreeInstance>> kvp in terrainTrees)
            {
                TerrainData td = kvp.Key.terrainData;
                td.treeInstances = kvp.Value.ToArray();
                kvp.Key.Flush();

                Debug.Log($"[ProceduralTreeManager] '{kvp.Key.name}' → prototypes: {td.treePrototypes.Length}, trees: {td.treeInstances.Length}");
            }

            Debug.Log($"[ProceduralTreeManager] All trees painted (total: {processed}).");
        }

        /// <summary>
        /// Generate all tree instances for a polygon, returning the correct terrain and instance.
        /// </summary>
        private List<(Terrain, TreeInstance)> GenerateTreesInPolygon(FlightData flight, List<Terrain> terrains, List<Vector2> contour)
        {
            List<(Terrain, TreeInstance)> result = new List<(Terrain, TreeInstance)>();

            Bounds bounds = ComputeBounds(contour);

            float areaWorld = bounds.size.x * bounds.size.z;
            float areaMeters = areaWorld / (flight.GlobalScale * flight.GlobalScale);
            int targetCount = Mathf.RoundToInt(areaMeters * _treeDensity);

            int placed = 0;
            int attempts = targetCount * 3;

            for (int i = 0; i < attempts && placed < targetCount; i++)
            {
                Vector2 rnd = new Vector2(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                if (!PointInPolygon(rnd, contour))
                {
                    continue;
                }

                Vector3 worldPos = new Vector3(rnd.x, 0, rnd.y);

                //Convert world → GPS → altitude
                FlightGPSData gps = flight.ConvertWorldToGPSPosition(worldPos);
                float terrainAlt = flight.GetAltitudeAtPosition(gps) * flight.GlobalScale;
                worldPos.y = terrainAlt;

                int protoIndex = Random.Range(0, _treePrefabs.Length);
                float scale = Random.Range(_randomScaleRange.x, _randomScaleRange.y) * flight.GlobalScale;

                foreach (Terrain t in terrains)
                {
                    if (!IsInsideTerrainBounds(t, worldPos))
                    {
                        continue;
                    }

                    TerrainData td = t.terrainData;
                    Vector3 localPos = worldPos - t.transform.position;

                    float normX = Mathf.Clamp01(localPos.x / td.size.x);
                    float normZ = Mathf.Clamp01(localPos.z / td.size.z);
                    float localHeight = td.GetInterpolatedHeight(normX, normZ);
                    float normY = Mathf.Clamp01(localHeight / td.size.y);

                    TreeInstance tree = new TreeInstance
                    {
                        prototypeIndex = protoIndex,
                        position = new Vector3(normX, normY, normZ),
                        widthScale = scale,
                        heightScale = scale,
                        color = Color.white,
                        lightmapColor = Color.white,
                        rotation = Random.Range(0f, 360f) * Mathf.Deg2Rad
                    };

                    result.Add((t, tree));
                    placed++;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Check if a world position lies inside a given terrain bounds.
        /// </summary>
        private bool IsInsideTerrainBounds(Terrain terrain, Vector3 worldPos)
        {
            Bounds b = new Bounds(terrain.transform.position + terrain.terrainData.size * 0.5f, terrain.terrainData.size);

            return b.Contains(worldPos);
        }

        /// <summary>
        /// Compute the bounding box of a polygon contour.
        /// </summary>
        private Bounds ComputeBounds(List<Vector2> contour)
        {
            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;

            for (int i = 0; i < contour.Count; i++)
            {
                Vector2 p = contour[i];
                if (p.x < minX)
                { 
                    minX = p.x;
                }

                if (p.y < minZ) 
                {
                    minZ = p.y;
                }

                if (p.x > maxX)
                {
                    maxX = p.x;
                }

                if (p.y > maxZ)
                {
                    maxZ = p.y;
                }
            }

            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(minX, 0f, minZ), new Vector3(maxX, 0f, maxZ));
            return b;
        }

        /// <summary>
        /// Test if a 2D point lies inside a polygon (raycasting method).
        /// </summary>
        private bool PointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            bool inside = false;
            int j = polygon.Count - 1;

            for (int i = 0; i < polygon.Count; j = i++)
            {
                Vector2 pi = polygon[i];
                Vector2 pj = polygon[j];

                if (((pi.y > point.y) != (pj.y > point.y)) && (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + 1e-12f) + pi.x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }
        #endregion
    }
}

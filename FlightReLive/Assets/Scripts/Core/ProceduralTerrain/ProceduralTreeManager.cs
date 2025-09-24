using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Loading;
using FlightReLive.Core.OpenMapTile;
using System.Collections;
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
    [RequireComponent(typeof(ProceduralTerrainManager))]
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

        [Tooltip("Safety cap to avoid huge spawns per polygon.")]
        [SerializeField] private int _maxTreesPerContour = 5000;

        private Terrain[] _terrains;
        private TreePrototype[] _treePrototypes;
        #endregion

        #region PROPERTIES
        public static ProceduralTreeManager Instance { get; private set; }
        #endregion

        #region UNITY METHODS
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            LoadingManager.Instance.OnFlightEndLoading += OnFlightEndLoading;
        }

        private void OnDestroy()
        {
            if (LoadingManager.Instance != null)
            {
                LoadingManager.Instance.OnFlightEndLoading -= OnFlightEndLoading;
            }
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Coroutine that scatters all trees much faster (bulk assignment).
        /// </summary>
        private IEnumerator SpawnTreesAsync(FlightData flight)
        {
            Dictionary<OpenMapTileFeature, List<List<Vector2>>> landcoverZones = OpenMapTileManager.Instance.GetZoneContours(OpenMapTileManager.OpenMapTileZone.LandCover);

            if (landcoverZones == null || landcoverZones.Count == 0)
            {
                Debug.LogWarning("No LandCover zones found for tree placement.");
                yield break;
            }

            // Buffer of tree instances per terrain
            Dictionary<Terrain, List<TreeInstance>> terrainTrees = new Dictionary<Terrain, List<TreeInstance>>();
            foreach (Terrain t in _terrains)
            {
                terrainTrees[t] = new List<TreeInstance>(1024);
            }

            foreach (KeyValuePair<OpenMapTileFeature, List<List<Vector2>>> featureEntry in landcoverZones)
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
                    // Generate trees for this contour and store them
                    foreach ((Terrain terrain, TreeInstance tree) in GenerateTreesInPolygon(flight, contour))
                    {
                        terrainTrees[terrain].Add(tree);
                    }

                    // Small yield per contour (not per tree)
                    yield return null;
                }
            }

            // Apply bulk assignment
            foreach (KeyValuePair<Terrain, List<TreeInstance>> kvp in terrainTrees)
            {
                TerrainData td = kvp.Key.terrainData;
                td.treeInstances = kvp.Value.ToArray();
                kvp.Key.Flush();

                Debug.Log($"[ProceduralTreeManager] '{kvp.Key.name}' → prototypes: {td.treePrototypes.Length}, trees: {td.treeInstances.Length}");
            }

            Debug.Log("[ProceduralTreeManager] All trees painted (bulk assignment).");
        }

        /// <summary>
        /// Generate all tree instances for a polygon, returning the correct terrain and instance.
        /// </summary>
        private IEnumerable<(Terrain, TreeInstance)> GenerateTreesInPolygon(FlightData flight, List<Vector2> contour)
        {
            Bounds bounds = ComputeBounds(contour);

            float areaWorld = bounds.size.x * bounds.size.z;
            float areaMeters = areaWorld / (flight.GlobalScale * flight.GlobalScale);
            int targetCount = Mathf.RoundToInt(areaMeters * _treeDensity);

            if (_maxTreesPerContour > 0 && targetCount > _maxTreesPerContour)
            {
                targetCount = _maxTreesPerContour;
            }

            int placed = 0;
            int attempts = targetCount * 3;

            for (int i = 0; i < attempts && placed < targetCount; i++)
            {
                Vector2 rnd = new Vector2(Random.Range(bounds.min.x, bounds.max.x),
                                          Random.Range(bounds.min.z, bounds.max.z));

                if (!PointInPolygon(rnd, contour))
                {
                    continue;
                }

                Vector3 worldPos = new Vector3(rnd.x, 0, rnd.y);
                FlightGPSData gps = WorldXZToGPSApprox(flight, worldPos);
                float terrainAlt = flight.GetAltitudeAtPosition(gps) * flight.GlobalScale;
                worldPos.y = terrainAlt;

                int protoIndex = Random.Range(0, _treePrefabs.Length);
                float scale = Random.Range(_randomScaleRange.x, _randomScaleRange.y) * flight.GlobalScale;

                foreach (Terrain t in _terrains)
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

                    yield return (t, tree);
                    placed++;
                    break;
                }
            }
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
                if (p.x < minX) { minX = p.x; }
                if (p.y < minZ) { minZ = p.y; }
                if (p.x > maxX) { maxX = p.x; }
                if (p.y > maxZ) { maxZ = p.y; }
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

                if (((pi.y > point.y) != (pj.y > point.y)) &&
                    (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y + 1e-12f) + pi.x))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Approximate inverse of FlightData.ConvertGPSPositionToWorld for XZ (local around SceneCenterGPS).
        /// </summary>
        private static FlightGPSData WorldXZToGPSApprox(FlightData flight, Vector3 worldPos)
        {
            float xMeters = worldPos.x / flight.GlobalScale;
            float zMeters = worldPos.z / flight.GlobalScale;

            double lat0 = flight.SceneCenterGPS.x;
            double lon0 = flight.SceneCenterGPS.y;

            const double metersPerDegLat = 111132.0;
            double metersPerDegLon = 111320.0 * Mathf.Cos((float)(lat0 * Mathf.Deg2Rad));

            double dLat = zMeters / metersPerDegLat;
            double dLon = (metersPerDegLon != 0.0) ? xMeters / metersPerDegLon : 0.0;

            return new FlightGPSData(lat0 + dLat, lon0 + dLon);
        }
        #endregion

        #region CALLBACKS
        private void OnFlightEndLoading()
        {
            FlightData flight = LoadingManager.Instance.CurrentFlightData;
            if (flight == null) { return; }

            _terrains = ProceduralTerrainManager.Instance.UnityTerrains.ToArray();
            if (_treePrefabs == null || _treePrefabs.Length == 0 || _terrains.Length == 0)
            {
                Debug.LogWarning("No tree prefabs or terrains available.");
                return;
            }

            // Configure prototypes only once
            _treePrototypes = new TreePrototype[_treePrefabs.Length];
            for (int i = 0; i < _treePrefabs.Length; i++)
            {
                _treePrototypes[i] = new TreePrototype { prefab = _treePrefabs[i] };
            }

            foreach (Terrain t in _terrains)
            {
                t.terrainData.treePrototypes = _treePrototypes;
                t.terrainData.treeInstances = new TreeInstance[0];
            }

            StopAllCoroutines();
            StartCoroutine(SpawnTreesAsync(flight));
        }
        #endregion
    }
}

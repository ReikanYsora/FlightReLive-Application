using FlightReLive.Core.Cameras;
using FlightReLive.Core.Database;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Settings;
using Fu.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.POI
{
    /// <summary>
    /// Simple POI Manager — handles only creation and destruction of POIs.
    /// POI visibility, scaling, and height are managed directly by POIEntity instances.
    /// </summary>
    public class POIManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private GameObject _poiPrefab;
        [SerializeField] private float _poiDefaultHeight = 30f;
        private Camera _camera;
        private readonly List<POIEntity> _poiList = new List<POIEntity>();
        #endregion

        #region PROPERTIES
        internal static POIManager Instance { get; private set; }
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
            _camera = CameraManager.Instance.ReLiveCamera;

            SettingsManager.OnPOIScaleChanged += OnPOIScaleChanged;
            SettingsManager.OnPOIHeightChanged += OnPOIHeightChanged;
            SettingsManager.OnPOIVisibilityChanged += OnPOIVisibilityChanged;
            SettingsManager.OnPOIMinFadeDistanceChanged += OnPOIMinFadeDistanceChanged;
            SettingsManager.OnPOIMaxFadeDistanceChanged += OnPOIMaxFadeDistanceChanged;
        }

        private void OnDestroy()
        {
            SettingsManager.OnPOIScaleChanged -= OnPOIScaleChanged;
            SettingsManager.OnPOIHeightChanged -= OnPOIHeightChanged;
            SettingsManager.OnPOIVisibilityChanged -= OnPOIVisibilityChanged;
            SettingsManager.OnPOIMinFadeDistanceChanged -= OnPOIMinFadeDistanceChanged;
            SettingsManager.OnPOIMaxFadeDistanceChanged -= OnPOIMaxFadeDistanceChanged;
        }
        #endregion

        #region METHODS : LOAD / UNLOAD
        /// <summary>
        /// Creates all POIs from loaded flight data.
        /// </summary>
        internal void Load(FlightData flightData)
        {
            ClearAllPOIs();

            if (flightData?.MapDefinition?.TileDefinitions == null)
            {
                return;
            }

            foreach (TileDefinition tile in flightData.MapDefinition.TileDefinitions)
            {
                FeatureCollection collection = tile.GeoData;
                if (collection?.features == null)
                {
                    continue;
                }

                foreach (Feature feature in collection.features)
                {
                    if (feature?.geometry?.coordinates == null || feature.geometry.coordinates.Count < 2)
                    {
                        continue;
                    }

                    string poiLabel = !string.IsNullOrEmpty(feature.text) ? feature.text : feature.place_name;

                    RealmDoubleVector2 poiGPS = new RealmDoubleVector2
                    {
                        X = feature.geometry.coordinates[1],
                        Y = feature.geometry.coordinates[0]
                    };

                    float altitude = flightData.GetAltitudeAtPosition(tile, poiGPS);
                    Vector3 position = flightData.ConvertGPSPositionToWorld(new Vector3(feature.geometry.coordinates[1], altitude, feature.geometry.coordinates[0]));
                    Color poiColor = GetColorForPOIType(feature.properties?.kind);
                    AddPOI(poiLabel, position, poiColor, _poiDefaultHeight);
                }
            }
        }

        /// <summary>
        /// Adds a POI instance at the specified position.
        /// </summary>
        internal POIEntity AddPOI(string name, Vector3 position, Color color, float height, bool ignoreDistanceFade = false)
        {
            GameObject go = GameObject.Instantiate(_poiPrefab, _mainCanvas.transform);
            go.transform.position = position;

            POIEntity poi = go.GetComponent<POIEntity>();
            poi.Initialize(_camera, position, color, name, height, ignoreDistanceFade: ignoreDistanceFade);
            poi.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;
            poi.MinFadeDistance = SettingsManager.CurrentSettings.POIMinFadeDistance;
            poi.MaxFadeDistance = SettingsManager.CurrentSettings.POIMaxFadeDistance;

            _poiList.Add(poi);
            return poi;
        }

        /// <summary>
        /// Adds a POI instance at the specified position.
        /// </summary>
        internal POIEntity AddPOI(string name, Transform transform, Color color, float height, bool ignoreDistanceFade = false)
        {
            GameObject go = GameObject.Instantiate(_poiPrefab, _mainCanvas.transform);
            go.transform.position = transform.position;

            POIEntity poi = go.GetComponent<POIEntity>();
            poi.Initialize(_camera, transform, color, name, height, ignoreDistanceFade: ignoreDistanceFade);
            poi.ElevationFactor = SettingsManager.CurrentSettings.POIHeight;
            poi.MinFadeDistance = SettingsManager.CurrentSettings.POIMinFadeDistance;
            poi.MaxFadeDistance = SettingsManager.CurrentSettings.POIMaxFadeDistance;

            _poiList.Add(poi);
            return poi;
        }

        /// <summary>
        /// Deletes a single POI and returns it to the pool.
        /// </summary>
        internal void DeletePOI(POIEntity entity)
        {
            if (entity == null)
            {
                return;
            }

            _poiList.Remove(entity);
            Destroy(entity.gameObject);
        }

        /// <summary>
        /// Clears and unloads all POIs.
        /// </summary>
        internal async Task Unload()
        {
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                ClearAllPOIs();
            });
        }

        private void ClearAllPOIs()
        {
            foreach (POIEntity poi in _poiList)
            {
                if (poi != null)
                {
                    Destroy(poi.gameObject);
                }
            }

            _poiList.Clear();
        }

        /// <summary>
        /// Returns a list of all POIs within a specified distance from the camera, sorted by distance (nearest first).
        /// </summary>
        /// <param name="maxDistance">Maximum distance from the camera (in world units).</param>
        /// <returns>List of POIEntity instances within range, sorted by proximity.</returns>
        internal List<POIEntity> GetPOIsWithinDistance(float maxDistance)
        {
            List<POIEntity> nearbyPOIs = new List<POIEntity>();

            if (_camera == null || _poiList == null || _poiList.Count == 0)
            {
                return nearbyPOIs;
            }

            Vector3 camPos = _camera.transform.position;
            float sqrMaxDist = maxDistance * maxDistance;

            //Collect all POIs within range
            foreach (POIEntity poi in _poiList)
            {
                if (poi == null || poi.IgnoreDistanceFade)
                {
                    continue;
                }

                float sqrDist = (poi.transform.position - camPos).sqrMagnitude;
                if (sqrDist <= sqrMaxDist)
                {
                    nearbyPOIs.Add(poi);
                }
            }

            //Sort them by squared distance
            nearbyPOIs.Sort((a, b) =>
            {
                float distA = (a.transform.position - camPos).sqrMagnitude;
                float distB = (b.transform.position - camPos).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            return nearbyPOIs;
        }
        #endregion

        #region COLOR LOGIC
        internal static Color GetColorForPOIType(string kind)
        {
            if (string.IsNullOrEmpty(kind))
            {
                return new Color(0.3f, 0.3f, 0.3f); // Neutral
            }

            kind = kind.ToLowerInvariant();

            if (kind.Contains("airport") || kind.Contains("aerodrome") || kind.Contains("station") || kind.Contains("fuel"))
            {
                return new Color(0.2f, 0.6f, 1f); // Transport
            }

            if (kind.Contains("school") || kind.Contains("college") || kind.Contains("university"))
            {
                return new Color(0.6f, 0.3f, 1f); // Education
            }

            if (kind.Contains("hospital") || kind.Contains("clinic") || kind.Contains("pharmacy"))
            {
                return new Color(1f, 0.3f, 0.3f); // Health
            }

            if (kind.Contains("museum") || kind.Contains("theatre") || kind.Contains("cinema") || kind.Contains("library") || kind.Contains("monument"))
            {
                return new Color(1f, 0.8f, 0.2f); // Culture
            }

            if (kind.Contains("park") || kind.Contains("stadium") || kind.Contains("golf") || kind.Contains("zoo"))
            {
                return new Color(0.2f, 0.9f, 0.4f); // Attraction
            }

            if (kind.Contains("restaurant") || kind.Contains("cafe") || kind.Contains("bar") || kind.Contains("shop") || kind.Contains("market"))
            {
                return new Color(1f, 0.5f, 0.1f); // Commerce
            }

            if (kind.Contains("hotel") || kind.Contains("camp") || kind.Contains("guest"))
            {
                return new Color(0.6f, 0.4f, 0.2f); // Lodging
            }

            if (kind.Contains("church") || kind.Contains("temple") || kind.Contains("mosque"))
            {
                return new Color(1f, 0.95f, 0.7f); // Religion
            }

            if (kind.Contains("townhall") || kind.Contains("police") || kind.Contains("fire") || kind.Contains("court"))
            {
                return new Color(0.1f, 0.4f, 1f); // Public
            }

            if (kind.Contains("mountain") || kind.Contains("lake") || kind.Contains("river") || kind.Contains("forest"))
            {
                return new Color(0.2f, 0.8f, 0.7f); // Nature
            }

            return new Color(0.3f, 0.3f, 0.3f); // Default
        }
        #endregion

        #region SETTINGS CALLBACKS
        private void OnPOIScaleChanged(float value)
        {
            float scale = value / 100f;
            foreach (POIEntity poi in _poiList)
            {
                if (poi != null)
                {
                    poi.ScaleFactor = scale;
                }
            }
        }

        private void OnPOIHeightChanged(float factor)
        {
            foreach (POIEntity poi in _poiList)
            {
                if (poi != null)
                {
                    poi.ElevationFactor = factor;
                }
            }
        }

        private void OnPOIVisibilityChanged(bool visible)
        {
            foreach (POIEntity poi in _poiList)
            {
                if (poi != null)
                {
                    poi.IsVisible = visible;
                }
            }
        }

        private void OnPOIMinFadeDistanceChanged(float minFadeDistance)
        {
            foreach (POIEntity poi in _poiList)
            {
                if (poi != null)
                {
                    poi.MinFadeDistance = minFadeDistance;
                }
            }
        }

        private void OnPOIMaxFadeDistanceChanged(float MaxFadeDistance)
        {
            foreach (POIEntity poi in _poiList)
            {
                if (poi != null)
                {
                    poi.MaxFadeDistance = MaxFadeDistance;
                }
            }
        }
        #endregion

        #region UI
        internal void DisplayPOISettings()
        {
            using (FuGrid grid = new FuGrid("gridPOISettings", new FuGridDefinition(3, new float[] { 0.3f, 0.58f, 0.12f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 4f, outterPadding: 10))
            {
                bool poiEnabled = SettingsManager.CurrentSettings.POIVisibility;

                SettingsManager.DisplaySettingsToggleWithReset(grid,
                    "Display POI",
                    "Display or hide all POIs.",
                    "Reset POI visibility to default.",
                    poiEnabled,
                    SettingsManager.POI_DISPLAY_STATE_DEFAULT_VALUE,
                    (x) => SettingsManager.SavePOIVisibility(x),
                    () => SettingsManager.ResetPOIVisibility());

                if (!poiEnabled)
                    grid.DisableNextElements();

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "POI scale",
                    "Adjust POI global scale.",
                    $"Reset to {SettingsManager.POI_SCALE_DEFAULT_VALUE}.",
                    SettingsManager.CurrentSettings.POIScale,
                    0.1f, 1.0f, 0.1f,
                    SettingsManager.POI_SCALE_DEFAULT_VALUE,
                    "%.1f",
                    (x) => SettingsManager.SavePOIScale(x),
                    () => SettingsManager.ResetPOIScale());

                SettingsManager.DisplaySettingsSliderWithReset(grid,
                    "POI height",
                    "Vertical offset of POIs.",
                    $"Reset to {SettingsManager.POI_HEIGHT_DEFAULT_VALUE}.",
                    SettingsManager.CurrentSettings.POIHeight,
                    0f, 3f, 0.1f,
                    SettingsManager.POI_HEIGHT_DEFAULT_VALUE,
                    "%.1f",
                    (x) => SettingsManager.SavePOIHeight(x),
                    () => SettingsManager.ResetPOIHeight());

                SettingsManager.DisplaySettingsRangeWithReset(grid,
                    "POI fade distance",
                    "Adjust the minimum and maximum distances for POI fading based on camera distance.",
                    "Reset to default values.",
                    SettingsManager.CurrentSettings.POIMinFadeDistance,
                    SettingsManager.CurrentSettings.POIMaxFadeDistance,
                    0f,
                    1000f,
                    1f,
                    new Vector2(SettingsManager.POI_MIN_FADE_DISTANCE_DEFAULT_VALUE, SettingsManager.POI_MAX_FADE_DISTANCE_DEFAULT_VALUE),
                    "%.0f m",
                    (min, max) =>
                    {
                        SettingsManager.SavePOIMinFadeDistance(min);
                        SettingsManager.SavePOIMaxFadeDistance(max);
                    },
                    () =>
                    {
                        SettingsManager.ResetPOIMinFadeDistance();
                        SettingsManager.ResetPOIMaxFadeDistance();
                    });
            }
        }
        #endregion
    }
}

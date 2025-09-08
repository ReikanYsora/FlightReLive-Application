using FlightReLive.Core.Building;
using FlightReLive.Core.FlightDefinition;
using FlightReLive.Core.Paths;
using FlightReLive.Core.Pipeline;
using FlightReLive.Core.Pipeline.API;
using FlightReLive.Core.Pipeline.Download;
using FlightReLive.Core.Rendering;
using FlightReLive.Core.Settings;
using FlightReLive.Core.Terrain;
using FlightReLive.Core.Workspace;
using FlightReLive.Core.WorldUI;
using FlightReLive.UI.FlightCharts;
using FlightReLive.UI.VideoPlayer;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightReLive.Core.Loading
{
    public class LoadingManager : MonoBehaviour
    {
        #region ATTRIBUTES
        [SerializeField] private LoadingAnimationManager _animationManager;
        #endregion

        #region PROPERTIES
        internal static LoadingManager Instance { get; private set; }

        internal float LoadingProgress { set; get; }

        internal bool IsLoading { set; get; }

        internal FlightFile CurrentFlightFile { set; get; }

        internal FlightData CurrentFlightData { set; get; }
        #endregion

        #region EVENTS
        internal event Action OnFlightStartLoading;

        internal event Action OnFlightEndLoading;

        internal event Action<float> OnFlightLoadingProgressChanged;

        internal event Action OnFlightUnloaded;
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
            WorkspaceManager.Instance.OnFlightFileSelected += OnFlightFileSelected;
        }

        private void OnDestroy()
        {
            WorkspaceManager.Instance.OnFlightFileSelected -= OnFlightFileSelected;
        }
        #endregion

        #region METHODS
        private void LoadFlightDataInModules(FlightData flightData)
        {
            //Load FlightData in all modules
            //Certains modules need to wait TerrainManager because we need data about topography altitude (display graph, buildins, path..). They work with the TerrainManager.Instance.OnTerrainLoaded event
            TerrainManager.Instance.LoadFlightMap(flightData);
            VideoPlayerManager.Instance.LoadFlightVideo(flightData);
            SunManager.Instance.LoadFlightRendering(flightData);

            OnFlightEndLoading?.Invoke();
            IsLoading = false;
        }

        private void UnloadFlightDataInModules()
        {
            //Unload FLightData in all modules
            TerrainManager.Instance.UnloadFlightMap();
            FlightChartsManager.Instance.UnloadFlightCharts();
            VideoPlayerManager.Instance.UnloadFlightVideo();
            PathManager.Instance.UnloadFlightPath();
            SunManager.Instance.UnloadFlightRendering();
            WorldUIManager.Instance.UnloadFlightPOIs();
            BuildingManager.Instance.UnloadFLightBuildings();
            CurrentFlightData = null;

            OnFlightUnloaded?.Invoke();
        }

        private async void StartLoadingScene(FlightFile flightFile)
        {
            FlightData flightData = FlightMapDownloader.ConvertFileToFlight(flightFile);

            IsLoading = true;
            OnFlightStartLoading?.Invoke();

            string apiKey = SettingsManager.CurrentSettings.MapTilerAPIKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                OnFlightMapBuildEnd(flightData, new List<string> { "The application requires a valid MapTiler API key to download satellite imagery.\nPlease enter your key in 'Preferences' before continuing." });
                IsLoading = false;

                return;
            }

            bool isValid = await MapTilerAPIHelper.IsMapTilerKeyValidAsync(apiKey);

            if (!isValid)
            {
                OnFlightMapBuildEnd(flightData, new List<string> { "The provided MapTiler API key is not authorized or has expired.\nPlease verify your key and try again.\nDownloads cannot proceed until a valid key is set." });
                IsLoading = false;

                return;
            }

            FlightMapDownloader builder = new FlightMapDownloader();

            builder.OnDownloadStarted += () =>
            {
                OnFlightMapBuildStart();
            };

            builder.OnGlobalProgressUpdated += progress =>
            {
                OnFlightMapBuildProgressChanged(progress);
            };

            builder.OnDownloadCompleted += errors =>
            {
                OnFlightMapBuildEnd(flightData, errors);
                IsLoading = false;
            };

            builder.BuildFlightMap(flightData);
        }

        #endregion

        #region CALLBACKS
        private void OnFlightFileSelected(FlightFile flightFile)
        {
            if (CurrentFlightData != null)
            {
                UnloadFlightDataInModules();
            }

            CurrentFlightFile = flightFile;

            //Start loading
            StartLoadingScene(flightFile);
        }

        private void OnFlightMapBuildStart()
        {
            _animationManager.StartLoadingAnimation();
        }

        private void OnFlightMapBuildProgressChanged(float progress)
        {
            LoadingProgress = progress;

            _animationManager.Progress = LoadingProgress;

            OnFlightLoadingProgressChanged?.Invoke(progress);
        }

        private void OnFlightMapBuildEnd(FlightData flightData, List<string> errors)
        {
            LoadingProgress = 0f;

            _animationManager.StopLoadingAnimation();

            if (errors.Count > 0)
            {
                string error = string.Empty;

                for (int i = 0; i < errors.Count; i++)
                {
                    error += "\n" + errors[i];
                }

                Fugui.Notify("Resource loading error", error, StateType.Danger);
                return;
            }

            //Bake scene center GPS 
            double averageLatitude = flightData.MapDefinition.TileDefinitions.Average(t => (t.BoundingBox.MinLatitude + t.BoundingBox.MaxLatitude) / 2.0);
            double averageLongitude = flightData.MapDefinition.TileDefinitions.Average(t => (t.BoundingBox.MinLongitude + t.BoundingBox.MaxLongitude) / 2.0);
            flightData.SceneCenterGPS = new Vector2((float)averageLatitude, (float)averageLongitude);

            //Load FLightData in all modules
            LoadFlightDataInModules(flightData);

            CurrentFlightData = flightData;

            Fugui.Notify("Successful operation", $"{flightData.Name} loaded successfully.", StateType.Info);
        }
        #endregion
    }
}

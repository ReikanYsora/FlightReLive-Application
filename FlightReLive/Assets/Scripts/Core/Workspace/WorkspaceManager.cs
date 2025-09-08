using FlightReLive.Core.Cache;
using FlightReLive.Core.FFmpeg;
using FlightReLive.Core.Settings;
using Fu;
using Fu.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Workspace
{
    public class WorkspaceManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private FileSystemWatcher _watcher;
        #endregion

        #region PROPERTIES
        internal static WorkspaceManager Instance { get; private set; }

        internal ConcurrentBag<FlightFile> LoadedFlights { get; private set; }
        #endregion

        #region EVENTS
        internal event Action OnWorkspaceStartLoading;
        internal event Action<float> OnWorkspaceLoading;
        internal event Action OnWorkspaceEndLoading;
        internal event Action<FlightFile> OnFlightFileSelected;
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

            LoadedFlights = new ConcurrentBag<FlightFile>();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // Enable Mono-managed watcher for macOS compatibility
            Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif
        }

        private void Start()
        {
            SettingsManager.OnWorkspacePathChanged += OnWorkspacePathChanged;
            StartWatching(SettingsManager.CurrentSettings.WorkspacePath);
            LoadWorkSpace(SettingsManager.CurrentSettings.WorkspacePath);
        }

        private void OnDestroy()
        {
            SettingsManager.OnWorkspacePathChanged -= OnWorkspacePathChanged;
            StopWatching();
        }
        #endregion

        #region METHODS
        private void LoadWorkSpace(string workspacePath)
        {
            //Load flights files in workspace
            _ = StartLoadingWorkspaceAsync();
        }

        private void StartWatching(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                Debug.LogError($"WorkspaceWatcher: Invalid folder path: {folder}");
                return;
            }

            _watcher = new FileSystemWatcher(folder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                Filter = "*.mp4",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Changed += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;
        }

        private void StopWatching()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private async Task StartLoadingWorkspaceAsync()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                OnWorkspaceStartLoading?.Invoke();
            });

            OnWorkspaceLoading?.Invoke(0f);
            LoadedFlights.Clear();
            int completedCount = 0;
            object progressLock = new object();
            string workspacePath = SettingsManager.CurrentSettings.WorkspacePath;

            if (!Directory.Exists(workspacePath))
            {
                return;
            }

            string[] videoFiles = Directory.GetFiles(workspacePath, "*.mp4");

            int cpuCores = Environment.ProcessorCount;
            int fileCount = videoFiles.Length;
            int maxConcurrency = Math.Clamp(fileCount >= cpuCores ? cpuCores : fileCount, 2, 16);
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);
            List<Task> tasks = new List<Task>();

            foreach (string videoPath in videoFiles)
            {
                await semaphore.WaitAsync();

                Task task = Task.Run(() =>
                {
                    try
                    {
                        LoadVideoFile(videoPath);
                    }
                    catch (Exception ex)
                    {
                        UnityMainThreadDispatcher.AddActionInMainThread(() =>
                        {
                            Debug.LogError($"Failed to load flight from {videoPath}: {ex.Message}");
                        });
                    }
                    finally
                    {
                        lock (progressLock)
                        {
                            completedCount++;
                            float progress = (float)completedCount / fileCount;

                            UnityMainThreadDispatcher.AddActionInMainThread(() =>
                            {
                                OnWorkspaceLoading?.Invoke(progress);
                            });
                        }

                        semaphore.Release();
                    }
                });

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                int l = LoadedFlights.Count;
                OnWorkspaceLoading?.Invoke(1f);
                OnWorkspaceEndLoading?.Invoke();
            });
        }

        private void LoadVideoFile(string videoPath)
        {
            if (SettingsManager.CurrentSettings == null || string.IsNullOrEmpty(SettingsManager.CurrentSettings.WorkspacePath) || !Directory.Exists(SettingsManager.CurrentSettings.WorkspacePath))
            {
                return;
            }

            string fullVideoPath = Path.Combine(SettingsManager.CurrentSettings.WorkspacePath, videoPath);

            if (!File.Exists(fullVideoPath))
            {
                return;
            }

            FlightDataContainer container = FFmpegHelper.ExtractOrLoadFlightData(fullVideoPath);

            if (container == null)
            {
                return;
            }

            FlightFile tempFile = new FlightFile
            {
                VideoPath = container.VideoPath,
                Name = container.Name,
                DataPoints = container.DataPoints,
                CreationDate = container.CreationDate,
                EstimateTakeOffPosition = container.EstimateTakeOffPosition,
                FlightGPSCoordinates = container.FlightGPSCoordinates,
                HasExtractionError = container.HasExtractionError,
                HasTakeOffPosition = container.TakeOffPositionAvailable,
                ErrorMessages = container.ErrorMessages,
                Duration = container.Duration,
                IsValid = container.IsValid
            };

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(container.Thumbnail);
                tempFile.Thumbnail = texture;

                LoadedFlights.Add(tempFile);
            });
        }

        internal void SelectFlight(FlightFile file)
        {
            if (file == null || !file.IsValid || file.HasExtractionError)
            {
                return;
            }

            OnFlightFileSelected?.Invoke(file);
        }
        #endregion

        #region CALLBACKS
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            //Load flights files in workspace
            _ = StartLoadingWorkspaceAsync();
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            //Load flights files in workspace
            _ = StartLoadingWorkspaceAsync();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            //Load flights files in workspace
            _ = StartLoadingWorkspaceAsync();
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            //Load flights files in workspace
            _ = StartLoadingWorkspaceAsync();
        }

        private void OnWorkspacePathChanged(string workspacePath)
        {
            //Load flights files in workspace
            _ = StartLoadingWorkspaceAsync();
        }
        #endregion
    }
}

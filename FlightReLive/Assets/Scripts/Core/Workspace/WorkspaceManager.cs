using FlightReLive.Core.Cache;
using FlightReLive.Core.FFmpeg;
using FlightReLive.Core.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FlightReLive.Core.Workspace
{
    public class WorkspaceManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private FileSystemWatcher _watcher;
        private readonly ConcurrentDictionary<string, byte> _inFlightOps = new ConcurrentDictionary<string, byte>();
        #endregion

        #region PROPERTIES
        internal static WorkspaceManager Instance { get; private set; }

        internal ConcurrentDictionary<string, FlightFile> LoadedFlights { get; private set; }
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

            LoadedFlights = new ConcurrentDictionary<string, FlightFile>();

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // Enable Mono-managed watcher for macOS compatibility
            System.Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "enabled");
#endif
        }

        private void Start()
        {
            SettingsManager.OnWorkspacePathChanged += OnWorkspacePathChanged;

            string ws = SettingsManager.CurrentSettings.WorkspacePath;

            if (Directory.Exists(ws))
            {
                StartWatching(ws);
                _ = InitialScanAsync(ws);
            }
        }

        private void OnDestroy()
        {
            SettingsManager.OnWorkspacePathChanged -= OnWorkspacePathChanged;
            StopWatching();
        }
        #endregion

        #region METHODS
        internal void SelectFlight(FlightFile file)
        {
            if (file == null || !file.IsValid || file.HasExtractionError)
            {
                return;
            }

            OnFlightFileSelected?.Invoke(file);
        }

        private void StartWatching(string folder)
        {
            StopWatching();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                Debug.LogError($"WorkspaceWatcher: Invalid folder path: {folder}");
                return;
            }

            _watcher = new FileSystemWatcher(folder)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                Filter = "*.mp4",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFsCreatedOrChanged;
            _watcher.Changed += OnFsCreatedOrChanged;
            _watcher.Deleted += OnFsDeleted;
            _watcher.Renamed += OnFsRenamed;
        }

        private void StopWatching()
        {
            if (_watcher == null)
            {
                return;
            }

            _watcher.Created -= OnFsCreatedOrChanged;
            _watcher.Changed -= OnFsCreatedOrChanged;
            _watcher.Deleted -= OnFsDeleted;
            _watcher.Renamed -= OnFsRenamed;
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        private async Task InitialScanAsync(string workspacePath)
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() => OnWorkspaceStartLoading?.Invoke());
            UnityMainThreadDispatcher.AddActionInMainThread(() => OnWorkspaceLoading?.Invoke(0f));

            if (string.IsNullOrEmpty(workspacePath) || !Directory.Exists(workspacePath))
            {
                UnityMainThreadDispatcher.AddActionInMainThread(() =>
                {
                    OnWorkspaceLoading?.Invoke(1f);
                    OnWorkspaceEndLoading?.Invoke();
                });
                return;
            }

            string[] videoFiles = Directory.GetFiles(workspacePath, "*.mp4", SearchOption.TopDirectoryOnly);
            int fileCount = videoFiles.Length;

            // Remove flights that no longer exist on disk
            HashSet<string> current = new HashSet<string>(videoFiles, StringComparer.OrdinalIgnoreCase);
            foreach (string loaded in LoadedFlights.Keys.ToList())
            {
                if (!current.Contains(loaded))
                {
                    LoadedFlights.TryRemove(loaded, out _);
                }
            }

            if (fileCount == 0)
            {
                UnityMainThreadDispatcher.AddActionInMainThread(() =>
                {
                    OnWorkspaceLoading?.Invoke(1f);
                    OnWorkspaceEndLoading?.Invoke();
                });
                return;
            }

            int cpuCores = System.Environment.ProcessorCount;
            int maxConcurrency = Math.Clamp(fileCount >= cpuCores ? cpuCores : fileCount, 2, 16);
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);
            List<Task> tasks = new List<Task>();
            int completed = 0;
            object progressLock = new object();

            foreach (string path in videoFiles)
            {
                await semaphore.WaitAsync();

                Task t = Task.Run(async () =>
                {
                    try
                    {
                        await AddOrUpdateFlightAsync(path);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Workspace] Failed to process file '{path}': {ex.Message}");
                    }
                    finally
                    {
                        lock (progressLock)
                        {
                            completed++;
                            float progress = Mathf.Clamp01(fileCount > 0 ? (float)completed / fileCount : 1f);
                            UnityMainThreadDispatcher.AddActionInMainThread(() => OnWorkspaceLoading?.Invoke(progress));
                        }
                        semaphore.Release();
                    }
                });

                tasks.Add(t);
            }

            await Task.WhenAll(tasks);

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                OnWorkspaceLoading?.Invoke(1f);
                OnWorkspaceEndLoading?.Invoke();
            });
        }

        /// <summary>
        /// Add or update a flight entry for the given video path.
        /// First tries the cache (absolute path + file size). If miss, extracts and then saves to cache.
        /// </summary>
        private async Task AddOrUpdateFlightAsync(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            // Avoid concurrent/duplicated processing on the same file
            if (!_inFlightOps.TryAdd(absolutePath, 0))
            {
                return;
            }

            try
            {
                long? stableSize = await WaitForStableFileSizeAsync(absolutePath, 10, 200);
                if (stableSize == null)
                {
                    // File still being written or not accessible; skip quietly
                    return;
                }

                long fileSize = stableSize.Value;

                //Try cache
                bool existsInCache = await CacheManager.FlightFileExistsAsync(absolutePath, fileSize);
                if (existsInCache)
                {
                    FlightFile cached = await CacheManager.LoadFlightFileAsync(absolutePath, fileSize);

                    if (cached != null)
                    {
                        // Ensure textures are created on main thread (safety)
                        UnityMainThreadDispatcher.AddActionInMainThread(() =>
                        {
                            try
                            {
                                cached.DecodeTextures();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[Workspace] DecodeTextures (cached) failed for '{absolutePath}': {ex.Message}");
                            }

                            LoadedFlights[absolutePath] = cached;
                        });

                        return;
                    }
                }

                //Cache miss or failed to load , rebuild from FFmpeg
                FlightFile built = BuildFlightFileFromVideo(absolutePath);

                if (built == null)
                {
                    return;
                }

                //Save to cache (async), then register in memory with textures
                _ = CacheManager.SaveFlightFileAsync(built);

                UnityMainThreadDispatcher.AddActionInMainThread(() =>
                {
                    try
                    {
                        built.DecodeTextures();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Workspace] DecodeTextures (built) failed for '{absolutePath}': {ex.Message}");
                    }

                    LoadedFlights[absolutePath] = built;
                });
            }
            finally
            {
                _inFlightOps.TryRemove(absolutePath, out _);
            }
        }

        /// <summary>
        /// Build a FlightFile by extracting metadata and flight data with FFmpeg.
        /// </summary>
        private FlightFile BuildFlightFileFromVideo(string fullVideoPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullVideoPath) || !File.Exists(fullVideoPath))
                {
                    return null;
                }

                FlightDataContainer container = FFmpegHelper.ExtractOrLoadFlightData(fullVideoPath);
                FFmpegHelper.ExtractVideoMetadata(fullVideoPath, container);

                if (container == null || string.IsNullOrEmpty(container.VideoPath))
                {
                    return null;
                }

                FlightFile tempFile = new FlightFile
                {
                    VideoPath = container.VideoPath,
                    Name = container.Name,
                    Width = container.Width,
                    Height = container.Height,
                    Frequency = container.Frequency,
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

                //Encode thumbnail/map as raw bytes for serialization
                try
                {
                    if (container.Thumbnail != null && container.Thumbnail.Length > 0)
                    {
                        tempFile.ThumbnailData = container.Thumbnail;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Workspace] Could not attach thumbnail bytes: {ex.Message}");
                }

                tempFile.MapData = null;

                return tempFile;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Workspace] BuildFlightFileFromVideo failed for '{fullVideoPath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Wait until the file size is stable across two consecutive checks.
        /// Returns null if the file is not reachable/stable after retries.
        /// </summary>
        private async Task<long?> WaitForStableFileSizeAsync(string absolutePath, int attempts, int delayMs)
        {
            long lastSize = -1;

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    FileInfo fi = new FileInfo(absolutePath);
                    if (!fi.Exists)
                    {
                        return null;
                    }

                    long sizeNow = fi.Length;

                    if (sizeNow > 0 && sizeNow == lastSize)
                    {
                        return sizeNow;
                    }

                    lastSize = sizeNow;
                }
                catch
                {

                }

                await Task.Delay(delayMs);
            }

            //One last try
            try
            {
                FileInfo fi = new FileInfo(absolutePath);
                return fi.Exists ? fi.Length : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool IsVideoSupported(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string ext = Path.GetExtension(path);
            return ext != null && ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region CALLBACKS
        private void OnWorkspacePathChanged(string workspacePath)
        {
            //Switch watcher to new folder and perform initial scan there
            StartWatching(SettingsManager.CurrentSettings.WorkspacePath);
            _ = InitialScanAsync(SettingsManager.CurrentSettings.WorkspacePath);
        }
        private async void OnFsCreatedOrChanged(object sender, FileSystemEventArgs e)
        {
            //We only care about .mp4
            if (!IsVideoSupported(e.FullPath))
            {
                return;
            }

            try
            {
                await AddOrUpdateFlightAsync(e.FullPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Workspace] OnFsCreatedOrChanged error for '{e.FullPath}': {ex.Message}");
            }
        }

        private void OnFsDeleted(object sender, FileSystemEventArgs e)
        {
            if (!IsVideoSupported(e.FullPath))
            {
                return;
            }

            //Remove from in-memory list; do not touch cache
            LoadedFlights.TryRemove(e.FullPath, out _);
        }

        private async void OnFsRenamed(object sender, RenamedEventArgs e)
        {
            //Old file removed from memory
            if (IsVideoSupported(e.OldFullPath))
            {
                LoadedFlights.TryRemove(e.OldFullPath, out _);
            }

            //New file processed (will likely be a cache miss due to new absolute path)
            if (IsVideoSupported(e.FullPath))
            {
                try
                {
                    await AddOrUpdateFlightAsync(e.FullPath);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Workspace] OnFsRenamed error for '{e.FullPath}': {ex.Message}");
                }
            }
        }
        #endregion
    }
}

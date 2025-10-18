using FlightReLive.Core.FFmpeg;
using FlightReLive.Core.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Fu;
using Fu.Framework;
using System.Reflection;

namespace FlightReLive.Core.Library
{
    public class LibraryManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private readonly ConcurrentDictionary<string, byte> _inFlightOps = new ConcurrentDictionary<string, byte>();
        private CancellationTokenSource _importCancellationTokenSource;
        private float _smoothProgress = 0f;
        private bool _importCompleted;
        private int _importTotal;
        private int _importProcessed;
        private int _importSuccess;
        private int _importErrors;
        private string _importCurrentFile = "";
        private Dictionary<string, string> _importErrorList = new();
        #endregion

        #region PROPERTIES
        internal static LibraryManager Instance { get; private set; }

        internal List<RealmFlightItem> LoadedFlights { get; private set; }
        #endregion

        #region EVENTS
        internal event Action OnLibraryStartLoading;
        internal event Action<float> OnLibraryLoading;
        internal event Action OnLibraryEndLoading;
        internal event Action<RealmFlightItem> OnFlightFileSelected;
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
            LoadedFlights = new List<RealmFlightItem>();
        }
        #endregion

        #region METHODS
        internal void SelectFlight(RealmFlightItem file)
        {
            if (file == null)
                return;

            OnFlightFileSelected?.Invoke(file);
        }

        internal void LoadFlightsFromDatabase()
        {
            OnLibraryStartLoading?.Invoke();
            LoadedFlights = DatabaseManager.LoadFlightItems();
            int total = LoadedFlights.Count;

            if (total == 0)
            {
                OnLibraryLoading?.Invoke(1f);
                OnLibraryEndLoading?.Invoke();
                return;
            }

            for (int i = 0; i < total; i++)
            {
                LoadedFlights[i].DecodeTextures();
                float progress = (i + 1f) / total;
                OnLibraryLoading?.Invoke(progress);
            }

            OnLibraryEndLoading?.Invoke();
        }

        /// <summary>
        /// Build a FlightFile by extracting metadata and flight data with FFmpeg.
        /// </summary>
        private async Task BuildFlightFileFromVideo(string fullVideoPath)
        {
            try
            {
                if (string.IsNullOrEmpty(fullVideoPath) || !File.Exists(fullVideoPath))
                {
                    return;
                }

                FlightDataContainer container = FFmpegHelper.ExtractOrLoadFlightData(fullVideoPath);
                FFmpegHelper.ExtractVideoMetadata(fullVideoPath, container);

                RealmFlightItem tempFile = new RealmFlightItem
                {
                    Name = container.Name,
                    Width = container.Width,
                    Height = container.Height,
                    Frequency = container.Frequency,
                    CreationDate = container.CreationDate,
                    EstimateTakeOffPosition = container.EstimateTakeOffPosition,
                    FlightGPSCoordinates = container.FlightGPSCoordinates,
                    HasTakeOffPosition = container.TakeOffPositionAvailable,
                    Duration = container.Duration
                };

                foreach (RealmFlightPointItem item in container.DataPoints)
                {
                    tempFile.DataPoints.Add(item);
                }

                if (container.Thumbnail != null && container.Thumbnail.Length > 0)
                {
                    tempFile.ThumbnailData = container.Thumbnail;
                }

                await DatabaseManager.SaveFlightItemAsync(tempFile);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Clears all flights from Realm and refreshes the library view.
        /// </summary>
        internal async void ClearLibrary()
        {
            try
            {
                await UnityMainThreadDispatcher.AwaitOnMainThread(async () =>
                {
                    List<RealmFlightItem> detachedFlights = new List<RealmFlightItem>(LoadedFlights);
                    LoadedFlights.Clear();
                    await DatabaseManager.ClearAllFlightsAsync();
                    Fugui.Notify("Successful operation", "The flight library has been cleared successfully.", StateType.Info, 3f);
                });
            }
            catch (Exception ex)
            {
                Fugui.Notify("Operation failed", $"Unable to clear flight library.\n{ex.GetBaseException().Message}.", StateType.Danger, 3f);
            }
        }

        /// <summary>
        /// Imports multiple flight videos asynchronously with progress and cancellation.
        /// </summary>
        internal async Task ImportFlights(string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                return;
            }

            _importCancellationTokenSource?.Cancel();
            _importCancellationTokenSource = new CancellationTokenSource();
            _importCompleted = false;
            CancellationToken token = _importCancellationTokenSource.Token;

            _importTotal = paths.Length;
            _importProcessed = 0;
            _importSuccess = 0;
            _importErrors = 0;
            _importCurrentFile = "";
            _importErrorList.Clear();

            OnLibraryStartLoading?.Invoke();
            OnLibraryLoading?.Invoke(0f);

            //Display import modal
            await UnityMainThreadDispatcher.AwaitOnMainThread(async () =>
            {
                Fugui.ShowModal("Importing flight videos", (layout) =>
                {
                    float scale = Fugui.CurrentContext.Scale;
                    float paddingX = 10f;
                    layout.Spacing();
                    float targetProgress = _importTotal > 0 ? (float)_importProcessed / _importTotal : 0f;
                    _smoothProgress = Mathf.Lerp(_smoothProgress, targetProgress, 10f * Time.deltaTime);
                    float progress = _smoothProgress;
                    layout.CenterNextItemH(400f);
                    layout.ProgressBar("##importProgress", progress, new FuElementSize(400f, 6f), ProgressBarTextPosition.None);
                    layout.Spacing();

                    layout.Collapsable("Import details", () =>
                    {
                        using (FuGrid grid = new FuGrid("importDetailsGrid", new FuGridDefinition(2, new float[] { 0.4f, 0.6f }), FuGridFlag.LinesBackground, 2, 2, paddingX))
                        {
                            grid.Text("Current file");
                            grid.FramedText($"{_importCurrentFile}");

                            grid.Text("Files processed");
                            grid.FramedText($"{_importProcessed} / {_importTotal}");

                            grid.Text("Successful imports");
                            grid.FramedText($"{_importSuccess}");

                            grid.Text("Failed imports");
                            grid.FramedText($"{_importErrors}");
                        }

                    }, FuButtonStyle.Collapsable, defaultOpen: true);

                    if (_importErrorList.Count > 0)
                    {
                        layout.Collapsable("Errors log", () =>
                        {
                            using (FuGrid grid = new FuGrid("importErrorsGrid", new FuGridDefinition(2, new float[] { 0.4f, 0.6f }), FuGridFlag.LinesBackground, 2, 2, paddingX))
                            {
                                foreach (KeyValuePair<string, string> fileToErrors in _importErrorList)
                                {
                                    grid.Text($"{Path.GetFileName(fileToErrors.Key)}");
                                    grid.FramedText($"{fileToErrors.Value}");
                                }
                            }
                        }, FuButtonStyle.Danger, defaultOpen: true);
                    }
                },
                FuModalSize.Medium,
                new FuModalButton("Cancel import", () =>
                {
                    if (_importCompleted)
                    {
                        Fugui.CloseModal();
                        return;
                    }

                    _importCancellationTokenSource?.Cancel();
                    Fugui.CancelNextModalClose();
                }, FuButtonStyle.Danger, FuKeysCode.Escape));

                return Task.CompletedTask;
            });

            await Task.Run(async () =>
            {
                foreach (string path in paths)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    _importProcessed++;
                    _importCurrentFile = Path.GetFileName(path);

                    try
                    {
                        if (!_inFlightOps.TryAdd(path, 0))
                        {
                            continue;
                        }

                        await BuildFlightFileFromVideo(path);
                        LoadFlightsFromDatabase();
                        _importSuccess++;
                    }
                    catch (Exception ex)
                    {
                        _importErrors++;
                        _importErrorList[path] = ex.Message;
                    }
                    finally
                    {
                        _inFlightOps.TryRemove(path, out _);
                        await Task.Yield();
                    }
                }
            });

            _importCompleted = true;

            await UnityMainThreadDispatcher.AwaitOnMainThread(async () =>
            {
                OnLibraryLoading?.Invoke(1f);
                OnLibraryEndLoading?.Invoke();

                Fugui.Notify("Import finished", $"Imported {_importSuccess}/{_importTotal} flights ({_importErrors} failed).", _importErrors > 0 ? StateType.Warning : StateType.Success, 4f);

                FieldInfo modalButtonsField = typeof(Fugui).GetField("_modalButtons", BindingFlags.NonPublic | BindingFlags.Static);

                if (modalButtonsField?.GetValue(null) is FuModalButton[] buttons && buttons.Length > 0)
                {
                    buttons[0].Text = "Close window";
                    buttons[0].SetStyle(FuButtonStyle.Default);
                }

                LoadFlightsFromDatabase();
            });
        }
        #endregion
    }
}

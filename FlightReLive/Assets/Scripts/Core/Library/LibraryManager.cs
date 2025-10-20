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
    /// <summary>
    /// Centralized controller for library operations:
    /// importing, displaying, and selecting flights.
    /// Reacts automatically to DatabaseManager changes.
    /// </summary>
    public class LibraryManager : MonoBehaviour
    {
        #region ATTRIBUTES
        private readonly ConcurrentDictionary<string, byte> _inFlightOps = new ConcurrentDictionary<string, byte>();
        private CancellationTokenSource _importCancellationTokenSource;
        private float _smoothProgress;
        private bool _importCompleted;
        private int _importTotal;
        private int _importProcessed;
        private int _importSuccess;
        private int _importErrors;
        private string _importCurrentFile = "";
        private readonly Dictionary<string, string> _importErrorList = new();
        #endregion

        #region PROPERTIES
        internal static LibraryManager Instance { get; private set; }
        internal List<SerializedFlightData> LoadedFlights { get; private set; } = new();
        #endregion

        #region EVENTS
        internal event Action<SerializedFlightData> OnFlightFileSelected;
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
            DatabaseManager.OnFlightsChanged += OnDatabaseChanged;

            //Load initial state
            RefreshLoadedFlights();
        }

        private void OnDestroy()
        {
            DatabaseManager.OnFlightsChanged -= OnDatabaseChanged;
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Refresh list of current flights library
        /// </summary>
        private void RefreshLoadedFlights()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                LoadedFlights = DatabaseManager.GetAllFlights();

                for (int i = 0; i < LoadedFlights.Count; i++)
                {
                    LoadedFlights[i].DecodeTextures();
                }

                Fugui.RefreshWindowsInstances(FlightReLiveWindowsNames.Library);
            });
        }

        /// <summary>
        /// Select a flight and raise the event (loadingManager is listening to this event)
        /// </summary>
        /// <param name="file"></param>
        internal void SelectFlight(SerializedFlightData file)
        {
            if (file == null)
            {
                return;
            }

            OnFlightFileSelected?.Invoke(file);
        }

        /// <summary>
        /// Build a FlightFile by extracting metadata and flight data with FFmpeg.
        /// </summary>
        private void BuildFlightFileFromVideo(string fullVideoPath)
        {
            if (string.IsNullOrEmpty(fullVideoPath) || !File.Exists(fullVideoPath))
            {
                return;
            }

            FlightDataContainer container = FFmpegHelper.ExtractOrLoadFlightData(fullVideoPath);
            FFmpegHelper.ExtractVideoMetadata(fullVideoPath, container);

            SerializedFlightData tempFile = new SerializedFlightData
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

            tempFile.ComputeUniqueKey();

            foreach (SerializedFlightDataPoint item in container.DataPoints)
            {
                tempFile.DataPoints.Add(item);
            }

            if (container.Thumbnail is { Length: > 0 })
            {
                tempFile.ThumbnailData = container.Thumbnail;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                DatabaseManager.SaveFlight(tempFile);
            });
        }

        /// <summary>
        /// Clears all flights from Realm.
        /// </summary>
        internal void ClearLibrary()
        {
            try
            {
                DatabaseManager.ClearAllFlights();
                Fugui.Notify("Successful operation", "The flight library has been cleared successfully.", StateType.Info, 3f);
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

            // Display progress modal
            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                Fugui.ShowModal("Importing flight videos", (layout) =>
                {
                    float targetProgress = _importTotal > 0 ? (float)_importProcessed / _importTotal : 0f;
                    float paddingX = 10f;
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

                            grid.Text("Processed");
                            grid.FramedText($"{_importProcessed} / {_importTotal}");

                            grid.Text("Success");
                            grid.FramedText($"{_importSuccess}");

                            grid.Text("Errors");
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

            await Task.Run(() =>
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

                        BuildFlightFileFromVideo(path);
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
                    }
                }
            });

            _importCompleted = true;

            await UnityMainThreadDispatcher.AwaitOnMainThread(() =>
            {
                FieldInfo modalButtonsField = typeof(Fugui).GetField("_modalButtons", BindingFlags.NonPublic | BindingFlags.Static);

                if (modalButtonsField?.GetValue(null) is FuModalButton[] buttons && buttons.Length > 0)
                {
                    buttons[0].Text = "Close window";
                    buttons[0].SetStyle(FuButtonStyle.Default);
                }

                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Delete a flight from library
        /// </summary>
        /// <param name="flight"></param>
        internal void DeleteFlightItem(SerializedFlightData flight)
        {
            if (flight == null)
            {
                return;
            }

            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                DatabaseManager.DeleteFlight(flight.UniqueKey);
            });
        }
        #endregion


        #region CALLBACKS
        /// <summary>
        /// Triggered whenever the Realm data changes (add/update/delete).
        /// </summary>
        private void OnDatabaseChanged()
        {
            UnityMainThreadDispatcher.AddActionInMainThread(() =>
            {
                RefreshLoadedFlights();
            });
        }
        #endregion
    }
}

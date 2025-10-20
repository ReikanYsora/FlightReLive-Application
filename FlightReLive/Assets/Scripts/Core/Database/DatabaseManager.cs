using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FlightReLive.Core.Database
{
    /// <summary>
    /// Centralized manager for all flight storage operations using MessagePack.
    /// Replaces the Realm-based implementation with a file-based system.
    /// </summary>
    public static class DatabaseManager
    {
        #region CONSTANTS
        private const string LIBRARY_FOLDER_NAME = "Library";
        private const string FILE_EXTENSION = ".msgpack";
        #endregion

        #region ATTRIBUTES
        private static string _libraryPath;
        private static readonly object _fileLock = new object();
        #endregion

        #region EVENTS
        internal static event Action OnFlightsChanged;
        #endregion

        #region INITIALIZATION
        internal static void Initialize()
        {
            _libraryPath = Path.Combine(Application.persistentDataPath, LIBRARY_FOLDER_NAME);

            if (!Directory.Exists(_libraryPath))
            {
                Directory.CreateDirectory(_libraryPath);
            }
            else
            {
                Debug.Log($"[DatabaseManager] Using existing library folder: {_libraryPath}");
            }
        }
        #endregion

        #region SAVE METHODS
        /// <summary>
        /// Saves or updates a single flight file.
        /// </summary>
        internal static void SaveFlight(SerializedFlightData item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(item.UniqueKey))
                {
                    item.ComputeUniqueKey();
                }

                string filePath = GetFlightFilePath(item.UniqueKey);

                // Encode texture if needed
                item.EncodeTextures();

                lock (_fileLock)
                {
                    byte[] bytes = MessagePackSerializer.Serialize(item);
                    File.WriteAllBytes(filePath, bytes);
                }

                OnFlightsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] SaveFlight failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves or updates multiple flights at once.
        /// </summary>
        internal static void SaveFlights(IEnumerable<SerializedFlightData> items)
        {
            if (items == null)
            {
                return;
            }

            try
            {
                foreach (SerializedFlightData item in items)
                {
                    SaveFlight(item);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] SaveFlights failed: {ex.Message}");
            }
        }
        #endregion

        #region LOAD METHODS
        /// <summary>
        /// Loads all stored flights from disk.
        /// </summary>
        internal static List<SerializedFlightData> GetAllFlights()
        {
            List<SerializedFlightData> flights = new List<SerializedFlightData>();

            try
            {
                if (!Directory.Exists(_libraryPath))
                {
                    Directory.CreateDirectory(_libraryPath);
                    return flights;
                }

                string[] files = Directory.GetFiles(_libraryPath, "*" + FILE_EXTENSION);

                foreach (string file in files)
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(file);
                        SerializedFlightData flight = MessagePackSerializer.Deserialize<SerializedFlightData>(data);
                        flights.Add(flight);
                    }
                    catch (Exception innerEx)
                    {
                        Debug.LogWarning($"[DatabaseManager] Failed to load flight {Path.GetFileName(file)}: {innerEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] GetAllFlights failed: {ex.Message}");
            }

            return flights;
        }

        // <summary>
        /// Returns all flight file paths available in the library.
        /// </summary>
        internal static List<string> GetAllFlightFiles()
        {
            List<string> files = new List<string>();

            try
            {
                if (!Directory.Exists(_libraryPath))
                {
                    Directory.CreateDirectory(_libraryPath);
                    return files;
                }

                files.AddRange(Directory.GetFiles(_libraryPath, "*" + FILE_EXTENSION));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] GetAllFlightFiles failed: {ex.Message}");
            }

            return files;
        }

        /// <summary>
        /// Loads a single flight file from disk.
        /// </summary>
        internal static SerializedFlightData LoadFlight(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[DatabaseManager] LoadFlight: file not found: {filePath}");
                    return null;
                }

                byte[] data = File.ReadAllBytes(filePath);
                SerializedFlightData flight = MessagePackSerializer.Deserialize<SerializedFlightData>(data);
                return flight;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DatabaseManager] Failed to load flight {Path.GetFileName(filePath)}: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region DELETE METHODS
        /// <summary>
        /// Deletes a single flight by unique ID or key.
        /// </summary>
        internal static void DeleteFlight(string uniqueKey)
        {
            try
            {
                string filePath = GetFlightFilePath(uniqueKey);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    OnFlightsChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] DeleteFlight failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes multiple flights by their unique keys.
        /// </summary>
        internal static void DeleteFlights(IEnumerable<string> uniqueKeys)
        {
            if (uniqueKeys == null)
            {
                return;
            }

            foreach (string key in uniqueKeys)
            {
                DeleteFlight(key);
            }
        }

        /// <summary>
        /// Clears the entire library.
        /// </summary>
        internal static void ClearAllFlights()
        {
            try
            {
                if (Directory.Exists(_libraryPath))
                {
                    Directory.Delete(_libraryPath, true);
                }

                Directory.CreateDirectory(_libraryPath);
                OnFlightsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DatabaseManager] ClearAllFlights failed: {ex.Message}");
            }
        }
        #endregion

        #region UTILS
        private static string GetFlightFilePath(string uniqueKey)
        {
            return Path.Combine(_libraryPath, $"{uniqueKey}{FILE_EXTENSION}");
        }
        #endregion
    }
}

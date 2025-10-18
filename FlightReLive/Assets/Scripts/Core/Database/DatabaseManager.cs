using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FlightReLive.Core.FFmpeg;
using Realms;

namespace FlightReLive.Core.Database
{
    /// <summary>
    /// Centralized manager for all Realm database operations.
    /// Handles FlightItem storage, retrieval, and lifecycle management.
    /// </summary>
    public static class DatabaseManager
    {
        #region CONSTANTS
        private const string REALM_DATABASE_NAME = "FlightReLive.realm";
        #endregion

        #region ATTRIBUTES
        private static Realm _realm;
        #endregion

        #region INITIALIZATION
        /// <summary>
        /// Initializes the Realm instance if not already done (async and thread-safe).
        /// </summary>
        internal static void Initialize()
        {
            if (_realm != null)
            {
                return;
            }

            try
            {
                RealmConfiguration config = new RealmConfiguration(REALM_DATABASE_NAME)
                {
                    ShouldDeleteIfMigrationNeeded = false,
                    SchemaVersion = 1
                };

                _realm = Realm.GetInstance(config);
            }
            catch (Exception) { }
        }
        #endregion

        #region METHODS
        internal static void ImportFlight(string[] videoPaths)
        {
            foreach (string videoPath in videoPaths)
            {
                FFmpegHelper.ExtractFlightData(videoPath);
            }
        }

        /// <summary>
        /// Loads all stored flight items from Realm.
        /// </summary>
        /// <returns>List of all RealmFlightItem objects stored in the local Realm database.</returns>
        internal static List<RealmFlightItem> LoadFlightItems()
        {
            try
            {
                if (_realm == null)
                {
                    return new List<RealmFlightItem>();
                }

                IQueryable<RealmFlightItem> query = _realm.All<RealmFlightItem>();
                return query.ToList();
            }
            catch (Exception)
            {
                return new List<RealmFlightItem>();
            }
        }

        /// <summary>
        /// Saves (or updates) a flight item in Realm.
        /// If an existing flight with the same Id exists, it will be updated.
        /// </summary>
        internal static async Task SaveFlightItemAsync(RealmFlightItem item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                await UnityMainThreadDispatcher.AwaitOnMainThread(async () =>
                {
                    _realm.Write(() => { _realm.Add(item, update: true); });
                    return Task.CompletedTask;
                });
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Returns the number of stored flight items.
        /// </summary>
        internal static int FlightItemsCount()
        {
            try
            {
                return _realm?.All<RealmFlightItem>().Count() ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Deletes a flight from Realm by its Id.
        /// </summary>
        internal static async Task<bool> DeleteFlightItemAsync(string id)
        {
            try
            {
                RealmFlightItem toDelete = _realm.Find<RealmFlightItem>(id);
                if (toDelete == null)
                {
                    return false;
                }

                await _realm.WriteAsync(() =>
                {
                    _realm.Remove(toDelete);
                });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Clears all stored flights.
        /// </summary>
        internal static async Task ClearAllFlightsAsync()
        {
            try
            {
                await _realm.WriteAsync(() =>
                {
                    _realm.RemoveAll<RealmFlightItem>();
                });
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Closes the Realm database safely (e.g. on app quit).
        /// </summary>
        internal static void Close()
        {
            if (_realm != null)
            {
                _realm.Dispose();
                _realm = null;
            }
        }
        #endregion
    }
}

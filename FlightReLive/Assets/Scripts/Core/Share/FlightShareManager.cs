using FlightReLive.Core.Loading;
using Fu;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using FlightReLive.Core.Database;

namespace FlightReLive.Core.Share
{
    public static class FlightShareManager
    {
        /// <summary>
        /// Import a shared FlightFile (.FRS) into the current workspace.
        /// </summary>
        /// <param name="importPath">Path to the .FRS file</param>
        internal static bool ImportAsync(string importPath)
        {
            if (string.IsNullOrEmpty(importPath) || !File.Exists(importPath))
            {
                Debug.LogWarning($"[FlightShareManager] Import failed: invalid path {importPath}");

                return false;
            }

            try
            {
                //Load FlightFile from .FRS
                SerializedFlightData importedFile = null; //await CacheManager.ImportFlightFileAsync(importPath);

                if (importedFile == null)
                {
                    throw new Exception("Incorrect format file. Import aborted.");
                }

                //Add it into the workspace (WorkspaceManager will handle conflicts)
                LoadingManager.Instance.StartLoadingScene(importedFile);

                return true;
            }
            catch (Exception ex)
            {
                Fugui.Notify("Import failed", "Incorrect format file. Import aborted.", Fu.Framework.StateType.Danger, 3f);
                Debug.LogError($"[FlightShareManager] Import failed: {ex.Message}");
                return false;
            }
        }
    }
}

using System;
using System.IO;
using System.Threading;
using Fu;
using UnityEngine;

namespace FlightReLive.Core.Layouts
{
    internal class LayoutManager : MonoBehaviour
    {
        #region CONSTANTS
        private const string LAYOUT_PATH = "Fugui/Layouts";
        private const string DEFAULT_LAYOUT_NAME = "Default";
        private const string USER_LAYOUT_NAME = "User";
        private const int SAVE_TIMEOUT_MS = 5000; // 5s max
        #endregion

        #region PROPERTIES
        public static LayoutManager Instance { get; private set; }
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
            LoadLayout();
        }

        private void OnApplicationQuit()
        {
            SaveCurrentLayout();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Sauvegarde du layout utilisateur avant de quitter
        /// </summary>
        private void SaveCurrentLayout()
        {
            string layoutPath = Path.Combine(Application.streamingAssetsPath, LAYOUT_PATH);

            FuDockingLayoutDefinition tempLayout = Fugui.Layouts.GenerateCurrentLayout();
            if (tempLayout == null)
            {
                Debug.LogWarning("[LayoutSaver] GenerateCurrentLayout returned null layout.");
            }
            else
            {
                tempLayout.Name = USER_LAYOUT_NAME;
                Fugui.Layouts.SaveLayoutFile(layoutPath, tempLayout);
                Debug.Log("[LayoutSaver] Layout file written.");
            }
        }

        private void LoadLayout()
        {
            string layoutPath = Path.Combine(Application.streamingAssetsPath, LAYOUT_PATH);
            Fugui.Layouts.LoadLayouts(layoutPath);

            if (Fugui.Layouts.Layouts.ContainsKey(USER_LAYOUT_NAME))
            {
                Fugui.Layouts.SetLayout(USER_LAYOUT_NAME);
            }
            else
            {
                Fugui.Layouts.SetLayout(DEFAULT_LAYOUT_NAME);
            }
        }

        internal void RestoreDefaultLayout()
        {
            string layoutPath = Path.Combine(Application.streamingAssetsPath, LAYOUT_PATH);
            Fugui.Layouts.LoadLayouts(layoutPath);

            if (Fugui.Layouts.Layouts.ContainsKey(DEFAULT_LAYOUT_NAME))
            {
                Fugui.Layouts.SetLayout(DEFAULT_LAYOUT_NAME);
            }
        }
        #endregion
    }
}

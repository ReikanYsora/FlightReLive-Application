using System.IO;
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
        /// Save user layout before exit
        /// </summary>
        private void SaveCurrentLayout()
        {
            string layoutPath = Path.Combine(Application.streamingAssetsPath, LAYOUT_PATH);

            FuDockingLayoutDefinition tempLayout = Fugui.Layouts.GenerateCurrentLayout();
            if (tempLayout != null)
            {
                tempLayout.Name = USER_LAYOUT_NAME;
                Fugui.Layouts.SaveLayoutFile(layoutPath, tempLayout);
            }
        }

        /// <summary>
        /// Load user layout
        /// </summary>
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

        /// <summary>
        /// Resotre default layout
        /// </summary>
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

using System.IO;
using Fu;
using UnityEngine;

namespace FlightReLive.Core.Layouts
{
    internal class LayoutManager : MonoBehaviour
    {
        #region CONSTANTS
        private const string LAYOUT_FOLDER = "Fugui/Layouts";
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
            LoadUserLayout();
        }

        private void OnEnable()
        {
            Application.wantsToQuit += OnWantsToQuit;
        }

        private void OnDisable()
        {
            Application.wantsToQuit -= OnWantsToQuit;
        }

        private bool OnWantsToQuit()
        {
            SaveCurrentLayout();
            return true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveCurrentLayout();
            }
        }

        private void OnApplicationQuit()
        {
            SaveCurrentLayout();
        }
        #endregion

        #region METHODS
        /// <summary>
        /// Save current layout
        /// </summary>
        private void SaveCurrentLayout()
        {
            string userLayoutPath = Path.Combine(Application.persistentDataPath, LAYOUT_FOLDER);

            if (!Directory.Exists(userLayoutPath))
            {
                Directory.CreateDirectory(userLayoutPath);
            }

            FuDockingLayoutDefinition tempLayout = Fugui.Layouts.GenerateCurrentLayout();

            if (tempLayout != null)
            {
                tempLayout.Name = USER_LAYOUT_NAME;
                Fugui.Layouts.SaveLayoutFile(userLayoutPath, tempLayout, false);
            }
        }

        /// <summary>
        /// Load user or default layout
        /// </summary>
        private void LoadUserLayout()
        {
            //Load defaults layout first
            string defaultLayoutPath = Path.Combine(Application.streamingAssetsPath, LAYOUT_FOLDER);
            Fugui.Layouts.LoadLayouts(defaultLayoutPath);

            //Load user layout
            string userLayoutPath = Path.Combine(Application.persistentDataPath, LAYOUT_FOLDER);
            if (Directory.Exists(userLayoutPath))
            {
                Fugui.Layouts.LoadLayouts(userLayoutPath);
            }

            //Load user layout if found first
            if (Fugui.Layouts.Layouts.ContainsKey(USER_LAYOUT_NAME))
            {
                Fugui.Layouts.SetLayout(USER_LAYOUT_NAME);
            }
            else if (Fugui.Layouts.Layouts.ContainsKey(DEFAULT_LAYOUT_NAME))
            {
                Fugui.Layouts.SetLayout(DEFAULT_LAYOUT_NAME);
            }
        }


        /// <summary>
        /// REstore default app layout
        /// </summary>
        internal void RestoreDefaultLayout()
        {
            string defaultLayoutPath = Path.Combine(Application.streamingAssetsPath, LAYOUT_FOLDER);
            Fugui.Layouts.LoadLayouts(defaultLayoutPath);

            if (Fugui.Layouts.Layouts.ContainsKey(DEFAULT_LAYOUT_NAME))
            {
                Fugui.Layouts.SetLayout(DEFAULT_LAYOUT_NAME);
            }

            //Delete user layout
            string userLayoutPath = Path.Combine(Application.persistentDataPath, LAYOUT_FOLDER);
            string userFile = Path.Combine(userLayoutPath, USER_LAYOUT_NAME + ".fdl");

            if (File.Exists(userFile))
            {
                File.Delete(userFile);
            }
        }
        #endregion
    }
}

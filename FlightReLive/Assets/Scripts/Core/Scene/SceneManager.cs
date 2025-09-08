using FlightReLive.Core.Building;
using FlightReLive.Core.Settings;
using FlightReLive.Core.WorldUI;
using Fu.Framework;
using UnityEngine;

namespace FlightReLive.Core.Scene
{
    public class SceneManager : MonoBehaviour
    {
        #region ATTRIBUTES
        #endregion

        #region PROPERTIES
        public static SceneManager Instance { get; private set; }
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
        #endregion

        #region UI
        internal void DrawSceneSettings(FuLayout layout)
        {
            using (FuGrid grid = new FuGrid("gridMapSettings", new FuGridDefinition(2, new float[2] { 0.3f, 0.7f }), FuGridFlag.AutoToolTipsOnLabels, rowsPadding: 3f, outterPadding:10))
            {
                WorldUIManager.Instance.DisplayWorldUISettings(grid);
                BuildingManager.Instance.DisplayBuildingsSettings(grid);
            }
        }
        #endregion
    }
}

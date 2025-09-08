using UnityEngine;

namespace FlightReLive.Core.Rendering
{
    [CreateAssetMenu(fileName = "SkyboxProfile", menuName = "FlightReLive/Skybox Profile")]
    public class SkyboxProfile : ScriptableObject
    {
        #region ATTRIBUTES
        [Range(0, 23)] public int startHour;
        public Material skyboxMaterial;
        public Color sunColor;
        #endregion
    }
}

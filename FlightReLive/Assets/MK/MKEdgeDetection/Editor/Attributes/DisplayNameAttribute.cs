/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/
#if UNITY_EDITOR
using UnityEngine;

namespace MK.EdgeDetection.Editor
{
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class DisplayNameAttribute : PropertyAttribute
    {
        public string displayName { get ; private set; }
        
        public DisplayNameAttribute(string displayName)
        {
            this.displayName = displayName;
        }
    }
}
#endif
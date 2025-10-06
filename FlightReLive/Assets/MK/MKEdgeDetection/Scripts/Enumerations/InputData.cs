/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MK.EdgeDetection
{
    [System.Flags]
    public enum InputData
    {
        None = 0,
        [InspectorName("Depth")]
        Depth = 1 << 0,
        [InspectorName("Normal")]
        Normal = 1 << 1,
        [InspectorName("Scene Color")]
        SceneColor = 1 << 2,
        All = ~None
    }
}
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
    public enum LargeKernel
    {
        [InspectorName("Sobel")]
        Sobel,
        [InspectorName("Scharr")]
        Scharr,
        [InspectorName("Prewitt")]
        Prewitt
    }
}
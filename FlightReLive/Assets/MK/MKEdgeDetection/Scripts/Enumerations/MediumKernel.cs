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
    public enum MediumKernel
    {
        [InspectorName("Roberts Cross (Diagonal)")]
        RobertsCrossDiagonal,
        [InspectorName("Roberts Cross (Axis)")]
        RobertsCrossAxis
    }
}
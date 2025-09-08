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
    public enum Workflow
    {
        [InspectorName("Generic")]
        Generic = 0,
        #if MK_URP_COMPONENT && MK_SELECTIVE_MASK_ENABLED
        [InspectorName("Selective")]
        Selective = 1,
        #endif
    }
}
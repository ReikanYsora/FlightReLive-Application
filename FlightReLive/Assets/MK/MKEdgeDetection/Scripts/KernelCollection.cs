/*****************************************************
Copyright © 2024 Michael Kremmel
https://www.michaelkremmel.de
All rights reserved
*****************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using MK.EdgeDetection.PostProcessing.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MK.EdgeDetection
{
    public static class KernelCollection
    {
        #if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void Validate()
        {
            MK.EdgeDetection.Generic.Editor.Utility.ValidateDictionary(_sobelKernelLookup);
            MK.EdgeDetection.Generic.Editor.Utility.ValidateDictionary(_robertsCrossKernelLookup);
            MK.EdgeDetection.Generic.Editor.Utility.ValidateDictionary(_setupMethodLookup);
        }
        #endif

        public static void SetKernels(MaterialPropertyBlock materialPropertyBlock, Material material, Workflow workflow, Precision precision, LargeKernel largeKernel, MediumKernel mediumKernel, SmallKernel smallKernel)
        {
            workflow = Workflow.Generic;
            if(workflow == Workflow.Generic)
            {
                System.Action<MaterialPropertyBlock, LargeKernel, MediumKernel, SmallKernel, Material> action;
                _setupMethodLookup.TryGetValue(precision, out action);
                action.Invoke(materialPropertyBlock, largeKernel, mediumKernel, smallKernel, material);
            }
            else
            {
                SetupSelectiveWorkflow(materialPropertyBlock, mediumKernel, material);
            }
        }

        private const float kRobertsCrossNormalizer = 3;
        private const float kPrewittNormalizer = 1.33f;
        private const float kHalfCrossNormalizer = 1.33f;
        private const float kScharrNormalizer = (1.0f / 47.0f) * 0.725f;

        //All kernels aligned with sobel
        private static readonly Matrix4x4 _robertsCrossDiagonalX = new Matrix4x4
        (
            new Vector4(1, 0, 0, 0) * kRobertsCrossNormalizer,
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, -1, 0) * kRobertsCrossNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _robertsCrossDiagonalY = new Matrix4x4
        (
            new Vector4(0, 0, 1, 0) * kRobertsCrossNormalizer,
            new Vector4(0, 0, 0, 0),
            new Vector4(-1, 0, 0, 0) * kRobertsCrossNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _robertsCrossAxisX = new Matrix4x4
        (
            new Vector4(0, 0, 0, 0),
            new Vector4(-1, 0, 1, 0) * kRobertsCrossNormalizer,
            new Vector4(0, 0, 0, 0),
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _robertsCrossAxisY = new Matrix4x4
        (
            new Vector4(0, 1, 0, 0) * kRobertsCrossNormalizer,
            new Vector4(0, 0, 0, 0),
            new Vector4(0, -1, 0, 0) * kRobertsCrossNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _sobelX = new Matrix4x4
        (
            new Vector4(-1, 0, 1, 0),
            new Vector4(-2, 0, 2, 0),
            new Vector4(-1, 0, 1, 0),
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _sobelY = new Matrix4x4
        (
            new Vector4(1, 2, 1, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(-1, -2, -1, 0),
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _prewittX = new Matrix4x4
        (
            new Vector4(-1, 0, 1, 0) * kPrewittNormalizer,
            new Vector4(-1, 0, 1, 0) * kPrewittNormalizer,
            new Vector4(-1, 0, 1, 0) * kPrewittNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _prewittY = new Matrix4x4
        (
            new Vector4(1, 1, 1, 0) * kPrewittNormalizer,
            new Vector4(0, 0, 0, 0),
            new Vector4(-1, -1, -1, 0) * kPrewittNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _scharrX = new Matrix4x4
        (
            new Vector4(47, 0, -47, 0) * kScharrNormalizer,
            new Vector4(162, 0, -162, 0) * kScharrNormalizer,
            new Vector4(47, 0, -47, 0) * kScharrNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _scharrY = new Matrix4x4
        (
            new Vector4(47, 162, 47, 0) * kScharrNormalizer,
            new Vector4(0, 0, 0, 0) * kScharrNormalizer,
            new Vector4(-47, -162, -47, 0) * kScharrNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _halfCrossX = new Matrix4x4
        (
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 2, 0, 0) * kHalfCrossNormalizer,
            new Vector4(-1, 0, -1, 0) * kHalfCrossNormalizer,
            Vector4.zero
        ).transpose;
        private static readonly Matrix4x4 _halfCrossY = new Matrix4x4
        (
            new Vector4(-1, 0, 0, 0) * kHalfCrossNormalizer,
            new Vector4(0, 2, 0, 0) * kHalfCrossNormalizer,
            new Vector4(-1, 0, 0, 0) * kHalfCrossNormalizer,
            Vector4.zero
        ).transpose;

        private static readonly Dictionary<LargeKernel, ValueTuple<Matrix4x4, Matrix4x4>> _sobelKernelLookup = new Dictionary<LargeKernel, ValueTuple<Matrix4x4, Matrix4x4>>()
        {
            {
                LargeKernel.Sobel,
                new (_sobelX, _sobelY)
            },
            {
                LargeKernel.Prewitt,
                new (_prewittX, _prewittY)
            },
            {
                LargeKernel.Scharr,
                new (_scharrX, _scharrY)
            }
        };
        private static readonly Dictionary<MediumKernel, ValueTuple<Matrix4x4, Matrix4x4>> _robertsCrossKernelLookup = new Dictionary<MediumKernel, ValueTuple<Matrix4x4, Matrix4x4>>()
        {
            {
                MediumKernel.RobertsCrossDiagonal,
                new (_robertsCrossDiagonalX, _robertsCrossDiagonalY)
            },
            {
                MediumKernel.RobertsCrossAxis,
                new (_robertsCrossAxisX, _robertsCrossAxisY)
            }
        };

        private static void SetupSelectiveWorkflow(MaterialPropertyBlock materialPropertyBlock, MediumKernel mediumKernel, Material material)
        {
            ValueTuple<Matrix4x4, Matrix4x4> kernels;
            _robertsCrossKernelLookup.TryGetValue(mediumKernel, out kernels);
            materialPropertyBlock.SetMatrix(PropertyIDs.kernelX, kernels.Item1);
            materialPropertyBlock.SetMatrix(PropertyIDs.kernelY, kernels.Item2);
            bool utilizeAxis = mediumKernel == MediumKernel.RobertsCrossAxis ? true : false;
            Keywords.SetKeyword(material, Keywords.utilizeAxisOnly, utilizeAxis);
            Keywords.SetKeyword(material, Keywords.precisionMedium, true);
            Keywords.SetKeyword(material, Keywords.precisionLow, false);
        }

        private static readonly Dictionary<Precision, System.Action<MaterialPropertyBlock, LargeKernel, MediumKernel, SmallKernel, Material>> _setupMethodLookup = new Dictionary<Precision, Action<MaterialPropertyBlock, LargeKernel, MediumKernel, SmallKernel, Material>>()
        {
            {
                Precision.High,
                (materialPropertyBlock, largeKernel, mediumKernel, smallKernel, material) => 
                {
                    ValueTuple<Matrix4x4, Matrix4x4> kernels;
                    _sobelKernelLookup.TryGetValue(largeKernel, out kernels);
                    materialPropertyBlock.SetMatrix(PropertyIDs.kernelX, kernels.Item1);
                    materialPropertyBlock.SetMatrix(PropertyIDs.kernelY, kernels.Item2);
                    Keywords.SetKeyword(material, Keywords.utilizeAxisOnly, false);
                    Keywords.SetKeyword(material, Keywords.precisionMedium, false);
                    Keywords.SetKeyword(material, Keywords.precisionLow, false);
                }
            },
            {
                Precision.Medium,
                (materialPropertyBlock, largeKernel, mediumKernel, smallKernel, material) => 
                {
                    ValueTuple<Matrix4x4, Matrix4x4> kernels;
                    _robertsCrossKernelLookup.TryGetValue(mediumKernel, out kernels);
                    materialPropertyBlock.SetMatrix(PropertyIDs.kernelX, kernels.Item1);
                    materialPropertyBlock.SetMatrix(PropertyIDs.kernelY, kernels.Item2);
                    bool utilizeAxis = mediumKernel == MediumKernel.RobertsCrossAxis ? true : false;
                    Keywords.SetKeyword(material, Keywords.utilizeAxisOnly, utilizeAxis);
                    Keywords.SetKeyword(material, Keywords.precisionMedium, true);
                    Keywords.SetKeyword(material, Keywords.precisionLow, false);
                }
            },
            {
                Precision.Low,
                (materialPropertyBlock, largeKernel, mediumKernel, smallKernel, material) => 
                {
                    materialPropertyBlock.SetMatrix(PropertyIDs.kernelX, _halfCrossX);
                    materialPropertyBlock.SetMatrix(PropertyIDs.kernelY, _halfCrossY);
                    bool utilizeHorizontal = smallKernel == SmallKernel.HalfCrossHorizontal ? true : false;
                    Keywords.SetKeyword(material, Keywords.utilizeAxisOnly, utilizeHorizontal);
                    Keywords.SetKeyword(material, Keywords.precisionMedium, false);
                    Keywords.SetKeyword(material, Keywords.precisionLow, true);
                }
            }
        };
    }
}
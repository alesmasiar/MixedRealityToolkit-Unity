﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Internal.Utilities.Lines.DataProviders
{
    /// <summary>
    /// Base Parabola line data provider.
    /// </summary>
    public abstract class ParabolaLineDataProvider : BaseMixedRealityLineDataProvider
    {
        [SerializeField]
        private Vector3 startPoint = Vector3.zero;

        /// <summary>
        /// The Starting point of this line.
        /// </summary>
        /// <remarks>Always located at this <see cref="GameObject"/>'s <see cref="Transform.position"/></remarks>
        public Vector3 StartPoint => startPoint;

        #region Monobehaviour Implementation

        protected override void OnValidate()
        {
            startPoint = LineTransform.position;
        }

        #endregion Monobehaviour Implementation

        #region Line Data Provider Implementation

        protected override float GetUnClampedWorldLengthInternal()
        {
            // Crude approximation
            // TODO optimize
            float distance = 0f;
            Vector3 last = GetUnClampedPoint(0f);
            for (int i = 1; i < 10; i++)
            {
                Vector3 current = GetUnClampedPoint((float)i / 10);
                distance += Vector3.Distance(last, current);
            }

            return distance;
        }

        protected override Vector3 GetUpVectorInternal(float normalizedLength)
        {
            return transform.up;
        }

        #endregion Line Data Provider Implementation
    }
}
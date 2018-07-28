﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Internal.Utilities.Lines.DataProviders
{
    /// <summary>
    /// A simple line with two points.
    /// </summary>
    public class SimpleLineDataProvider : BaseMixedRealityLineDataProvider
    {
        [SerializeField]
        private Vector3 start = Vector3.zero;

        /// <summary>
        /// The Starting point of this line.
        /// </summary>
        /// <remarks>Always located at this <see cref="GameObject"/>'s <see cref="Transform.position"/></remarks>
        public Vector3 Start
        {
            get { return start; }
            private set { start = value; }
        }

        [SerializeField]
        [Tooltip("The point where this line will end.\nNote: Start point is always located at the GameObject's transform position.")]
        private Vector3 endPoint = Vector3.zero;

        /// <summary>
        /// The point where this line will end.
        /// </summary>
        public Vector3 EndPoint
        {
            get { return endPoint; }
            set { endPoint = value; }
        }

        #region Monobehaviour Implementation

        protected override void OnValidate()
        {
            base.OnValidate();

            start = LineTransform.position;

            if (endPoint == start)
            {
                endPoint = start + Vector3.forward;
            }
        }

        #endregion Monobehaviour Implementation

        #region Line Data Provider Implementation

        /// <inheritdoc />
        public override int PointCount => 2;

        /// <inheritdoc />
        protected override Vector3 GetPointInternal(int pointIndex)
        {
            switch (pointIndex)
            {
                case 0:
                    return start;
                case 1:
                    return endPoint;
                default:
                    Debug.LogError("Invalid point index");
                    return Vector3.zero;
            }
        }

        /// <inheritdoc />
        protected override void SetPointInternal(int pointIndex, Vector3 point)
        {
            switch (pointIndex)
            {
                case 0:
                    start = point;
                    break;
                case 1:
                    endPoint = point;
                    break;
                default:
                    Debug.LogError("Invalid point index");
                    break;
            }
        }

        /// <inheritdoc />
        protected override Vector3 GetPointInternal(float normalizedDistance)
        {
            return Vector3.Lerp(start, endPoint, normalizedDistance);
        }

        /// <inheritdoc />
        protected override float GetUnClampedWorldLengthInternal()
        {
            return Vector3.Distance(start, endPoint);
        }

        /// <inheritdoc />
        protected override Vector3 GetUpVectorInternal(float normalizedLength)
        {
            return transform.up;
        }

        #endregion Line Data Provider Implementation
    }
}
﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;

namespace HoloToolkit.Unity
{
    /// <summary>
    ///  InBetween solver positions an object in-between two tracked transforms.
    /// </summary>
    public class SolverInBetween : Solver
    {
        [SerializeField]
        [Tooltip("Distance along the center line the object will be located. 0.5 is halfway, 1.0 is at the second transform, 0.0 is at the first transform.")]
        private float partwayOffset = 0.5f;

        public float PartwayOffset
        {
            get
            {
                return partwayOffset;
            }

            set
            {
                partwayOffset = value;
            }
        }

        [SerializeField]
        [Tooltip("Tracked object to calculate position and orientation for the second object. If you want to manually override and use a scene object, use the TransformTarget field")]
        [HideInInspector]
        private SolverHandler.TrackedObjectToReferenceEnum trackedObjectForSecondTransform = SolverHandler.TrackedObjectToReferenceEnum.Head;

        public SolverHandler.TrackedObjectToReferenceEnum TrackedObjectForSecondTransform
        {
            get { return trackedObjectForSecondTransform; }
            set
            {
                trackedObjectForSecondTransform = value;
                if (secondSolverHandler != null)
                {
                    secondSolverHandler.TrackedObjectToReference = value;
                }
            }
        }

        [SerializeField]
        [Tooltip("This transform overrides any Tracked Object as the second point in the In Between.")]
        private Transform secondTransformOverride = null;

        private SolverHandler secondSolverHandler;

        private void OnValidate()
        {
            UpdateSecondSolverHandler();
        }

        protected void Start()
        {
            // We need to get the secondSolverHandler ready before we tell them both to seek a tracked object.
            secondSolverHandler = gameObject.AddComponent<SolverHandler>();
            secondSolverHandler.UpdateSolvers = false;
            UpdateSecondSolverHandler();
        }

        public override void SolverUpdate()
        {
            if (solverHandler != null && secondSolverHandler != null)
            {
                if (solverHandler.TransformTarget != null && secondSolverHandler.TransformTarget != null)
                {
                    AdjustPositionForOffset(solverHandler.TransformTarget, secondSolverHandler.TransformTarget);
                }
            }
        }

        private void AdjustPositionForOffset(Transform targetTransform, Transform secondTransform)
        {
            if (targetTransform != null && secondTransform != null)
            {

                Vector3 centerline = targetTransform.position - secondTransform.position;

                GoalPosition = secondTransform.position + (centerline * partwayOffset);

                UpdateWorkingPosToGoal();
            }
        }

        private void UpdateSecondSolverHandler()
        {
            if (secondSolverHandler != null)
            {
                if (secondTransformOverride != null)
                {
                    secondSolverHandler.TransformTarget = secondTransformOverride;
                }
                else
                {
                    secondSolverHandler.TrackedObjectToReference = trackedObjectForSecondTransform;
                }
            }
        }

        /// <summary>
        /// This should only be called from the SolverInBetweenEditor to cause the secondary SolverHandler to reattach this solver.
        /// </summary>
        public void AttachSecondTransformToNewTrackedObject()
        {
            UpdateSecondSolverHandler();
        }
    }
}

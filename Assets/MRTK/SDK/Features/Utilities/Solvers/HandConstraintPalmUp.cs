﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Serialization;

namespace Microsoft.MixedReality.Toolkit.Utilities.Solvers
{
    /// <summary>
    /// Augments the HandConstraint to also check if the palm is facing the user before activation. This solver only works 
    /// with <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityHand"/> controllers, with other <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityController"/> types this solver will behave just like it's base class.
    /// </summary>
    [AddComponentMenu("Scripts/MRTK/SDK/HandConstraintPalmUp")]
    public class HandConstraintPalmUp : HandConstraint
    {
        [Header("Palm Up")]
        [SerializeField]
        [FormerlySerializedAs("facingThreshold")]
        [Tooltip("The angle (in degrees) of the cone between the palm's up and camera's forward have to match. Only supported by IMixedRealityHand controllers.")]
        [Range(0.0f, 90.0f)]
        private float facingCameraTrackingThreshold = 80.0f;

        /// <summary>
        /// The angle (in degrees) of the cone between the palm's up and camera's forward have to match. Only supported by <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityHand"/> controllers.
        /// </summary>
        public float FacingCameraTrackingThreshold
        {
            get => facingCameraTrackingThreshold;
            set => facingCameraTrackingThreshold = value;
        }

        [System.Obsolete("Use FacingCameraTrackingThreshold property instead")]
        public float FacingThreshold
        {
            get => FacingCameraTrackingThreshold;
            set => FacingCameraTrackingThreshold = value;
        }

        [SerializeField]
        [Tooltip("Do the fingers on the hand need to be straightened, rather than curled, to form a flat hand shape. Only supported by IMixedRealityHand controllers.")]
        private bool requireFlatHand = false;

        /// <summary>
        /// Do the fingers on the hand need to be straightened, rather than curled, to form a flat hand shape. Only supported by <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityHand"/> controllers.
        /// </summary>
        public bool RequireFlatHand
        {
            get => requireFlatHand;
            set => requireFlatHand = value;
        }

        [SerializeField]
        [Tooltip("The angle (in degrees) of the cone between the palm's up and triangle's normal formed from the palm, to index, to ring finger tip have to match. Only supported by IMixedRealityHand controllers.")]
        [Range(0.0f, 90.0f)]
        private float flatHandThreshold = 45.0f;

        /// <summary>
        /// The angle (in degrees) of the cone between the palm's up and triangle's normal formed from the palm, to index, to ring finger tip have to match. Only supported by <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityHand"/> controllers.
        /// </summary>
        public float FlatHandThreshold
        {
            get => flatHandThreshold;
            set => flatHandThreshold = value;
        }

        [SerializeField]
        [Tooltip("With this active, solver will follow hand rotation until the menu is sufficiently aligned with the gaze, at which point it faces the camera.")]
        private bool followHandUntilFacingCamera = false;

        /// <summary>
        /// With this active, solver will follow hand rotation until the menu is sufficiently aligned with the gaze, at which point it faces the camera.
        /// </summary>
        public bool FollowHandUntilFacingCamera
        {
            get => followHandUntilFacingCamera;
            set => followHandUntilFacingCamera = value;
        }

        [SerializeField]
        [Tooltip("Angle (in degrees) between hand up and camera forward, below which the hand menu follows the gaze, if followHandUntilFacingCamera is active.")]
        private float followHandCameraFacingThresholdAngle = 60f;

        /// <summary>
        /// Angle (in degrees) between hand up and camera forward, below which the hand menu follows the gaze, if followHandUntilFacingCamera is active.
        /// </summary>
        public float FollowHandCameraFacingThresholdAngle
        {
            get => followHandCameraFacingThresholdAngle;
            set => followHandCameraFacingThresholdAngle = value;
        }

        [SerializeField]
        [Tooltip("With this active, solver will activate the UI after the palm threshold has been met and the user gazes at the activation point")]
        private bool useGazeActivation = false;

        /// <summary> 
        /// With this active, solver will activate after the palm threshold has been met and the user gazes at the activation point
        /// </summary>
        public bool UseGazeActivation
        {
            get => useGazeActivation;
            set => useGazeActivation = value;
        }

        [SerializeField]
        [Tooltip("The distance between the planar intersection of the eye gaze ray and the activation transform. Uses square magnitude between two points for distance")]
        [Range(0.0f, .1f)]
        private float gazeProximityThreshold = .005f;

        /// <summary>
        /// The distance between the planar intersection of the eye gaze ray and the activation transform
        /// </summary>
        public float GazeProximityThreshold
        {
            get => gazeProximityThreshold;
            set => gazeProximityThreshold = value;
        }

        private bool targetWorldLocked = false;

        /// <summary>
        /// Is the current solver object world-locked? If so, don't compute whether it's a valid controller.
        /// </summary>
        public bool TargetWorldLocked
        {
            get => targetWorldLocked;
            set => targetWorldLocked = value;
        }

        private bool eyeGazeActivationAlreadyTriggered = false;

        /// <summary>
        /// Determines if a controller meets the requirements for use with constraining the tracked object and determines if the 
        /// palm is currently facing the user.
        /// </summary>
        /// <param name="controller">The hand to check against.</param>
        /// <returns>True if this hand should be used from tracking.</returns>
        protected override bool IsValidController(IMixedRealityController controller)
        {
            if (!base.IsValidController(controller))
            {
                return false;
            }

            MixedRealityPose palmPose;
            var jointedHand = controller as IMixedRealityHand;

            bool palmFacingThresholdMet = false;

            if (jointedHand != null)
            {
                if (jointedHand.TryGetJoint(TrackedHandJoint.Palm, out palmPose))
                {
                    float palmCameraAngle = Vector3.Angle(palmPose.Up, CameraCache.Main.transform.forward);

                    palmFacingThresholdMet = IsPalmFacingUpwards(jointedHand, palmPose, palmCameraAngle);

                    // If using hybrid hand rotation, we proceed with additional checks
                    if (palmFacingThresholdMet)
                    {
                        if (followHandUntilFacingCamera)
                        {
                            // If we are above the threshold angle, keep the menu mapped to the tracked object
                            if (palmCameraAngle > followHandCameraFacingThresholdAngle)
                            {
                                RotationBehavior = SolverRotationBehavior.LookAtTrackedObject;
                                OffsetBehavior = SolverOffsetBehavior.TrackedObjectRotation;
                            }
                            // If we are within the threshold angle, we snap to follow the camera
                            else
                            {
                                RotationBehavior = SolverRotationBehavior.LookAtMainCamera;
                                OffsetBehavior = SolverOffsetBehavior.LookAtCameraRotation;
                            }
                        }

                        if (useGazeActivation && (!eyeGazeActivationAlreadyTriggered || targetWorldLocked))
                        {
                            return IsUserGazingAtActivationPoint(jointedHand);
                        }
                    }

                    eyeGazeActivationAlreadyTriggered = palmFacingThresholdMet ? eyeGazeActivationAlreadyTriggered : false;

                    return palmFacingThresholdMet;
                }
                else
                {
                    Debug.LogError("HandConstraintPalmUp requires controllers of type IMixedRealityHand to perform hand activation tests.");
                }

                eyeGazeActivationAlreadyTriggered = palmFacingThresholdMet ? eyeGazeActivationAlreadyTriggered : false;
                return palmFacingThresholdMet;
            }

            return true;
        }

        /// <summary>
        /// Checks to see if the palm is currently facing the user
        /// </summary>
        /// <param name="jointedHand"></param>
        /// <param name="palmPose"></param>
        /// <param name="palmCameraAngle"></param>
        /// <returns></returns>
        private bool IsPalmFacingUpwards(IMixedRealityHand jointedHand, MixedRealityPose palmPose, float palmCameraAngle)
        {
            if (requireFlatHand)
            {
                // Check if the triangle's normal formed from the palm, to index, to ring finger tip roughly matches the palm normal.
                MixedRealityPose indexTipPose, ringTipPose;

                if (jointedHand.TryGetJoint(TrackedHandJoint.IndexTip, out indexTipPose) &&
                    jointedHand.TryGetJoint(TrackedHandJoint.RingTip, out ringTipPose))
                {
                    var handNormal = Vector3.Cross(indexTipPose.Position - palmPose.Position,
                                                   ringTipPose.Position - indexTipPose.Position).normalized;
                    handNormal *= (jointedHand.ControllerHandedness == Handedness.Right) ? 1.0f : -1.0f;

                    if (Vector3.Angle(palmPose.Up, handNormal) > flatHandThreshold)
                    {
                        return false;
                    }
                }
            }

            // Check if the palm angle meets the prescribed threshold
            return palmCameraAngle < facingCameraTrackingThreshold;
        }

        /// <summary>
        /// Checks to see if the user is currently gazing at the activation point
        /// </summary>
        /// <param name="jointedHand"></param>
        /// <returns></returns>
        private bool IsUserGazingAtActivationPoint(IMixedRealityHand jointedHand)
        {
            var gazeProvider = (GazeProvider)CoreServices.InputSystem.EyeGazeProvider;

            if (gazeProvider != null)
            {
                Ray eyeRay = new Ray(gazeProvider.GazeOrigin, gazeProvider.GazeDirection);

                // Generate the hand plane that we're using to generate a distance value.
                // This is done by using the index knuckle, pinky knuckle, and wrist
                MixedRealityPose indexKnuckle;
                MixedRealityPose pinkyKnuckle;
                MixedRealityPose wrist;

                if (jointedHand.TryGetJoint(TrackedHandJoint.IndexKnuckle, out indexKnuckle) &&
                    jointedHand.TryGetJoint(TrackedHandJoint.PinkyKnuckle, out pinkyKnuckle) &&
                    jointedHand.TryGetJoint(TrackedHandJoint.Wrist, out wrist))
                {
                    Plane handPlane = new Plane(indexKnuckle.Position, pinkyKnuckle.Position, wrist.Position);
                    float distanceToHandPlane;

                    if (handPlane.Raycast(eyeRay, out distanceToHandPlane))
                    {
                        // Define the activation point as a vector between the wrist and pinky knuckle; then cast it against the plane to get a smooth location
                        Vector3 activationPoint = Vector3.Lerp(pinkyKnuckle.Position, wrist.Position, .5f);

                        // Now that we know the dist to the plane, create a vector at that point
                        Vector3 gazePosOnPlane = eyeRay.origin + eyeRay.direction.normalized * distanceToHandPlane;
                        Vector3 PlanePos = handPlane.ClosestPointOnPlane(gazePosOnPlane);
                        Vector3 activationPointPlanePos = handPlane.ClosestPointOnPlane(activationPoint);

                        float gazePosDistToActivationPosition = (activationPointPlanePos - PlanePos).sqrMagnitude;

                        var gazeActivated = eyeGazeActivationAlreadyTriggered = (gazePosDistToActivationPosition < gazeProximityThreshold);

                        return gazeActivated;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Coroutine function called by the ManipulationHandler of the attached object whenever the object is done 
        /// being manipulated by the user. This triggers a coroutine that checks to see whether the object should 
        /// reattach to the hand.
        /// </summary>
        public void StartWorldLockReattachCheckCorotine()
        {
            StartCoroutine(WorldLockedReattachCheck());
        }

        /// <summary>
        /// Coroutine function that's invoked when the attached object becomes world-locked. It uses the 
        /// logical checks invoked during IsValidController to determine whether the menu should reattach
        /// to the hand or not.
        /// </summary>
        /// <returns></returns>
        private IEnumerator WorldLockedReattachCheck()
        {
            while (targetWorldLocked && useGazeActivation)
            {
                MixedRealityPose palmPose;
                var jointedHand = GetController(SolverHandler.CurrentTrackedHandedness) as IMixedRealityHand;
                if (jointedHand != null)
                {
                    if (jointedHand.TryGetJoint(TrackedHandJoint.Palm, out palmPose))
                    {
                        float palmCameraAngle = Vector3.Angle(palmPose.Up, CameraCache.Main.transform.forward);
                        if (IsPalmFacingUpwards(jointedHand, palmPose, palmCameraAngle) &&
                            IsUserGazingAtActivationPoint(jointedHand))
                        {
                            eyeGazeActivationAlreadyTriggered = false;
                            targetWorldLocked = false;
                            SolverHandler.UpdateSolvers = true;
                        }
                    }
                }

                yield return null;
            }
        }
    }
}

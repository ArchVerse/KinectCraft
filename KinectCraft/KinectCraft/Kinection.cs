using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Research.Kinect.Nui;

namespace KinectCraft
{
    public delegate void KinectionEvent();
    class Kinection
    {
        public Runtime Kinect;
        public SkeletonData trackedSkeleton;
        public MovementPosition CurrentMovementPosition = MovementPosition.Idle;
        public MovementPosition CurrentJumpCrouch = MovementPosition.Idle;
        public bool rhForeward = false, lhForeward = false, handsTogether = false;
        bool fore = false;
        bool back = false;
        public float idleX, idleY, idleZ, idleLHX, idleLHY, idleLHZ, idleRHX, idleRHY, idleRHZ, shoulderHIdleRotation, shoulderHRotation, bodyPitch, bodyIdlePitch, headPitch, headIdlePitch, headYaw, headIdleYaw;
        public float forePercent, strafePercent;
        bool tracking = false;
        bool callsDelegate = true;
        Form1 form1;

        bool idleSet = false;
        bool idleReady = false;
        bool resetreadyPosition = false;
        bool resetPosition = false;

        //////Values//////
        double stepFore = -0.4;
        double stepBack = 0.4;
        double stepLeft = -0.4;
        double stepRight = 0.4;
        double jumpVal = 0.02;
        double crouchVal = -0.1;
        double lhFore = 0.5;
        double rhFore = 0.5;

        public Kinection(Form1 form1) 
        {
            SkeletonEnters += skeleton_Enters;
            SteppedForeward += stepped_Foreward;
            ReturnToIdle += returnto_Idle;
            this.form1 = form1; 
        }

        #region Admin
        
        public Runtime DiscoverKinects()
        {
            Runtime.Kinects.StatusChanged += new EventHandler<StatusChangedEventArgs>(Kinects_StatusChanged);

            Runtime returning;
            foreach (Runtime kinect in Runtime.Kinects)
            {
                if (kinect.Status == KinectStatus.Connected)
                {
                    if (Kinect == null)
                    {
                        Kinect = kinect;
                    }
                    return Kinect;
                }
            }
            return null;
        }

        public KinectStatus GetKinectStatus()
        {
            if (Kinect != null)
                return Kinect.Status;
            else
            {
                return KinectStatus.Disconnected;
            }
        }

        private void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (Kinect == null)
                    {
                        Kinect = e.KinectRuntime;
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (Kinect == e.KinectRuntime)
                    {
                        Kinect = null;
                    }
                    break;
                case KinectStatus.NotReady:
                    if (Kinect == null)
                    {
                        
                    }
                    break;
                case KinectStatus.NotPowered:
                    if (Kinect == e.KinectRuntime)
                    {
                        Kinect = null;
                    }
                    break;
                default:
                    throw new Exception("Unhandled Status: " + e.Status);
            }
            if (Kinect == null)
            {
                
            }
        }

        public bool InitializeKinect(RuntimeOptions options)
        {
            try
            {
                Kinect.Initialize(options);
            }
            catch (COMException comException)
            {
                //TODO: make CONST
                if (comException.ErrorCode == -2147220947)  //Runtime is being used by another app.
                {
                    Kinect = null;
                    MessageBox.Show("Kinect is being used by another application, please close that application before continuing.");
                }
                return false;
            }

            Kinect.SkeletonEngine.TransformSmooth = true;
            Kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonsReady);


            return true;
        }
        #endregion

        #region SkeletonStuffs

        void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame skeletonFrame = e.SkeletonFrame;


            //KinectSDK TODO: This nullcheck shouldn't be required. 
            //Unfortunately, this version of the Kinect Runtime will continue to fire some skeletonFrameReady events after the Kinect USB is unplugged.
            if (skeletonFrame == null)
            {
                return;
            }

            //Bool to stop tracking more than one skeleton
            bool tracked = false;
            bool someonethere = false;

            foreach (SkeletonData skeleton in skeletonFrame.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == skeleton.TrackingState)
                {
                    if (!tracked)
                    {
                        //Once first person is tracked, then no more
                        tracked = true;

                        trackedSkeleton = skeleton;
                        //this.form1.thingy.Text = "" + skeleton.Joints[JointID.HandLeft].Position.X + " : " + skeleton.Joints[JointID.HandRight].Position.X + " : " + ((handsTogether) ? "Together!" : "Not Together :(");

                        someonethere = true;
                        if (!tracking)
                        {
                            tracking = true;
                            Debug.WriteLine("Skeleton Entered Frame");
                            if (SkeletonEnters != null && callsDelegate)
                                SkeletonEnters();
                        }

                        //Check if stepped forewards
                        if (skeleton.Position.Z - idleZ < -0.1 && idleSet)
                        {
                            forePercent = (float)((skeleton.Position.Z - idleZ < stepFore) ? 1 : (skeleton.Position.Z - idleZ) / stepFore);
                            if (this.CurrentMovementPosition != MovementPosition.SteppedForeward && this.CurrentMovementPosition != MovementPosition.ForeLeft && this.CurrentMovementPosition != MovementPosition.ForeRight)
                            {
                                this.CurrentMovementPosition = MovementPosition.SteppedForeward;
                                fore = true;
                                back = false;
                                idleReady = false;
                                //We have stepped foreward
                                if (SteppedForeward != null && callsDelegate)
                                    SteppedForeward();
                                Debug.WriteLine("Stepped Foreward");
                            }
                        }
                        //Check if stepped back
                        else if (skeleton.Position.Z - idleZ > 0.1 && idleSet)
                        {
                            forePercent = (float)((skeleton.Position.Z - idleZ > stepBack) ? 1 : (skeleton.Position.Z - idleZ) / stepBack);
                            if (this.CurrentMovementPosition != MovementPosition.SteppedBack && this.CurrentMovementPosition != MovementPosition.BackLeft && this.CurrentMovementPosition != MovementPosition.BackRight)
                            {
                                this.CurrentMovementPosition = MovementPosition.SteppedBack;
                                back = true;
                                fore = false;
                                idleReady = false;
                                //We have stepped back
                                if (SteppedBack != null && callsDelegate)
                                    SteppedBack();
                                Debug.WriteLine("Stepped Back");
                            }
                        }
                        else
                        {
                            fore = back = false;
                        }
                        

                        //Check if stepped to the right
                        if (skeleton.Position.X - idleX > 0.1 && idleSet)
                        {
                            strafePercent = (float)((skeleton.Position.X - idleX > stepRight) ? 1 : (skeleton.Position.X - idleX) / stepRight);
                            if (this.CurrentMovementPosition != MovementPosition.SteppedRight && this.CurrentMovementPosition != MovementPosition.ForeRight && this.CurrentMovementPosition != MovementPosition.BackRight)
                            {
                                this.CurrentMovementPosition = MovementPosition.SteppedRight;

                                if (fore || back)
                                {
                                    if (fore)
                                        this.CurrentMovementPosition = MovementPosition.ForeRight;
                                    else if (back)
                                        this.CurrentMovementPosition = MovementPosition.BackRight;
                                }
                                idleReady = false;
                                //We have stepped to the right
                                if (SteppedRight != null && callsDelegate)
                                    SteppedRight();
                                Debug.WriteLine("Stepped To The Right");
                            }
                        }
                        //Check if stepped to the left
                        else if (skeleton.Position.X - idleX < -0.1 && idleSet)
                        {
                            strafePercent = (float)((skeleton.Position.X - idleX < stepLeft) ? 1 : (skeleton.Position.X - idleX) / stepLeft);
                            
                            if (this.CurrentMovementPosition != MovementPosition.SteppedLeft && this.CurrentMovementPosition != MovementPosition.ForeLeft && this.CurrentMovementPosition != MovementPosition.BackLeft)
                            {
                                this.CurrentMovementPosition = MovementPosition.SteppedLeft;

                                if (fore || back)
                                {
                                    if (fore)
                                        this.CurrentMovementPosition = MovementPosition.ForeLeft;
                                    else if (back)
                                        this.CurrentMovementPosition = MovementPosition.BackLeft;
                                }
                                idleReady = false;
                                //We have stepped to the left
                                if (SteppedLeft != null && callsDelegate)
                                    SteppedLeft();
                                Debug.WriteLine("Stepped To The Left");
                            }
                        }

                        //Check if the above are all false - so basically idle...
                        if (-0.05 < skeleton.Position.Z - idleZ && skeleton.Position.Z - idleZ < 0.05 && -0.05 < skeleton.Position.X - idleX && skeleton.Position.X - idleX < 0.05 && idleSet)
                        {
                            if (this.CurrentMovementPosition != MovementPosition.Idle)
                            {
                                this.CurrentMovementPosition = MovementPosition.Idle;

                                //Return to idle
                                if (ReturnToIdle != null && callsDelegate)
                                    ReturnToIdle();
                                Debug.WriteLine("Currently Idle");
                            }
                        }

                        //Check if jumping
                        if ((skeleton.Position.Y + ((2 * (skeleton.Position.Z - idleZ)) / 10)) - idleY > jumpVal && idleSet)
                        {
                            if (this.CurrentJumpCrouch != MovementPosition.Jumping)
                            {
                                this.CurrentJumpCrouch = MovementPosition.Jumping;
                                idleReady = false;
                                //We are Jumping
                                if (Jump != null && callsDelegate)
                                    Jump();
                                Debug.WriteLine("Jumping");
                            }
                        }

                        //Check if crouching
                        else if ((skeleton.Position.Y + ((2 * (skeleton.Position.Z - idleZ)) / 10)) - idleY < crouchVal && idleSet)
                        {
                            if (this.CurrentJumpCrouch != MovementPosition.Crouching)
                            {
                                this.CurrentJumpCrouch = MovementPosition.Crouching;
                                idleReady = false;
                                //We are Crouching
                                if (Crouch != null && callsDelegate)
                                    Crouch();
                                Debug.WriteLine("Crouching");
                            }
                        }

                        else
                        {
                            //Else - We are not jumping or crouching
                            if (this.CurrentJumpCrouch != MovementPosition.Idle)
                            {
                                this.CurrentJumpCrouch = MovementPosition.Idle;
                                //We are Jumping
                                if (JumpCrouchIdle != null && callsDelegate)
                                    JumpCrouchIdle();
                                Debug.WriteLine("Not Jumping or Crouching");
                            }
                        }

                        /////////////////
                        //Hand Tracking//
                        /////////////////

                        //Check if both hands are to the side then ready to reset position
                        if ((skeleton.Joints[JointID.ShoulderLeft].Position.X - skeleton.Joints[JointID.HandLeft].Position.X > lhFore - 0.1) && (skeleton.Joints[JointID.HandRight].Position.X - skeleton.Joints[JointID.ShoulderRight].Position.X > rhFore - 0.1))
                        {
                            if (!resetreadyPosition)
                            {
                                resetreadyPosition = true;
                                this.ReadyIdlePosition();
                                idleReady = true;
                                //Pause until reset is complete so user can move with out affecting the game
                                idleSet = false;
                            }
                        }
                        else
                        {
                            resetreadyPosition = false;
                        }

                        //Check if Right Hand is Foreward
                        if ((float)Math.Sqrt(Math.Pow(skeleton.Joints[JointID.HandRight].Position.Z - skeleton.Joints[JointID.ShoulderRight].Position.Z, 2.0) + Math.Pow(skeleton.Joints[JointID.HandRight].Position.X - skeleton.Joints[JointID.ShoulderRight].Position.X, 2.0)) > rhFore && idleSet)
                        {
                            if (!rhForeward)
                            {
                                rhForeward = true;
                                idleReady = false;
                                if (RightHandForeward != null && callsDelegate)
                                    RightHandForeward();
                                Debug.WriteLine("Right Hand Foreward");
                            }
                        }
                        else
                        {
                            if (rhForeward)
                            {
                                rhForeward = false;
                                if (RightHandBack != null && callsDelegate)
                                    RightHandBack();
                                Debug.WriteLine("Right Hand Back");
                            }
                        }
                        //Check if Left Hand is Foreward
                        if ((float)Math.Sqrt(Math.Pow(skeleton.Joints[JointID.HandLeft].Position.Z - skeleton.Joints[JointID.ShoulderLeft].Position.Z, 2.0) + Math.Pow(skeleton.Joints[JointID.HandLeft].Position.X - skeleton.Joints[JointID.ShoulderLeft].Position.X, 2.0)) > lhFore && idleSet)
                        {
                            if (!lhForeward)
                            {
                                lhForeward = true;
                                idleReady = false;
                                if (LeftHandForeward != null && callsDelegate)
                                    LeftHandForeward();
                                Debug.WriteLine("Left Hand Foreward");
                            }
                        }
                        else
                        {
                            if (lhForeward)
                            {
                                lhForeward = false;
                                if (LeftHandBack != null && callsDelegate)
                                    LeftHandBack();
                                Debug.WriteLine("Left Hand Back");
                            }
                        }

                        

                        //Check if both hands are above the head then reset position
                        if (skeleton.Joints[JointID.HandLeft].Position.Y > skeleton.Joints[JointID.Head].Position.Y && skeleton.Joints[JointID.HandRight].Position.Y > skeleton.Joints[JointID.Head].Position.Y)
                        {
                            if (!resetPosition)
                            {
                                resetPosition = true;
                                if (idleReady)
                                {
                                    this.SetIdlePosition(!idleSet);
                                    idleSet = true;
                                    idleReady = false;
                                }
                            }
                        }
                        else
                        {
                            if (resetPosition == true)
                            {
                                //Call Ready 3 as reset is now complete
                                if (Ready3 != null && callsDelegate)
                                    Ready3();
                            }

                            resetPosition = false;
                        }

                        //Check if both hands are together
                        if ((skeleton.Joints[JointID.HandLeft].Position.X + 0.1 >= skeleton.Joints[JointID.HandRight].Position.X - 0.03 && skeleton.Joints[JointID.HandLeft].Position.X + 0.1 <= skeleton.Joints[JointID.HandRight].Position.X + 0.03) && idleSet)
                        {
                            if (shoulderHRotation > -0.05 && shoulderHRotation < 0.05)
                            {
                                if (!handsTogether)
                                {
                                    if (HandsTogether != null && callsDelegate)
                                        HandsTogether();
                                    handsTogether = true;
                                }
                            }
                        }
                        else if ((skeleton.Joints[JointID.HandLeft].Position.X <= skeleton.Joints[JointID.ShoulderLeft].Position.X + 0.01) && (skeleton.Joints[JointID.HandRight].Position.X >= skeleton.Joints[JointID.ShoulderRight].Position.X - 0.01))
                        {
                            handsTogether = false;
                        }

                        /////////////////////
                        //Shoulder Tracking//
                        /////////////////////
                        if (idleSet)
                        {
                            shoulderHRotation = (shoulderHIdleRotation - ((skeleton.Joints[JointID.ShoulderLeft].Position.Z - skeleton.Position.Z) - (skeleton.Joints[JointID.ShoulderRight].Position.Z - skeleton.Position.Z)));
                            bodyPitch = (bodyIdlePitch - ((skeleton.Joints[JointID.ShoulderCenter].Position.Z - skeleton.Position.Z) - (skeleton.Joints[JointID.HipCenter].Position.Z - skeleton.Position.Z)));
                        }

                        /////////////////
                        //Head Tracking//
                        /////////////////
                        if (idleSet)
                        {
                            headPitch = headIdlePitch - (skeleton.Joints[JointID.Head].Position.Z - skeleton.Position.Z - ((skeleton.Joints[JointID.ShoulderLeft].Position.Z + skeleton.Joints[JointID.ShoulderCenter].Position.Z + skeleton.Joints[JointID.ShoulderRight].Position.Z) / 3));
                        }

                        if (FluidUpdate != null && callsDelegate && idleSet)
                            FluidUpdate();
                    }
                }
            }
            if (tracking && !someonethere)
            {
                //Skeleton Leaves Frame
                Debug.WriteLine("Skeleton Left Frame");
                tracking = false;
                if (ReturnToIdle != null && callsDelegate)
                    ReturnToIdle();
                Debug.WriteLine("Currently Idle");
                if (JumpCrouchIdle != null && callsDelegate)
                    JumpCrouchIdle();
                Debug.WriteLine("Not Jumping or Crouching");
                lhForeward = false;
                if (LeftHandBack != null && callsDelegate)
                    LeftHandBack();
                Debug.WriteLine("Left Hand Back");
                rhForeward = false;
                if (RightHandBack != null && callsDelegate)
                    RightHandBack();
                Debug.WriteLine("Right Hand Back");
                this.CurrentJumpCrouch = MovementPosition.Idle;
                this.CurrentMovementPosition = MovementPosition.Idle;
                idleReady = false;
                idleSet = false;
                if (SkelLeft != null && callsDelegate)
                    SkelLeft();
            }
        }

        public SkeletonData TrackedSkeleton
        {
            get { return trackedSkeleton; }
        }
        #endregion

        #region MovementMethods

        public enum MovementPosition
        {
            Idle, SteppedForeward, SteppedBack, SteppedLeft, SteppedRight, ForeLeft, ForeRight, BackLeft, BackRight, Jumping, Crouching
        }

        public void SetIdlePosition(bool setHands)
        {
            if (trackedSkeleton != null)
            {
                idleX = trackedSkeleton.Position.X;
                idleY = trackedSkeleton.Position.Y;
                idleZ = trackedSkeleton.Position.Z;

                shoulderHIdleRotation = ((trackedSkeleton.Joints[JointID.ShoulderLeft].Position.Z - trackedSkeleton.Position.Z) - (trackedSkeleton.Joints[JointID.ShoulderRight].Position.Z - trackedSkeleton.Position.Z));
                bodyIdlePitch = ((trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Z - trackedSkeleton.Position.Z) - (trackedSkeleton.Joints[JointID.HipCenter].Position.Z - trackedSkeleton.Position.Z));
                headIdlePitch = trackedSkeleton.Joints[JointID.Head].Position.Z - trackedSkeleton.Position.Z - ((trackedSkeleton.Joints[JointID.ShoulderLeft].Position.Z + trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Z + trackedSkeleton.Joints[JointID.ShoulderRight].Position.Z) / 3);
                if (setHands)
                {
                    idleLHX = (trackedSkeleton.Joints[JointID.HandLeft].Position.X - trackedSkeleton.Position.X);
                    idleRHX = (trackedSkeleton.Joints[JointID.HandRight].Position.X - trackedSkeleton.Position.X);
                }
                Debug.WriteLine("Idle Position Set");
                if (Ready2 != null && callsDelegate)
                    Ready2();
            }
        }

        public void ReadyIdlePosition()
        {
            if (trackedSkeleton != null)
            {
                idleLHY = (trackedSkeleton.Joints[JointID.HandLeft].Position.Y - trackedSkeleton.Position.Y);
                idleLHZ = (trackedSkeleton.Joints[JointID.HandLeft].Position.Z - trackedSkeleton.Position.Z);
                idleRHY = (trackedSkeleton.Joints[JointID.HandRight].Position.Y - trackedSkeleton.Position.Y);
                idleRHZ = (trackedSkeleton.Joints[JointID.HandRight].Position.Z - trackedSkeleton.Position.Z);
                if (ReturnToIdle != null && callsDelegate)
                    ReturnToIdle();
                Debug.WriteLine("Currently Idle");
                if (JumpCrouchIdle != null && callsDelegate)
                    JumpCrouchIdle();
                Debug.WriteLine("Not Jumping or Crouching");
                lhForeward = false;
                if (LeftHandBack != null && callsDelegate)
                    LeftHandBack();
                Debug.WriteLine("Left Hand Back");
                rhForeward = false;
                if (RightHandBack != null && callsDelegate)
                    RightHandBack();
                Debug.WriteLine("Right Hand Back");
                this.CurrentJumpCrouch = MovementPosition.Idle;
                this.CurrentMovementPosition = MovementPosition.Idle;
                Debug.WriteLine("Idle Position Ready'd");
                if (Ready1 != null && callsDelegate)
                    Ready1();
            }
        }

        private void skeleton_Enters()
        {
            
        }

        private void stepped_Foreward()
        {
            
        }

        private void returnto_Idle()
        {
            
        }

        #endregion

        #region Events

        public KinectionEvent SkeletonEnters, SteppedForeward, SteppedBack, SteppedLeft, SteppedRight, ReturnToIdle, Jump, Crouch, JumpCrouchIdle, LeftHandForeward, LeftHandBack, RightHandForeward, RightHandBack, HandsTogether, FluidUpdate, SkelLeft, Ready1, Ready2, Ready3;

        #endregion
    }
}

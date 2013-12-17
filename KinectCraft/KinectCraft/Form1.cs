using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Research.Kinect.Nui;

namespace KinectCraft
{

    public partial class Form1 : Form
    {
        
        System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
        WoollySocks ws;
        bool moving = false;
        bool lhForeward = false, rhForeward = false;
        public Label thingy;
        Kinection kinect;
        float lastShoulderHRotation, lastBodyPitch, lastHeadPitch, lastHeadYaw;
        int screenWidth, screenHeight;
        bool fullscreen = false;
        bool enteredSpin = false, spinLeft = false;
        bool headMode = false, switchSpinOnly = false, skelLeftBool = true;
        TurnMode turnMode = TurnMode.Switch;

        enum TurnMode { LookAndSpin, SpinOnly, Hybrid, Switch }

        public Form1()
        {
            InitializeComponent();
            InputController iC = new InputController();
            t.Interval = 500;
            t.Tick += sendMessage;
            t.Start();
            ws = new WoollySocks();
            textBox1.Text = "";
            thingy = new Label();
            thingy.Text = "Position: ";
            Font courierFont10point = new Font("Courier New", (float)20.0);
            thingy.AutoSize = true;
            thingy.Font = courierFont10point;
            this.Controls.Add(thingy);

            kinect = new Kinection(this);
            kinect.SteppedForeward += stepped_Foreward;
            kinect.ReturnToIdle += returnto_Idle;
            kinect.SteppedBack += stepped_Back;
            kinect.SteppedLeft += stepped_Left;
            kinect.SteppedRight += stepped_Right;
            kinect.Jump += jump;
            kinect.Crouch += crouch;
            kinect.JumpCrouchIdle += jumpcrouch_Idle;
            kinect.RightHandForeward += attack;
            kinect.RightHandBack += dontAttack;
            kinect.LeftHandForeward += use;
            kinect.LeftHandBack += dontUse;
            kinect.HandsTogether += handsTogether;
            kinect.FluidUpdate += fluidUpdate;
            kinect.SkelLeft += skelLeft;
            kinect.Ready1 += ready1;
            kinect.Ready2 += ready2;
            kinect.Ready3 += ready3;

            if (kinect.DiscoverKinects() != null)
            {
                Debug.WriteLine("Kinect Discovered");
                if (kinect.GetKinectStatus() == KinectStatus.Connected)
                {
                    Debug.WriteLine("Kinect Status: Connected");
                    kinect.InitializeKinect(RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking);
                }
                else
                {
                    Debug.WriteLine("Kinect Status: " + kinect.GetKinectStatus().ToString());
                }
            }
            else
            {
                Debug.WriteLine("No Kinect Detected");
            }
            ws.NewMessage += receivedMessage;
            ws.WaitForConnect = true;
            ws.InitializeConnection();
        }

        void receivedMessage(string message)
        {
            string[] messages = message.Split(';');
            for (int i = 0; i < messages.Length; i++)
            {
                string command = messages[i].Split('[')[0];
                string args = messages[i].Split('[')[1].Split(']')[0];

                if (command.CompareTo("fullscreen") == 0)
                {
                    this.fullscreen = (args.CompareTo("true") == 0) ? true : false;
                }
            }
        }

        void stepped_Foreward()
        {
            if(!headMode)
                ws.SendMessage("move_foreward");
        }

        void stepped_Back()
        {
            if(!headMode)
                ws.SendMessage("move_back");
        }

        void stepped_Left()
        {
            ws.SendMessage("strafe_left");
        }

        void stepped_Right()
        {
            ws.SendMessage("strafe_right");
        }

        void returnto_Idle()
        {
            ws.SendMessage("idle");
        }

        void jump()
        {
            ws.SendMessage("jump;!sneak");
        }

        void crouch()
        {
            ws.SendMessage("sneak;!jump");
        }

        void jumpcrouch_Idle()
        {
            ws.SendMessage("!jump;!sneak");
        }

        void attack()
        {
            ws.SendMessage("attack");
        }

        void dontAttack()
        {
            ws.SendMessage("!attack");
        }

        void use()
        {
            ws.SendMessage("use");
        }

        void dontUse()
        {
            ws.SendMessage("!use");
        }

        void handsTogether()
        {
            if (this.turnMode == TurnMode.Switch)
            {
                switchSpinOnly = !switchSpinOnly;
                ws.SendMessage("spinMode[" + ((switchSpinOnly) ? "spinOnly" : "look") +"]");
            }
        }

        void fluidUpdate()
        {
            if(kinect.CurrentMovementPosition == Kinection.MovementPosition.SteppedForeward || kinect.CurrentMovementPosition == Kinection.MovementPosition.SteppedBack || kinect.CurrentMovementPosition == Kinection.MovementPosition.ForeLeft || kinect.CurrentMovementPosition == Kinection.MovementPosition.ForeRight || kinect.CurrentMovementPosition == Kinection.MovementPosition.BackLeft || kinect.CurrentMovementPosition == Kinection.MovementPosition.BackRight)
            {
                ws.SendMessage("foreward[" + kinect.forePercent + "]");
            }

            if (kinect.CurrentMovementPosition == Kinection.MovementPosition.SteppedLeft || kinect.CurrentMovementPosition == Kinection.MovementPosition.SteppedRight || kinect.CurrentMovementPosition == Kinection.MovementPosition.ForeLeft || kinect.CurrentMovementPosition == Kinection.MovementPosition.ForeRight || kinect.CurrentMovementPosition == Kinection.MovementPosition.BackLeft || kinect.CurrentMovementPosition == Kinection.MovementPosition.BackRight)
            {
                ws.SendMessage("strafe[" + kinect.strafePercent + "]");
            }

            screenWidth = (fullscreen) ? Screen.GetBounds(this).Width : Screen.GetWorkingArea(this).Width;
            screenHeight = (fullscreen) ? Screen.GetBounds(this).Height : Screen.GetWorkingArea(this).Height;
            int heightModifier = (fullscreen) ? 0 : 10;

            if (headMode)
            {
                float XYRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.Head].Position.X, kinect.trackedSkeleton.Joints[JointID.Head].Position.Y);
                float hipXYRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Y);
                float finalXY = (((XYRotation - hipXYRotation) + (((XYRotation - hipXYRotation) < 0) ? 90 : -90)) + ((((XYRotation - hipXYRotation) + (((XYRotation - hipXYRotation) < 0) ? 90 : -90)) < 0) ? 90 : -90));
                if (!enteredSpin)
                {
                    
                    
                    if (finalXY > lastHeadYaw + 0.1)
                    {
                        Cursor.Position = new Point((int)((screenWidth / 2) - ((finalXY - lastHeadYaw) * 20)), Cursor.Position.Y);
                    }
                    else if (finalXY < lastHeadYaw - 0.1)
                    {
                        Cursor.Position = new Point((int)((screenWidth / 2) - ((finalXY - lastHeadYaw) * 20)), Cursor.Position.Y);
                    }
                    else
                    {
                        Cursor.Position = new Point((screenWidth / 2), Cursor.Position.Y);
                    }
                }
                lastHeadYaw = finalXY;

                turnMode = TurnMode.SpinOnly;
            }

            if ((turnMode == TurnMode.SpinOnly) || (turnMode == TurnMode.Hybrid && kinect.CurrentMovementPosition == Kinection.MovementPosition.Idle) || (turnMode == TurnMode.Switch && switchSpinOnly))
            {
                if (kinect.shoulderHRotation < -0.05)
                {
                    Cursor.Position = new Point((int)((screenWidth / 2) + ((kinect.shoulderHRotation + 0.05) * 200)), Cursor.Position.Y);
                    enteredSpin = true;
                }
                else if (kinect.shoulderHRotation > 0.05)
                {
                    Cursor.Position = new Point((int)((screenWidth / 2) + ((kinect.shoulderHRotation - 0.05) * 200)), Cursor.Position.Y);
                    enteredSpin = true;
                }
                else if (kinect.shoulderHRotation > -0.05 && kinect.shoulderHRotation < 0.05)
                {
                    if(!headMode)
                        Cursor.Position = new Point((screenWidth / 2), Cursor.Position.Y);
                    if (enteredSpin)
                        enteredSpin = false;
                }
            }
            else if (((turnMode == TurnMode.LookAndSpin) || (turnMode == TurnMode.Hybrid && kinect.CurrentMovementPosition != Kinection.MovementPosition.Idle) || (turnMode == TurnMode.Switch && !switchSpinOnly)) && !headMode)
            {
                if (kinect.shoulderHRotation > -0.2 && kinect.shoulderHRotation < 0.2)
                {
                    if (kinect.shoulderHRotation > -0.01 && kinect.shoulderHRotation < 0.01)
                    {
                        if (enteredSpin)
                            enteredSpin = false;
                    }

                    
                    if (kinect.shoulderHRotation > lastShoulderHRotation + 0.001)
                    {
                        if ((enteredSpin) ? !spinLeft : true)
                        {
                            Cursor.Position = new Point((int)((screenWidth / 2) + ((kinect.shoulderHRotation - lastShoulderHRotation) * 2000)), Cursor.Position.Y);
                            if (enteredSpin)
                                enteredSpin = false;
                        }
                    }
                    else if (kinect.shoulderHRotation < lastShoulderHRotation - 0.001)
                    {
                        if ((enteredSpin) ? spinLeft : true)
                        {
                            Cursor.Position = new Point((int)((screenWidth / 2) + ((kinect.shoulderHRotation - lastShoulderHRotation) * 2000)), Cursor.Position.Y);
                            if (enteredSpin)
                                enteredSpin = false;
                        }
                    }
                    else
                    {
                        Cursor.Position = new Point((screenWidth / 2), Cursor.Position.Y);
                        if (enteredSpin)
                            enteredSpin = false;
                    }
                    

                    lastShoulderHRotation = kinect.shoulderHRotation;
                }
                else if(kinect.shoulderHRotation < -0.2)
                {
                    Cursor.Position = new Point((int)((screenWidth / 2) + ((kinect.shoulderHRotation) * 100)), Cursor.Position.Y);
                    enteredSpin = true;
                    spinLeft = true;
                }
                else if(kinect.shoulderHRotation > 0.2)
                {
                    Cursor.Position = new Point((int)((screenWidth / 2) + ((kinect.shoulderHRotation) * 100)), Cursor.Position.Y);
                    enteredSpin = true;
                    spinLeft = false;
                }
            }


            if (!headMode)
            {
                if (!enteredSpin)
                {
                    if (kinect.bodyPitch > -0.01 && kinect.bodyPitch < 0.01)
                    {
                        //ws.SendMessage("ready3");
                    }
                    if (kinect.bodyPitch > lastBodyPitch + 0.001)
                    {
                        Cursor.Position = new Point(Cursor.Position.X, (int)((screenHeight / 2) + ((kinect.bodyPitch - lastBodyPitch) * 2000)) + heightModifier);
                        //Cursor.Position = new Point(Cursor.Position.X, (1080 / 2));
                    }
                    else if (kinect.bodyPitch < lastBodyPitch - 0.001)
                    {
                        Cursor.Position = new Point(Cursor.Position.X, (int)((screenHeight / 2) + ((kinect.bodyPitch - lastBodyPitch) * 2000)) + heightModifier);
                        //Cursor.Position = new Point(Cursor.Position.X, (1080 / 2));
                    }
                    else
                    {
                        Cursor.Position = new Point(Cursor.Position.X, (screenHeight / 2) + heightModifier);
                    }
                    lastBodyPitch = kinect.bodyPitch;
                }
            }
            else if (headMode)
            {
                float YZRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Z, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.Head].Position.Z, kinect.trackedSkeleton.Joints[JointID.Head].Position.Y);
                float hipYZRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.Z, kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.Head].Position.Z, kinect.trackedSkeleton.Joints[JointID.Head].Position.Y);
                float finalYZ = (((YZRotation - hipYZRotation) + (((YZRotation - hipYZRotation) < 0) ? 90 : -90)) + ((((YZRotation - hipYZRotation) + (((YZRotation - hipYZRotation) < 0) ? 90 : -90)) < 0) ? 90 : -90));

                if ((kinect.bodyPitch + kinect.bodyIdlePitch > kinect.bodyIdlePitch + 0.1) && kinect.CurrentMovementPosition != Kinection.MovementPosition.SteppedForeward)
                {
                    kinect.CurrentMovementPosition = Kinection.MovementPosition.SteppedForeward;
                    ws.SendMessage("move_foreward");
                }
                else if ((kinect.bodyPitch + kinect.bodyIdlePitch < kinect.bodyIdlePitch - 0.08) && kinect.CurrentMovementPosition != Kinection.MovementPosition.SteppedBack)
                {
                    kinect.CurrentMovementPosition = Kinection.MovementPosition.SteppedBack;
                    ws.SendMessage("move_back");
                }
                else if (kinect.CurrentMovementPosition != Kinection.MovementPosition.Idle)
                {
                    kinect.CurrentMovementPosition = Kinection.MovementPosition.Idle;
                    ws.SendMessage("idle");
                }

                if (!enteredSpin)
                {
                    if (kinect.bodyPitch + kinect.bodyIdlePitch < kinect.bodyIdlePitch + 0.05 && kinect.bodyPitch + kinect.bodyIdlePitch > kinect.bodyIdlePitch - 0.05)
                    {
                        if (finalYZ > lastHeadPitch + 0.1)
                        {
                            Cursor.Position = new Point(Cursor.Position.X, (int)((screenHeight / 2) + ((finalYZ - lastHeadPitch) * 20)) + heightModifier);
                            //Cursor.Position = new Point(Cursor.Position.X, (1080 / 2));
                        }
                        else if (finalYZ < lastHeadPitch - 0.1)
                        {
                            Cursor.Position = new Point(Cursor.Position.X, (int)((screenHeight / 2) + ((finalYZ - lastHeadPitch) * 20)) + heightModifier);
                            //Cursor.Position = new Point(Cursor.Position.X, (1080 / 2));
                        }
                        else
                        {
                            Cursor.Position = new Point(Cursor.Position.X, (screenHeight / 2) + heightModifier);
                        }
                    }
                    lastHeadPitch = finalYZ;
                }
            }
               
        }


        void fluidUpdate2()
        {
            float XYRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.Head].Position.X, kinect.trackedSkeleton.Joints[JointID.Head].Position.Y);
            float hipXYRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Y);
            float YZRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Z, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.Head].Position.Z, kinect.trackedSkeleton.Joints[JointID.Head].Position.Y);
            float hipYZRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.Z, kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.Y, kinect.trackedSkeleton.Joints[JointID.Head].Position.Z, kinect.trackedSkeleton.Joints[JointID.Head].Position.Y);
            float XZRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.Z, kinect.trackedSkeleton.Joints[JointID.ShoulderCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.Head].Position.Z, kinect.trackedSkeleton.Joints[JointID.Head].Position.X);
            float hipXZRotation = angleFromPoints(kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.Z, kinect.trackedSkeleton.Joints[JointID.HipCenter].Position.X, kinect.trackedSkeleton.Joints[JointID.Head].Position.Z, kinect.trackedSkeleton.Joints[JointID.Head].Position.X);
            //headJointLbl.Text = "XY Plane Rotation: " + (XYRotation + ((XYRotation < 0) ? 90 : -90));
            //hipXYLbl.Text = "Final YZ: " + (((YZRotation - hipYZRotation) + (((YZRotation - hipYZRotation) < 0) ? 90 : -90)) + ((((YZRotation - hipYZRotation) + (((YZRotation - hipYZRotation) < 0) ? 90 : -90)) < 0) ? 90 : -90));
            //finalXYLbl.Text = "Final XY: " + (((XYRotation - hipXYRotation) + (((XYRotation - hipXYRotation) < 0) ? 90 : -90)) + ((((XYRotation - hipXYRotation) + (((XYRotation - hipXYRotation) < 0) ? 90 : -90)) < 0) ? 90 : -90));
            //neckJointLbl.Text = "YZ Plane Rotation: " + (YZRotation + ((YZRotation < 0) ? 90 : -90));
            //depthHeadLbl.Text = "XZ Plane Rotation: " + (XZRotation + ((XZRotation < 0) ? 90 : -90));
            //finalXZLbl.Text = "Final XZ: " + (((XZRotation - hipXZRotation) + (((XZRotation - hipXZRotation) < 0) ? 90 : -90)) + ((((XZRotation - hipXZRotation) + (((XZRotation - hipXZRotation) < 0) ? 90 : -90)) < 0) ? 90 : -90));
        }

        float angleFromPoints(float point1x, float point1y, float point2x, float point2y)
        {
            return (float)((Math.Atan((point2y - point1y) / (point2x - point1x))) / (Math.PI / 180));
        }
        void skelLeft()
        {
            if (!skelLeftBool)
            {
                ws.SendMessage("skelLeft");
                skelLeftBool = true;
            }
        }

        void ready1()
        {
            ws.SendMessage("ready1");
        }

        void ready2()
        {
            ws.SendMessage("ready2");
            skelLeftBool = false;
        }

        void ready3()
        {
            ws.SendMessage("ready3");
        }

        private void textBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //keybd_event(0x41, 0, 0, 0); // KEY_DOWN

            System.Threading.Thread.Sleep(5000);

            //keybd_event(0x41, 0, 2, 0); // KEY_UP
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        void sendMessage(object sender, EventArgs e)
        {
            
            if (rhForeward)
            {
               //ws.SendMessage("attack");
            }
            else
            {
                //ws.SendMessage("!attack");
            }
            if (lhForeward)
            {
                //ws.SendMessage("use");
            }
            else
            {
                //ws.SendMessage("!use");
            }
            
            /*if (!moving)
            {
                
                    moving = true;
                    ws.SendMessage("click_left;shift");
                
            }
            else
            {
                
                    moving = false;
                    ws.SendMessage("!shift");
                
            }*/
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ws.SendMessage("move_foreward");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ws.InitializeConnection();
            //t.Start();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            ws.SendMessage(textBox1.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ws.SendMessage("idle");
        }
    }
}

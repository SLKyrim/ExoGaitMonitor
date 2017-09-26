﻿//
// Example 6 PVT Rev 3
//
// This program demonstrates Copley Motion PVT Motion
//
// The purpose of this lab is perform Position-Velocity-Time move
// on a Linkage (Coordinated) Object and draw the equivalent of a circle
// using two axes.
//
// This program assumes the following axis configuration:
// 1. Upon startup it will enable one axis at Can Node ID 1 and
//    one at Can Node ID 2, or one axis at EtherCAT node -1 and
//    one at EtherCAT node -2.
// 2. Both motors have an encoders with an indexes
//
// This code also includes the following prerequisites:
// 1. The both amplifiers and motors must be preconfigured and set up properly to run.
// 2. The hardware enable switch must be installed on both amplifiers 
// and both are easily accessible
// 
// As with any motion product extreme caution must used! Read and understand
// all parameter settings before attemping to send to amplifier.
// This program assumes the following axis configurations:
//
// Copley Motion Objects are Copyright, 2002-2015, Copley Controls.
//
// for more information on Copley Motion products see:
// http://www.copleycontrols.com
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using CMLCOMLib;
using System.IO;

namespace EX6_PVT
{
    public partial class PVT : Form
    {

        //***************************************************
        //*
        //*  EtherCAT Network
        //*
        //***************************************************
        //// A negative node number refers to the drive's physical position on the network
        //// -1 for the first drive, -2 for the second, etc.
        //const int X_AXIS_ECAT_NODE = -1;
        //const int Y_AXIS_ECAT_NODE = -2;
        //EcatObj ecatObj;

        //***************************************************
        //
        //  CANOpen Network
        //
        //***************************************************
        const int X_AXIS_CAN_ADDRESS = 3;
        //const int Y_AXIS_CAN_ADDRESS = 2;
        canOpenObj canOpen;

        LinkageObj Linkage;
        AmpObj[] ampArray = new AmpObj [1];

        HomeSettingsObj Home;
        eventObj EventObj ;

        const short X_AXIS_AMP = 0;
        //const short Y_AXIS_AMP = 1;
        const int ARRAY_LEN = 200;

        double[,] positions = new double[ARRAY_LEN, 1];
        double[,] velocities = new double [ARRAY_LEN,1];
        int[] times = new int [ARRAY_LEN];
        int numberOfEvents= 0;
        double ang, angV;

        // Create a delegate to updateGui down the application in a thread safe way
        delegate void UpdateGui();
        // Create a delegate to close down the application in a thread safe way
        delegate void CloseApp();
        public PVT()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try{
            //Initialize code here

            ampArray[X_AXIS_AMP] = new AmpObj();
            //ampArray[Y_AXIS_AMP] = new AmpObj();
            Linkage = new LinkageObj();

            //***************************************************
            //
            //  CANOpen Network
            //
            //***************************************************
            canOpen = new canOpenObj();
            //
            //**************************************************************************
            //* The next two lines of code are optional. If no bit rate is specified,
            //* then the default bit rate (1 Mbit per second) is used.  If no port name
            //* is specified, then CMO will use the first supported CAN card found and
            //* use channel 0 of that card.
            //**************************************************************************
            // Set the bit rate to 1 Mbit per second
            canOpen.BitRate = CML_BIT_RATES.BITRATE_1_Mbit_per_sec;
            // Indicate that channel 0 of a Copley CAN card should be used
            canOpen.PortName = "copley0";
            //// Indicate that channel 0 of a KVaser card should be used
            //canOpen.PortName = "kvaser0";
            //// Indicate that channel 0 of an IXXAT card should be used
            //canOpen.PortName = "IXXAT0";
            //// Indicate that channel 0 of an IXXAT card (V3.x or newer drivers) should be used
            //canOpen.PortName = "IXXATV30";
            //***************************************************
            //* Initialize the CAN card and network
            //***************************************************
            canOpen.Initialize();
            //***************************************************
            //* Initialize the amplifier
            //***************************************************
            ampArray[X_AXIS_AMP].Initialize(canOpen, X_AXIS_CAN_ADDRESS);
            //ampArray[Y_AXIS_AMP].Initialize(canOpen, Y_AXIS_CAN_ADDRESS);


            //***************************************************
            //*
            //*  EtherCAT Network
            //*
            //***************************************************
            //ecatObj = new EcatObj();
            //
            //***************************************************
            //* The next line of code is optional. The port name is the IP address of 
            //* the Ethernet adapter. Alternatively, a shortcut name “eth” can be used 
            //* in conjunction with the adapter number. For example “eth0” for the first 
            //* Ethernet adapter, “eth1” for the second adapter. If no port name is 
            //* supplied, it will default to “eth0”.
            //**************************************************************************
            //// Indicate that the first Ethernet adapter is to be used
            //ecatObj.PortName = "eth0";
            //// Specify an IP address
            //ecatObj.PortName = "192.168.1.1";
            //
            //***************************************************
            //* Initialize the EtherCAT network
            //***************************************************
            //ecatObj.Initialize();
            //
            //***************************************************
            //* Initialize the amplifier
            //***************************************************
            //ampArray[X_AXIS_AMP].InitializeEcat(ecatObj, X_AXIS_ECAT_NODE);
            //ampArray[Y_AXIS_AMP].InitializeEcat(ecatObj, Y_AXIS_ECAT_NODE);
            //
            //***************************************************
            //* Initialize both axis of a 2 axis EtherCAT drive (AE2, XE2, etc)
            //***************************************************
            //// Initialize the first axis. 2 axis etherCAT drives are treated
            //// like a single node on the EtherCAT bus, so only one ecat node ID
            //// is used.
            //ampArray[X_AXIS_AMP].InitializeEcat(ecatObj, X_AXIS_ECAT_NODE);
            //// Initalize the second axis by passing in the previously initialized
            //// x axis amp object as a paramter
            //ampArray[Y_AXIS_AMP].InitializeEcatSubAxis(ampArray[X_AXIS_AMP]);




            Linkage.Initialize(ampArray);


            EventObj = Linkage.CreateEvent(CML_LINK_EVENT.LINKEVENT_LOWWATER,
                                          CML_EVENT_CONDITION.CML_EVENT_ALL);
                EventObj.EventNotify += new eventObj.EventHandler(EventObj_EventNotify);
            }
            catch (Exception ex){
            DisplayError(ex);
            }
        }

        private void calcSegments(){
            try{

            //  This sub is used to calculate the points of the PVT profile used to generate 
            // a circle.
          //  int i;
            double x, y, xv, yv, angA;
            double xc, yc, t, angVmax, angAmax, R, PI;
            double velocityScalor;


            ////*****************
            // Use the following parameters to adjust the speed
            // and size of the profile circle

            R = 64000.0; // Radius of circle in (counts)
            xc = 64000.0;  // x-value for center of circle in (counts)
            yc = 5000.0;    // y-value for center of circle in (counts)
            velocityScalor = 1; // used to control the max velocity
            // *****************

            t = 0.01;
            PI = 4 * System.Math.Atan(1);
            angVmax = PI * velocityScalor;
            angAmax = angVmax;

            for (int j = 0; j<ARRAY_LEN; j++){

                if (angV < angVmax){
                    angA = angAmax;
                }
                else{
                    angA = 0;
                }

                ang = ang + t * angV + t * t * angA / 2;
                angV = angV + t * angA;


                x = xc + R * System.Math.Cos(ang);
                //y = yc + R * System.Math.Sin(ang);
                xv = -System.Math.Sin(ang) * angV * R;
                //yv = System.Math.Cos(ang) * angV * R;

                positions[j, 0] = x;
                velocities[j, 0] = xv;
                //positions[j, 1] = y;
                //velocities[j, 1] = yv;
                times[j] = Convert.ToInt32(1000 * t);
                
                StreamWriter toText = new StreamWriter("FKData.txt", true);//打开记录数据文本,可于
                toText.WriteLine(j.ToString() + '\t' +
                velocities[j, 0].ToString() + '\t' +
                positions[j, 0].ToString() + '\t' +
                times[j].ToString());
                toText.Close();
            }
            }
            catch (Exception ex){
                DisplayError(ex);
            }

        }

        private void startButton_Click(object sender, EventArgs e)
        {
            try{

            HomeAxisButton.Enabled = false;
            startButton.Enabled = false;

            ang = 4 * System.Math.Atan(1); // = PI;
            angV = 0;

            calcSegments();

                numberOfEvents = 0;
                numberOfEventsTextBox.Text = Convert.ToString(numberOfEvents);

                ProfileSettingsObj ProfileSettings;

                ProfileSettings = ampArray[X_AXIS_AMP].ProfileSettings;
                ProfileSettings.ProfileAccel = (ampArray[X_AXIS_AMP].VelocityLoopSettings.VelLoopMaxAcc) / 10;
                ProfileSettings.ProfileDecel = (ampArray[X_AXIS_AMP].VelocityLoopSettings.VelLoopMaxDec) / 10;
                ProfileSettings.ProfileVel = (ampArray[X_AXIS_AMP].VelocityLoopSettings.VelLoopMaxVel) / 10;
                ProfileSettings.ProfileType = CML_PROFILE_TYPE.PROFILE_TRAP;
                ampArray[X_AXIS_AMP].ProfileSettings = ProfileSettings;


                //ProfileSettings = ampArray[Y_AXIS_AMP].ProfileSettings;
                //ProfileSettings.ProfileAccel = (ampArray[Y_AXIS_AMP].VelocityLoopSettings.VelLoopMaxAcc) / 10;
                //ProfileSettings.ProfileDecel = (ampArray[Y_AXIS_AMP].VelocityLoopSettings.VelLoopMaxDec) / 10;
                //ProfileSettings.ProfileVel = (ampArray[Y_AXIS_AMP].VelocityLoopSettings.VelLoopMaxVel) / 10;
                //ampArray[Y_AXIS_AMP].ProfileSettings = ProfileSettings;


                ampArray[X_AXIS_AMP].MoveAbs(positions[0, 0]);
                //ampArray[Y_AXIS_AMP].MoveAbs(positions[0, 1]);   // start y motor at midpoint

                ampArray[X_AXIS_AMP].WaitMoveDone(4000);
                //ampArray[Y_AXIS_AMP].WaitMoveDone(4000);


                Linkage.TrajectoryInitialize(positions, velocities, times, 100);

                EventObj.Start(true, 5000); // false for repeating 5000 for timout
            }
            catch (Exception ex){
            DisplayError(ex);
            }
        }
        private void EventObj_EventNotify(CML_AMP_EVENT match, bool hasError)
        {// Handles EventObj.EventNotify
            try
            {
                if (hasError)
                {
                    Linkage.HaltMove();
                    DialogResult errormsgbox = DialogResult;
                    MessageBox.Show("Error reported by amplifier in Event Notify", "CMO Error", MessageBoxButtons.RetryCancel);
                    if (errormsgbox == DialogResult.Cancel)
                    {
                        // it is possible that this method was called from a thread other than the 
                        // GUI thread - if this is the case we must use a delegate to close the application.
                        CloseApp d = new CloseApp(ThreadSafeClose);
                        this.Invoke(d);
                    }
                    return;
                }


                calcSegments();

                numberOfEvents = numberOfEvents + 1;

                // Since this thread is different from the GUI thread we must use a delegate
                // to call a method to update the GUI component (numberOfEventsTextBox)
                //UpdateGui textBoxDelegate = new UpdateGui(UpdateNumberOfEvents);
                //numberOfEventsTextBox.Invoke(textBoxDelegate);

                Linkage.TrajectoryAdd(positions, velocities, times, 100);
            }
            catch (Exception ex)
            {
                DisplayError(ex);
            }
        }

        //public void UpdateNumberOfEvents(){
        //// If the calling thread is different than the GUI thread, then use the
        //// delegate to update the application, otherwise update text box directly
        //    if (numberOfEventsTextBox.InvokeRequired){
        //        UpdateGui d = new UpdateGui(UpdateNumberOfEvents);
        //        numberOfEventsTextBox.Invoke(d);
        //    }
        //    else{
        //        numberOfEventsTextBox.Text = Convert.ToString(numberOfEvents);
        //    }
        //}

        private void StopPVTButton_Click(object sender, EventArgs e)
        {
            try{

            //EventObj.Stop();
            Linkage.HaltMove();
            startButton.Enabled = true;
            HomeAxisButton.Enabled = true;
            }
            catch(Exception ex){
            DisplayError(ex);
            }
        }

        private void HomeAxisButton_Click(object sender, EventArgs e)
        {
            try{

            HomeAxisButton.Enabled = false;

            Home = ampArray[X_AXIS_AMP].HomeSettings;
            Home.HomeVelFast = (ampArray[X_AXIS_AMP].VelocityLoopSettings.VelLoopMaxVel) / 10;
            Home.HomeVelSlow = (ampArray[X_AXIS_AMP].VelocityLoopSettings.VelLoopMaxVel) / 15;
            Home.HomeAccel = ampArray[X_AXIS_AMP].VelocityLoopSettings.VelLoopMaxAcc / 10;
            Home.HomeMethod = CML_HOME_METHOD.CHOME_INDEX_POSITIVE;
            Home.HomeOffset = -4000;
            ampArray[X_AXIS_AMP].HomeSettings = Home;
            ampArray[X_AXIS_AMP].GoHome();
            ampArray[X_AXIS_AMP].WaitMoveDone(10000);


            //Home = ampArray[Y_AXIS_AMP].HomeSettings;
            //Home.HomeVelFast = (ampArray[Y_AXIS_AMP].VelocityLoopSettings.VelLoopMaxVel) / 10;
            //Home.HomeVelSlow = (ampArray[Y_AXIS_AMP].VelocityLoopSettings.VelLoopMaxVel) / 15;
            //Home.HomeAccel = ampArray[Y_AXIS_AMP].VelocityLoopSettings.VelLoopMaxAcc / 10;
            //Home.HomeMethod = CML_HOME_METHOD.CHOME_INDEX_POSITIVE;
            //Home.HomeOffset = -1000;
            //ampArray[Y_AXIS_AMP].HomeSettings = Home;
            //ampArray[Y_AXIS_AMP].GoHome();
            //ampArray[Y_AXIS_AMP].WaitMoveDone(10000);

            HomeAxisButton.Enabled = true;
            startButton.Enabled = true;
            }
            catch(Exception ex){
            HomeAxisButton.Enabled = true;
            DisplayError(ex);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try{

            //ampArray[Y_AXIS_AMP].Disable();
            ampArray[X_AXIS_AMP].Disable();
            }
            catch (Exception ex){
            DisplayError(ex);
            }
        }
        public void DisplayError(Exception ex)
        {
            DialogResult errormsgbox = DialogResult;
            MessageBox.Show("Error Message: " + ex.Message + "\n" + "Error Source: "
                + ex.Source, "CMO Error", MessageBoxButtons.RetryCancel);
            if (errormsgbox == DialogResult.Cancel)
            {
                // it is possible that this method was called from a thread other than the 
                // GUI thread - if this is the case we must use a delegate to close the application.
                //Dim d As New CloseApp(AddressOf ThreadSafeClose)
                CloseApp d = new CloseApp(ThreadSafeClose);
                this.Invoke(d);
            }
        }

        public void ThreadSafeClose()
        {
            //If the calling thread is different than the GUI thread, then use the
            //delegate to close the application, otherwise call close() directly
            if (this.InvokeRequired)
            {
                CloseApp d = new CloseApp(ThreadSafeClose);
                this.Invoke(d);
            }
            else
                Close();
        }

    }
}
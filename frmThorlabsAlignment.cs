using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using InstrumentsLib.Tools.Instruments.OpticalAlignment;
using InstrumentsLib.Tools.Instruments.PowerMeter;
using InstrumentsLib;
using WaferLevelTestLib;
using DataLib;
using Utility;
using System.IO;

namespace ThorlabsAlignSys
{
    public partial class frmThorlabsAlignment : Form
    {
        protected IPowerMeter _PWM;
        protected List<string> _arguments;
        public bool OOConfig = false; // Added by Alex 05/10/17

        public void CloseForm()
        {
            if (this.InvokeRequired)
            {
                //Call the delegate
                this.Invoke(new MethodInvoker(() => { this.CloseForm(); }));
            }
            else
            {
                this.Close();
            }
        }

        public frmThorlabsAlignment()
        {
            _objSafeLocation = null;
            InitializeComponent();

            //spectra_measure.AlignThorlabs += AlignThorlabs;

            _arguments = new List<string>();

        }

        public frmThorlabsAlignment(ThorlabsOOAlignConfig config, IAlignmentSysSafeLocation objSafeLocation)
        {
            Config = config;
            _objSafeLocation = objSafeLocation;
            InitializeComponent();

            //spectra_measure.AlignThorlabs += AlignThorlabs;

            _arguments = new List<string>();

        }



        //private void AlignThorlabs(object sender, AlignEventArgs e)
        //{
        //    FindPeakOO(e.Threshold);
        //}

        public ThorlabsOOAlignConfig Config { get; set; }

        private IAlignmentSysSafeLocation _objSafeLocation { get; set; }

        CAligmentMemory _InputAlignMem;

        CAligmentMemory _OutputAlignMem;


        public float _TestFileOffsetX_in = 0;

        public float _TestFileOffsetY_in = 0;

        public float _TestFileOffsetX_out = 0;

        public float _TestFileOffsetY_out = 0;

        public float output_power = 0;
        public bool coupling_status = false;

        public void InitializeAPT()
        {
            StartThorlabsWatchDog();

            if (Config.InputMotorSN != 0) OOConfig = true; // Added by Alex 05/10/17
            if (OOConfig)
            {
                axMotorInput.CreateControl();
                axMotorInput.StartCtrl();// Added by Alex 05/10/17
            }
            axMotorZ.StartCtrl();
            axMotorOutput.StartCtrl();
            if (OOConfig)
            {
                axMotorInput.CreateControl();
                axNanoInput.StartCtrl();// Added by Alex 05/10/17
            }
            axNanoZ.StartCtrl();
            axNanoOutput.StartCtrl();

            _InputAlignMem = new CAligmentMemory(@"C:\Temp\InputMotorMemAlign.xml");
            _InputAlignMem.deserialize();


            _OutputAlignMem = new CAligmentMemory(@"C:\Temp\OutputMotorMemAlign.xml");
            _OutputAlignMem.deserialize();

            if (OOConfig) axMotorInput.HWSerialNum = Config.InputMotorSN; // Added by Alex 05/10/17
            axMotorZ.HWSerialNum = Config.ZMotorSN;
            axMotorOutput.HWSerialNum = Config.OutputMotorSN;
            if (OOConfig) axNanoInput.HWSerialNum = Config.InputNanoSN; // Added by Alex 05/10/17
            axNanoZ.HWSerialNum = Config.ZNanoSN;
            axNanoOutput.HWSerialNum = Config.OutputNanoSN;

            bool tmp = false;
            axMotorOutput.GetHWCommsOK(ref tmp);
            if (tmp == false)
            {
                MessageBox.Show("Com Error");
                return;
            }

            bool HomeStatus1 = false; // Added by Alex 05/10/17
            bool HomeStatus2 = false; // Added by Alex 05/10/17
            bool HomeStatus3 = false; // Added by Alex 05/10/17
            bool HomeStatus4 = false; // Added by Alex 05/10/17
            bool HomeStatus5 = false; // Added by Alex 05/10/17
            bool HomeStatus6 = false; // Added by Alex 05/10/17

            if (OOConfig) HomeStatus1 = CheckHomeStatus(axMotorInput, 0); // Added by Alex 05/10/17
            if (OOConfig) HomeStatus2 = CheckHomeStatus(axMotorInput, 1); // Added by Alex 05/10/17
            HomeStatus3 = CheckHomeStatus(axMotorOutput, 0);
            HomeStatus4 = CheckHomeStatus(axMotorOutput, 1);
            if (OOConfig) HomeStatus5 = CheckHomeStatus(axMotorZ, 0);// Added by Alex 05/10/17
            HomeStatus6 = CheckHomeStatus(axMotorZ, 1);


            if (OOConfig) // Added by Alex 05/10/17
            { // Added by Alex 05/10/17
                if (!HomeStatus1 || !HomeStatus2 || !HomeStatus3 || !HomeStatus4 || !HomeStatus5 || !HomeStatus6)// Added by Alex 05/10/17
                {
                    MessageBox.Show("Homing Error");
                }
            } // Added by Alex 05/10/17
            else // Added by Alex 05/10/17
            { // Added by Alex 05/10/17
                if (!HomeStatus3 || !HomeStatus4 || !HomeStatus6) // Added by Alex 05/10/17
                { // Added by Alex 05/10/17
                    MessageBox.Show("Homing Error"); // Added by Alex 05/10/17
                } // Added by Alex 05/10/17
            } // Added by Alex 05/10/17


            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoZ.Latch();

            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();
            axNanoOutput.Latch();

            if (OOConfig)
            {
                axNanoInput.SetCircHomePos(5, 5);
                axNanoInput.MoveCircHome();
                axNanoInput.Latch();
            }

            if (OOConfig) axNanoInput.SetInputSrc(1); // Added by Alex 05/10/17
            axNanoZ.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);

            if (OOConfig) axNanoInput.SetUnitsMode(3, .8f, 1, 1); // Added by Alex 05/10/17
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            if (OOConfig) axNanoInput.SetRangingMode(1, 2); // Added by Alex 05/10/17
            axNanoOutput.SetRangingMode(1, 2);
            axNanoZ.SetRangingMode(1, 2);

            axNanoOutput.SetLoopGain(300);
            if (OOConfig) axNanoInput.SetLoopGain(300); // Added by Alex 05/10/17
            axNanoZ.SetLoopGain(300);

            axMotorOutput.SetJogStepSize(0, 0.005f);
            axMotorOutput.SetJogStepSize(1, 0.005f);
            if (OOConfig) axMotorInput.SetJogStepSize(0, 0.005f); // Added by Alex 05/10/17
            if (OOConfig) axMotorInput.SetJogStepSize(1, 0.005f); // Added by Alex 05/10/17
            if (OOConfig) axMotorZ.SetJogStepSize(0, 0.005f); // Added by Alex 05/10/17
            axMotorZ.SetJogStepSize(1, 0.005f);

            //Int64 rslt = 0;
            //axNanoZ.SetCircDiaMode(1);
            //float dia = 2f;
            //try
            //{
            //    rslt = axNanoZ.SetCircDia(dia);
            //}
            //catch
            //{

            //}

            //axNanoInput.SetCircDiaMode(1);
            //try
            //{
            //    rslt = axNanoInput.SetCircDia(0.035f);
            //}
            //catch
            //{

            //}

            ////axNanoOutput.SetLoopGain( 100);
            //axNanoOutput.SetCircDiaMode(1);

            //try
            //{
            //   rslt = axNanoOutput.SetCircDia(0.035f);
            //}
            //catch
            //{

            //}

            //textBox_thresh.Text = "qqq";
            //string aaa = textBox_thresh.Text;

            axNanoZ.SetRangingParams(95, 5, 4);
            if (OOConfig) axNanoInput.SetRangingParams(95, 5, 4); // Added by Alex 05/10/17
            axNanoOutput.SetRangingParams(95, 5, 4);

            axNanoZ.SetPhaseComp(-10, -10);
            if (OOConfig) axNanoInput.SetPhaseComp(-20, -27); // Added by Alex 05/10/17
            axNanoOutput.SetPhaseComp(-20, -27);

            axNanoZ.SetLPFilter(5);
            if (OOConfig) axNanoInput.SetLPFilter(5); // Added by Alex 05/10/17
            axNanoOutput.SetLPFilter(5);

            axNanoZ.SetCircFreq(29);
            if (OOConfig) axNanoInput.SetCircFreq(35); // Added by Alex 05/10/17
            axNanoOutput.SetCircFreq(44);

        }


        public void init_circ_size()
        {
            Int64 rslt = 0;
            axNanoZ.SetCircDiaMode(1);

            if (0 != Config.ZNanoSN)    // Z motors exists
            {
                try
                {
                    rslt = axNanoZ.SetCircDia(0.035f);
                }
                catch
                {

                }
            }

            if (0 != Config.InputNanoSN)   // Input motor exists
            {
                axNanoInput.SetCircDiaMode(1);
                try
                {
                    rslt = axNanoInput.SetCircDia(0.035f);
                }
                catch
                {

                }
            }

            if (0 != Config.OutputNanoSN)   // Output motor exists
            {
                axNanoOutput.SetCircDiaMode(1);
                try
                {
                    rslt = axNanoOutput.SetCircDia(0.035f);
                }
                catch
                {

                }
            }
        }
        public void NanoRaster()
        {
            double[,] opt_power = new double[10, 10];

            float opt_power1 = 0f;
            int range = 0;
            float rel = 0f;
            int overunder = 0;
            float xpos = 2;
            float ypos = 2;
            for (int i = 0; i < 10; i++)
            {
                xpos += .5f;
                for (int j = 0; j < 10; j++)
                {
                    ypos += .5f;
                    axNanoOutput.SetCircHomePos(xpos, xpos);
                    axNanoOutput.MoveCircHome();
                    System.Threading.Thread.Sleep(150);
                    axNanoOutput.GetReading(ref opt_power1, ref range, ref rel, ref overunder);
                    opt_power[i, j] = opt_power1;
                }
            }
            MessageBox.Show("done");

        }


        //Hex Value     Bit Number  Description
        //0x00000001    1           CW hardware limit switch (0 - no contact, 1 - contact).
        //0x00000002    2           CCW hardware limit switch (0 - no contact, 1 - contact).
        //0x00000004    3           CW software limit switch (0 - no contact, 1 - contact).
        //                          Not applicable to Part Number ODC001 and TDC001 controllers
        //0x00000008    4           CCW software limit switch (0 - no contact, 1 - contact).
        //                          Not applicable to Part Number ODC001 and TDC001 controllers
        //0x00000010    5           Motor shaft moving clockwise (1 - moving, 0 - stationary).
        //0x00000020    6           Motor shaft moving counterclockwise (1 - moving, 0 - stationary)
        //0x00000040    7           22) Shaft jogging clockwise (1 - moving, 0 - stationary).
        //0x00000080    8           21) Shaft jogging counterclockwise (1 - moving, 0 - stationary). 
        //0x00000100    9           Motor connected (1 - connected, 0 - not connected).
        //                          Not applicable to Part Number BMS001 and BMS002 controllers
        //                          Not applicable to Part Number ODC001 and TDC001 controllers
        //0x00000200                Motor homing (1 - homing, 0 - not homing).
        //0x00000400                Motor homed (1 - homed, 0 - not homed).
        //0x00000800                For Future Use

        // protected const int MotorShaftMovingCW = 0x10;
        // protected const int MotorShaftMovingCCW = 0x20;
        //protected const int HomeComplete = 0x400;

        private bool waitForMotorStatus(AxMG17MotorLib.AxMG17Motor motor, int channel, double timeout_s = 2.0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan ts;
            int stBits = 0;
            do
            {
                ts = DateTime.Now - startTime;

                motor.LLGetStatusBits(channel, ref stBits);
                string stBitsB = Convert.ToString(stBits, 2);
                char stBitsB27 = stBitsB[27]; //Motor moving CW
                char stBitsB26 = stBitsB[26]; //Motor moving CCW
                if (stBitsB27.Equals('0') && stBitsB26.Equals('0'))
                {
                    //Debug.WriteLine(string.Format("Motors report not moving"));
                    return true;
                }
            }
            while (ts.TotalSeconds < timeout_s);

            Debug.WriteLine("FAILED to FIND motor move complete condition");
            return false;
        }

        private bool CheckHomeStatus(AxMG17MotorLib.AxMG17Motor motor, int channel)
        {
            int stBits = 0;
            motor.LLGetStatusBits(channel, ref stBits);
            string stBitsB = Convert.ToString(stBits, 2);
            char stBitsB21 = stBitsB[21];
            if (stBitsB21.Equals('1'))
            {
                Debug.WriteLine(string.Format("Motors are homed"));
                return true;
            }

            Debug.WriteLine("FAILED to FIND condition");
            return false;
        }

        /// <summary>
        /// Enable/disable tracking of the active align signal during TX testing (e.g., RIN)
        /// </summary>
        /// <param name="status">Set to true to track, set to false to stop tracking</param>
        public void TrackPeakTX(bool status)
        {
            switch (status)
            {
                case true:
                    axNanoOutput.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_BNC1V);
                    axNanoZ.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_BNC1V);
                    axNanoOutput.Track();
                    axNanoZ.Track();
                    break;

                case false:
                    axNanoOutput.Latch();
                    axNanoZ.Latch();
                    break;
            }
        }

        public void TrackPeakTXOptical(bool status)
        {
            switch (status)
            {
                case true:
                    axNanoOutput.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_TIA);
                    axNanoZ.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_TIA);
                    axNanoOutput.Track();
                    axNanoZ.Track();
                    break;

                case false:
                    axNanoOutput.Latch();
                    axNanoZ.Latch();
                    break;
            }
        }

        public void TrackPeakTXParallel(bool status)
        {
            switch (status)
            {
                case true:
                    axNanoOutput.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_TIA);
                    axNanoZ.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_TIA);
                    axNanoOutput.Track();
                    axNanoZ.TrackEx(3);
                    break;

                case false:
                    axNanoOutput.Latch();
                    axNanoZ.Latch();
                    break;
            }
        }

        public void InputSpiralRaster(int array_size, float Threshold, ref CArrayData myCoordinates)
        {

            axNanoZ.Latch();
            axNanoInput.Latch();
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);

            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            float rel = 0f;
            int overunder = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();


            axNanoZ.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_BNC5V);
            axNanoInput.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_BNC5V);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoInput.GetReading(ref opt_power1, ref range, ref rel, ref overunder);

            List<(float x, float y)> points = new List<(float, float)>();
            int x = 0, y = 0, dx = 0, dy = -1;
            for (int i = 0; i < array_size; i++)
            {
                points.Add((x: x, y: y));

                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }
                x += dx;
                y += dy;
            }


            float motor_x_in = 0;
            float motor_y_in = 0;
            axMotorInput.GetPosition((int)0, ref motor_x_in);
            axMotorInput.GetPosition((int)1, ref motor_y_in);
            (float x, float y) startingpoint = (x: motor_x_in, y: motor_y_in);
            float stepsize = 0.005f;

            var iopoints = points.Select(point => (x: point.x * stepsize + startingpoint.x, y: point.y * stepsize + startingpoint.y)).ToList();

            DateTime startTime = DateTime.Now;
            bool success = false;
            float max_power = opt_power1;
            for (int i=0; i < iopoints.Count; i++)
            {
                MoveXYAbsoluteInputMotor(iopoints[i].x, iopoints[i].y);
                axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
                Console.WriteLine($"{i}/{iopoints.Count} Input: ({iopoints[i]} Power: {opt_power1.ToString()}");

                if (opt_power1 > Threshold/2.0)
                {
                    success = true;
                    break;
                }
            }
            if (!success)
            {
                MoveXYAbsoluteInputMotor(motor_x_in, motor_y_in);
            }

            textBoxTextWrite(textBox_ScanRslt, Convert.ToString(max_power));
            return;
        }

        public void FindPeakOOFirstLight(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bRaster = false)
        {

            int array_size = 400;
            // coords = new List<double>();
            // Threshold = 50.0f;
            //textBox_thresh.Text = Convert.ToString(Threshold);
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 800, 500, 300, 100 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] ZCD = new float[] { 3f, 2f, 1f, .5f };

            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            float V_TRACK_LEVEL = -55;
            float DBM_TRACK_LEVEL = 0.5f;
            bool RESIZE = true;
            bool DBM = true;
            int rslt = 0;
            int loop_count = 0;
            float DXout = 0f, DYout = 0f, DZout = 0f, Dout = 0f;
            float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            float DXin = 0f, DYin = 0f, DZin = 0f, Din = 0f;
            float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            //bool result = false;

            _PWM = (IPowerMeter)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.PWM];
            string curr_range = _PWM.Range;
            bool curr_aRange = _PWM.AutoRange;
            // _PWM.autoRange = false;
            // _PWM.range = "-50";





            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();


            axNanoZ.SetInputSrc(4);
            axNanoInput.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            //Thread.Sleep(500);
            //[~,X_IN(loop_count),Y_IN(loop_count),Power(loop_count)]=axMG17NanoTrak3.GetCircPosReading(0,0,0,0,0,0);
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            //axMG17NanoTrak3.GetReading(ref opt_power1, ref range, ref rel, ref overunder);


            List<(float x, float y)> points = new List<(float , float)>();
            int x = 0, y = 0, dx = 0, dy = -1;
            for (int i = 0; i < 100; i++) // approx 10x10
            {
                points.Add((x: x, y: y));

                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    int temp = dx;
                    dx = -dy;
                    dy = temp;
                }
                x += dx;
                y += dy;
            }


            float motor_x_in = 0; 
            float motor_y_in = 0;
            float motor_x_out = 0;
            float motor_y_out = 0;
            axMotorInput.GetPosition((int)0, ref motor_x_in);
            axMotorInput.GetPosition((int)1, ref motor_y_in);
            axMotorOutput.GetPosition((int)0, ref motor_x_out);
            axMotorOutput.GetPosition((int)1, ref motor_y_out);
            float stepsize = 0.010f;
            ((float x, float y)input, (float x, float y) output) startingpoint = (input: (x: motor_x_in, y: motor_y_in), 
                output: (x: motor_x_out, y: motor_y_out));
            //var startingpoint = (input: (x: XinPos[0], y: YinPos[0]), output: (x: XoutPos[0], y: YoutPos[0]));
            var iopoints = points.SelectMany(input => points.Select(output =>
                (input: (x: input.x * stepsize + startingpoint.input.x, y: input.y * stepsize + startingpoint.input.y),
                output: (x: output.x * stepsize + startingpoint.output.x, y: output.y * stepsize + startingpoint.output.y))
                )).ToList();

            DateTime startTime = DateTime.Now;
            TimeSpan ts;
            bool largesearch = false;

            //if (opt_power1 < -30)
            //{
            //    InputRaster(100, 5, -55); // Try a 100 scan on input first.
            //}

            for (int i=0; i < iopoints.Count; i++)
            {
                //iopoints[i].input.x;

                MoveXYAbsoluteInputMotor(iopoints[i].input.x, iopoints[i].input.y);
                MoveXYAbsoluteOutputMotor(iopoints[i].output.x, iopoints[i].output.y);

                pause(startTime);

                axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
                Console.WriteLine($"{i}/{iopoints.Count} Input: ({iopoints[i].input} Output: {iopoints[i].output}, Power: {opt_power1.ToString()}");

                if (opt_power1 > -60)
                {
                    if (i >= 1)
                    {
                        largesearch = true; // Likely memory positions invalid for current device
                    }
                    break;
                }
            }

            if (opt_power1 < -30 && opt_power1 > -60)
            {
                InputRaster(100, 5, -30);
                OutputRaster(100, 5, -30);
            }
            FindPeakOO(Threshold, ref myCoordinates, bSave, bRaster);

            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            if (largesearch && opt_power1 > Threshold) 
            {
                for (int i = 0; i < 5; i++) // Align 5 times to flush memory and to to refine the median
                {
                    myCoordinates = new CArrayData();
                    FindPeakOO(Threshold, ref myCoordinates, bSave, bRaster);
                }
            }

            if (opt_power1 < Threshold)
            {
                MoveXYAbsoluteInputMotor(iopoints[0].input.x, iopoints[0].input.y);
                MoveXYAbsoluteOutputMotor(iopoints[0].output.x, iopoints[0].output.y);
            }
            ts = DateTime.Now - startTime;

            return;
        }

        //Adding thorlab alignment system for multidieTX
        public void FindPeakTx_MultiDieInput(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);

            string log_res;

            //int[] LG = new int[] { 1800, 1000, 700, 400, 150 };
            int[] LG = new int[] { 1800, 1200, 700, 400, 150 };
            //int[] LG_Z = new int[] { 5000, 2500, 1750, 1000, 500 };
            int[] LG_Z = new int[] { 5000, 3000, 1750, 1000, 500 };
            //float[] CD = new float[] { 4.5f, 3.6f, 3.0f, 3.0f, .4f };
            //float[] CD = new float[] { 4.5f, 4.5f, 3.5f, 3.0f, .4f };
            //float[] CD_Z = new float[] { 4.5f, 4f, 3.5f, 2.5f, .6f };
            //float[] CD_Z = new float[] { 4.5f, 4.5f, 3.5f, 2.5f, .6f };
            float[] CD = new float[] { 1.75f, .6f, .5f, .4f, .1f };
            float[] CD_Z = new float[] { 3.5f, 3f, 2.5f, 2f, .3f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 1;

            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            float FINE_TUNE_LEVEL = +5f;
            float JXin = 0f, JYin = 0f, JZin = 0f;
            float PXin = 0f, PYin = 0f, PZin = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;
            float min_initial_search_time = 0.7f;   // Minimum time before range can be changed from the initial value.  Helps keep the motors from getting stuck in a local maximum

            int rslt = 0;
            int loop_count = 0;
            //The below 4 lines need to comment out for mutli-die. Qing
            //axNanoZ.SetCircHomePos(5, 5);
            //axNanoZ.MoveCircHome();
            //axNanoInput.SetCircHomePos(5, 5);
            //axNanoInput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoInput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(2);
                axNanoInput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;
            //int k = 0;
            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            //while (k++ < 20 | ((RANGE != 2 | false == settled_flag)))
            {
                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");
                    InputRaster(40, 5, -45); //need to modify. No.1
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(2);
                    axNanoInput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXin = 0; JYin = 0; JZin = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXin = JXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        JYin = JYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                        JZin = JZin + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XinPosRecent = new float[BreakCheckSize];
                    float[] YinPosRecent = new float[BreakCheckSize];
                    float[] ZinPosRecent = new float[BreakCheckSize];
                    Array.Copy(XinPos, loop_count - BreakCheckSize, XinPosRecent, 0, BreakCheckSize);
                    PXin = XinPosRecent.Max() - XinPosRecent.Min();
                    Array.Copy(YinPos, loop_count - BreakCheckSize, YinPosRecent, 0, BreakCheckSize);
                    PYin = YinPosRecent.Max() - YinPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZinPosRecent, 0, BreakCheckSize);
                    PZin = ZinPosRecent.Max() - ZinPosRecent.Min();
                    float XJitterMoveratio = Math.Abs(JXin / PXin);
                    float YJitterMoveratio = Math.Abs(JYin / PYin);
                    float ZJitterMoveratio = Math.Abs(JZin / PZin);
                    float XYJitterMoveRatioLimit = 6f;
                    float ZJitterMoveRatioLimit = 4f;
                    float XYJitterLimit = 1.5f;
                    float ZJitterLimit = 1.5f;
                    float XYMoveLimit = 0.4f;
                    float ZMoveLimit = 0.4f;
                    bool breakout = false;
                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXin < XYJitterLimit & JYin < XYJitterLimit & JZin < ZJitterLimit));
                    bool cond2 = (PXin < XYMoveLimit) & (PYin < XYMoveLimit) & (PZin < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXin, JYin, JZin);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXin, PYin, PZin);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        break;
                    }
                }
                //end break out criteria
                float recenter_border = 1.5f;
                if ((ZinPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZinPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z_IN");
                    startTime = DateTime.Now;
                }
                if ((XinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XinPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YinPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("IN_MULTIDIE"); //Need modify. No. 2. For multidie tester setup, the Y axis is reversed compared to POR.
                    startTime = DateTime.Now;
                }
                //end recenter check

                /* Logic for changing the circle size */

                // Allow motors to freely go to a lower range
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL && RANGE != 0 && ts.TotalSeconds > 0.2f)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }

                // Keep motors from freely going to a higher range until after a certain amount of time
                if (ts.TotalSeconds > min_initial_search_time)
                {
                    if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                    {
                        RANGE = 1; RESIZE = true;
                        log_res = string.Format("To Range 1 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else if (opt_power1 >= Threshold & RANGE < 2)
                    {
                        RANGE = 2; RESIZE = true;
                        last_range_2_index = loop_count;
                        log_res = string.Format("To Range 2 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                    {
                        RANGE = 3; RESIZE = true;
                        last_range_2_index = loop_count;
                        log_res = string.Format("To Range 3 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else
                    {
                        //DONT SET RANGE
                    }
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoInput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoInput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }
            RANGE = 3;// 4;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            axNanoInput.SetLoopGain(LG[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            rslt = axNanoInput.SetCircDia(CD[RANGE]);
            Thread.Sleep(500);
            axNanoZ.Latch();
            axNanoInput.Latch();
            axNanoInput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(true, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakTx_MultiDieOutput(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            string log_res;

            //int[] LG = new int[] { 1800, 1000, 700, 400, 150 };
            int[] LG = new int[] { 1800, 1200, 700, 400, 150 };
            //int[] LG_Z = new int[] { 5000, 2500, 1750, 1000, 500 };
            int[] LG_Z = new int[] { 5000, 3000, 1750, 1000, 500 };
            //float[] CD = new float[] { 4.5f, 3.6f, 3.0f, 3.0f, .4f };
            //float[] CD = new float[] { 4.5f, 4.5f, 3.5f, 3.0f, .4f };
            //float[] CD_Z = new float[] { 4.5f, 4f, 3.5f, 2.5f, .6f };
            //float[] CD_Z = new float[] { 4.5f, 4.5f, 3.5f, 2.5f, .6f };
            float[] CD = new float[] { 1.75f, .6f, .5f, .4f, .1f };
            float[] CD_Z = new float[] { 3.5f, 3f, 2.5f, 2f, .3f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 1;

            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            float FINE_TUNE_LEVEL = +5f;
            float JXout = 0f, JYout = 0f, JZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;
            float min_initial_search_time = 0.7f;   // Minimum time before range can be changed from the initial value.  Helps keep the motors from getting stuck in a local maximum

            int rslt = 0;
            int loop_count = 0;
            //The below 4 lines need to comment out for mutli-die. Qing
            //axNanoZ.SetCircHomePos(5, 5);
            //axNanoZ.MoveCircHome();
            //axNanoOutput.SetCircHomePos(5, 5);
            //axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);  //this is per module, means two channels together. Can not track the other side while align this side.
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;

            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {
                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");
                    OutputRaster(40, 5, -45);
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(3);
                    axNanoOutput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXout = 0; JYout = 0; JZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXout = JXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        JYout = JYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        JZout = JZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];
                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float XJitterMoveratio = Math.Abs(JXout / PXout);
                    float YJitterMoveratio = Math.Abs(JYout / PYout);
                    float ZJitterMoveratio = Math.Abs(JZout / PZout);
                    float XYJitterMoveRatioLimit = 6f;
                    float ZJitterMoveRatioLimit = 4f;
                    float XYJitterLimit = 1.5f;
                    float ZJitterLimit = 1.5f;
                    float XYMoveLimit = 0.4f;
                    float ZMoveLimit = 0.4f;
                    bool breakout = false;
                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXout < XYJitterLimit & JYout < XYJitterLimit & JZout < ZJitterLimit));
                    bool cond2 = (PXout < XYMoveLimit) & (PYout < XYMoveLimit) & (PZout < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXout, JYout, JZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXout, PYout, PZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        break;
                    }
                }
                //end break out criteria
                float recenter_border = 1.5f;
                if ((ZoutPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z_OUT");  //need to modify. Qing
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XoutPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YoutPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check

                /* Logic for changing the circle size */

                // Allow motors to freely go to a lower range
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL && RANGE != 0 && ts.TotalSeconds > 0.2f)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }

                // Keep motors from freely going to a higher range until after a certain amount of time
                if (ts.TotalSeconds > min_initial_search_time)
                {
                    if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                    {
                        RANGE = 1; RESIZE = true;
                        log_res = string.Format("To Range 1 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else if (opt_power1 >= Threshold & RANGE < 2)
                    {
                        RANGE = 2; RESIZE = true;
                        last_range_2_index = loop_count;
                        log_res = string.Format("To Range 2 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                    {
                        RANGE = 3; RESIZE = true;
                        last_range_2_index = loop_count;
                        log_res = string.Format("To Range 3 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else
                    {
                        //DONT SET RANGE
                    }
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }
            RANGE = 3;// 4;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            Thread.Sleep(500);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoOutput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(true, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakOO(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bRaster = false)
        {

            int array_size = 400;
            // coords = new List<double>();
            // Threshold = 50.0f;
            //textBox_thresh.Text = Convert.ToString(Threshold);
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 800, 500, 300, 100 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] ZCD = new float[] { 3f, 2f, 1f, .5f };

            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            float V_TRACK_LEVEL = -55;
            float DBM_TRACK_LEVEL = 0.5f;
            bool RESIZE = true;
            bool DBM = true;
            int rslt = 0;
            int loop_count = 0;
            float DXout = 0f, DYout = 0f, DZout = 0f, Dout = 0f;
            float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            float DXin = 0f, DYin = 0f, DZin = 0f, Din = 0f;
            float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            //bool result = false;

            _PWM = (IPowerMeter)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.PWM];
            string curr_range = _PWM.Range;
            bool curr_aRange = _PWM.AutoRange;
            // _PWM.autoRange = false;
            // _PWM.range = "-50";





            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();


            axNanoZ.SetInputSrc(4);
            axNanoInput.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            //Thread.Sleep(500);
            //[~,X_IN(loop_count),Y_IN(loop_count),Power(loop_count)]=axMG17NanoTrak3.GetCircPosReading(0,0,0,0,0,0);
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            //axMG17NanoTrak3.GetReading(ref opt_power1, ref range, ref rel, ref overunder);





            if (opt_power1 < Threshold - RANGE_CHANGE_LEVEL & RANGE != 1)
            {
                RANGE = 0; RESIZE = true;
            }
            else if (opt_power1 >= Threshold - RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 2)
            {
                RANGE = 1; RESIZE = true;
            }
            else if (opt_power1 >= Threshold & RANGE != 3)
            {
                RANGE = 2; RESIZE = true;
            }

            if (RESIZE)
            {
                RESIZE = false;
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                // axNanoZ.SetLoopGain(ZLG[0]);
                // rslt = axNanoZ.SetCircDia(ZCD[0]);

                axNanoInput.Track();
                axNanoOutput.Track();
                //  axNanoZ.Track();
            }
            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
            // while (RANGE != 2 | loop_count<20)
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }
                // put pause here...
                //if (checkBox1.Checked)
                //{
                //    backgroundWorker1.CancelAsync();
                //    return;
                //}

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if (bRaster)
                {
                    if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                    {
                        clog.Log(clog.Level.Warn, "No light, raster scan");
                        Debug.WriteLine("no light, raster scan");
                        InputRaster(40, 5, -55);
                        OutputRaster(40, 5, -55);
                        startTime = DateTime.Now;//Reset the clock for min and max timeout
                        axNanoZ.TrackEx(3);
                        axNanoInput.Track();
                        axNanoOutput.Track();
                    }
                }

                //ALex Semakov 03/22/2017
                //This infinite loop puases the alignment and waits for the "Red" state of checkBox1
                pause(startTime);


                //  axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                int NumPoints = 8;

                if (RANGE == 2 & loop_count > (NumPoints - 1) & opt_power1 >= Threshold)
                {
                    //total distance traveled recently
                    DXout = Math.Abs(XoutPos[loop_count] - XoutPos[loop_count - NumPoints]);
                    DYout = Math.Abs(YoutPos[loop_count] - YoutPos[loop_count - NumPoints]);
                    // DZout = Math.Abs(ZoutPos[loop_count] - ZoutPos[loop_count - NumPoints]);
                    Dout = DXout + DYout;

                    DXin = Math.Abs(XinPos[loop_count] - XinPos[loop_count - NumPoints]);
                    DYin = Math.Abs(YinPos[loop_count] - YinPos[loop_count - NumPoints]);
                    //  DZin = Math.Abs(ZinPos[loop_count] - ZinPos[loop_count - NumPoints]);
                    Din = DXin + DYin;

                    //sum of all jitters recently
                    MXout = 0; MYout = 0;
                    for (int i = loop_count - (NumPoints - 1); i <= loop_count; i++)
                    {
                        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        // MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    Mout = MXout + MYout;

                    MXin = 0; MYin = 0;
                    for (int i = loop_count - (NumPoints - 1); i <= loop_count; i++)
                    {
                        MXin = MXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        MYin = MYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                        // MZin = MZin + Math.Abs(ZinPos[i] - ZinPos[i - 1]);
                    }
                    Min = MXin + MYin;

                    Double BreakPower = .025;

                    if ((Dout < BreakPower & Din < BreakPower) | (Dout < Mout / 8 & Din < Min / 8))
                    {
                        // bool breakflag = true;
                        break;
                    }
                }



                //recenter?


                if ((XinPos[loop_count] > (9 - CD[RANGE] / 2) || XinPos[loop_count] < (1 + CD[RANGE] / 2) || YinPos[loop_count] > (9 - CD[RANGE] / 2) || YinPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                {
                    ReCenter("IN");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (DBM)
                {
                    if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                    {
                        RANGE = 0; RESIZE = true;
                    }
                    else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                    {
                        RANGE = 1; RESIZE = true;
                    }
                    else if (opt_power1 >= Threshold & RANGE != 2)
                    {
                        RANGE = 2; RESIZE = true;
                        // Thread.Sleep(500);
                    }
                    else
                    {
                        //DONT SET RANGE
                    }



                    if (RESIZE)
                    {
                        RESIZE = false;
                        axNanoInput.SetLoopGain(LG[RANGE]);
                        rslt = axNanoInput.SetCircDia(CD[RANGE]);
                        axNanoOutput.SetLoopGain(LG[RANGE]);
                        rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                        Thread.Sleep(250);
                    }

                }
                //end range/circle size check

                if (opt_power1 < V_TRACK_LEVEL & DBM)
                {
                    DBM = false;
                    axNanoInput.SetInputSrc(4);
                    axNanoOutput.SetInputSrc(4);
                    _PWM.AutoRange = false;
                    _PWM.Range = "-50";
                    Thread.Sleep(500);
                }

                if (opt_power1 > DBM_TRACK_LEVEL & DBM == false)
                {
                    DBM = true;
                    axNanoInput.SetInputSrc(1);
                    axNanoOutput.SetInputSrc(1);

                    Thread.Sleep(250);
                }
                ts = DateTime.Now - startTime;
            }



            if (DBM == false)
            {
                DBM = true;
                axNanoInput.SetInputSrc(1);
                axNanoOutput.SetInputSrc(1);
            }

            RANGE = 3;
            try
            {
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }
            axNanoInput.SetLoopGain(LG[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            axNanoZ.SetLoopGain(ZLG[RANGE]);
            rslt = axNanoZ.SetCircDia(ZCD[RANGE]);



            _PWM.AutoRange = curr_aRange;
            if (_PWM.Range != string.Empty)
            {
                _PWM.Range = curr_range;
            }
            Thread.Sleep(200);
            //axNanoZ.Latch();
            axNanoInput.Latch();
            axNanoOutput.Latch();
            axNanoZ.Latch();
            Thread.Sleep(100);
            axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            // CArrayData myCoordinates = new CArrayData();
            StoreMotorCoords(true, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });

        }

        // Single-sided GC with purely optical feedback.  Defaults to output motor but if that motor doesn't exist, use input motors
        public void FindPeakGratingCoupler(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bRaster = false)
        {
            bool use_input = false;
            AxMG17MotorLib.AxMG17Motor motor;
            AxMG17NanoTrakLib.AxMG17NanoTrak piezo;
            if(0 == Config.OutputMotorSN || 0 == Config.OutputNanoSN)
            {
                motor = axMotorInput;
                piezo = axNanoInput;
                use_input = true;
            }
            else
            {
                motor = axMotorOutput;
                piezo = axNanoOutput;
                use_input = false;
            }

            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            waitForMotorStatus(motor, 0);
            waitForMotorStatus(motor, 1);

            double[,] opt_power = new double[1, 10];
            /* Commented lines are POR for OO */
            //int[] LG = new int[] { 800, 500, 300, 100 };
            int[] LG = new int[] { 800, 500, 300, 100 };
            //int[] ZLG = new int[] { 1000, 700, 500, 300 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            //float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] CD = new float[] { 1.75f, 1f, .5f, .5f };
            //float[] ZCD = new float[] { 3f, 2f, 1f, .5f };
            float[] ZCD = new float[] { 3f, 2f, 1f, 1f };

            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float[] ZPos = new float[array_size];
            float opt_power1 = 0f;
            float opt_powerz = 0f;  // Only used to satisfy call function syntax
            int range = 0;
            int rangez = 0;     // Only used to satisfy call function syntax
            int RANGE = 0;
            float rel = 0f;     // Only used to satisfy call function syntax
            int overunder = 0;  // Only used to satisfy call function syntax
            float ZinPos = 0;
            float ZoutPos = 0;
            float RANGE_CHANGE_LEVEL = -5;
            bool RESIZE = true;
            int rslt = 0;
            int loop_count = 0;
            float DXin = 0f, DYin = 0f, Din = 0f;
            float MXin = 0f, MYin = 0f, Min = 0f;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoZ.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_TIA);

            piezo.SetCircHomePos(5, 5);
            piezo.MoveCircHome();
            piezo.SetInputSrc((int)MG17NanoTrakLib.INPUTSOURCE.INPUT_TIA);
            piezo.SetUnitsMode(3, .8f, 1, 1);
            piezo.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoZ.GetCircPosReading(ref ZinPos, ref ZoutPos, ref opt_powerz, ref rangez, ref rel, ref overunder);
            if (false == use_input)
            {
                ZPos[0] = ZoutPos;
            }
            else
            {
                ZPos[0] = ZinPos;
            }

            if (opt_power1 < Threshold - RANGE_CHANGE_LEVEL & RANGE != 1)
            {
                RANGE = 0; RESIZE = true;
            }
            else if (opt_power1 >= Threshold - RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 2)
            {
                RANGE = 1; RESIZE = true;
            }
            else if (opt_power1 >= Threshold & RANGE != 3)
            {
                RANGE = 2; RESIZE = true;
            }

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(ZLG[RANGE]);
                rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
                piezo.SetLoopGain(LG[RANGE]);
                rslt = piezo.SetCircDia(CD[RANGE]);

                if (false == use_input) // If using output side as single GC
                {
                    axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_VERT);
                }
                else
                {
                    axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_HORZ);
                }
                piezo.Track();
            }
            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                pause(startTime);

                piezo.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoZ.GetCircPosReading(ref ZinPos, ref ZoutPos, ref opt_powerz, ref rangez, ref rel, ref overunder);
                if(false == use_input)
                {
                    ZPos[loop_count] = ZoutPos;
                }
                else
                {
                    ZPos[loop_count] = ZinPos;
                }
                int NumPoints = 8;

                if (RANGE == 2 & loop_count > (NumPoints - 1) & opt_power1 >= Threshold)
                {
                    //total distance traveled recently
                    DXin = Math.Abs(XinPos[loop_count] - XinPos[loop_count - NumPoints]);
                    DYin = Math.Abs(YinPos[loop_count] - YinPos[loop_count - NumPoints]);
                    Din = DXin + DYin;

                    //sum of all jitters recently
                    MXin = 0; MYin = 0;
                    for (int i = loop_count - (NumPoints - 1); i <= loop_count; i++)
                    {
                        MXin = MXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        MYin = MYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                    }
                    Min = MXin + MYin;

                    double BreakPower = .025;

                    if ((Din < BreakPower & Din < BreakPower) | (Din < Min / 8 & Din < Min / 8))
                    {
                        break;
                    }
                }

                //recenter check?
                float recenter_border = 1.0f;
                if ((ZPos[loop_count] > (10 - recenter_border - ZCD[RANGE] / 2) || ZPos[loop_count] < (recenter_border + ZCD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XinPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YinPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -60)
                {
                    if (true == use_input)
                    {
                        ReCenter("IN");
                    }
                    else
                    {
                        ReCenter("OUT");
                    }
                    startTime = DateTime.Now;
                }

                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                {
                    RANGE = 1; RESIZE = true;
                }
                else if (opt_power1 >= Threshold & RANGE != 2)
                {
                    RANGE = 2; RESIZE = true;
                }
                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(ZLG[RANGE]);
                    rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
                    piezo.SetLoopGain(LG[RANGE]);
                    rslt = piezo.SetCircDia(CD[RANGE]);
                    Thread.Sleep(250);
                }
                ts = DateTime.Now - startTime;
            }

            RANGE = 3;
            try
            {
                rslt = piezo.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }
            piezo.SetLoopGain(LG[RANGE]);
            axNanoZ.SetLoopGain(ZLG[RANGE]);
            rslt = axNanoZ.SetCircDia(ZCD[RANGE]);

            Thread.Sleep(500);
            piezo.Latch();
            axNanoZ.Latch();
            Thread.Sleep(100);
            piezo.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            if (true == use_input)
            {
                StoreMotorCoords(true, false, ref myCoordinates, bSave);
            }
            else
            {
                StoreMotorCoords(false, true, ref myCoordinates, bSave);
            }

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });

            if(opt_power1 < -60)  // Catch possibility that light is too low and negatively impacts OO alignment on a subsequent device.  Reset to home
            {
                piezo.MoveCircHome();
                axNanoZ.MoveCircHome();
            }
        }

        /// <summary>
        /// Active align routine for RX (electrical signal read by the Thorlabs through a custom TIA)
        /// </summary>
        /// <param name="Threshold">Used to decide whether coordinates are saved and for ranging</param>
        /// <param name="myCoordinates">Motor position output</param>
        /// <param name="bSave">Flag to turn coordinate saving, for the purposes of future active alignment, on/off</param>
        /// <param name="bLowPassFilter">Flag to turn on a 10Hz low pass filter on/off</param>
        public void FindPeakRx(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bLowPassFilter = false)
        {
            int array_size = 400;

            //  List<double> coords = new List<double>();
            // Threshold = 50.0f;
            //textBox_thresh.Text = Convert.ToString(Threshold);
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            this.init_circ_size();
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 1000, 800, 650, 100 };
            int[] LG_Z = new int[] { 4500, 4000, 3500, 3000 };

            float[] CD = new float[] { 3.5f, 3.5f, 3.5f, 3.5f };
            float[] CD_Z = new float[] { 4f, 3f, 3f, 3f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float[] PowerArray = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -.1f;
            bool RESIZE = true;
            float DXout = 0f, DYout = 0f, DZout = 0f, Dout = 0f;
            float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;
            int rslt = 0;
            int loop_count = 0;
            float DBreakLimit = 0.04f;
            int MDRatioBreakLimit = 7;
            int z_filter = -1;          // Low pass filter mode for the z nano
            int output_filter = -1;     // Low pass filter mode for the output nano

            //bool result = false;

            // Set filtering, if enabled
            if (bLowPassFilter)
            {
                axNanoZ.GetLPFilter(ref z_filter);
                axNanoOutput.GetLPFilter(ref output_filter);
                axNanoZ.SetLPFilter(3);         // 10Hz filter
                axNanoOutput.SetLPFilter(3);    // 10Hz filter
            }

            // Set center
            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            // Set source
            axNanoZ.SetInputSrc(5);
            axNanoOutput.SetInputSrc(5);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref PowerArray[0], ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);

            // Update circle size
            if (opt_power1 < Threshold - RANGE_CHANGE_LEVEL & RANGE != 1)
            {
                RANGE = 0; RESIZE = true;
            }
            else if (opt_power1 >= Threshold - RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 2)
            {
                RANGE = 1; RESIZE = true;
            }
            else if (opt_power1 >= Threshold & RANGE != 3)
            {
                RANGE = 2; RESIZE = true;
            }

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            // Align within max time
            while ((ts.TotalSeconds < min_search_time) | (range != 2 & ts.TotalSeconds <= max_search_time))
            {
                //Debug.WriteLine("Waiting for alignment");
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }
                // Debug.WriteLine(loop_count);
                //Debug.WriteLine(ts.TotalSeconds);

                //This infinite loop pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);
                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref PowerArray[loop_count], ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // Check if it is just circling
                int BreakCheckSize = 7;
                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold & ts.TotalSeconds > min_search_time)
                {
                    //total distance traveled recently
                    DXout = Math.Abs(XoutPos[loop_count] - XoutPos[loop_count - BreakCheckSize]);
                    DYout = Math.Abs(YoutPos[loop_count] - YoutPos[loop_count - BreakCheckSize]);
                    DZout = Math.Abs(ZoutPos[loop_count] - ZoutPos[loop_count - BreakCheckSize]);
                    Dout = DXout + DYout + DZout;

                    //sum of all jitters recently
                    MXout = 0; MYout = 0; MZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    Mout = MXout + MYout + MZout;

                    Debug.WriteLine(Dout);
                    Debug.WriteLine(Mout);
                    if (Dout < DBreakLimit | Dout < Mout / MDRatioBreakLimit)
                    {
                        break;
                    }
                }


                if ((ZoutPos[loop_count] > (9 - CD_Z[RANGE]) || ZoutPos[loop_count] < (1 + CD_Z[RANGE])) && opt_power1 > .2)
                {
                    // ReCenter("Z");
                }
                if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > .2)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                    loop_count = 0;
                }
                //end recenter check


                //change range/circle size?

                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                {
                    RANGE = 1; RESIZE = true;
                }
                else if (opt_power1 >= Threshold & RANGE != 2)
                {
                    RANGE = 2; RESIZE = true;
                }
                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    try
                    {

                        RESIZE = false;
                        axNanoZ.SetLoopGain(LG_Z[RANGE]);
                        rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                        axNanoOutput.SetLoopGain(LG[RANGE]);
                        rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                    }
                    catch
                    {

                    }

                    Thread.Sleep(250);
                }

                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            // update loop gain
            RANGE = 3;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            try
            {
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }

            Thread.Sleep(100);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            Thread.Sleep(100);
            axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            {
                bSave = false;
            }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });

            // Restore filtering to its previous state
            if (bLowPassFilter)
            {
                axNanoZ.SetLPFilter(z_filter);         // Restore the previous filter setting
                axNanoOutput.SetLPFilter(z_filter);    // Restore the previous filter setting
            }
        }

        /// <summary>
        /// Active align routine for RX (electrical signal read by the Thorlabs through a custom TIA) using the input motor
        /// </summary>
        /// <param name="Threshold">Used to decide whether coordinates are saved and for ranging</param>
        /// <param name="myCoordinates">Motor position output</param>
        /// <param name="bSave">Flag to turn coordinate saving, for the purposes of future active alignment, on/off</param>
        /// <param name="bLowPassFilter">Flag to turn on a 10Hz low pass filter on/off</param>
        public void FindPeakRxInputMotor(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bLowPassFilter = false)
        {
            int array_size = 400;

            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            this.init_circ_size();
            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 1000, 800, 650, 100 };
            int[] LG_Z = new int[] { 4500, 4000, 3500, 3000 };

            float[] CD = new float[] { 3.5f, 3.5f, 3.5f, 3.5f };
            float[] CD_Z = new float[] { 4f, 3f, 3f, 3f };

            float[] ZinPos = new float[array_size];
            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float[] PowerArray = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -.1f;
            bool RESIZE = true;
            float DXin = 0f, DYin = 0f, DZin = 0f, Din = 0f;
            float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;
            int rslt = 0;
            int loop_count = 0;
            float DBreakLimit = 0.04f;
            int MDRatioBreakLimit = 7;
            int z_filter = -1;          // Low pass filter mode for the z nano
            int input_filter = -1;     // Low pass filter mode for the input nano

            //bool result = false;

            // Set filtering, if enabled
            if (bLowPassFilter)
            {
                axNanoZ.GetLPFilter(ref z_filter);
                axNanoInput.GetLPFilter(ref input_filter);
                axNanoZ.SetLPFilter(3);         // 10Hz filter
                axNanoInput.SetLPFilter(3);    // 10Hz filter
            }

            // Set center
            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();

            // Set source
            axNanoZ.SetInputSrc(5);
            axNanoInput.SetInputSrc(5);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);

            float garbage = 0;
            axNanoZ.GetCircPosReading(ref ZinPos[0], ref garbage, ref PowerArray[0], ref range, ref rel, ref overunder);
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);

            // Update circle size
            if (opt_power1 < Threshold - RANGE_CHANGE_LEVEL & RANGE != 1)
            {
                RANGE = 0; RESIZE = true;
            }
            else if (opt_power1 >= Threshold - RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 2)
            {
                RANGE = 1; RESIZE = true;
            }
            else if (opt_power1 >= Threshold & RANGE != 3)
            {
                RANGE = 2; RESIZE = true;
            }

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_HORZ);    // Track input Z motor
                axNanoInput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            // Align within max time
            while ((ts.TotalSeconds < min_search_time) | (range != 2 & ts.TotalSeconds <= max_search_time))
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                //This infinite loop pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);
                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref garbage, ref PowerArray[loop_count], ref range, ref rel, ref overunder);
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // Check if it is just circling
                int BreakCheckSize = 7;
                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold & ts.TotalSeconds > min_search_time)
                {
                    //total distance traveled recently
                    DXin = Math.Abs(XinPos[loop_count] - XinPos[loop_count - BreakCheckSize]);
                    DYin = Math.Abs(YinPos[loop_count] - YinPos[loop_count - BreakCheckSize]);
                    DZin = Math.Abs(ZinPos[loop_count] - ZinPos[loop_count - BreakCheckSize]);
                    Din = DXin + DYin + DZin;

                    //sum of all jitters recently
                    MXin = 0; MYin = 0; MZin = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXin = MXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        MYin = MYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                        MZin = MZin + Math.Abs(ZinPos[i] - ZinPos[i - 1]);
                    }
                    Min = MXin + MYin + MZin;

                    Debug.WriteLine(Din);
                    Debug.WriteLine(Min);
                    if (Din < DBreakLimit | Din < Min / MDRatioBreakLimit)
                    {
                        break;
                    }
                }


                if ((ZinPos[loop_count] > (9 - CD_Z[RANGE]) || ZinPos[loop_count] < (1 + CD_Z[RANGE])) && opt_power1 > .2)
                {
                    // ReCenter("Z");
                }
                if ((XinPos[loop_count] > (9 - CD[RANGE] / 2) || XinPos[loop_count] < (1 + CD[RANGE] / 2) || YinPos[loop_count] > (9 - CD[RANGE] / 2) || YinPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > .2)
                {
                    ReCenter("IN");
                    startTime = DateTime.Now;
                    loop_count = 0;
                }
                //end recenter check


                //change range/circle size?

                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                {
                    RANGE = 1; RESIZE = true;
                }
                else if (opt_power1 >= Threshold & RANGE != 2)
                {
                    RANGE = 2; RESIZE = true;
                }
                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    try
                    {

                        RESIZE = false;
                        axNanoZ.SetLoopGain(LG_Z[RANGE]);
                        rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                        axNanoInput.SetLoopGain(LG[RANGE]);
                        rslt = axNanoInput.SetCircDia(CD[RANGE]);
                    }
                    catch
                    {

                    }

                    Thread.Sleep(250);
                }

                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            // update loop gain
            RANGE = 3;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            axNanoInput.SetLoopGain(LG[RANGE]);
            try
            {
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }

            Thread.Sleep(100);
            axNanoZ.Latch();
            axNanoInput.Latch();
            Thread.Sleep(100);
            axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref garbage, ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            {
                bSave = false;
            }

            StoreMotorCoords(true, false, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });

            // Restore filtering to its previous state
            if (bLowPassFilter)
            {
                axNanoZ.SetLPFilter(z_filter);         // Restore the previous filter setting
                axNanoInput.SetLPFilter(z_filter);    // Restore the previous filter setting
            }
        }

        public void FindPeakTx(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {
            clog.Log(clog.Level.Fatal, "POR TX Alignment");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            int[] LG = new int[] { 1200, 500, 250, 150 };
            //int[] LG = new int[] { 1200, 500, 350, 100 }; MATLAB POR

            int[] LG_Z = new int[] { 5000, 2000, 1000, 500 };
            //int[] LG_Z = new int[] { 5000, 2000, 1200, 500 }; MATLAB POR

            float[] CD = new float[] { 1.75f, .6f, .35f, .1f };
            //float[] CD = new float[] { 1.75f, .6f, .35f, .035f }; MATLAB POR
            float[] CD_Z = new float[] { 3.5f, 2f, 1.5f, .3f }; 

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            // int RANGE = 0; MATLAB POR
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            //float RANGE_CHANGE_LEVEL = -10; MATLAB POR
            float MXout = 0f, MYout = 0f, MZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0; 

            bool RESIZE = true;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            
            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;

            //while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
                while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) && (last_range_2_index <= loop_count - 10) && opt_power1 > Threshold)
//if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold)
                {
                    //sum of all jitters recently
                    MXout = 0; MYout = 0; MZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];

                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float Xratio = MXout / PXout;
                    float Yratio = MYout / PYout;
                    float Zratio = MZout / PZout;



                    if ((PXout< MXout/4 & PYout < MYout / 4 & PZout < MZout / 3) | (PXout<.075 & PYout<.075 & PZout<.25))
                    {
                        settled_flag = true;
                        log_res = string.Format("Break out ");
                        clog.Log(clog.Level.Fatal, log_res);
                        break;
                    }
                      /*      
                   float dlimit = 0.025f;

                    //Debug.WriteLine(Dout);
                    //Debug.WriteLine(Mout);
                    if (Dout < dlimit | (Dout < Mout / 5 & Mout<1))
                    {
                        settled_flag = true;
                        log_res = string.Format("Break out ");
                        CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                        break;
                    }
                    /*      
                 float dlimit = 0.025f;

                  //Debug.WriteLine(Dout);
                  //Debug.WriteLine(Mout);
                  if (Dout < dlimit | (Dout < Mout / 5 & Mout<1))
                  {
                      // bool breakflag = true;
                      break;
                  }
                  */
                }
                //end break out criteria

                if ((ZoutPos[loop_count] > (9 - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (1 + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?

                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE != 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            RANGE = 3;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakTx_Raster(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);


            string log_res;
            int[] LG = new int[] { 1800, 1000, 700, 400, 150 };
            int[] LG_Z = new int[] { 5000, 2500, 1750, 1000, 500 };
            float[] CD = new float[] { 1.75f, .6f, .5f, .4f, .1f };
            float[] CD_Z = new float[] { 3.5f, 3f, 2.5f, 2f, .3f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            float FINE_TUNE_LEVEL = +5f;
            float JXout = 0f, JYout = 0f, JZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            // Logging at the end of each alignment loop for debug 
            //string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);
            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;



            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");
                    OutputRaster(40, 5, -45);
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(3);
                    axNanoOutput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXout = 0; JYout = 0; JZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXout = JXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        JYout = JYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        JZout = JZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];
                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float XJitterMoveratio = Math.Abs(JXout / PXout);
                    float YJitterMoveratio = Math.Abs(JYout / PYout);
                    float ZJitterMoveratio = Math.Abs(JZout / PZout);
                    float XYJitterMoveRatioLimit = 6f;
                    float ZJitterMoveRatioLimit = 4f;
                    float XYJitterLimit = 1.5f;
                    float ZJitterLimit = 1.5f;
                    float XYMoveLimit = 0.4f;
                    float ZMoveLimit = 0.4f;
                    bool breakout = false;
                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXout < XYJitterLimit & JYout < XYJitterLimit & JZout < ZJitterLimit));
                    bool cond2 = (PXout < XYMoveLimit) & (PYout < XYMoveLimit) & (PZout < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXout, JYout, JZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXout, PYout, PZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        break;
                    }
                }
                //end break out criteria
                float recenter_border = 1.5f;
                if ((ZoutPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XoutPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YoutPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE < 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                {
                    RANGE = 3; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 3 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else
                {
                    //DONT SET RANGE
                }
                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }
            RANGE = 4;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoOutput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakTx_Raster_SOA_NF(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        // This is identical to FindPeakTx_Raster, with the sole exception that 2006 axNanoZ.SetCircHomePos(5, 5); and
        // 2007 axNanoZ.MoveCircHome(); are commented out.
        // -- Srinivasan "Cheenu" Sethuraman, 13 October 2022
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);


            string log_res;
            int[] LG = new int[] { 1800, 1000, 700, 400, 150 };
            int[] LG_Z = new int[] { 5000, 2500, 1750, 1000, 500 };
            float[] CD = new float[] { 1.75f, .6f, .5f, .4f, .1f };
            float[] CD_Z = new float[] { 3.5f, 3f, 2.5f, 2f, .3f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            float FINE_TUNE_LEVEL = +5f;
            float JXout = 0f, JYout = 0f, JZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            //axNanoZ.SetCircHomePos(5, 5);
            //axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1); // 1 is PIN TIA, 5 is "10V BNC Input" -- S. Sethuraman, 17 Oct. 2022
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            // Logging at the end of each alignment loop for debug 
            //string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);
            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;



            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");
                    OutputRaster(40, 5, -45);
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(3);
                    axNanoOutput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXout = 0; JYout = 0; JZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXout = JXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        JYout = JYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        JZout = JZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];
                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float XJitterMoveratio = Math.Abs(JXout / PXout);
                    float YJitterMoveratio = Math.Abs(JYout / PYout);
                    float ZJitterMoveratio = Math.Abs(JZout / PZout);
                    float XYJitterMoveRatioLimit = 6f;
                    float ZJitterMoveRatioLimit = 4f;
                    float XYJitterLimit = 1.5f;
                    float ZJitterLimit = 1.5f;
                    float XYMoveLimit = 0.4f;
                    float ZMoveLimit = 0.4f;
                    bool breakout = false;
                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXout < XYJitterLimit & JYout < XYJitterLimit & JZout < ZJitterLimit));
                    bool cond2 = (PXout < XYMoveLimit) & (PYout < XYMoveLimit) & (PZout < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXout, JYout, JZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXout, PYout, PZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        break;
                    }
                }
                //end break out criteria
                float recenter_border = 1.5f;
                if ((ZoutPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XoutPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YoutPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE < 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                {
                    RANGE = 3; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 3 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else
                {
                    //DONT SET RANGE
                }
                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }
            RANGE = 4;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoOutput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        /// <summary>
        /// Multimode alignment routine.  Similar to the "raster" single mode routine except that a) the X/Y circle diameters are quadrupled
        /// and b) A minimum time in the starting range is enforced
        /// </summary>
        /// <param name="Threshold">Threshold for what is considered a good alignment, in dBm</param>
        /// <param name="myCoordinates">reference to found alignment coordinates</param>
        /// <param name="bSave">True if the coordinates found during a good alignment are to be saved to the memory file, false otherwise</param>
        public void FindPeakTx_Multimode(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            string log_res;

          //int[] LG = new int[] { 1800, 1000, 700, 400, 150 };
            int[] LG = new int[] { 1800, 1200, 700, 400, 150 };
          //int[] LG_Z = new int[] { 5000, 2500, 1750, 1000, 500 };
            int[] LG_Z = new int[] { 5000, 3000, 1750, 1000, 500 };
          //float[] CD = new float[] { 4.5f, 3.6f, 3.0f, 3.0f, .4f };
            float[] CD = new float[] { 4.5f, 4.5f, 3.5f, 3.0f, .4f };
            //float[] CD_Z = new float[] { 4.5f, 4f, 3.5f, 2.5f, .6f };
            float[] CD_Z = new float[] { 4.5f, 4.5f, 3.5f, 2.5f, .6f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 1;

            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            float FINE_TUNE_LEVEL = +5f;
            float JXout = 0f, JYout = 0f, JZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;
            float min_initial_search_time = 0.7f;   // Minimum time before range can be changed from the initial value.  Helps keep the motors from getting stuck in a local maximum

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;

            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {
                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");
                    OutputRaster(40, 5, -45);
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(3);
                    axNanoOutput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXout = 0; JYout = 0; JZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXout = JXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        JYout = JYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        JZout = JZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];
                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float XJitterMoveratio = Math.Abs(JXout / PXout);
                    float YJitterMoveratio = Math.Abs(JYout / PYout);
                    float ZJitterMoveratio = Math.Abs(JZout / PZout);
                    float XYJitterMoveRatioLimit = 6f;
                    float ZJitterMoveRatioLimit = 4f;
                    float XYJitterLimit = 1.5f;
                    float ZJitterLimit = 1.5f;
                    float XYMoveLimit = 0.4f;
                    float ZMoveLimit = 0.4f;
                    bool breakout = false;
                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXout < XYJitterLimit & JYout < XYJitterLimit & JZout < ZJitterLimit));
                    bool cond2 = (PXout < XYMoveLimit) & (PYout < XYMoveLimit) & (PZout < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXout, JYout, JZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXout, PYout, PZout);
                            clog.Log(clog.Level.Fatal, log_res);
                        }
                        break;
                    }
                }
                //end break out criteria
                float recenter_border = 1.5f;
                if ((ZoutPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XoutPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YoutPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check

                /* Logic for changing the circle size */

                // Allow motors to freely go to a lower range
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL && RANGE != 0 && ts.TotalSeconds > 0.2f)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }

                // Keep motors from freely going to a higher range until after a certain amount of time
                if (ts.TotalSeconds > min_initial_search_time)
                {
                    if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                    {
                        RANGE = 1; RESIZE = true;
                        log_res = string.Format("To Range 1 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else if (opt_power1 >= Threshold & RANGE < 2)
                    {
                        RANGE = 2; RESIZE = true;
                        last_range_2_index = loop_count;
                        log_res = string.Format("To Range 2 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                    {
                        RANGE = 3; RESIZE = true;
                        last_range_2_index = loop_count;
                        log_res = string.Format("To Range 3 ");
                        clog.Log(clog.Level.Fatal, log_res);
                    }
                    else
                    {
                        //DONT SET RANGE
                    }
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }
            RANGE = 3;// 4;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            Thread.Sleep(500);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoOutput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakTxFeedbackToleranceMultiMode(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {
            clog.Log(clog.Level.Fatal, "POR TX Alignment");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            //int[] LG = new int[] { 1200, 500, 250, 150 };
            //int[] LG_Z = new int[] { 5000, 2000, 1000, 500 };
            //float[] CD = new float[] { 1.75f, .6f, .35f, .1f };
            float[] CD_Z = new float[] { 5.0f, 5.0f, 3.0f, 3.0f, 3.0f }; 

            int[] LG = new int[] { 1000, 500, 500, 500, 500 }; 
            int[] LG_Z = new int[] { 5000, 2000, 1200, 1000, 1000 }; 
            float[] CD = new float[] { 5.00f, 3.5f, 3.0f, 1.0f, .35f, 0.1f}; 
            int[] sleep_time = new int[] { 1000, 1000,1000, 1000, 1000,1000};

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float MXout = 0f, MYout = 0f, MZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0; 

            bool RESIZE = true;
            float max_search_time = 20.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            
            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.Latch();
                //axNanoZ.TrackEx(3);
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            for (RANGE=0; RANGE<LG.Count(); RANGE++)
            {
                if (ts.TotalSeconds >= max_search_time)
                {
                    break;
                }

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                //// test break out criteria
                //int BreakCheckSize = 20;

                //if (RANGE == 2 & loop_count > (BreakCheckSize - 1) && (last_range_2_index <= loop_count - 10) && opt_power1 > Threshold)
                //{
                //    //sum of all jitters recently
                //    MXout = 0; MYout = 0; MZout = 0;
                //    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                //    {
                //        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                //        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                //        MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                //    }
                //}

                if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || 
                    XoutPos[loop_count] < (1 + CD[RANGE] / 2) || 
                    YoutPos[loop_count] > (9 - CD[RANGE] / 2) || 
                    YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    continue;
                }

                // Walk through ranges
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                Thread.Sleep(sleep_time[RANGE]);
                ts = DateTime.Now - startTime;
            }

            axNanoZ.TrackEx(3);
            Thread.Sleep(1000);

            axNanoZ.Latch();
            Thread.Sleep(200);
            axNanoOutput.Latch();
            axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
            Thread.Sleep(200);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakTxFeedbackToleranceSingleMode(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool z_recenter_enable = true)
        {
            clog.Log(clog.Level.Fatal, "POR TX Alignment");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            //int[] LG = new int[] { 1200, 500, 250, 150 };
            //int[] LG_Z = new int[] { 5000, 2000, 1000, 500 };
            //float[] CD = new float[] { 1.75f, .6f, .35f, .1f };
            float[] CD_Z = new float[] { 3.5f, 2f, 1.5f, .3f }; 

            int[] LG = new int[] { 1200, 500, 350, 100 }; // MATLAB POR
            int[] LG_Z = new int[] { 5000, 2000, 1200, 500 }; // MATLAB POR
            float[] CD = new float[] { 1.75f, .6f, .35f, .035f }; // MATLAB POR

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            // int RANGE = 0; MATLAB POR
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            //float RANGE_CHANGE_LEVEL = -10; MATLAB POR
            float MXout = 0f, MYout = 0f, MZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0; 

            bool RESIZE = true;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            
            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;

            //while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
                while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) && (last_range_2_index <= loop_count - 10) && opt_power1 > Threshold)
//if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold)
                {
                    //sum of all jitters recently
                    MXout = 0; MYout = 0; MZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];

                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float Xratio = MXout / PXout;
                    float Yratio = MYout / PYout;
                    float Zratio = MZout / PZout;



                    if ((PXout< MXout/4 & PYout < MYout / 4 & PZout < MZout / 3) | (PXout<.075 & PYout<.075 & PZout<.25))
                    {
                        settled_flag = true;
                        log_res = string.Format("Break out ");
                        clog.Log(clog.Level.Fatal, log_res);
                        break;
                    }
                      /*      
                   float dlimit = 0.025f;

                    //Debug.WriteLine(Dout);
                    //Debug.WriteLine(Mout);
                    if (Dout < dlimit | (Dout < Mout / 5 & Mout<1))
                    {
                        settled_flag = true;
                        log_res = string.Format("Break out ");
                        CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                        break;
                    }
                    /*      
                 float dlimit = 0.025f;

                  //Debug.WriteLine(Dout);
                  //Debug.WriteLine(Mout);
                  if (Dout < dlimit | (Dout < Mout / 5 & Mout<1))
                  {
                      // bool breakflag = true;
                      break;
                  }
                  */
                }
                //end break out criteria

                if (z_recenter_enable)
                {
                    if ((ZoutPos[loop_count] > (9 - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (1 + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                    {
                        ReCenter("Z");
                        startTime = DateTime.Now;
                    }
                }
                if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?

                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE != 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            RANGE = 3;
            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        /// <param name="Threshold">Threshold, likely in volts if using this algorithm, and likely in logarithmic units</param>
        /// <param name="bBigRaster">false = 50um raster scan with 5um steps, true = 200um raster scan with 10um steps</param>
        public void FindPeak_VIT_INPUT(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bBigRaster = false)
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);



            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 2000, 1500, 500, 200, 200 };
            int[] LG_Z = new int[] { 5000, 3000, 500, 200, 200 };

            float[] CD = new float[]  { 3.5f, 2.5f, 1.0f, 0.5f, 0.5f };
            float[] CD_Z = new float[] { 3.5f, 2.5f,   1.0f, 0.5f, 0.5f };


            string log_res;
            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -0.4f;
            float FINE_TUNE_LEVEL = +0.2f;
            float JXout = 0f, JYout = 0f, JZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 10.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            axNanoOutput.SetInputSrc(5);

            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            // Logging at the end of each alignment loop for debug 
            //string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);
            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_HORZ);    // Track input Z motor
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;



            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");

                    if (false == bBigRaster) OutputRaster(50, 5, -999);
                    else OutputRaster(400, 10, Threshold);
                    
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(3);
                    axNanoOutput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXout = 0; JYout = 0; JZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXout = JXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        JYout = JYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        JZout = JZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];

                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();


                    float XJitterMoveratio = Math.Abs(JXout / PXout);
                    float YJitterMoveratio = Math.Abs(JYout / PYout);
                    float ZJitterMoveratio = Math.Abs(JZout / PZout);

                    //Debug.WriteLine(string.Format("{0},{1}", loop_count, range));
                    //Debug.WriteLine(string.Format("{6}--{0},{1},{2}--{3},{4},{5}--{7},{8},{9}", MXout, JYout, MZout, PXout, PYout, PZout, opt_power1, XJitterMoveratio, YJitterMoveratio, ZJitterMoveratio));


                    float XYJitterMoveRatioLimit = 3f;
                    float ZJitterMoveRatioLimit = 2f;

                    float XYJitterLimit = 1.0f;
                    float ZJitterLimit = 1.0f;

                    float XYMoveLimit = 0.2f;
                    float ZMoveLimit = 0.2f;

                    bool breakout = false;

                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXout < XYJitterLimit & JYout < XYJitterLimit & JZout < ZJitterLimit));
                    bool cond2 = (PXout < XYMoveLimit) & (PYout < XYMoveLimit) & (PZout < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXout, JYout, JZout);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXout, PYout, PZout);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        break;
                    }

                }
                //end break out criteria
                float recenter_border = 1.5f;

                if ((ZoutPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XoutPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YoutPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE < 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                {
                    RANGE = 3; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 3 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }

                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            RANGE = 4;

            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoOutput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        //Same as Findpeak_VIT_INPUT but use input otors instead of output
        public void FindPeak_VIT_INPUT_V2(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bBigRaster = false)
        {
            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 200;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);



            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 500, 500, 500, 200, 200 };
            int[] LG_Z = new int[] { 500, 500, 500, 200, 200 };

            float[] CD = new float[]  { 3.5f, 2.5f, 1.0f, 0.5f, 0.5f };
            float[] CD_Z = new float[] { 3.5f, 2.5f,   1.0f, 0.5f, 0.5f };


            string log_res;
            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -0.4f;
            float FINE_TUNE_LEVEL = +0.2f;
            float JXout = 0f, JYout = 0f, JZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 90.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            axNanoInput.SetInputSrc(5);

            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoInput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            // Logging at the end of each alignment loop for debug 
            //string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);
            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);

                //axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_VERT);    // Track input Z motor
                if (RANGE > 2)
                {
                    axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_HORZ);    // Track input Z motor
                }
                axNanoInput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;



            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            //while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag)))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");

                    if (false == bBigRaster)
                    {
                        InputRaster(50, 5, Threshold);
                    }
                    else
                    {
                        //InputRaster(200, 5, Threshold);
                        CArrayData temp = null;
                        InputSpiralRaster(625, Threshold, ref temp);
                    }
                    
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    //axNanoZ.TrackEx(3);
                    //axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_HORZ);    // Track input Z motor
                    axNanoInput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoInput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXout = 0; JYout = 0; JZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXout = JXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        JYout = JYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        JZout = JZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];

                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();


                    float XJitterMoveratio = Math.Abs(JXout / PXout);
                    float YJitterMoveratio = Math.Abs(JYout / PYout);
                    float ZJitterMoveratio = Math.Abs(JZout / PZout);

                    //Debug.WriteLine(string.Format("{0},{1}", loop_count, range));
                    //Debug.WriteLine(string.Format("{6}--{0},{1},{2}--{3},{4},{5}--{7},{8},{9}", MXout, JYout, MZout, PXout, PYout, PZout, opt_power1, XJitterMoveratio, YJitterMoveratio, ZJitterMoveratio));


                    float XYJitterMoveRatioLimit = 3f;
                    float ZJitterMoveRatioLimit = 2f;

                    float XYJitterLimit = 1.0f;
                    float ZJitterLimit = 1.0f;

                    float XYMoveLimit = 0.1f;
                    float ZMoveLimit = 0.1f;

                    bool breakout = false;

                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXout < XYJitterLimit & JYout < XYJitterLimit & JZout < ZJitterLimit));
                    bool cond2 = (PXout < XYMoveLimit) & (PYout < XYMoveLimit) & (PZout < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXout, JYout, JZout);
                            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXout, PYout, PZout);
                            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        break;
                    }

                }
                //end break out criteria
                float recenter_border = 1.5f;

                if ((ZoutPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XoutPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YoutPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("IN");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                }
                else if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE < 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                {
                    RANGE = 3; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 3 ");
                    //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);
                }
                else
                {
                    //DONT SET RANGE
                }

                if (RANGE > 2)
                {
                    axNanoZ.TrackEx((int)MG17NanoTrakLib.TRAKMODESET.TRAK_HORZ);    // Track input Z motor
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoInput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoInput.SetCircDia(CD[RANGE]);
                    Thread.Sleep(500);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            RANGE = 4;

            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoInput.SetLoopGain(LG[RANGE]);
            rslt = axNanoInput.SetCircDia(CD[RANGE]);

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoInput.Latch();
            axNanoInput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(true, false, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        /// <summary>Same as FindPeak_VIT_INPUT but with the input motor instead of the output</summary>
        /// <param name="Threshold">Threshold, likely in volts if using this algorithm, and likely in logarithmic units</param>
        /// <param name="bBigRaster">false = 50um raster scan with 5um steps, true = 200um raster scan with 10um steps</param>
        public void FindPeak_VIT_INPUT_INPUTMOTOR(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bBigRaster = false)
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 2000, 1500, 500, 200, 200 };
            int[] LG_Z = new int[] { 5000, 3000, 500, 200, 200 };

            float[] CD = new float[] { 3.5f, 2.5f, 1.0f, 0.5f, 0.5f };
            float[] CD_Z = new float[] { 3.5f, 2.5f, 1.0f, 0.5f, 0.5f };

            string log_res;
            float[] ZinPos = new float[array_size];
            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -0.4f;
            float FINE_TUNE_LEVEL = +0.2f;
            float JXin = 0f, JYin = 0f, JZin = 0f;
            float PXin = 0f, PYin = 0f, PZin = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 10.0f;
            float min_search_time = 1.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            axNanoInput.SetInputSrc(5);

            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);

            float garbage = 0;
            axNanoZ.GetCircPosReading(ref ZinPos[0], ref garbage, ref opt_power1, ref range, ref rel, ref overunder);
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoInput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;

            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");

                    if (false == bBigRaster) InputRaster(50, 5, -999);
                    else InputRaster(400, 10, Threshold);

                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(3);
                    axNanoInput.Track();
                }

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXin = 0; JYin = 0; JZin = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXin = JXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        JYin = JYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                        JZin = JZin + Math.Abs(ZinPos[i] - ZinPos[i - 1]);
                    }

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XinPosRecent = new float[BreakCheckSize];
                    float[] YinPosRecent = new float[BreakCheckSize];
                    float[] ZinPosRecent = new float[BreakCheckSize];

                    Array.Copy(XinPos, loop_count - BreakCheckSize, XinPosRecent, 0, BreakCheckSize);
                    PXin = XinPosRecent.Max() - XinPosRecent.Min();
                    Array.Copy(YinPos, loop_count - BreakCheckSize, YinPosRecent, 0, BreakCheckSize);
                    PYin = YinPosRecent.Max() - YinPosRecent.Min();
                    Array.Copy(ZinPos, loop_count - BreakCheckSize, ZinPosRecent, 0, BreakCheckSize);
                    PZin = ZinPosRecent.Max() - ZinPosRecent.Min();


                    float XJitterMoveratio = Math.Abs(JXin / PXin);
                    float YJitterMoveratio = Math.Abs(JYin / PYin);
                    float ZJitterMoveratio = Math.Abs(JZin / PZin);

                    float XYJitterMoveRatioLimit = 3f;
                    float ZJitterMoveRatioLimit = 2f;

                    float XYJitterLimit = 1.0f;
                    float ZJitterLimit = 1.0f;

                    float XYMoveLimit = 0.2f;
                    float ZMoveLimit = 0.2f;

                    bool breakout = false;

                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXin < XYJitterLimit & JYin < XYJitterLimit & JZin < ZJitterLimit));
                    bool cond2 = (PXin < XYMoveLimit) & (PYin < XYMoveLimit) & (PZin < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXin, JYin, JZin);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXin:{0}, PYin:{1}, PZin:{2}", PXin, PYin, PZin);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        break;
                    }

                }
                //end break out criteria
                float recenter_border = 1.5f;

                if ((ZinPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZinPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XinPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YinPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > -45)
                {
                    ReCenter("IN");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE < 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                {
                    RANGE = 3; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 3 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }

                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoInput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoInput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            RANGE = 4;

            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoInput.SetLoopGain(LG[RANGE]);
            rslt = axNanoInput.SetCircDia(CD[RANGE]);

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoInput.Latch();
            axNanoInput.GetCircPosReading(ref ZinPos[loop_count], ref garbage, ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(true, false, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }


        public void FindPeak_VIT_INPUT_LIDAR(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bLowPassFilter = false)
        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);



            double[,] opt_power = new double[1, 10];
            //int[] LG = new int[] { 2000, 1500, 500, 200, 200 };
            //int[] LG_Z = new int[] { 5000, 3000, 500, 200, 200 };

            //float[] CD = new float[]  { 3.5f, 2.5f, 1.0f, 0.5f, 0.5f };
            //float[] CD_Z = new float[] { 3.5f, 2.5f,   1.0f, 0.5f, 0.5f };
            //From TX
            int[] LG = new int[] { 1800, 1000, 700, 400, 150 };
            int[] LG_Z = new int[] { 5000, 2500, 1750, 1000, 500 };
            //float[] CD = new float[] { 3.5f, .6f, .5f, .4f, .1f };//initial values
            float[] CD = new float[] { 3.0f, 0.6f, .5f, .4f, .1f };
            float[] CD_Z = new float[] { 3.5f, 3f, 2.5f, 2f, .3f };

            string log_res;
            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            float rel = 0f;
            int overunder = 0;
            //float RANGE_CHANGE_LEVEL = -0.4f; //origional
            float RANGE_CHANGE_LEVEL = -0.5f;
            float FINE_TUNE_LEVEL = +0.2f;
            float JXout = 0f, JYout = 0f, JZout = 0f;
            float PXout = 0f, PYout = 0f, PZout = 0f;

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 15.0f;
            float min_search_time = 2.0f;

            int rslt = 0;
            int loop_count = 0;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            axNanoOutput.SetInputSrc(5);

            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            // Logging at the end of each alignment loop for debug 
            //string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);
            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);

                axNanoZ.TrackEx(3);
                axNanoOutput.Track();
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;



            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");
                    OutputRaster(50, 5, .2f);
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    axNanoZ.TrackEx(3);
                    axNanoOutput.Track();
                }


                //// Raster scan at 1/4 of the array, to make sure it get closed to first light
                //if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                //{
                //    CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Warn, "No light, raster scan");
                //    Debug.WriteLine("no light, raster scan");
                //    OutputRaster(40, 5, -45);
                //    startTime = DateTime.Now;//Reset the clock for min and max timeout
                //    axNanoZ.TrackEx(3);
                //    axNanoOutput.Track();
                //}

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXout = 0; JYout = 0; JZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXout = JXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        JYout = JYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        JZout = JZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];

                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();


                    float XJitterMoveratio = Math.Abs(JXout / PXout);
                    float YJitterMoveratio = Math.Abs(JYout / PYout);
                    float ZJitterMoveratio = Math.Abs(JZout / PZout);

                    //Debug.WriteLine(string.Format("{0},{1}", loop_count, range));
                    //Debug.WriteLine(string.Format("{6}--{0},{1},{2}--{3},{4},{5}--{7},{8},{9}", MXout, JYout, MZout, PXout, PYout, PZout, opt_power1, XJitterMoveratio, YJitterMoveratio, ZJitterMoveratio));


                    float XYJitterMoveRatioLimit = 6f;
                    float ZJitterMoveRatioLimit = 4f;

                    float XYJitterLimit = 1.5f;
                    float ZJitterLimit = 1.5f;

                    float XYMoveLimit = 0.4f;
                    float ZMoveLimit = 0.4f;

                    bool breakout = false;

                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXout < XYJitterLimit & JYout < XYJitterLimit & JZout < ZJitterLimit));
                    bool cond2 = (PXout < XYMoveLimit) & (PYout < XYMoveLimit) & (PZout < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXout, JYout, JZout);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        if (cond2)
                        {
                            log_res = string.Format("Break out condition 2 satisfied: \nPXout:{0}, PYOut:{1}, PZout:{2}", PXout, PYout, PZout);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        break;
                    }

                }
                //end break out criteria
                float recenter_border = 1.5f;

                if ((ZoutPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZoutPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > 0.28)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                if ((XoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XoutPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YoutPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YoutPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > 0.28)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE < 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                {
                    RANGE = 3; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 3 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }

                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            RANGE = 4;

            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            rslt = axNanoOutput.SetCircDia(CD[RANGE]);

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoOutput.Latch();
            axNanoOutput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeak_VIT_OE_InputMotor(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bLowPassFilter = false)
        // This is Chris Seibert's FindPeak_VIT_INPUT_LIDAR, but it operates on the input fiber aligner. Chris's
        // method operates on the output fiber aligner, which makes it really confusingly named.
        // This just takes every reference to "output" and changes it to "input".
        // -- Srinivasan "Cheenu" Sethuraman, 02 Oct. 2021

        {
            clog.Log(clog.Level.Fatal, "Raster Enabled Alignment with new breakout Algo");
            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());
            waitForMotorStatus(axMotorInput, 0); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
            waitForMotorStatus(axMotorInput, 1); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021

            double[,] opt_power = new double[1, 10];
            //int[] LG = new int[] { 2000, 1500, 500, 200, 200 };
            //int[] LG_Z = new int[] { 5000, 3000, 500, 200, 200 };

            //float[] CD = new float[]  { 3.5f, 2.5f, 1.0f, 0.5f, 0.5f };
            //float[] CD_Z = new float[] { 3.5f, 2.5f,   1.0f, 0.5f, 0.5f };
            //From TX
            int[] LG = new int[] { 1800, 1000, 700, 400, 150 };
            int[] LG_Z = new int[] { 5000, 2500, 1750, 1000, 500 };
            //float[] CD = new float[] { 3.5f, .6f, .5f, .4f, .1f };//initial values
            float[] CD = new float[] { 3.0f, 0.6f, .5f, .4f, .1f };
            float[] CD_Z = new float[] { 3.5f, 3f, 2.5f, 2f, .3f };

            string log_res;
            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float[] XinPos = new float[array_size]; // New for this method -- S. Sethuraman, 02 Oct. 2021
            float[] YinPos = new float[array_size]; // New for this method -- S. Sethuraman, 02 Oct. 2021
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 2;
            float rel = 0f;
            int overunder = 0;
            //float RANGE_CHANGE_LEVEL = -0.4f; //origional
            float RANGE_CHANGE_LEVEL = -0.5f;
            float FINE_TUNE_LEVEL = +0.2f;
            float JXin = 0f, JYin = 0f, JZin = 0f; // New for this method -- S. Sethuraman, 02 Oct. 2021
            float PXin = 0f, PYin = 0f, PZin = 0f; // New for this method -- S. Sethuraman, 02 Oct. 2021

            // Index at which RANGE = 2 was last achieved.  Used to enforce a minimum amount of time in RANGE = 2,
            // which is the smallest range before latching
            int last_range_2_index = 0;

            bool RESIZE = true;
            float max_search_time = 15.0f;
            float min_search_time = 2.0f;

            int rslt = 0;
            int loop_count = 0;

            // axNanoZ.SetCircHomePos(5, 5);
            // axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
            axNanoInput.MoveCircHome();       // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021

            axNanoZ.SetInputSrc(5);
            axNanoInput.SetInputSrc(5);       // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021

            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021

            axNanoZ.GetCircPosReading(ref ZinPos[0], ref ZoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            // For following line, changed Output to Input and Xout/Yout to Xin/Yin -- S. Sethuraman, 02 Oct. 2021
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);

            // Logging at the end of each alignment loop for debug 
            //string log_res = string.Format("PX,PY,PZ,Power,Count:{0},{1},{2},{3},{4}", XoutPos[loop_count], YoutPos[loop_count], ZoutPos[loop_count], opt_power1, loop_count);
            //CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Fatal, log_res);

            if (RESIZE)
            {
                RESIZE = false;
                axNanoZ.SetLoopGain(LG_Z[RANGE]);
                rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                axNanoInput.SetLoopGain(LG[RANGE]); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
                rslt = axNanoInput.SetCircDia(CD[RANGE]); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021

                //axNanoZ.TrackEx(3);
                axNanoZ.TrackEx(2);
                axNanoInput.Track();  // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
            }

            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            bool settled_flag = false;



            while (ts.TotalSeconds < min_search_time | ((RANGE != 2 | false == settled_flag) & ts.TotalSeconds <= max_search_time))
            {

                //Pauses the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                {
                    clog.Log(clog.Level.Warn, "No light, raster scan");
                    Debug.WriteLine("no light, raster scan");
                    InputRaster(50, 5, .2f);  // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
                    startTime = DateTime.Now;//Reset the clock for min and max timeout
                    //axNanoZ.TrackEx(3);
                    axNanoZ.TrackEx(2);
                    axNanoInput.Track(); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
                }


                //// Raster scan at 1/4 of the array, to make sure it get closed to first light
                //if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                //{
                //    CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Warn, "No light, raster scan");
                //    Debug.WriteLine("no light, raster scan");
                //    OutputRaster(40, 5, -45);
                //    startTime = DateTime.Now;//Reset the clock for min and max timeout
                //    axNanoZ.TrackEx(3);
                //    axNanoOutput.Track();
                //}

                //recenter?
                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                // For following line, changed Output to Input and Xout/Yout to Xin/Yin -- S. Sethuraman, 02 Oct. 2021
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                // test break out criteria
                int BreakCheckSize = 20;

                if ((RANGE >= 2) && (loop_count > (BreakCheckSize - 1)) && (last_range_2_index <= loop_count - 10) && (opt_power1 > Threshold))
                {
                    //sum of all jitters recently
                    JXin = 0; JYin = 0; JZin = 0;  // Changed to Xin/Yin/Zin for this method -- S. Sethuraman, 02 Oct. 2021
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        JXin = JXin + Math.Abs(XinPos[i] - XinPos[i - 1]); // Changed to Xin for this method -- S. Sethuraman, 02 Oct. 2021
                        JYin = JYin + Math.Abs(YinPos[i] - YinPos[i - 1]); // Changed to Yin for this method -- S. Sethuraman, 02 Oct. 2021
                        JZin = JZin + Math.Abs(ZinPos[i] - ZinPos[i - 1]); // Changed to Zin for this method -- S. Sethuraman, 02 Oct. 2021
                    }

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XinPosRecent = new float[BreakCheckSize]; // Changed to Xin for this method -- S. Sethuraman, 02 Oct. 2021
                    float[] YinPosRecent = new float[BreakCheckSize]; // Changed to Yin for this method -- S. Sethuraman, 02 Oct. 2021
                    float[] ZinPosRecent = new float[BreakCheckSize]; // Changed to Zin for this method -- S. Sethuraman, 02 Oct. 2021

                    // In following 6 lines, Xout/Yout/Zout changed to Xin/Yin/Zin -- S. Sethuraman, 02 Oct. 2021
                    Array.Copy(XinPos, loop_count - BreakCheckSize, XinPosRecent, 0, BreakCheckSize);
                    PXin = XinPosRecent.Max() - XinPosRecent.Min();
                    Array.Copy(YinPos, loop_count - BreakCheckSize, YinPosRecent, 0, BreakCheckSize);
                    PYin = YinPosRecent.Max() - YinPosRecent.Min();
                    Array.Copy(ZinPos, loop_count - BreakCheckSize, ZinPosRecent, 0, BreakCheckSize);
                    PZin = ZinPosRecent.Max() - ZinPosRecent.Min();


                    float XJitterMoveratio = Math.Abs(JXin / PXin); // Changed to Xin for this method -- S. Sethuraman, 02 Oct. 2021
                    float YJitterMoveratio = Math.Abs(JYin / PYin); // Changed to Yin for this method -- S. Sethuraman, 02 Oct. 2021
                    float ZJitterMoveratio = Math.Abs(JZin / PZin); // Changed to Zin for this method -- S. Sethuraman, 02 Oct. 2021

                    //Debug.WriteLine(string.Format("{0},{1}", loop_count, range));
                    //Debug.WriteLine(string.Format("{6}--{0},{1},{2}--{3},{4},{5}--{7},{8},{9}", MXout, JYout, MZout, PXout, PYout, PZout, opt_power1, XJitterMoveratio, YJitterMoveratio, ZJitterMoveratio));


                    float XYJitterMoveRatioLimit = 6f;
                    float ZJitterMoveRatioLimit = 4f;

                    float XYJitterLimit = 1.5f;
                    float ZJitterLimit = 1.5f;

                    float XYMoveLimit = 0.4f;
                    float ZMoveLimit = 0.4f;

                    bool breakout = false;

                    // In following 2 lines, Xout/Yout/Zout changed to Xin/Yin/Zin -- S. Sethuraman, 02 Oct. 2021
                    bool cond1 = ((XYJitterMoveRatioLimit < XJitterMoveratio & XYJitterMoveRatioLimit < YJitterMoveratio & ZJitterMoveRatioLimit < ZJitterMoveratio) & (JXin < XYJitterLimit & JYin < XYJitterLimit & JZin < ZJitterLimit));
                    bool cond2 = (PXin < XYMoveLimit) & (PYin < XYMoveLimit) & (PZin < ZMoveLimit);
                    breakout = cond1 | cond2;

                    // Break out 
                    if (breakout)
                    {
                        settled_flag = true;
                        if (cond1)
                        {
                            // In following line, Xout/Yout/Zout changed to Xin/Yin/Zin -- S. Sethuraman, 02 Oct. 2021
                            log_res = string.Format("Break out condition 1 satisfied: \nXJMRatio:{0}, YJMRatio:{1}, ZJMRatio:{2}\nXJitter:{3}, YJitter:{4}, ZJitter:{5}", XJitterMoveratio, YJitterMoveratio, ZJitterLimit, JXin, JYin, JZin);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        if (cond2)
                        {
                            // In following line, Xout/Yout/Zout changed to Xin/Yin/Zin -- S. Sethuraman, 02 Oct. 2021
                            log_res = string.Format("Break out condition 2 satisfied: \nPXin:{0}, PYin:{1}, PZin:{2}", PXin, PYin, PZin);
                            clog.Log(clog.Level.Fatal, log_res);
                            Debug.WriteLine(log_res);
                        }
                        break;
                    }

                }
                //end break out criteria
                float recenter_border = 1.5f;

                // In following line, Zout changed to Zin -- S. Sethuraman, 02 Oct. 2021
                if ((ZinPos[loop_count] > (10 - recenter_border - CD_Z[RANGE] / 2) || ZinPos[loop_count] < (recenter_border + CD_Z[RANGE] / 2)) && opt_power1 > 0.28)
                {
                    ReCenter("Z");
                    startTime = DateTime.Now;
                }
                // In following line, Xout/Yout changed to Xin/Yin -- S. Sethuraman, 02 Oct. 2021
                if ((XinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || XinPos[loop_count] < (recenter_border + CD[RANGE] / 2) || YinPos[loop_count] > (10 - recenter_border - CD[RANGE] / 2) || YinPos[loop_count] < (recenter_border + CD[RANGE] / 2)) && opt_power1 > 0.28)
                {
                    ReCenter("IN"); // "OUT" changed to "IN" -- S. Sethuraman, 02 Oct. 2021
                    startTime = DateTime.Now;
                }
                //end recenter check


                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                    log_res = string.Format("To Range 0 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if ((opt_power1 >= Threshold + RANGE_CHANGE_LEVEL) && (opt_power1 < Threshold & RANGE != 1))
                {
                    RANGE = 1; RESIZE = true;
                    log_res = string.Format("To Range 1 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold & RANGE < 2)
                {
                    RANGE = 2; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 2 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }
                else if (opt_power1 >= Threshold + FINE_TUNE_LEVEL & RANGE < 3)
                {
                    RANGE = 3; RESIZE = true;
                    last_range_2_index = loop_count;
                    log_res = string.Format("To Range 3 ");
                    clog.Log(clog.Level.Fatal, log_res);
                }

                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(LG_Z[RANGE]);
                    rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
                    axNanoInput.SetLoopGain(LG[RANGE]); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
                    rslt = axNanoInput.SetCircDia(CD[RANGE]); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
                }
                ts = DateTime.Now - startTime;
                //end range/circle size check
            }

            RANGE = 4;

            axNanoZ.SetLoopGain(LG_Z[RANGE]);
            rslt = axNanoZ.SetCircDia(CD_Z[RANGE]);
            axNanoInput.SetLoopGain(LG[RANGE]); // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
            rslt = axNanoInput.SetCircDia(CD[RANGE]);  // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoInput.Latch();  // Changed to Input for this method -- S. Sethuraman, 02 Oct. 2021
            // In following line, Output changed to Input -- S. Sethuraman, 02 Oct. 2021
            axNanoInput.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            StoreMotorCoords(true, false, ref myCoordinates, bSave); // Bugfix, 11 Oct. 2022 -- S. Sethuraman

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakOBR_PD(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bRaster = false)
        {

            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 1000, 700, 400, 200 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);
            // waitForMotorStatus(axMotorOutput, 0);
            // waitForMotorStatus(axMotorOutput, 1);

            float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] ZCD = new float[] { 3f, 2f, 1f, .5f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            // float[] XoutPos = new float[array_size];
            // float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -.1f;
            //float RANGE_CHANGE_LEVEL = -5;
            //float V_TRACK_LEVEL = -55;
            // float DBM_TRACK_LEVEL = 0.5f;
            bool RESIZE = true;
            // bool DBM = true;
            int rslt = 0;
            int loop_count = 0;
            //float DXout = 0f, DYout = 0f, DZout = 0f, Dout = 0f;
            //float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            float PXin = 0f, PYin = 0f, PZin = 0f, Pin = 0f;
            float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 10.0f;
            float min_search_time = 5f;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();
            //axNanoOutput.SetCircHomePos(5, 5);
            // axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            axNanoInput.SetInputSrc(5);
            axNanoOutput.SetInputSrc(5);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            RANGE = 2; RESIZE = true;

            if (RESIZE)
            {
                RESIZE = false;
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                axNanoZ.SetLoopGain(ZLG[RANGE]);
                rslt = axNanoZ.SetCircDia(ZCD[RANGE]);

                axNanoInput.Track();
                //axNanoOutput.Track();
                axNanoZ.TrackEx(2);
            }
            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
            // while (RANGE != 2 | loop_count<20)
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if (bRaster)
                {
                    if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                    {
                        clog.Log(clog.Level.Warn, "No light, raster scan");
                        Debug.WriteLine("no light, raster scan");
                        InputRaster(300, 10, 0.05f);
                        startTime = DateTime.Now;//Reset the clock for min and max timeout
                        axNanoZ.TrackEx(2);
                        axNanoInput.Track();
                    }
                }

                //This infinite loop puases the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                //axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                int BreakCheckSize = 20;

                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold)
                {
                    //sum of all jitters recently
                    MXin = 0; MYin = 0; MZin = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXin = MXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        MYin = MYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                        MZin = MZin + Math.Abs(ZinPos[i] - ZinPos[i - 1]);
                    }
                    Min = MXin + MYin + MZin / 5;

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XinPosRecent = new float[BreakCheckSize];
                    float[] YinPosRecent = new float[BreakCheckSize];
                    float[] ZinPosRecent = new float[BreakCheckSize];

                    Array.Copy(XinPos, loop_count - BreakCheckSize, XinPosRecent, 0, BreakCheckSize);
                    PXin = XinPosRecent.Max() - XinPosRecent.Min();
                    Array.Copy(YinPos, loop_count - BreakCheckSize, YinPosRecent, 0, BreakCheckSize);
                    PYin = YinPosRecent.Max() - YinPosRecent.Min();
                    Array.Copy(ZinPos, loop_count - BreakCheckSize, ZinPosRecent, 0, BreakCheckSize);
                    PZin = ZinPosRecent.Max() - ZinPosRecent.Min();
                    float Xratio = MXin / PXin;
                    float Yratio = MYin / PYin;
                    float Zratio = MZin / PZin;

                    if ((PXin < MXin / 4 & PYin < MYin / 4 & PZin < MZin / 3) | (PXin < .075 & PYin < .075 & PZin < .25))
                    {
                        break;
                    }
                }

                //recenter?
                if ((XinPos[loop_count] > (9 - CD[RANGE] / 2) || XinPos[loop_count] < (1 + CD[RANGE] / 2) || YinPos[loop_count] > (9 - CD[RANGE] / 2) || YinPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                {
                    ReCenter("IN");
                    startTime = DateTime.Now;
                }

                //if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                //{
                //    ReCenter("OUT");
                //    startTime = DateTime.Now;
                //}
                //end recenter check
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                { RANGE = 0; RESIZE = true; }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                { RANGE = 1; RESIZE = true; }
                else if (opt_power1 >= Threshold & RANGE != 2)
                { RANGE = 2; RESIZE = true; }
                else
                { //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(ZLG[RANGE]);
                    rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                    axNanoInput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoInput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
            }

            RANGE = 3;
            try
            {
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }
            axNanoInput.SetLoopGain(LG[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            axNanoZ.SetLoopGain(ZLG[RANGE]);
            rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
            Thread.Sleep(500);
            axNanoZ.Latch();
            axNanoInput.Latch();
            axNanoOutput.Latch();
            Thread.Sleep(100);
            axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            // CArrayData myCoordinates = new CArrayData();
            StoreMotorCoords(true, false, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakOBR_PD_OUT(float Threshold, ref CArrayData myCoordinates, bool bSave = false, bool bRaster = false)
        {

            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 1000, 700, 400, 200 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            //waitForMotorStatus(axMotorInput, 0);
            //waitForMotorStatus(axMotorInput, 1);
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] ZCD = new float[] { 3f, 2f, 1f, .5f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            //float[] XinPos = new float[array_size];
            //float[] YinPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -.1f;
            //float RANGE_CHANGE_LEVEL = -5;
            //float V_TRACK_LEVEL = -55;
            // float DBM_TRACK_LEVEL = 0.5f;
            bool RESIZE = true;
            // bool DBM = true;
            int rslt = 0;
            int loop_count = 0;
            float PXout = 0f, PYout = 0f, PZout = 0f, Pout = 0f;
            float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            //float PXin = 0f, PYin = 0f, PZin = 0f, Pin = 0f;
            //float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 10.0f;
            float min_search_time = 5f;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            //axNanoInput.SetCircHomePos(5, 5);
            //axNanoInput.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            axNanoInput.SetInputSrc(5);
            axNanoOutput.SetInputSrc(5);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            RANGE = 2; RESIZE = true;

            if (RESIZE)
            {
                RESIZE = false;
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                axNanoZ.SetLoopGain(ZLG[RANGE]);
                rslt = axNanoZ.SetCircDia(ZCD[RANGE]);

                //axNanoInput.Track();
                axNanoOutput.Track();
                //axNanoZ.Track();
                axNanoZ.TrackEx(3);
            }
            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
            // while (RANGE != 2 | loop_count<20)
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

               // Raster scan at 1/4 of the array, to make sure it get closed to first light
                if (bRaster)
                {
                    if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                    {
                        clog.Log(clog.Level.Warn, "No light, raster scan");
                        Debug.WriteLine("no light, raster scan");
                        OutputRaster(300, 10, 0.05f);
                        startTime = DateTime.Now;//Reset the clock for min and max timeout
                        axNanoZ.TrackEx(3);
                        axNanoOutput.Track();
                    }
                }

                //This infinite loop puases the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                //axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                int BreakCheckSize = 20;

                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold)
                {
                    //sum of all jitters recently
                    MXout = 0; MYout = 0; MZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    Mout = MXout + MYout + MZout / 5;

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];

                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float Xratio = MXout / PXout;
                    float Yratio = MYout / PYout;
                    float Zratio = MZout / PZout;

                    if ((PXout < MXout / 4 & PYout < MYout / 4 & PZout < MZout / 3) | (PXout < .075 & PYout < .075 & PZout < .25))
                    {
                        break;
                    }
                }

                //recenter?
                //if ((XinPos[loop_count] > (9 - CD[RANGE] / 2) || XinPos[loop_count] < (1 + CD[RANGE] / 2) || YinPos[loop_count] > (9 - CD[RANGE] / 2) || YinPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                //{
                //    ReCenter("IN");
                //   startTime = DateTime.Now;
                //}

                if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }
                //end recenter check
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                { RANGE = 0; RESIZE = true; }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                { RANGE = 1; RESIZE = true; }
                else if (opt_power1 >= Threshold & RANGE != 2)
                { RANGE = 2; RESIZE = true; }
                else
                { //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(ZLG[RANGE]);
                    rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                    axNanoInput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoInput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
            }

            RANGE = 3;
            try
            {
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }
            axNanoInput.SetLoopGain(LG[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            axNanoZ.SetLoopGain(ZLG[RANGE]);
            rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
            Thread.Sleep(500);
            axNanoZ.Latch();
            axNanoInput.Latch();
            axNanoOutput.Latch();
            Thread.Sleep(100);
            //axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            // CArrayData myCoordinates = new CArrayData();
            StoreMotorCoords(true, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }


        public void FindPeakOBR_WAPD(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {

            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 1000, 700, 400, 200 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            //waitForMotorStatus(axMotorInput, 0);
            //waitForMotorStatus(axMotorInput, 1);
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] ZCD = new float[] { 3f, 2f, 1f, .5f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            //float[] XinPos = new float[array_size];
            //float[] YinPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -.1f;
            //float RANGE_CHANGE_LEVEL = -5;
            //float V_TRACK_LEVEL = -55;
            // float DBM_TRACK_LEVEL = 0.5f;
            bool RESIZE = true;
            // bool DBM = true;
            int rslt = 0;
            int loop_count = 0;
            float PXout = 0f, PYout = 0f, PZout = 0f, Pout = 0f;
            float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            //float PXin = 0f, PYin = 0f, PZin = 0f, Pin = 0f;
            //float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 10.0f;
            float min_search_time = 5f;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            //axNanoInput.SetCircHomePos(5, 5);
            //axNanoInput.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            //axNanoInput.SetInputSrc(5);
            axNanoOutput.SetInputSrc(5);
            //axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            RANGE = 2; RESIZE = true;

            if (RESIZE)
            {
                RESIZE = false;
                //axNanoInput.SetLoopGain(LG[RANGE]);
                //rslt = axNanoInput.SetCircDia(CD[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                axNanoZ.SetLoopGain(ZLG[RANGE]);
                rslt = axNanoZ.SetCircDia(ZCD[RANGE]);

                //axNanoInput.Track();
                axNanoOutput.Track();
                axNanoZ.Track();
            }
            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
            // while (RANGE != 2 | loop_count<20)
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }


                //// Raster scan at 1/4 of the array, to make sure it get closed to first light
                //if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                //{
                //    CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Warn, "No light, raster scan");
                //    Debug.WriteLine("no light, raster scan");
                //    OutputRaster(40, 5, -40);
                //    startTime = DateTime.Now;//Reset the clock for min and max timeout
                //    axNanoZ.TrackEx(3);
                //    axNanoOutput.Track();
                //}


                //This infinite loop puases the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                //axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                int BreakCheckSize = 20;

                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold)
                {
                    //sum of all jitters recently
                    MXout = 0; MYout = 0; MZout = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    Mout = MXout + MYout + MZout / 5;

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XoutPosRecent = new float[BreakCheckSize];
                    float[] YoutPosRecent = new float[BreakCheckSize];
                    float[] ZoutPosRecent = new float[BreakCheckSize];

                    Array.Copy(XoutPos, loop_count - BreakCheckSize, XoutPosRecent, 0, BreakCheckSize);
                    PXout = XoutPosRecent.Max() - XoutPosRecent.Min();
                    Array.Copy(YoutPos, loop_count - BreakCheckSize, YoutPosRecent, 0, BreakCheckSize);
                    PYout = YoutPosRecent.Max() - YoutPosRecent.Min();
                    Array.Copy(ZoutPos, loop_count - BreakCheckSize, ZoutPosRecent, 0, BreakCheckSize);
                    PZout = ZoutPosRecent.Max() - ZoutPosRecent.Min();
                    float Xratio = MXout / PXout;
                    float Yratio = MYout / PYout;
                    float Zratio = MZout / PZout;

                    if ((PXout < MXout / 4 & PYout < MYout / 4 & PZout < MZout / 3) | (PXout < .075 & PYout < .075 & PZout < .25))
                    {
                        break;
                    }
                }

                //recenter?
                if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                {
                    ReCenter("OUT");
                    startTime = DateTime.Now;
                }

                //if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                //{
                //    ReCenter("OUT");
                //    startTime = DateTime.Now;
                //}
                //end recenter check
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                { RANGE = 0; RESIZE = true; }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                { RANGE = 1; RESIZE = true; }
                else if (opt_power1 >= Threshold & RANGE != 2)
                { RANGE = 2; RESIZE = true; }
                else
                { //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(ZLG[RANGE]);
                    rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                    //axNanoInput.SetLoopGain(LG[RANGE]);
                    //rslt = axNanoInput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
            }

            RANGE = 3;
            try
            {
                //rslt = axNanoInput.SetCircDia(CD[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }
            //axNanoInput.SetLoopGain(LG[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            axNanoZ.SetLoopGain(ZLG[RANGE]);
            rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
            Thread.Sleep(500);
            axNanoZ.Latch();
            //axNanoInput.Latch();
            axNanoOutput.Latch();
            Thread.Sleep(100);
            axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            // CArrayData myCoordinates = new CArrayData();
            StoreMotorCoords(false, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakOBR_WAPD_INPUTGC(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {

            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 1000, 700, 400, 200 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);
            //waitForMotorStatus(axMotorOutput, 0);
            //waitForMotorStatus(axMotorOutput, 1);

            float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] ZCD = new float[] { 3f, 2f, 1f, .5f };

            float[] ZinPos = new float[array_size];
            float[] ZoutPos = new float[array_size];
            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            //float[] XoutPos = new float[array_size];
            //float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -.1f;
            //float RANGE_CHANGE_LEVEL = -5;
            //float V_TRACK_LEVEL = -55;
            // float DBM_TRACK_LEVEL = 0.5f;
            bool RESIZE = true;
            // bool DBM = true;
            int rslt = 0;
            int loop_count = 0;
            //float PXout = 0f, PYout = 0f, PZout = 0f, Pout = 0f;
            //float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            float PXin = 0f, PYin = 0f, PZin = 0f, Pin = 0f;
            float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 10.0f;
            float min_search_time = 5f;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();
            //axNanoOutput.SetCircHomePos(5, 5);
            //axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(5);
            axNanoInput.SetInputSrc(5);
            //axNanoOutput.SetInputSrc(5);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            //axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            RANGE = 2; RESIZE = true;

            if (RESIZE)
            {
                RESIZE = false;
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                //axNanoOutput.SetLoopGain(LG[RANGE]);
                //rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                axNanoZ.SetLoopGain(ZLG[RANGE]);
                rslt = axNanoZ.SetCircDia(ZCD[RANGE]);

                axNanoInput.Track();
                //axNanoOutput.Track();
                axNanoZ.Track();
            }
            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
            // while (RANGE != 2 | loop_count<20)
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }


                //// Raster scan at 1/4 of the array, to make sure it get closed to first light
                //if ((loop_count == Math.Round(array_size / 4.0, 0)) & (RANGE == 0))
                //{
                //    CLogger.Instance().getSysLogger().Log(NLog.LogLevel.Warn, "No light, raster scan");
                //    Debug.WriteLine("no light, raster scan");
                //    OutputRaster(40, 5, -40);
                //    startTime = DateTime.Now;//Reset the clock for min and max timeout
                //    axNanoZ.TrackEx(3);
                //    axNanoOutput.Track();
                //}


                //This infinite loop puases the alignment and waits for the "Red" state of checkBox1
                pause(startTime);

                axNanoZ.GetCircPosReading(ref ZinPos[loop_count], ref ZoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                //axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

                int BreakCheckSize = 20;

                if (RANGE == 2 & loop_count > (BreakCheckSize - 1) & opt_power1 > Threshold)
                {
                    //sum of all jitters recently
                    MXin = 0; MYin = 0; MZin = 0;
                    for (int i = loop_count - (BreakCheckSize - 1); i <= loop_count; i++)
                    {
                        MXin = MXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        MYin = MYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                        MZin = MZin + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    Min = MXin + MYin + MZin / 5;

                    //use shorter check (less points) for power stability case
                    BreakCheckSize = 15;

                    //max - min position
                    float[] XinPosRecent = new float[BreakCheckSize];
                    float[] YinPosRecent = new float[BreakCheckSize];
                    float[] ZinPosRecent = new float[BreakCheckSize];

                    Array.Copy(XinPos, loop_count - BreakCheckSize, XinPosRecent, 0, BreakCheckSize);
                    PXin = XinPosRecent.Max() - XinPosRecent.Min();
                    Array.Copy(YinPos, loop_count - BreakCheckSize, YinPosRecent, 0, BreakCheckSize);
                    PYin = YinPosRecent.Max() - YinPosRecent.Min();
                    Array.Copy(ZinPos, loop_count - BreakCheckSize, ZinPosRecent, 0, BreakCheckSize);
                    PZin = ZinPosRecent.Max() - ZinPosRecent.Min();
                    float Xratio = MXin / PXin;
                    float Yratio = MYin / PYin;
                    float Zratio = MZin / PZin;

                    if ((PXin < MXin / 4 & PYin < MYin / 4 & PZin < MZin / 3) | (PXin < .075 & PYin < .075 & PZin < .25))
                    {
                        break;
                    }
                }

                //recenter?
                if ((XinPos[loop_count] > (9 - CD[RANGE] / 2) || XinPos[loop_count] < (1 + CD[RANGE] / 2) || YinPos[loop_count] > (9 - CD[RANGE] / 2) || YinPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                {
                    ReCenter("IN");
                    startTime = DateTime.Now;
                }

                //if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                //{
                //    ReCenter("OUT");
                //    startTime = DateTime.Now;
                //}
                //end recenter check
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                { RANGE = 0; RESIZE = true; }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                { RANGE = 1; RESIZE = true; }
                else if (opt_power1 >= Threshold & RANGE != 2)
                { RANGE = 2; RESIZE = true; }
                else
                { //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoZ.SetLoopGain(ZLG[RANGE]);
                    rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
                    //axNanoOutput.SetLoopGain(LG[RANGE]);
                    //rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                    axNanoInput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoInput.SetCircDia(CD[RANGE]);
                }
                ts = DateTime.Now - startTime;
            }

            RANGE = 3;
            try
            {
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                //rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }
            axNanoInput.SetLoopGain(LG[RANGE]);
            //axNanoOutput.SetLoopGain(LG[RANGE]);
            axNanoZ.SetLoopGain(ZLG[RANGE]);
            rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
            Thread.Sleep(500);
            axNanoZ.Latch();
            axNanoInput.Latch();
            //axNanoOutput.Latch();
            Thread.Sleep(100);
            axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            // CArrayData myCoordinates = new CArrayData();
            StoreMotorCoords(true, false, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void FindPeakOBR_Focuser(float Threshold, ref CArrayData myCoordinates, bool bSave = false)
        {

            int array_size = 400;
            this.textBoxTextWrite(textBox_thresh, Threshold.ToString());

            double[,] opt_power = new double[1, 10];
            int[] LG = new int[] { 1000, 700, 300, 100 };
            int[] ZLG = new int[] { 1000, 700, 500, 300 };

            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            float[] CD = new float[] { 1.75f, 1f, .5f, .35f };
            float[] ZCD = new float[] { 3f, 2f, 1f, .5f };

            float[] XinPos = new float[array_size];
            float[] YinPos = new float[array_size];
            float[] XoutPos = new float[array_size];
            float[] YoutPos = new float[array_size];
            float opt_power1 = 0f;
            int range = 0;
            int RANGE = 0;
            float rel = 0f;
            int overunder = 0;
            float RANGE_CHANGE_LEVEL = -5;
            bool RESIZE = true;
            int rslt = 0;
            int loop_count = 0;
            float DXout = 0f, DYout = 0f, DZout = 0f, Dout = 0f;
            float MXout = 0f, MYout = 0f, MZout = 0f, Mout = 0f;
            float DXin = 0f, DYin = 0f, DZin = 0f, Din = 0f;
            float MXin = 0f, MYin = 0f, MZin = 0f, Min = 0f;
            float max_search_time = 8.0f;
            float min_search_time = 1.0f;

            axNanoZ.SetCircHomePos(5, 5);
            axNanoZ.MoveCircHome();
            axNanoInput.SetCircHomePos(5, 5);
            axNanoInput.MoveCircHome();
            axNanoOutput.SetCircHomePos(5, 5);
            axNanoOutput.MoveCircHome();

            axNanoZ.SetInputSrc(1);
            axNanoInput.SetInputSrc(1);
            axNanoOutput.SetInputSrc(1);
            axNanoZ.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.SetUnitsMode(3, .8f, 1, 1);
            axNanoOutput.SetUnitsMode(3, .8f, 1, 1);
            axNanoInput.GetCircPosReading(ref XinPos[0], ref YinPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            axNanoOutput.GetCircPosReading(ref XoutPos[0], ref YoutPos[0], ref opt_power1, ref range, ref rel, ref overunder);
            RANGE = 2; RESIZE = true;

            if (RESIZE)
            {
                RESIZE = false;
                axNanoInput.SetLoopGain(LG[RANGE]);
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                axNanoOutput.SetLoopGain(LG[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                axNanoInput.Track();
                axNanoOutput.Track();
                axNanoZ.Track();
            }
            DateTime startTime = DateTime.Now;
            DateTime TruestartTime = DateTime.Now;
            TimeSpan ts;
            ts = DateTime.Now - startTime;

            while (ts.TotalSeconds < min_search_time | (range != 2 & ts.TotalSeconds <= max_search_time))
            {
                loop_count++;
                if (loop_count >= array_size - 1)
                {
                    Debug.WriteLine("out of index, no light");
                    break;
                }

                pause(startTime);
                axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                axNanoOutput.GetCircPosReading(ref XoutPos[loop_count], ref YoutPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);
                int NumPoints = 8;

                if (RANGE == 2 & loop_count > (NumPoints - 1) & opt_power1 >= Threshold)
                {
                    //total distance traveled recently
                    DXout = Math.Abs(XoutPos[loop_count] - XoutPos[loop_count - NumPoints]);
                    DYout = Math.Abs(YoutPos[loop_count] - YoutPos[loop_count - NumPoints]);
                    // DZout = Math.Abs(ZoutPos[loop_count] - ZoutPos[loop_count - NumPoints]);
                    Dout = DXout + DYout;

                    DXin = Math.Abs(XinPos[loop_count] - XinPos[loop_count - NumPoints]);
                    DYin = Math.Abs(YinPos[loop_count] - YinPos[loop_count - NumPoints]);
                    //  DZin = Math.Abs(ZinPos[loop_count] - ZinPos[loop_count - NumPoints]);
                    Din = DXin + DYin;

                    //sum of all jitters recently
                    MXout = 0; MYout = 0;
                    for (int i = loop_count - (NumPoints - 1); i <= loop_count; i++)
                    {
                        MXout = MXout + Math.Abs(XoutPos[i] - XoutPos[i - 1]);
                        MYout = MYout + Math.Abs(YoutPos[i] - YoutPos[i - 1]);
                        // MZout = MZout + Math.Abs(ZoutPos[i] - ZoutPos[i - 1]);
                    }
                    Mout = MXout + MYout;

                    MXin = 0; MYin = 0;
                    for (int i = loop_count - (NumPoints - 1); i <= loop_count; i++)
                    {
                        MXin = MXin + Math.Abs(XinPos[i] - XinPos[i - 1]);
                        MYin = MYin + Math.Abs(YinPos[i] - YinPos[i - 1]);
                        // MZin = MZin + Math.Abs(ZinPos[i] - ZinPos[i - 1]);
                    }
                    Min = MXin + MYin;

                    Double BreakPower = .025;

                    if ((Dout < BreakPower & Din < BreakPower) | (Dout < Mout / 8 & Din < Min / 8))
                    {
                        // bool breakflag = true;
                        break;
                    }
                }

                //recenter?
                if ((XinPos[loop_count] > (9 - CD[RANGE] / 2) || XinPos[loop_count] < (1 + CD[RANGE] / 2) || YinPos[loop_count] > (9 - CD[RANGE] / 2) || YinPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                {
                    ReCenter("IN");
                    startTime = DateTime.Now;
                }
                //if ((XoutPos[loop_count] > (9 - CD[RANGE] / 2) || XoutPos[loop_count] < (1 + CD[RANGE] / 2) || YoutPos[loop_count] > (9 - CD[RANGE] / 2) || YoutPos[loop_count] < (1 + CD[RANGE] / 2)) && opt_power1 > -65)
                //{
                //    ReCenter("OUT");
                //    startTime = DateTime.Now;
                //}
                //end recenter check

                //change range/circle size?
                if (opt_power1 < Threshold + RANGE_CHANGE_LEVEL & RANGE != 0)
                {
                    RANGE = 0; RESIZE = true;
                }
                else if (opt_power1 >= Threshold + RANGE_CHANGE_LEVEL & opt_power1 < Threshold & RANGE != 1)
                {
                    RANGE = 1; RESIZE = true;
                }
                else if (opt_power1 >= Threshold & RANGE != 2)
                {
                    RANGE = 2; RESIZE = true;
                    // Thread.Sleep(500);
                }
                else
                {
                    //DONT SET RANGE
                }

                if (RESIZE)
                {
                    RESIZE = false;
                    axNanoInput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoInput.SetCircDia(CD[RANGE]);
                    axNanoOutput.SetLoopGain(LG[RANGE]);
                    rslt = axNanoOutput.SetCircDia(CD[RANGE]);
                    axNanoZ.SetLoopGain(ZLG[RANGE]);
                    rslt = axNanoZ.SetCircDia(ZCD[RANGE]);
                    Thread.Sleep(250);
                }

                //end range/circle size check
                ts = DateTime.Now - startTime;
            }

            RANGE = 3;
            try
            {
                rslt = axNanoInput.SetCircDia(CD[RANGE]);
                rslt = axNanoOutput.SetCircDia(CD[RANGE]);
            }
            catch
            {

            }
            axNanoInput.SetLoopGain(LG[RANGE]);
            axNanoOutput.SetLoopGain(LG[RANGE]);
            axNanoZ.SetLoopGain(ZLG[RANGE]);
            rslt = axNanoZ.SetCircDia(ZCD[RANGE]);

            Thread.Sleep(200);
            axNanoZ.Latch();
            axNanoInput.Latch();
            axNanoOutput.Latch();
            Thread.Sleep(100);
            axNanoInput.GetCircPosReading(ref XinPos[loop_count], ref YinPos[loop_count], ref opt_power1, ref range, ref rel, ref overunder);

            if (opt_power1 < Threshold)
            { bSave = false; }

            // CArrayData myCoordinates = new CArrayData();
            StoreMotorCoords(true, true, ref myCoordinates, bSave);

            myCoordinates.AddCol("Align_Time", new List<double> { Math.Round(Convert.ToDouble((DateTime.Now - TruestartTime).TotalSeconds), 2) });
            myCoordinates.AddCol("Align_Power", new List<double> { opt_power1 });
        }

        public void StoreMotorCoords(bool input, bool output, ref CArrayData myCoordinates, bool bSave)
        {
            //  coords=new List<double>();

            int var1 = 0;
            float motor_z_in = 0.0f, nano_z_in = 0.0f, motor_z_out = 0.0f, nano_z_out = 0.0f;
            float motor_x_in = 0.0f, motor_y_in = 0.0f;
            float nano_x_in = 0.0f, nano_y_in = 0.0f;
            float motor_x_out = 0.0f, motor_y_out = 0.0f;
            float nano_x_out = 0.0f, nano_y_out = 0.0f;

            float opt_power1 = 0f, rel = 0f;
            int range = 0, overunder = 0;
            List<int> coordIn = new List<int>();
            CAligmentMemory.CLocation myLocIn = new CAligmentMemory.CLocation();
            List<int> coordOut = new List<int>();
            CAligmentMemory.CLocation myLocOut = new CAligmentMemory.CLocation();

            axNanoZ.GetCircPosReading(ref nano_z_in, ref nano_z_out, ref opt_power1, ref range, ref rel, ref overunder);

            if (input == true)
            {

                axNanoInput.GetCircPosReading(ref nano_x_in, ref nano_y_in, ref opt_power1, ref range, ref rel, ref overunder);

                var1 = axMotorInput.GetPosition((int)0, ref motor_x_in);
                var1 = axMotorInput.GetPosition((int)1, ref motor_y_in);
                var1 = axMotorZ.GetPosition((int)0, ref motor_z_in);

                motor_x_in += (nano_x_in * 2 - 10) / 1000;
                motor_y_in += (nano_y_in * 2 - 10) / 1000;
                motor_z_in += (nano_z_in * 2 - 10) / 1000;

                if (bSave)
                {
                    coordIn.Add((int)Math.Round(_TestFileOffsetX_in + CAlignData.RecipeOffsetX_in * 1000.0)); coordIn.Add((int)(_TestFileOffsetY_in + CAlignData.RecipeOffsetY_in * 1000.0));
                    myLocIn.addCoords(motor_x_in, motor_y_in, motor_z_in - CAlignData.RecipeOffsetZ_in);
                    _InputAlignMem.setLocation(coordIn, (float)CWaferTestContainer.s_TestTempC, myLocIn);
                }
            }


            if (output == true)
            {
                axNanoOutput.GetCircPosReading(ref nano_x_out, ref nano_y_out, ref opt_power1, ref range, ref rel, ref overunder);

                var1 = axMotorOutput.GetPosition((int)0, ref motor_x_out);
                var1 = axMotorOutput.GetPosition((int)1, ref motor_y_out);
                var1 = axMotorZ.GetPosition((int)1, ref motor_z_out);

                motor_x_out += (nano_x_out * 2 - 10) / 1000;
                motor_y_out += (nano_y_out * 2 - 10) / 1000;
                motor_z_out += (nano_z_out * 2 - 10) / 1000;

                if (bSave)
                {
                    coordOut.Add((int)Math.Round(_TestFileOffsetX_out + CAlignData.RecipeOffsetX_out * 1000.0)); coordOut.Add((int)(_TestFileOffsetY_out + CAlignData.RecipeOffsetY_out * 1000.0));
                    myLocOut.addCoords(motor_x_out, motor_y_out, motor_z_out - CAlignData.RecipeOffsetZ_out);
                    _OutputAlignMem.setLocation(coordOut, (float)CWaferTestContainer.s_TestTempC, myLocOut);
                }
            }

            myCoordinates.AddCol("motor_X_in", new List<double> { motor_x_in });
            myCoordinates.AddCol("motor_Y_in", new List<double> { motor_y_in });
            myCoordinates.AddCol("motor_Z_in", new List<double> { motor_z_in });
            myCoordinates.AddCol("nano_X_in", new List<double> { nano_x_in });
            myCoordinates.AddCol("nano_Y_in", new List<double> { nano_y_in });
            myCoordinates.AddCol("nano_Z_in", new List<double> { nano_z_in });

            myCoordinates.AddCol("motor_X_out", new List<double> { motor_x_out });
            myCoordinates.AddCol("motor_Y_out", new List<double> { motor_y_out });
            myCoordinates.AddCol("motor_Z_out", new List<double> { motor_z_out });
            myCoordinates.AddCol("nano_X_out", new List<double> { nano_x_out });
            myCoordinates.AddCol("nano_Y_out", new List<double> { nano_y_out });
            myCoordinates.AddCol("nano_Z_out", new List<double> { nano_z_out });
        }



        public void ReCenter(string stage)
        {
            // double[,] opt_power = new double[1, 10];
            /* float XNinPos = 0f;
             float YNinPos = 0f;
             float XMinPos = 0f;
             float YMinPos = 0f;

             float XNoutPos = 0f;
             float YNoutPos = 0f;
             float XMoutPos = 0f;
             float YMoutPos = 0f;
             */
            float opt_power1 = 0f;
            int range = 0;
            float rel = 0f;
            int overunder = 0;

            int rslt = 0;
            if (stage == "IN")
            {
                float XNinPos = 0f;
                float YNinPos = 0f;
                float XMinPos = 0f;
                float YMinPos = 0f;
                axMotorInput.GetPosition(0, ref XMinPos);
                axMotorInput.GetPosition(1, ref YMinPos);
                axNanoInput.GetCircPosReading(ref XNinPos, ref YNinPos, ref opt_power1, ref range, ref rel, ref overunder);
                float XSTEP = XNinPos - 5;
                float YSTEP = YNinPos - 5;

                if ((XNinPos > 5 && XMinPos > 3.98) || (XNinPos < 5 && XMinPos < 0.02))
                {
                    XSTEP = 0;
                }
                if ((YNinPos > 5 && YMinPos > 3.98) || (YNinPos < 5 && YMinPos < 0.02))
                {
                    YSTEP = 0;
                }


                if (Math.Abs(XSTEP) > .1)
                {
                    axMotorInput.SetRelMoveDist(0, XSTEP * 2e-3f);
                    axMotorInput.MoveRelative(0, true);
                }
                if (Math.Abs(YSTEP) > .1)
                {
                    axMotorInput.SetRelMoveDist(1, YSTEP * 2e-3f);
                    axMotorInput.MoveRelative(1, true);
                }


                axNanoInput.SetCircHomePos(5, 5);
                axNanoInput.MoveCircHome();
            }

            if (stage == "OUT")
            {
                float XNoutPos = 0f;
                float YNoutPos = 0f;
                float XMoutPos = 0f;
                float YMoutPos = 0f;
                axMotorOutput.GetPosition(0, ref XMoutPos);
                axMotorOutput.GetPosition(1, ref YMoutPos);
                axNanoOutput.GetCircPosReading(ref XNoutPos, ref YNoutPos, ref opt_power1, ref range, ref rel, ref overunder);
                float XSTEP = XNoutPos - 5;
                float YSTEP = YNoutPos - 5;
                // Logging for recenter 
                string log_res = string.Format("X,Y before Recenter:{0},{1}", XMoutPos, YMoutPos);
                clog.Log(clog.Level.Fatal, log_res);

                if ((XNoutPos > 5 && XMoutPos > 3.98) || (XNoutPos < 5 && XMoutPos < 0.02))
                {
                    XSTEP = 0;
                }
                if ((YNoutPos > 5 && YMoutPos > 3.98) || (YNoutPos < 5 && YMoutPos < 0.02))
                {
                    YSTEP = 0;
                }


                if (Math.Abs(XSTEP) > .1)
                {
                    axMotorOutput.SetRelMoveDist(0, XSTEP * 2e-3f);
                    axMotorOutput.MoveRelative(0, true);
                }
                if (Math.Abs(YSTEP) > .1)
                {
                    axMotorOutput.SetRelMoveDist(1, YSTEP * 2e-3f);
                    axMotorOutput.MoveRelative(1, true);
                }


                axNanoOutput.SetCircHomePos(5, 5);
                axNanoOutput.MoveCircHome();
            }

            if (stage == "Z")
            {
                float ZNinPos = 0f;
                float ZNoutPos = 0f;
                float ZMinPos = 0f;
                float ZMoutPos = 0f;
                axMotorZ.GetPosition(0, ref ZMinPos);
                axMotorZ.GetPosition(1, ref ZMoutPos);
                axNanoZ.GetCircPosReading(ref ZNinPos, ref ZNoutPos, ref opt_power1, ref range, ref rel, ref overunder);
                float ZinSTEP = ZNinPos - 5;
                float ZoutSTEP = ZNoutPos - 5;
                // Logging for recenter 
                string log_res = string.Format("Z before Recenter:{0}", ZMoutPos);
                clog.Log(clog.Level.Fatal, log_res);
                if ((ZNinPos > 5 && ZMinPos > 3.98) || (ZNinPos < 5 && ZMinPos < 0.02))
                {
                    ZinSTEP = 0;
                }
                if ((ZNoutPos > 5 && ZMoutPos > 3.98) || (ZNoutPos < 5 && ZMoutPos < 0.02))
                {
                    ZoutSTEP = 0;
                }


                if (Math.Abs(ZinSTEP) > .1)
                {
                    axMotorZ.SetRelMoveDist(0, ZinSTEP * 2e-3f);
                    axMotorZ.MoveRelative(0, true);
                }
                if (Math.Abs(ZoutSTEP) > .1)
                {
                    axMotorZ.SetRelMoveDist(1, ZoutSTEP * 2e-3f);
                    axMotorZ.MoveRelative(1, true);
                }


                axNanoZ.SetCircHomePos(5, 5);
                axNanoZ.MoveCircHome();
            }
        }

        // Motion controller common functions

        public void MoveXYAbsoluteInputMotor(float x, float y)
        {
            axMotorInput.SetAbsMovePos(0, x);
            axMotorInput.SetAbsMovePos(1, y);
            axMotorInput.MoveAbsolute(0, true);
            axMotorInput.MoveAbsolute(1, true);
        }

        public void MoveXYAbsoluteOutputMotor(float x, float y)
        {
            axMotorOutput.SetAbsMovePos(0, x);
            axMotorOutput.SetAbsMovePos(1, y);
            axMotorOutput.MoveAbsolute(0, true);
            axMotorOutput.MoveAbsolute(1, true);
        }

        public void MoveXYRelativeInputMotor(float x, float y)
        {
            axMotorInput.SetRelMoveDist(0, x);
            axMotorInput.SetRelMoveDist(1, y);
            axMotorInput.MoveRelative(0, true);
            axMotorInput.MoveRelative(1, true);
        }

        public void MoveXYRelativeOutputMotor(float x, float y)
        {
            axMotorOutput.SetRelMoveDist(0, x);
            axMotorOutput.SetRelMoveDist(1, y);
            axMotorOutput.MoveRelative(0, true);
            axMotorOutput.MoveRelative(1, true);
        }

        /// <summary>
        /// Absolute z movement
        /// </summary>
        /// <param name="motor">Motor to use</param>
        /// <param name="z">z movement value in mm</param>
        public void MoveZAbsolute(Motors motor, float z)
        {
            switch(motor)
            {
                case Motors.ThorLabsZInputMotor:
                    axMotorZ.SetAbsMovePos(0, z);
                    axMotorZ.MoveAbsolute(0, true);
                    break;

                case Motors.ThorLabsZOutputMotor:
                    axMotorZ.SetAbsMovePos(1, z);
                    axMotorZ.MoveAbsolute(1, true);
                    break;
            }
        }

        /// <summary>
        /// Relative z movement
        /// </summary>
        /// <param name="motor">Motor to use</param>
        /// <param name="z">z coordinate in mm</param>
        public void MoveZRelative(Motors motor, float z)
        {
            switch (motor)
            {
                case Motors.ThorLabsZInputMotor:
                    axMotorZ.SetRelMoveDist(0, z);
                    axMotorZ.MoveRelative(0, true);
                    break;

                case Motors.ThorLabsZOutputMotor:
                    axMotorZ.SetRelMoveDist(1, z);
                    axMotorZ.MoveRelative(1, true);
                    break;
            }
        }

        /// <summary>
        /// Absolute X movement
        /// </summary>
        /// <param name="motor">Motor to use</param>
        /// <param name="pos">position in mm</param>
        public void MoveXAbsolute(Motors motor, float pos)
        {
            switch (motor)
            {
                case Motors.ThorLabsInputMotor:
                    axMotorInput.SetAbsMovePos(0, pos);
                    axMotorInput.MoveAbsolute(0, true);
                    break;

                case Motors.ThorLabsOutputMotor:
                    axMotorOutput.SetAbsMovePos(0, pos);
                    axMotorOutput.MoveAbsolute(0, true);
                    break;
            }
        }

        /// <summary>
        /// Relative X movement
        /// </summary>
        /// <param name="motor">Motor to use</param>
        /// <param name="dist">distance in mm</param>
        public void MoveXRelative(Motors motor, float dist)
        {
            switch (motor)
            {
                case Motors.ThorLabsInputMotor:
                    axMotorInput.SetRelMoveDist(0, dist);
                    axMotorInput.MoveRelative(0, true);
                    break;

                case Motors.ThorLabsOutputMotor:
                    axMotorOutput.SetRelMoveDist(0, dist);
                    axMotorOutput.MoveRelative(0, true);
                    break;
            }
        }

        /// <summary>
        /// Absolute Y movement
        /// </summary>
        /// <param name="motor">Motor to use</param>
        /// <param name="pos">position in mm</param>
        public void MoveYAbsolute(Motors motor, float pos)
        {
            switch (motor)
            {
                case Motors.ThorLabsInputMotor:
                    axMotorInput.SetAbsMovePos(1, pos);
                    axMotorInput.MoveAbsolute(1, true);
                    break;

                case Motors.ThorLabsOutputMotor:
                    axMotorOutput.SetAbsMovePos(1, pos);
                    axMotorOutput.MoveAbsolute(1, true);
                    break;
            }
        }

        /// <summary>
        /// Relative Y movement
        /// </summary>
        /// <param name="motor">Motor to use</param>
        /// <param name="dist">distance in mm</param>
        public void MoveYRelative(Motors motor, float dist)
        {
            switch (motor)
            {
                case Motors.ThorLabsInputMotor:
                    axMotorInput.SetRelMoveDist(1, dist);
                    axMotorInput.MoveRelative(1, true);
                    break;

                case Motors.ThorLabsOutputMotor:
                    axMotorOutput.SetRelMoveDist(1, dist);
                    axMotorOutput.MoveRelative(1, true);
                    break;
            }
        }

        protected bool computeMotorCoord(CAligmentMemory mem, float x, float y, ref float motorX, ref float motorY, ref float motorZ)
        {
            List<int> coord = new List<int>();
            coord.Add((int)Math.Round(x)); coord.Add((int)Math.Round(y));

            CAligmentMemory.CLocationBuffer locBuffer = mem.getLocation(coord, (float)CWaferTestContainer.s_TestTempC);
            if (locBuffer != null)
            {
                //Find exact match position
                motorX = locBuffer.getValue(0);
                motorY = locBuffer.getValue(1);
                motorZ = locBuffer.getValue(2);
            }
            else
            {
                //Now attempt to find the closest position
                CAligmentMemory.CLocationOverTemp closestLoc = mem.findClosestPosition(coord);
                if (null != closestLoc)
                {
                    locBuffer = closestLoc.findClosestMatchingTemp((float)CWaferTestContainer.s_TestTempC).LocationBuffer;
                    List<int> closestCoord = closestLoc.arLocationAtTemp[0].coord;
                    if (null != locBuffer)
                    {
                        //Compute relative coordinates
                        motorX = locBuffer.getValue(0) + (coord[0] - closestCoord[0]) / 1000.0f;
                        motorY = locBuffer.getValue(1) + (coord[1] - closestCoord[1]) / 1000.0f;
                        motorZ = locBuffer.getValue(2);
                    }
                }
            }

            return true;
        }

        public void MoveXYCalInputMotor(float x, float y)
        {
            _TestFileOffsetX_in = x;
            _TestFileOffsetY_in = y;

            float motorX = -1f;
            float motorY = -1f;
            float motorZ = -1f;
            computeMotorCoord(_InputAlignMem, x + CAlignData.RecipeOffsetX_in * 1000.0F, y + CAlignData.RecipeOffsetY_in * 1000.0F, ref motorX, ref motorY, ref motorZ);

            if (motorX != -1 && motorY != -1 && motorZ != -1)
            {
                axMotorInput.SetAbsMovePos(0, motorX);
                axMotorInput.SetAbsMovePos(1, motorY);
                axMotorZ.SetAbsMovePos(0, motorZ + CAlignData.RecipeOffsetZ_in);
                axMotorInput.MoveAbsolute(0, true);
                axMotorInput.MoveAbsolute(1, true);
                axMotorZ.MoveAbsolute(0, true);
            }
        }

        public void MoveXYCalOutputMotor(float x, float y)
        {
            _TestFileOffsetX_out = x;
            _TestFileOffsetY_out = y;

            float motorX = -1f;
            float motorY = -1f;
            float motorZ = -1f;
            computeMotorCoord(_OutputAlignMem, x + CAlignData.RecipeOffsetX_out * 1000.0F, y + CAlignData.RecipeOffsetY_out * 1000.0F, ref motorX, ref motorY, ref motorZ);

            if (motorX != -1 && motorY != -1 && motorZ != -1)
            {
                axMotorOutput.SetAbsMovePos(0, motorX);
                axMotorOutput.SetAbsMovePos(1, motorY);
                axMotorZ.SetAbsMovePos(1, motorZ + CAlignData.RecipeOffsetZ_out);
                axMotorOutput.MoveAbsolute(0, true);
                axMotorOutput.MoveAbsolute(1, true);
                axMotorZ.MoveAbsolute(1, true);
            }
        }

        // GUI button click events



        private void Output_Recenter_Click(object sender, EventArgs e)
        {
            ReCenter("OUT");
        }

        private void Input_Recenter_Click(object sender, EventArgs e)
        {
            ReCenter("IN");
        }

        private void Z_Recenter_Click(object sender, EventArgs e)
        {
            ReCenter("Z");
        }

        private void Align_Click(object sender, EventArgs e)
        {
            if (chkSaveToMem.Checked) CWaferTestContainer.s_TestTempC = float.Parse(txtTempC.Text);

            // List<double> coords = new List<double>();
            CArrayData myCoordinates = new CArrayData();
            FindPeakOO(float.Parse(textBox_thresh.Text), ref myCoordinates, chkSaveToMem.Checked);

        }
        private void frmThorlabsAlignment_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            axMotorInput.StopCtrl();
            axMotorZ.StopCtrl();
            axMotorOutput.StopCtrl();

            axNanoInput.StopCtrl();
            axNanoZ.StopCtrl();
            axNanoOutput.StopCtrl();
        }
        private void ZScan(string stage, float scan_size, float step_size, float threshold)
        {
            scan_size = scan_size / 1000;
            step_size = step_size / 1000;
            int num_steps = (int)Math.Floor(scan_size / step_size);
            float[] opt_power = new float[num_steps];
            float[] zvalues = new float[num_steps];
            AxMG17NanoTrakLib.AxMG17NanoTrak nanoStage;
            int averageNum = 20;
            float[] powerArray = new float[averageNum];


            int range = 0;
            float rel = 0f;
            int overunder = 0;
            int stageIndex = 0;
            //float xpos = 2;
            //float ypos = 2;
            //float RANGE_PERCENT = 0f;
            //float MODE = 0f;

            if (stage == "IN")
            {
                stageIndex = 0;
                axNanoOutput.Latch();
                axNanoZ.Latch();
                axNanoInput.Track();
                nanoStage = axNanoInput;
            }
            else
            {
                stageIndex = 1;
                axNanoOutput.Track();
                axNanoZ.Latch();
                axNanoInput.Latch();
                nanoStage = axNanoOutput;
            }

            int CURRENT_RANGE = 7;
            // no comment
            axNanoInput.SetRange(CURRENT_RANGE + 2);
            float Z = 0.0f;
            int var1 = axMotorZ.GetPosition(stageIndex, ref Z);

            for (int i = 0; i < num_steps; i++)
            {
                zvalues[i] = Z - scan_size / 2 + i * step_size;
            }

            axMotorZ.SetAbsMovePos(stageIndex, zvalues[0]);
            axMotorZ.MoveAbsolute(stageIndex, true);
            waitForMotorStatus(axMotorZ, stageIndex);
            ReCenter(stage);
            Thread.Sleep(1500);
            ReCenter(stage);
            Thread.Sleep(1500);
            ReCenter(stage);
            Thread.Sleep(1000);

            float max_power = -999f;
            nanoStage.GetReading(ref max_power, ref range, ref rel, ref overunder);
            float maxZ = zvalues[0];

            for (int i = 0; i < num_steps; i++)
            {
                axMotorZ.SetAbsMovePos(stageIndex, zvalues[i]);
                axMotorZ.MoveAbsolute(stageIndex, true);
                waitForMotorStatus(axMotorZ, stageIndex);
                ReCenter(stage);
                Thread.Sleep(500);

                for (int n = 0; n < averageNum; n++)
                {
                    nanoStage.GetReading(ref powerArray[n], ref range, ref rel, ref overunder);
                    opt_power[i] = opt_power[i] + powerArray[n] / averageNum;
                }


                if (opt_power[i] > max_power)
                {
                    max_power = opt_power[i];
                    maxZ = zvalues[i];
                }
            }


            if (max_power > threshold)
            {
                axMotorZ.SetAbsMovePos(stageIndex, maxZ);
                axMotorZ.MoveAbsolute(stageIndex, true);
            }
            else
            {
                axMotorZ.SetAbsMovePos(stageIndex, Z);
                axMotorZ.MoveAbsolute(stageIndex, true);
            }
            waitForMotorStatus(axMotorZ, stageIndex);
            ReCenter(stage);
            Thread.Sleep(1500);
            ReCenter(stage);
            Thread.Sleep(1500);
            ReCenter(stage);
            Thread.Sleep(1000);
            nanoStage.Latch();

            textBox_ScanRslt.Text = Convert.ToString(max_power);
        }


        private void OutputRaster(float scan_size, float step_size, float threshold)
        {
            scan_size = scan_size / 1000;
            step_size = step_size / 1000;
            int num_steps = (int)Math.Floor(scan_size / step_size);
            float[,] opt_power = new float[num_steps, num_steps];
            float[] xvalues = new float[num_steps];
            float[] yvalues = new float[num_steps];

            //float opt_power1 = 0f;
            int range = 0;
            float rel = 0f;
            int overunder = 0;
            //float xpos = 2;
            //float ypos = 2;
            //float RANGE_PERCENT = 0f;
            //float MODE = 0f;

            axNanoOutput.Latch();
            if (OOConfig)
            {
                axNanoInput.Latch();
            }
            axNanoZ.Latch();

            int CURRENT_RANGE = 7;
            // no comment
            if (OOConfig) axNanoInput.SetRange(CURRENT_RANGE + 2);
            float X = 0.0f;
            int var1 = axMotorOutput.GetPosition((int)0, ref X);

            float Y = 0.0f;
            var1 = axMotorOutput.GetPosition((int)1, ref Y);

            for (int i = 0; i < num_steps; i++)
            {
                xvalues[i] = X - scan_size / 2 + i * step_size;
                yvalues[i] = Y - scan_size / 2 + i * step_size;
            }

            axMotorOutput.SetAbsMovePos((int)0, xvalues[0]);
            axMotorOutput.MoveAbsolute((int)0, true);
            axMotorOutput.SetAbsMovePos((int)1, yvalues[0]);
            axMotorOutput.MoveAbsolute((int)1, true);
            //Thread.Sleep(500);
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);

            float max_power = -999f;
            axNanoOutput.GetReading(ref max_power, ref range, ref rel, ref overunder);
            float maxX = xvalues[0];
            float maxY = yvalues[0];

            for (int i = 0; i < num_steps; i++)
            {
                axMotorOutput.SetAbsMovePos(0, xvalues[i]);
                axMotorOutput.MoveAbsolute(0, true);
                waitForMotorStatus(axMotorOutput, 0);

                if (i % 2 == 0)
                {
                    for (int j = 0; j < num_steps; j++)
                    {
                        axMotorOutput.SetAbsMovePos(1, yvalues[j]);
                        axMotorOutput.MoveAbsolute(1, true);
                        waitForMotorStatus(axMotorOutput, 1);
                        //Thread.Sleep(100);
                        axNanoOutput.GetReading(ref opt_power[i, j], ref range, ref rel, ref overunder);
                        if (opt_power[i, j] > max_power)
                        {
                            max_power = opt_power[i, j];
                            maxX = xvalues[i];
                            maxY = yvalues[j];
                        }
                    }
                }
                else
                {
                    for (int j = num_steps - 1; j >= 0; j--)
                    {
                        axMotorOutput.SetAbsMovePos(1, yvalues[j]);
                        axMotorOutput.MoveAbsolute(1, true);
                        waitForMotorStatus(axMotorOutput, 1);
                        //Thread.Sleep(100);
                        axNanoOutput.GetReading(ref opt_power[i, j], ref range, ref rel, ref overunder);
                        if (opt_power[i, j] > max_power)
                        {
                            max_power = opt_power[i, j];
                            maxX = xvalues[i];
                            maxY = yvalues[j];
                        }
                    }
                }
            }

            if (max_power > threshold)
            {
                axMotorOutput.SetAbsMovePos(0, maxX);
                axMotorOutput.MoveAbsolute(0, true);
                axMotorOutput.SetAbsMovePos(1, maxY);
                axMotorOutput.MoveAbsolute(1, true);
            }
            else
            {
                axMotorOutput.SetAbsMovePos(0, X);
                axMotorOutput.MoveAbsolute(0, true);
                axMotorOutput.SetAbsMovePos(1, Y);
                axMotorOutput.MoveAbsolute(1, true);
            }
            waitForMotorStatus(axMotorOutput, 0);
            waitForMotorStatus(axMotorOutput, 1);
            if (OOConfig) axNanoInput.SetRangingMode(1, 1);
            axNanoOutput.SetRangingMode(1, 1);
            textBoxTextWrite(textBox_ScanRslt, Convert.ToString(max_power));



        }

        public void InputRaster(float scan_size, float step_size, float threshold)
        {
            scan_size = scan_size / 1000;
            step_size = step_size / 1000;
            int num_steps = (int)Math.Floor(scan_size / step_size);
            float[,] opt_power = new float[num_steps, num_steps];
            float[] xvalues = new float[num_steps];
            float[] yvalues = new float[num_steps];

            //float opt_power1 = 0f;
            int range = 0;
            float rel = 0f;
            int overunder = 0;
            //float xpos = 2;
            //float ypos = 2;
            //float RANGE_PERCENT = 0f;
            //float MODE = 0f;

            axNanoOutput.Latch();
            axNanoInput.Latch();
            axNanoZ.Latch();

            int CURRENT_RANGE = 7;
            // no comment
            axNanoInput.SetRange(CURRENT_RANGE + 2);
            float X = 0.0f;
            int var1 = axMotorInput.GetPosition((int)0, ref X);
            
            float Y = 0.0f;
            var1 = axMotorInput.GetPosition((int)1, ref Y);


            for (int i = 0; i < num_steps; i++)
            {
                xvalues[i] = X - scan_size / 2 + i * step_size;
                yvalues[i] = Y - scan_size / 2 + i * step_size;
            }

            axMotorInput.SetAbsMovePos((int)0, xvalues[0]);
            axMotorInput.MoveAbsolute((int)0, true);
            axMotorInput.SetAbsMovePos((int)1, yvalues[0]);
            axMotorInput.MoveAbsolute((int)1, true);
            //Thread.Sleep(500);
            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);

            float max_power = -999f;
            axNanoInput.GetReading(ref max_power, ref range, ref rel, ref overunder);
            float maxX = xvalues[0];
            float maxY = yvalues[0];

            for (int i = 0; i < num_steps; i++)
            {
                axMotorInput.SetAbsMovePos(0, xvalues[i]);
                axMotorInput.MoveAbsolute(0, true);
                waitForMotorStatus(axMotorInput, 0);

                if (i % 2 == 0)
                {
                    for (int j = 0; j < num_steps; j++)
                    {
                        axMotorInput.SetAbsMovePos(1, yvalues[j]);
                        axMotorInput.MoveAbsolute(1, true);
                        waitForMotorStatus(axMotorInput, 1);
                        //Thread.Sleep(100);
                        axNanoInput.GetReading(ref opt_power[i, j], ref range, ref rel, ref overunder);
                        if (opt_power[i, j] > max_power)
                        {
                            max_power = opt_power[i, j];
                            maxX = xvalues[i];
                            maxY = yvalues[j];
                        }
                    }
                }
                else
                {
                    for (int j = num_steps - 1; j >= 0; j--)
                    {
                        axMotorInput.SetAbsMovePos(1, yvalues[j]);
                        axMotorInput.MoveAbsolute(1, true);
                        waitForMotorStatus(axMotorOutput, 1);
                        //Thread.Sleep(100);
                        axNanoInput.GetReading(ref opt_power[i, j], ref range, ref rel, ref overunder);
                        if (opt_power[i, j] > max_power)
                        {
                            max_power = opt_power[i, j];
                            maxX = xvalues[i];
                            maxY = yvalues[j];
                        }
                    }
                }
            }

            if (max_power > threshold)
            {
                axMotorInput.SetAbsMovePos(0, maxX);
                axMotorInput.MoveAbsolute(0, true);
                axMotorInput.SetAbsMovePos(1, maxY);
                axMotorInput.MoveAbsolute(1, true);
            }
            else
            {
                axMotorInput.SetAbsMovePos(0, X);
                axMotorInput.MoveAbsolute(0, true);
                axMotorInput.SetAbsMovePos(1, Y);
                axMotorInput.MoveAbsolute(1, true);
            }
            waitForMotorStatus(axMotorInput, 0);
            waitForMotorStatus(axMotorInput, 1);

            // Get back to autorange
            axNanoInput.SetRangingMode(1, 1);
            textBoxTextWrite(textBox_ScanRslt, Convert.ToString(max_power));
        }


        private void Output_Raster_Click(object sender, EventArgs e)
        {

            float scan_size = float.Parse(textBox_ScanSize.Text);
            float step_size = float.Parse(textBox_StepSize.Text);
            float threshold = float.Parse(textBox_thresh.Text);
            OutputRaster(scan_size, step_size, threshold);
        }

        private void Input_Raster_Click(object sender, EventArgs e)
        {
            float scan_size = float.Parse(textBox_ScanSize.Text);
            float step_size = float.Parse(textBox_StepSize.Text);
            float threshold = float.Parse(textBox_thresh.Text);
            InputRaster(scan_size, step_size, threshold);
        }

        private void Dual_Raster_Click(object sender, EventArgs e)
        {

        }

        private void chkSaveToMem_CheckedChanged(object sender, EventArgs e)
        {

            txtLayoutX.Enabled = chkSaveToMem.Checked;
            txtLayoutY.Enabled = chkSaveToMem.Checked;
            txtTempC.Enabled = chkSaveToMem.Checked;
        }

        private void btnMoveCalXY_Input_Click(object sender, EventArgs e)
        {
            CWaferTestContainer.s_TestTempC = float.Parse(txtTempC.Text);
            MoveXYCalInputMotor(float.Parse(txtLayoutX.Text), float.Parse(txtLayoutY.Text));
        }

        private void btnMoveCalXY_Output_Click(object sender, EventArgs e)
        {
            CWaferTestContainer.s_TestTempC = float.Parse(txtTempC.Text);
            MoveXYCalOutputMotor(float.Parse(txtLayoutX.Text), float.Parse(txtLayoutY.Text));
        }

        private void btnClearMem_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult confirm_clear = MessageBox.Show("Confirm Removing Memory?", "Remove Alignment Memory", MessageBoxButtons.YesNo);
                if (confirm_clear == DialogResult.Yes)
                {
                    // Move Memory with time stamp for back up 
                    string input_src = string.Format(@"C:\temp\InputMotorMemAlign.xml");
                    string output_src = string.Format(@"C:\temp\OutputMotorMemAlign.xml");
                    string dt = DateTime.Now.ToString("yyMMdd_HHmmss");

                    string input_dst = string.Format(@"C:\temp\InputMotorMemAlign_{0}.xml", dt);
                    string output_dst = string.Format(@"C:\temp\OutputMotorMemAlign_{0}.xml", dt);

                    if(File.Exists(input_src)) File.Move(input_src, input_dst);
                    if(File.Exists(output_src)) File.Move(output_src, output_dst);
                    _InputAlignMem.clearSiteData();
                    _OutputAlignMem.clearSiteData();
                    MessageBox.Show(string.Format("{0}\n{1}", input_dst, output_dst), "Memory Files backed up to ");
                }
                else
                {
                    MessageBox.Show("", "Cancelled");
                }

            }
            catch
            {

            }


        }

        private void btnRecipeOffsetSet_Click(object sender, EventArgs e)
        {
            try
            {
                CAlignData.RecipeOffsetX_in = float.Parse(txtRecipeOffsetX_in.Text);
                CAlignData.RecipeOffsetY_in = float.Parse(txtRecipeOffsetY_in.Text);
                CAlignData.RecipeOffsetZ_in = float.Parse(txtRecipeOffsetZ_in.Text);
                CAlignData.RecipeOffsetX_out = float.Parse(txtRecipeOffsetX_out.Text);
                CAlignData.RecipeOffsetY_out = float.Parse(txtRecipeOffsetY_out.Text);
                CAlignData.RecipeOffsetZ_out = float.Parse(txtRecipeOffsetZ_out.Text);
            }
            catch (Exception ex)
            {
                clog.Log(clog.Level.Error, ex.ToString());
            }
        }




        private void textBoxTextWrite(TextBox textBox, string text)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke((MethodInvoker)delegate { textBox.Text = text; });
            }
            else
            {
                textBox.Text = text;
            }
        }

        // Alex Semakov 03/22/2017
        // Thread safe Read/Write to textBox1
        private string textBoxTextRead(TextBox textBox)
        {
            string text = "";
            if (textBox.InvokeRequired)
            {
                textBox.Invoke((MethodInvoker)delegate { text = textBox.Text; });
            }
            else
            {
                text = textBox.Text;
            }
            return text;
        }

        // Alex Semakov 03/22/2017
        // Pause/Resume button event handler
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox1.BackColor = Color.Green;
                checkBox1.Text = "Resume";
                Debug.WriteLine("RESUME");
                // if(backgroundWorker1.IsBusy) backgroundWorker1.CancelAsync();
            }
            else
            {
                checkBox1.BackColor = Color.Red;
                checkBox1.Text = "Pause";
                Debug.WriteLine("PAUSE");
                // if (!backgroundWorker1.IsBusy) startBackgroundWorker(_arguments);
            }
        }


        // Alex Semakov 03/22/2017
        // This method accepts arguments of the type List<string>.
        // The 1st element of the string is a name of the alignment algorithm to execute.
        // The following elements are arguments for the algorithms in a string format.
        public void startBackgroundWorker(List<string> arguments)
        {
            _arguments = arguments;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.DoWork += new DoWorkEventHandler(backgroundWorker1_DoWork);
            backgroundWorker1.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker1_RunWorkerCompleted);
            backgroundWorker1.RunWorkerAsync();
        }

        void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            List<double> coords = new List<double>();

            string algorithm = "";
            if (_arguments.Count > 0) algorithm = _arguments[0];
            else
            {
                MessageBox.Show("Alignment algorithm was not specified.");
                backgroundWorker1.CancelAsync();
                return;
            }

            try
            {
                CArrayData myCoordinates = new CArrayData();
                switch (algorithm)
                {
                    case "NanoRaster":
                        NanoRaster();
                        break;

                    case "FindPeakOO":
                        FindPeakOO(float.Parse(_arguments[1]), ref myCoordinates, bool.Parse(_arguments[2]));
                        break;

                    case "FindPeakRx":
                        float dummy_power = 0f;
                        FindPeakRx(float.Parse(_arguments[1]), ref myCoordinates, bool.Parse(_arguments[2]));
                        break;

                    case "FindPeakTx":
                        FindPeakTx(float.Parse(_arguments[1]), ref myCoordinates, bool.Parse(_arguments[2]));
                        break;

                    case "InitializeAPT":
                        InitializeAPT();
                        break;
                }
            }
            catch
            {
                MessageBox.Show("Alignment algorithm error!");
                backgroundWorker1.CancelAsync();
                return;
            }
        }

        public void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }


        private void ZOut_Scan_Click(object sender, EventArgs e)
        {
            float scan_size = float.Parse(textBox_ScanSize.Text);
            float step_size = float.Parse(textBox_StepSize.Text);
            float threshold = float.Parse(textBox_thresh.Text);
            ZScan("OUT", scan_size, step_size, threshold);
        }


        private void ZIn_Scan_Click(object sender, EventArgs e)
        {
            float scan_size = float.Parse(textBox_ScanSize.Text);
            float step_size = float.Parse(textBox_StepSize.Text);
            float threshold = float.Parse(textBox_thresh.Text);
            ZScan("IN", scan_size, step_size, threshold);
        }

        #region THORLABS_WATCHDOG
        static void StartThorlabsWatchDog()
        {
            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            // Delete the created batch script after execution
            Process pp = new Process();
            // /C closes the terminal after completion. /K for persistant
            pp.StartInfo.Arguments = string.Format("/C ThorlabsAptCleanup {0}", currentProcessName);
            pp.StartInfo.UseShellExecute = false;
            pp.StartInfo.CreateNoWindow = true;
            pp.StartInfo.FileName = "cmd.exe";
            pp.Start();
        }
        #endregion

        private void btnSafeLocation_Click(object sender, EventArgs e)
        {
            try
            {
                btnSafeLocation.Enabled = false;

                if (DialogResult.Yes == MessageBox.Show("This is a dangerous operation, which should not be perforemed during the wafer testing.\nIt should be only used for tool recovery after test failure.\n\nDo you still want to move optical probe to safe location?", "Move To Safe Location", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2))
                    _objSafeLocation.GoToSafeLocation();
                else
                    return;
            }
            catch
            {
                MessageBox.Show("Failed to move optical probe to safe location. Please move it manually.", "Move To Safe Location", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
                return;
            }
            finally
            {
                btnSafeLocation.Enabled = true;
            }
        }

        /// <summary>
        /// wait function for alignment GUI
        /// </summary>
        private void pause(DateTime startTime)
        {
            if (!checkBox1.Checked)
                return;
            
            Color fc = checkBox1.ForeColor;
            try
            {
                const int counter_max = 10;
                int counter = counter_max;

                while (checkBox1.Checked)
                {
                    counter++;
                    if (counter > counter_max)
                    {
                        counter = 0;
                        checkBox1.ForeColor = (checkBox1.ForeColor == Color.White ? checkBox1.ForeColor = Color.Black : checkBox1.ForeColor = Color.White);
                    }

                    Thread.Sleep(100);
                }
                startTime = DateTime.Now;
            }
            catch
            { }
            finally
            {
                checkBox1.ForeColor = fc;
            }
        }

    }

}

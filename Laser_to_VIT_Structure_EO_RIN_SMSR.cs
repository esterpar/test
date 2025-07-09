using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using DataLib;
using Utility;
using InstrumentsLib;
using WaferLevelTestLib;
using InstrumentsLib.Tools.Instruments.SPA;
using WpfGraphService;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Statistics;
using MathLib;
using InstrumentsLib.Tools.Instruments.Attenuator;
using InstrumentsLib.Tools.Instruments.Switch;
using InstrumentsLib.Tools.Instruments.PowerMeter;
using InstrumentsLib.Tools.Instruments.Laser;
using InstrumentsLib.Tools.Instruments.OpticalAlignment;
using InstrumentsLib.Tools.Instruments.OSA;
using InstrumentsLib.Tools.Instruments.ESA;
using InstrumentsLib.Tools.Instruments.O2E;
using System.Collections;
using System.Drawing;
using System.Threading;

namespace AutoSPANMeasLib
{
    public class Laser_to_VIT_Structure_EO_RIN_SMSR : SweepBase
    {
        // Feedback tolerance test converted from matlab version prod_031, core 04

        public const string TESTNAME = "Laser_to_VIT_Structure_EO_RIN_SMSR_V00_01";

        public Laser_to_VIT_Structure_EO_RIN_SMSR(SetupWLT setup)
            : base(setup)
        {
            this._data.TestName = TESTNAME;
            this._data.TestFlowName = TestConstants.FLOWNAME_DC_SCREEN;
            this._data.testDepHeader.Operation = TestConstants.OPER_UNKNOWN;
            Debug.Print("Sweep_V.Sweep_V(SetupWLT setup)");
        }

        public Laser_to_VIT_Structure_EO_RIN_SMSR()
            : base()
        {
            this._data.TestName = TESTNAME;
            this._data.TestFlowName = TestConstants.FLOWNAME_DC_SCREEN;
            this._data.testDepHeader.Operation = TestConstants.OPER_UNKNOWN;
            Debug.Print("Sweep_V.Sweep_V()");
        }

        #region INPUTS
        // Required Inputs
        private double _PD_COMP;
        private double _PD_BIAS;
        private double _VOA_COMP;
        private double _LZR_COMP;
        private List<double> _arAA_BIAS;
        private double _AA_THRESHOLD;
        private bool _bSKIP_ALIGN;
        private List<double> _arLIV_START;
        private List<double> _arLIV_STOP;
        private List<double> _arLIV_STEP;
        private List<double> _arATTN_SETTINGS;
        private double _CENTER_WL;
        private List<double> _arFBT_START;
        private List<double> _arFBT_STOP;
        private List<double> _arFBT_STEP;
        private bool _bFBT_MODE_OSA;
        private bool _bFBT_MODE_ESA;
        private List<double> _arFBT_WINDOW;
        private List<double> _arFBT_SPECS;
        private bool _bFBT_RAW_DATA_OSA;
        private bool _bFBT_RAW_DATA_ESA;
        private List<double> _arSLI_START;
        private List<double> _arSLI_STOP;
        private List<double> _arSLI_STEP;
        private bool _bSLI_MODE_OSA;
        private bool _bSLI_MODE_ESA;
        private List<double> _arSLI_WINDOW;
        private List<double> _arSLI_SPECS;
        private bool _bSLI_RAW_DATA_OSA;
        private bool _bSLI_RAW_DATA_ESA;
        private double _OSA_SPAN;
        private double _OSA_RBW;
        private string _OSA_SENS;
        private double _MOTOR_X;
        private double _MOTOR_Y;

        // Optional Inputs
        private List<double> _arVOA1_START;
        private List<double> _arVOA1_STOP;
        private List<double> _arVOA1_STEP;
        private List<double> _arVOA2_START;
        private List<double> _arVOA2_STOP;
        private List<double> _arVOA2_STEP;
        private List<double> _arREPORT_PWR;
        private List<double> _arREPORT_CUR;
        private List<double> _arSMSR_LIMITS_DB;
        private List<double> _arREPORT_FBT;
        private int _OSA_DWN_SMPL_FLAG;
        private double _OSA_DWN_SMPL_STEP;
        private double _OSA_CLMP_LMT;
        private int _RIN_DWN_SMPL_FLAG;
        private double _RIN_DWN_SMPL_STEP;
        private double _RIN_DWN_SMPL_WINDOW;
        private double _RIN_DWN_SMPL_THRESHOLD;
        private double _RIN_DWN_SMPL_START_FREQ;
        //private bool _bOSA_ESA_DATA_WRITE_FLAG_OSA; // Not used in FBT measurement, legacy from RIN algorithm?
        //private bool _bOSA_ESA_DATA_WRITE_FLAG_ESA; // Not used in FBT measurement, legacy from RIN algorithm?
        private int _ESA_POINTS;
        private double _ESA_STOP_FREQ;
        private double _PD_TIA_RF_GAIN;
        private int _NUM_VTIA_REPEATS;
        private double _LIV_MIN_PWR_LIMIT;
        private List<double> _SPECTRA_WIDTH_SETTINGS;
        private List<double> _SPECTRA_WIDTH_TRANSITION_LIMIT;

        private Dictionary<string, AlignmentTypes> _coupler_types = new Dictionary<string, AlignmentTypes>()
        {
            {"EITM", AlignmentTypes.SMTx}, // POR for TX systems. MM on EITM. Verify if also good for MM on VIT
            {"VIT" , AlignmentTypes.TxOpticalFBT}, // Matlab POR, uses optical tracking. SM on VIT. MM disables tracking
            //{"EIT" , AlignmentTypes.SMTxLowNA} // POR PIAlignSys for edge coupler
            {"EIT" , AlignmentTypes.TRX_SINGLE_CH}, // POR PIAlignSys for edge coupler
            {"SMTx" , AlignmentTypes.SMTx} // 6/24 ester added for WLT-1242. use SMTx, GC or OO???
        };
        private AlignmentTypes _alignmentType;
        private string _COUPLER; // Optical coupler, EITM,VIT
        #endregion

        #region LOCALDATA
        Dictionary<string, double> _params = new Dictionary<string, double> ();
        #endregion
        // Other Data
        private double _fMaxLaserDriveCurrent;  // Called LZR_COMP_CURR in Matlab
        private double _fMaxVOADriveCurrent;    // Called VOA_COMP_CURR in Matlab

        private List<string> _arPins_GNDU;
        private List<string> _arPins_GND2;
        private List<string> _arPins_SMU1;
        private List<string> _arPins_SMU2;
        private List<string> _arPins_SMU3;
        private List<string> _arPins_SMU4;
        private List<string> _arPins_SMU5;
        //private List<string> _arPins_SMU6;

        /* 6/24 edit
        * cntrl+f, keyword match pattern for "override" and "virtual"
        * AutoSPANMeasLibrary inherits to customize functionality of its unique implementation
        * ^customization happens mainly in overriding of its abstract base class
        * we will modify these members and attributes to handle
        * 1) grating coupler output instead of VIT out
        * 2) SMU and matrix pipeline
        * 3) spectral-LI sweeps, while being biased, and measuring RIN, SMSR, 
        * 4) hong wants this using best practices
        */
        public override bool pretest()
        {
            // Validate inputs first.  Use TryParse to read in decimal and exponential format, error on fail to parse
            Debug.Print("Laser_to_VIT_Structure_EO_RIN_SMSR.pretest()");
            base.pretest();

            //_loss = new InsertionLossObj(); // 6/24 ester we dont need insertion loss 
            InitializeDataTables();
            ParseInputs();
            ParseTerminals();
            InitializeOutputs();
            return true;
        }

        #region PRETEST_HELPER
        private void InitializeOutputs()
        {
            Initialize_Scalar_Parameters();
        }

        private void ParseTerminals()
        {
            GetParseListInputValue("PD1N", out _arPins_SMU1);
            GetParseListInputValue("PD2N", out _arPins_SMU2);
            GetParseListInputValue("VOA1P", out _arPins_SMU3);
            GetParseListInputValue("VOA2P", out _arPins_SMU4);
            GetParseListInputValue("LZRP", out _arPins_SMU5);
            GetParseListInputValue("LZRN", out _arPins_GNDU);
            _arPins_GND2 = new List<string>();
            List<string> temp;
            GetParseListInputValue("PD1P", out temp);
            _arPins_GND2.AddRange(temp);
            GetParseListInputValue("PD2P", out temp);
            _arPins_GND2.AddRange(temp);
            GetParseListInputValue("VOA1N", out temp);
            _arPins_GND2.AddRange(temp);
            GetParseListInputValue("VOA2N", out temp);
            _arPins_GND2.AddRange(temp);
        }

        private void ParseInputs()
        {
            GetParseInputValue("PD_COMP", out _PD_COMP);
            GetParseInputValue("PD_BIAS", out _PD_BIAS);
            GetParseInputValue("VOA_COMP", out _VOA_COMP);
            GetParseInputValue("LZR_COMP", out _LZR_COMP);
            GetParseListInputValue("AA_BIAS", out _arAA_BIAS);
            GetParseInputValue("AA_THRESHOLD", out _AA_THRESHOLD, null, 0.8); // Secret default, set onthe fly in Perform Optical Alignment
            GetParseInputValue("SKIP_ALIGN", out _bSKIP_ALIGN);
            GetParseListInputValue("LIV_START", out _arLIV_START);
            GetParseListInputValue("LIV_STOP", out _arLIV_STOP);
            GetParseListInputValue("LIV_STEP", out _arLIV_STEP);
            GetParseListInputValue("ATTN_SETTINGS", out _arATTN_SETTINGS, null, new List<double>() {-10, 4});
            GetParseInputValue("CENTER_WL", out _CENTER_WL);
            GetParseListInputValue("FBT_START", out _arFBT_START);
            GetParseListInputValue("FBT_STOP", out _arFBT_STOP);
            GetParseListInputValue("FBT_STEP", out _arFBT_STEP);
            List<bool> bool_array;
            GetParseListInputValue("FBT_MODE", out bool_array, null, new List<bool>() {true, true});
            if (bool_array.Count != 2)
            {
                throw new Exception(String.Format("Invalid format for FBT_MODE. Expect x,y"));
            }
            _bFBT_MODE_OSA = bool_array.ElementAt(0);
            _bFBT_MODE_ESA = bool_array.ElementAt(1);
            GetParseListInputValue("FBT_WINDOW", out _arFBT_WINDOW, null, new List<double>() { 1, 0.02 });
            GetParseListInputValue("FBT_SPECS", out _arFBT_SPECS, null, new List<double>() { 35, -32.5 });
            GetParseListInputValue("FBT_RAW_DATA", out bool_array, null, new List<bool>() { false, false });
            if (bool_array.Count != 2)
            {
                throw new Exception(String.Format("Invalid format for FBT_RAW_DATA. Expect x,y"));
            }
            _bFBT_RAW_DATA_OSA = bool_array.ElementAt(0);
            _bFBT_RAW_DATA_ESA = bool_array.ElementAt(1);
            GetParseListInputValue("SLI_START", out _arSLI_START);
            GetParseListInputValue("SLI_STOP", out _arSLI_STOP);
            GetParseListInputValue("SLI_STEP", out _arSLI_STEP);
            GetParseListInputValue("SLI_MODE", out bool_array, null, new List<bool>() { true, false });
            if (bool_array.Count != 2)
            {
                throw new Exception(String.Format("Invalid format for SLI_MODE. Expect x,y"));
            }
            _bSLI_MODE_OSA = bool_array.ElementAt(0);
            _bSLI_MODE_ESA = bool_array.ElementAt(1);
            GetParseListInputValue("SLI_WINDOW", out _arSLI_WINDOW, null, new List<double>() { 1.0, 1.0 });
            GetParseListInputValue("SLI_SPECS", out _arSLI_SPECS, null, new List<double>() { 35, -32.5 });
            GetParseListInputValue("SLI_RAW_DATA", out bool_array, null, new List<bool>() { true, false });
            if (bool_array.Count != 2)
            {
                throw new Exception(String.Format("Invalid format for SLI_RAW_DATA. Expect x,y"));
            }
            _bSLI_RAW_DATA_OSA = bool_array.ElementAt(0);
            _bSLI_RAW_DATA_ESA = bool_array.ElementAt(1);

            GetParseInputValue("OSA_SPAN", out _OSA_SPAN);
            GetParseInputValue("OSA_RBW", out _OSA_RBW);
            GetParseInputValue("OSA_SENS", out _OSA_SENS);
            if (!"NORM,MID,HIGH1,HIGH2,HIGH3".Contains(_OSA_SENS))
            {
                throw new Exception(String.Format("Failed to parse OSA_SENS as string: {0}.  Expect NORM, MID, HIGH1, HIGH2, or HIGH3", _OSA_SENS));
            }
            GetParseInputValue("MOTOR_X", out _MOTOR_X);
            GetParseInputValue("MOTOR_Y", out _MOTOR_Y);
            GetParseListInputValue("REPORT_PWR", out _arREPORT_PWR, null, new List<double>() { 1.2 });
            GetParseListInputValue("REPORT_CUR", out _arREPORT_CUR, null, new List<double>() { 0.1 });
            GetParseListInputValue("SMSR_LIMITS_DB", out _arSMSR_LIMITS_DB, null, new List<double>() { 35 });
            GetParseListInputValue("REPORT_FBT", out _arREPORT_FBT, null, new List<double>() { -32.5 });
            GetParseInputValue("OSA_DWN_SMPL_FLAG", out _OSA_DWN_SMPL_FLAG, null, 2);
            GetParseInputValue("OSA_DWN_SMPL_STEP", out _OSA_DWN_SMPL_STEP, null, _OSA_RBW/5.0);
            GetParseInputValue("OSA_CLMP_LMT", out _OSA_CLMP_LMT, null, 50);
            GetParseInputValue("RIN_DWN_SMPL_FLAG", out _RIN_DWN_SMPL_FLAG, null, 1);
            GetParseInputValue("RIN_DWN_SMPL_STEP", out _RIN_DWN_SMPL_STEP, null, 10e9 / 100);
            GetParseInputValue("RIN_DWN_SMPL_WINDOW", out _RIN_DWN_SMPL_WINDOW, null, 1e9);
            GetParseInputValue("RIN_DWN_SMPL_THRESHOLD", out _RIN_DWN_SMPL_THRESHOLD, null, -140); 
            GetParseInputValue("RIN_DWN_SMPL_START_FREQ", out _RIN_DWN_SMPL_START_FREQ, null, 0.5e9);

            // OSA_ESA_DATA_WRITE_FLAG is parsed but not used in this algorithm? Raw data control is set with FBT_RAW_DATA SLI_RAW_DATA
            //List<int> int_array;
            //GetParseListInputValue("OSA_ESA_DATA_WRITE_FLAG", out int_array, null, new List<int>() { 1, 1}); 
            //if (bool_array.Count != 2)
            //{
            //    throw new Exception(String.Format("Invalid format for OSA_ESA_DATA_WRITE_FLAG. Expect x,y"));
            //}
            //_bOSA_ESA_DATA_WRITE_FLAG_OSA = int_array.ElementAt(0) != 0 ; 
            //_bOSA_ESA_DATA_WRITE_FLAG_ESA = int_array.ElementAt(1) != 0; 

            GetParseInputValue("LIV_MIN_PWR_LIMIT", out _LIV_MIN_PWR_LIMIT, null, -5);
            GetParseListInputValue("SPECTRA_WIDTH_SETTING", out _SPECTRA_WIDTH_SETTINGS, null, new List<double>(3) { 10, 20, 30 });
            GetParseListInputValue("SPECTRA_WIDTH_TRANSITION_LIMIT", out _SPECTRA_WIDTH_TRANSITION_LIMIT, null, new List<double>(3) { 70, 80, 90 });
            GetParseInputValue("NUM_VTIA_REPEATS", out _NUM_VTIA_REPEATS, null, 20);
            GetParseListInputValue("VOA1_START", out _arVOA1_START, null, new List<double>() { });
            GetParseListInputValue("VOA1_STOP", out _arVOA1_STOP, null, new List<double>() { });
            GetParseListInputValue("VOA1_STEP", out _arVOA1_STEP, null, new List<double>() { });
            GetParseListInputValue("VOA2_START", out _arVOA2_START, null, new List<double>() { });
            GetParseListInputValue("VOA2_STOP", out _arVOA2_STOP, null, new List<double>() { });
            GetParseListInputValue("VOA2_STEP", out _arVOA2_STEP, null, new List<double>() { });

            // This should be automatically detected in the future
            GetParseInputValue("LZR_COMP_CURR", out _fMaxLaserDriveCurrent, null, 0.3); // 300mA using HPSMU
            GetParseInputValue("VOA_COMP_CURR", out _fMaxVOADriveCurrent, null, 0.2);  // 200mA using MPSMU

            // Secret options throughout matlab code
            GetParseInputValue("ESA_POINTS", out _ESA_POINTS, x=> x >= -1, -1);
            GetParseInputValue("ESA_STOP_FREQ", out _ESA_STOP_FREQ, null, 10e9); 
            GetParseInputValue("PD_TIA_RF_GAIN", out _PD_TIA_RF_GAIN, null, 46.5);
            //GetParseInputValue("COUPLER", out _COUPLER, x => _coupler_types.ContainsKey(x), "VIT");
            GetParseInputValue("COUPLER", out _COUPLER, x => _coupler_types.ContainsKey(x), "GC");
            _alignmentType = _coupler_types[_COUPLER];
        }
        #endregion


        public void Configure_Switch_Matrix_For_Measurement()
        {
            _matrix.DisconnectAll();
            _matrix.ConnectPins(SPA_CONSTANTS.SMU1, _arPins_SMU1);
            _matrix.ConnectPins(SPA_CONSTANTS.SMU2, _arPins_SMU2);
            _matrix.ConnectPins(SPA_CONSTANTS.SMU3, _arPins_SMU3);
            _matrix.ConnectPins(SPA_CONSTANTS.SMU4, _arPins_SMU4);
            _matrix.ConnectPins(SPA_CONSTANTS.SMU5, _arPins_SMU5);
            //_matrix.ConnectPins(SPA_CONSTANTS.SMU6, _arPins_SMU6);
            _matrix.ConnectPins(SPA_CONSTANTS.GND2, _arPins_GND2);
            _matrix.ConnectPins(SPA_CONSTANTS.GNDU, _arPins_GNDU);
            _matrix.executeCachedConnections();
        }

        public override bool execute()
        {
            bool result = true;
            base.execute();
            Debug.Print("Laser_to_VIT_Structure_EO_RIN_SMSR.execute()");

            try
            {
                ConfigureInstruments();

                // Set up instruments, VOA, switch, matrix, etc.
                if (_VOA != null)
                {
                    _VOA.SetWavelength(_CENTER_WL);
                    _VOA.SetRawAttenuation(_arATTN_SETTINGS.ElementAt(VOA.FIXED)); // First Attn setting for LIV
                }
                // how does this not error out??
                //_OptSwitch.Connect("COMMON", "ToAlign");

                // Align
                //Configure_Switch_Matrix_For_Measurement();
                _matrix.DisconnectAll();

                _matrix.ConnectPins(SPA_CONSTANTS.SMU1, _arPins_SMU1); //PD1N
                _matrix.ConnectPins(SPA_CONSTANTS.SMU2, _arPins_SMU2); //PD2N
                _matrix.ConnectPins(SPA_CONSTANTS.SMU3, _arPins_SMU3); //VOA1P
                _matrix.ConnectPins(SPA_CONSTANTS.SMU4, _arPins_SMU4); //VOA2P
                _matrix.ConnectPins(SPA_CONSTANTS.SMU5, _arPins_SMU5); // LZRP
                //_matrix.ConnectPins(SPA_CONSTANTS.SMU6, _arPins_SMU6); // for oo-test, there's no TIA so this errors
                _matrix.ConnectPins(SPA_CONSTANTS.GND2, _arPins_GND2); // Force gnd
                _matrix.ConnectPins(SPA_CONSTANTS.GNDU, _arPins_GNDU); // sens gnd
                _matrix.executeCachedConnections();


                // 6/26 ester. this fn Apply_DC_Stress() is used to biasing the laser. how tell if rev/forwad biasing? which
                Apply_DC_Stress(_PD_BIAS, _PD_BIAS, _arAA_BIAS.ElementAt(1), _arAA_BIAS.ElementAt(2), _arAA_BIAS.ElementAt(0), 0);
                CArrayData myCoordinates = new CArrayData();
                
                if (!_bSKIP_ALIGN)
                {
                    System.DateTime startTime = DateTime.UtcNow;
                    _alignment.MoveCalXY((float) _MOTOR_X, (float) _MOTOR_Y, Motors.ThorLabsOutputMotor);
                    

                    //_alignment.MoveToZ(6080f, Motors.ThorLabsZOutputMotor);
                    pause(1000); // 6/26. ester unit ms, so 1s wait
                    bool lowPassOrFocus = true;

                    /*if (_COUPLER == "EIT")   // 6/26 ester. Y only EIT need LPF? unsure if GC needs
                    { 
                        lowPassOrFocus = true; // no LPF by default
                    }
                    */
                    _alignment.Align(_alignmentType, (float) _AA_THRESHOLD, ref myCoordinates, true, lowPassOrFocus, false);
                    // Align is overloaded 
                    //_alignment.Align(_alignmentType, thres, ref myCoordinates, true, lowPassOrFocus, bRaster1);

                    _params["OPT_ALIGN_TIME"] =  (DateTime.UtcNow - startTime).TotalSeconds;
                }
                bool is_aligned=false;
                double outputpower;
                try
                {
                    if (myCoordinates.arHeader.Contains("Align_Power"))
                    {
                        outputpower = (double)((myCoordinates.colArray[myCoordinates.arHeader.IndexOf("Align_Power")]).ToList()[0]);
                    }
                    else
                    {
                        outputpower = (double)((myCoordinates.colArray[myCoordinates.arHeader.IndexOf("Alignment Power")]).ToList()[0]);
                    }
                    if (Double.IsInfinity(outputpower)){
                        outputpower = ErrorCodes.INITIAL_PARAMETER_VALUE; 
                    }

                    DataRow row = _Active_Align_Coordinates_DataTable.NewRow();

                    // ester. have to link this data table w the header info of output xyz motors of thorlabs--not nanocube header pins
                    
                    row["Motor_Input_X_um"] = 0;
                    row["Motor_Input_Y_um"] = 0; 
                    row["Motor_Input_Z_um"] = 0; 
                    row["Piezo_Input_X_um"] = 0; 
                    row["Piezo_Input_Y_um"] = 0; 
                    row["Piezo_Input_Z_um"] = 0;
                    
                    row["Motor_Output_X_um"] = (double)(myCoordinates.colArray[myCoordinates.arHeader.IndexOf("motor_X_out")].ToList()[0]) * 1000.0;
                    row["Motor_Output_Y_um"] = (double)(myCoordinates.colArray[myCoordinates.arHeader.IndexOf("motor_Y_out")].ToList()[0]) * 1000.0;
                    row["Motor_Output_Z_um"] = (double)(myCoordinates.colArray[myCoordinates.arHeader.IndexOf("motor_Z_out")].ToList()[0]) * 1000.0;
                    
                    row["Piezo_Output_X_um"] = myCoordinates.colArray[myCoordinates.arHeader.IndexOf("nano_X_out")].ToList()[0];
                    row["Piezo_Output_Y_um"] = myCoordinates.colArray[myCoordinates.arHeader.IndexOf("MOTOR_X")].ToList()[0];
                    row["Piezo_Output_Z_um"] = myCoordinates.colArray[myCoordinates.arHeader.IndexOf("nano_Z_out")].ToList()[0];
                    
                    

                    if (myCoordinates.arHeader.Contains("Align_Time"))
                    
                    {
                        row["Activealigntime_seconds"] = myCoordinates.colArray[myCoordinates.arHeader.IndexOf("Align_Time")].ToList()[0];
                    }
                    else
                    {
                        row["Activealigntime_seconds"] = myCoordinates.colArray[myCoordinates.arHeader.IndexOf("Total_Align_Time")].ToList()[0];
                    }
                    row["PowerLevel"] = outputpower; // this shouldnt be -9999.99, if so no power
                    _Active_Align_Coordinates_DataTable.Rows.Add(row);
                    log(string.Format("APT Optical Power after Alignment = {0}", outputpower));
                    is_aligned = outputpower >= (_AA_THRESHOLD - 10.0);
                }
                catch
                {
                    outputpower = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                    DataRow row = _Active_Align_Coordinates_DataTable.NewRow();
                    foreach (string label in _Active_Align_Coordinates_labels)
                    {
                        row[label] = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                    }
                    _Active_Align_Coordinates_DataTable.Rows.Add(row);
                }
                _spa.stop();
                CArrayData ALIGN_DATA = CArrayData.ConvertDataTable(_Active_Align_Coordinates_DataTable);
                saveArrayData("Active_Align_Coordinates", ALIGN_DATA);

                GraphEvent(new GraphEventArgsAddFigure(getDeviceInfoString()));

                // VOA Sweeps
                // ester
                if (_arVOA1_START.Count != 0 && _arVOA1_STOP.Count != 0 && _arVOA1_STEP.Count != 0)
                {
                    Measure_LIV_LZR_VOA1_VOA2(_VOA1_DataTable, _arVOA1_START, _arVOA1_STOP, _arVOA1_STEP, is_aligned);
                    if (_VOA1_DataTable.Rows.Count != 0)
                    {
                        CArrayData VOA1_DATA = CArrayData.ConvertDataTable(_VOA1_DataTable);
                        saveArrayData("VOA_1", VOA1_DATA);
                    }
                }
                if (_arVOA2_START.Count != 0 && _arVOA2_STOP.Count != 0 && _arVOA2_STEP.Count != 0)
                {
                    Measure_LIV_LZR_VOA1_VOA2(_VOA2_DataTable, _arVOA2_START, _arVOA2_STOP, _arVOA2_STEP, is_aligned);
                    if (_VOA2_DataTable.Rows.Count != 0)
                    {
                        CArrayData VOA2_DATA = CArrayData.ConvertDataTable(_VOA2_DataTable);
                        saveArrayData("VOA_2", VOA2_DATA);
                    }
                }
                
                // LIV sweeps
                Measure_LIV_LZR_VOA1_VOA2(_LZR_DataTable, _arLIV_START, _arLIV_STOP, _arLIV_STEP, is_aligned);
                {
                    DataView dv_liv = new DataView(_LZR_DataTable);
                    double voa1_bias = Math.Max(_arLIV_START[1], _arLIV_STOP[1]);
                    double voa2_bias = Math.Max(_arLIV_START[2], _arLIV_STOP[2]);
                    dv_liv.RowFilter = String.Format("Ivoa1_IN = {0} AND Ivoa2_IN = {1}", voa1_bias, voa2_bias);
                    string series_label = String.Format("VOA1 = {0}, VOA2 = {1}", voa1_bias, voa2_bias);
                    GraphDataView(dv_liv, "LIV", series_label, "Ilzr_IN", "Pout_dBm");
                    GraphDataView(dv_liv, "IV", series_label, "Ilzr_IN", "Vlzr_2Wire");
                }

                Dictionary<string, double> param_liv = Analyze_LIV_Data();
                foreach (KeyValuePair<string,double> param in param_liv)
                {
                    _params[param.Key] = param.Value;
                }

                if (_LZR_DataTable.Rows.Count != 0)
                {
                    CArrayData LIV_DATA = CArrayData.ConvertDataTable(_LZR_DataTable);
                    saveArrayData("FBT_LZR", LIV_DATA); // As per request for Lightsource issues -- S. Sethuraman, 07 Nov. 2023
                }
                //GraphEvent(new GraphEventArgsAddFigure(getDeviceInfoString()));
                //GraphEvent(new GraphEventArgsAddLineGraph("LIV", "ILZR", "Power Delta"));
                //List<double> ilzr = getArray("Ilzr_IN", _LZR_DataTable);
                //ilzr.RemoveAt(ilzr.Count-1);
                //List<double> pout = getArray("Pout_mW", _LZR_DataTable);
                //List<double> delta = VecMath.absdiff(pout.ToArray()).ToList();
                //GraphEvent(new GraphEventArgsPlot("", ilzr, delta, WpfGraphService.Styles.MatlabStyle("go-")));

                //ester 7/8, commented this out
                /*
                if (!Is_The_Device_Working(_LIV_MIN_PWR_LIMIT))
                {
                    this.addWaferTParam(_params);
                    return true;
                }
                */
                
                if (_bFBT_MODE_OSA || _bFBT_MODE_ESA)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    Dictionary<string, double> MISC_VARS = Measure_RIN_SMSR_LZR_VOA1_VOA2(param_liv,
                        _FBT_OSA_DataTable, _FBT_ESA_DataTable, _FBT_SMSR_DataTable, _FBT_RIN_DataTable,
                        _arFBT_START, _arFBT_STOP, _arFBT_STEP, _arFBT_WINDOW, _arFBT_SPECS, _bFBT_MODE_OSA, _bFBT_MODE_ESA, true);
                    sw.Stop();
                    log("FBT Algorithm took " + sw.Elapsed.TotalSeconds.ToString());

                    Dictionary<string, double> fbt_param_out = Analyze_FBT_Data(MISC_VARS);
                    foreach (KeyValuePair<string,double> param in fbt_param_out)
                    {
                        _params[param.Key] = param.Value;
                    }

                    if (_bFBT_RAW_DATA_OSA && _FBT_OSA_DataTable.Rows.Count != 0)
                    {
                        CArrayData OSA_RAW_DATA = CArrayData.ConvertDataTable(_FBT_OSA_DataTable);
                        if (_OSA_DWN_SMPL_FLAG == 3)
                        {
                            saveArrayData("FBT_OSA_POR", OSA_RAW_DATA);
                        }
                        if (_OSA_DWN_SMPL_FLAG == 1 || _OSA_DWN_SMPL_FLAG == 2 || _OSA_DWN_SMPL_FLAG == 3)
                        {
                            DataTable DWN_SAMPLED_DATATABLE = CreateDownSampledOsaDatatable(_FBT_OSA_DataTable);
                            CArrayData FBT_OSA_DWN_SAMPLED = CArrayData.ConvertDataTable(DWN_SAMPLED_DATATABLE);
                            saveArrayData("FBT_OSA", FBT_OSA_DWN_SAMPLED);
                        }
                        else
                        {
                            saveArrayData("FBT_OSA", OSA_RAW_DATA);
                        }
                    }

                    if (_bFBT_RAW_DATA_ESA && _FBT_ESA_DataTable.Rows.Count != 0)
                    {
                        CArrayData ESA_RAW_DATA = CArrayData.ConvertDataTable(_FBT_ESA_DataTable);
                        if (_RIN_DWN_SMPL_FLAG == 3)
                        {
                            saveArrayData("FBT_ESA_POR", ESA_RAW_DATA);
                        }
                        if (_RIN_DWN_SMPL_FLAG == 1 || _RIN_DWN_SMPL_FLAG == 2 || _RIN_DWN_SMPL_FLAG == 3)
                        {
                            DataTable DWN_SAMPLED_DATATABLE = CreateDownSampledEsaDatatable(_FBT_ESA_DataTable);
                            CArrayData FBT_ESA_DWN_SAMPLED = CArrayData.ConvertDataTable(DWN_SAMPLED_DATATABLE);
                            saveArrayData("FBT_ESA", FBT_ESA_DWN_SAMPLED);
                        }
                        else
                        {
                            saveArrayData("FBT_ESA", ESA_RAW_DATA);
                        }
                    }
                    if (_FBT_SMSR_DataTable.Rows.Count != 0)
                    {
                        CArrayData SMSR_DATA = CArrayData.ConvertDataTable(_FBT_SMSR_DataTable);
                        saveArrayData("FBT_SMSR", SMSR_DATA);
                    }
                    if (_FBT_RIN_DataTable.Rows.Count != 0)
                    {
                        CArrayData RIN_DATA = CArrayData.ConvertDataTable(_FBT_RIN_DataTable);
                        saveArrayData("FBT_RIN", RIN_DATA);
                    }
                    if (_FBT_VOA1_CUR_DataTable.Rows.Count != 0)
                    {
                        CArrayData VOA1_DATA = CArrayData.ConvertDataTable(_FBT_VOA1_CUR_DataTable); // Raw data created in analysis
                        saveArrayData("FBT_VOA1_CUR", VOA1_DATA);
                    }
                }

                if (_bSLI_MODE_OSA || _bSLI_MODE_ESA)
                {

                    //{
                    //    DataView dv_fbt = new DataView(_SLI_RIN_DataTable);
                    //    GraphWatcher(dv_fbt, "SLI_RIN", "Ilzr_IN", "Ivoa1_IN", "intRIN", 420, 15, true);
                    //    DataView dv_osa = new DataView(_SLI_SMSR_DataTable);
                    //    GraphWatcher(dv_osa, "SLI_SMSR", "Ilzr_IN", "Ivoa1_IN", "SMSR", 420, 15, true);
                    //    GraphWatcher(dv_osa, "SLI_CENTER_DBM", "Ilzr_IN", "Ivoa1_IN", "CENTER_DBM", 420, 15, true);
                    //    GraphWatcher(dv_osa, "SLI_SPECTRAL_WIDTH", "Ilzr_IN", "Ivoa1_IN", "SPECTRA_WIDTH_10DB", 420, 15, true);
                    //}

                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    Dictionary<string,double> MISC_VARS = Measure_RIN_SMSR_LZR_VOA1_VOA2( param_liv,
                         _SLI_OSA_DataTable, _SLI_ESA_DataTable, _SLI_SMSR_DataTable, _SLI_RIN_DataTable,
                        _arSLI_START, _arSLI_STOP, _arSLI_STEP, _arSLI_WINDOW, _arSLI_SPECS, _bSLI_MODE_OSA, _bSLI_MODE_ESA,false);
                    Dictionary<string, double> sli_param_out = Analyze_SLI_Data(MISC_VARS);
                    sw.Stop();
                    log("SLI Algorithm took " + sw.Elapsed.TotalSeconds.ToString());

                    foreach (KeyValuePair<string,double> param in sli_param_out)
                    {
                        _params[param.Key] = param.Value;
                    }
                    if (_bSLI_RAW_DATA_OSA && _SLI_OSA_DataTable.Rows.Count != 0)
                    {
                        CArrayData OSA_RAW_DATA = CArrayData.ConvertDataTable(_SLI_OSA_DataTable);
                        if (_OSA_DWN_SMPL_FLAG == 3)
                        {
                            saveArrayData("SLI_OSA_POR", OSA_RAW_DATA);
                        }
                        if (_OSA_DWN_SMPL_FLAG == 1 || _OSA_DWN_SMPL_FLAG == 2 || _OSA_DWN_SMPL_FLAG == 3)
                        {
                            DataTable DWN_SAMPLED_DATATABLE = CreateDownSampledOsaDatatable(_SLI_OSA_DataTable);
                            CArrayData SLI_OSA_DWN_SAMPLED = CArrayData.ConvertDataTable(DWN_SAMPLED_DATATABLE);
                            saveArrayData("SLI_OSA", SLI_OSA_DWN_SAMPLED);
                        }
                        else
                        {
                            saveArrayData("SLI_OSA", OSA_RAW_DATA);
                        }
                    }
                    if (_bSLI_RAW_DATA_ESA && _SLI_ESA_DataTable.Rows.Count != 0)
                    {
                        CArrayData ESA_RAW_DATA = CArrayData.ConvertDataTable(_SLI_ESA_DataTable);
                        if (_RIN_DWN_SMPL_FLAG == 3)
                        {
                            saveArrayData("SLI_ESA_POR", ESA_RAW_DATA);
                        }
                        if (_RIN_DWN_SMPL_FLAG == 1 || _RIN_DWN_SMPL_FLAG == 2 || _RIN_DWN_SMPL_FLAG == 3)
                        {
                            DataTable DWN_SAMPLED_DATATABLE = CreateDownSampledEsaDatatable(_SLI_ESA_DataTable);
                            CArrayData SLI_ESA_DWN_SAMPLED = CArrayData.ConvertDataTable(DWN_SAMPLED_DATATABLE);
                            saveArrayData("SLI_ESA", SLI_ESA_DWN_SAMPLED);
                        }
                        else
                        {
                            saveArrayData("SLI_ESA", ESA_RAW_DATA);
                        }
                    }
                    if (_SLI_RIN_DataTable.Rows.Count != 0)
                    {
                        CArrayData SLI_RIN_DATA = CArrayData.ConvertDataTable(_SLI_RIN_DataTable);
                        saveArrayData("SLI_RIN", SLI_RIN_DATA);
                    }
                    if (_SLI_SMSR_DataTable.Rows.Count != 0)
                    {
                        CArrayData SLI_SMSR_DATA = CArrayData.ConvertDataTable(_SLI_SMSR_DataTable);
                        saveArrayData("SLI_SMSR", SLI_SMSR_DATA);
                    }
                }
                this.addWaferTParam(_params);
            }
            catch (Exception ex)
            {
                _OptSwitch.Connect("COMMON", "ToPWM"); // Prevent light from reaching O/E incase accidentally left biased. Damage level at ~+4dBm
                result = false;
                this.addWaferTParam(_params);
                log(TESTNAME + ": " + ex.Message);
                //throw ex;
            }

            return result;
        }

        protected IOpticalAlignmentSys _alignment;
        protected InsertionLossObj _loss;  // Insertion loss data from StationInstrumentConfig
        protected IPowerMeter _PWM;
        protected ISwitch _OptSwitch = null;
        protected IAttenuator _VOA = null;
        protected IOSA _OSA;
        protected IESA _ESA;
        protected IO2E _O2E = null;
        protected bool has_queryable_o2e_power = false;

        private void ConfigureInstruments()
        {
            //FIX the ui for missing keys in station config?? TODO
            _PWM = (IPowerMeter)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.PWM];
            _OptSwitch = (ISwitch)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.OPTICAL_SWITCH];
            _alignment = (IOpticalAlignmentSys)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.OPTICAL_ALIGNMENT];

            bool has_o2e = StationHardware.Instance().MapInst.ContainsKey(WaferLevelTestLib.Constants.O2E);
            if (has_o2e)
            {
                _O2E = (IO2E)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.O2E];
                _O2E.Wavelength = _CENTER_WL;
                _O2E.OutputEnabled = true;
            }

            try
            {
                double dummy = _O2E.Power;
                has_queryable_o2e_power = true;
            }
            catch
            {
                has_queryable_o2e_power = false;
            }

            // Set VOA if it exists
            bool has_voa = StationHardware.Instance().MapInst.ContainsKey(WaferLevelTestLib.Constants.ATTEN);
            if (has_voa)
            {
                // Get VOA channel handle
                _VOA = (IAttenuator)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.ATTEN];
            }
            _OSA = (IOSA)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.OSA];
            //_ESA = (IESA)StationHardware.Instance().MapInst[WaferLevelTestLib.Constants.ESA];
        }

        public override bool posttest()
        {
            //_alignment.MoveToZ(6300f, Motors.ThorLabsZOutputMotor);
            if (_COUPLER == "EIT")
            { // TODO clean up use enum maybe?
                _alignment.MoveZ(0.5f, Motors.ThorLabsZOutputMotor);
            }
            base.posttest();
            _spa.stop(); // Turn off SMU outputs
            _matrix.DisconnectAll();
            //Thread.Sleep(1000);
            Debug.Print("Laser_to_VIT_Structure_EO_RIN_SMSR.posttest()");

            CleanUp();
            return true;
        }

        public void CleanUp()
        {
            // GC All the old data
            _VOA1_DataTable = null;
            _VOA2_DataTable = null;
            _LZR_DataTable = null;
            _FBT_OSA_DataTable = null;
            _FBT_ESA_DataTable = null;
            _FBT_VOA1_CUR_DataTable = null;
            _Active_Align_Coordinates_DataTable = null;
            _FBT_SMSR_DataTable = null;
            _FBT_RIN_DataTable = null;
            _SLI_OSA_DataTable = null;
            _SLI_ESA_DataTable = null;
            _SLI_SMSR_DataTable = null;
            _SLI_RIN_DataTable = null;
            _params = new Dictionary<string, double>(); // let GC pickup old one
        }


        public List<string> LZR_labels = new List<string> {
            "Ilzr_IN", "Ilzr","Vlzr_2Wire", "Ivoa1_IN", "Ivoa1", "Vvoa1",
            "Ivoa2_IN", "Ivoa2", "Vvoa2", "Vpd1_IN", "Vpd1",
            "Ipd1", "Vpd2_IN", "Vpd2", "Ipd2", "Pout_dBm", "Pout_mW"
        };

        public List<string> FBTSLI_OSA_labels = new List<string> {
            "Ilzr_IN", "Ivoa1_IN", "Ivoa2_IN", "wavelength_nm", "Pout_dBm",
            "smsr_dB", "peak_nm"
        };

        public List<string> FBTSLI_ESA_labels = new List<string> {
            "Ilzr_IN", "Ivoa1_IN", "Ivoa2_IN", "Vtia","Frequency_Hz","RIN_dBpHz"
        };

        public List<string> FBT_VOA1_CUR_labels = new List<string> {
            "Ilzr_IN", "FBT_VOA1_TRANS_intRIN", "FBT_VOA1_TRANS_CUR",
            "FBT_VOA1_TRANS_RIN_MAX_DB", "FBT_VOA1_TRANS_RIN_MIN_DB",
            "FBT_VOA1_TRANS_VTIA_MIN", "FBT_VOA1_TRANS_VTIA_MEDIAN",
            "FBT_VOA1_TRANS_VTIA_MAX", "SPECTRA_WIDTH_SETTING",
            "SPECTRA_WIDTH_TRANSITION_LIMIT", "FBT_SPCTR_WDTH_VOA1_TRANS_CUR",
            "FBT_SPCTR_WDTH_VOA1_TRANS_WDTH_SPRD"
        };

        public List<string> _Active_Align_Coordinates_labels = new List<string> {
            "Motor_Input_X_um", "Motor_Input_Y_um", "Motor_Input_Z_um",
            "Piezo_Input_X_um", "Piezo_Input_Y_um", "Piezo_Input_Z_um",
            "Motor_Output_X_um", "Motor_Output_Y_um", "Motor_Output_Z_um",
            "Piezo_Output_X_um", "Piezo_Output_Y_um", "Piezo_Output_Z_um",
            "Activealigntime_seconds", "PowerLevel"
        };

        public List<string> _FBT_SMSR_labels = new List<string> {
            "Ilzr_IN", "Ilzr", "Vlzr_2Wire", "Ivoa1_IN", "Ivoa1", "Vvoa1",
            "Ivoa2_IN", "Ivoa2", "Vvoa2", "Vpd1_IN", "Vpd1", "Ipd1",
            "Vpd2_IN", "Vpd2", "Ipd2", "MIN_SMSR_OVER_0P2", "CENTER_WL", "CENTER_DBM",
            "MODE_SEPARATION", "SMSR", "MODE_SEPARATION_2", "SMSR_2",
            "MODE_SEPARATION_3", "SMSR_3", "MODE_SEPARATION_4", "SMSR_4",
            "MODE_SEPARATION_5", "SMSR_5", "MODE_SEPARATION_6", "SMSR_6",
            "MODE_SEPARATION_7", "SMSR_7", "MODE_SEPARATION_8", "SMSR_8",
            "MODE_SEPARATION_9", "SMSR_9",
            "SPECTRA_WIDTH_10DB", "SPECTRA_WIDTH_20DB", "SPECTRA_WIDTH_30DB",
        };

        public List<string> _FBT_RIN_labels = new List<string> {
            "Ilzr_IN", "Ilzr", "Vlzr_2Wire", "Ivoa1_IN", "Ivoa1", "Vvoa1",
            "Ivoa2_IN", "Ivoa2", "Vvoa2", "Vpd1_IN", "Vpd1", "Ipd1",
            "Vpd2_IN", "Vpd2", "Ipd2", "Vtia", "intRIN"
        };

        public DataTable _VOA1_DataTable;
        public DataTable _VOA2_DataTable;
        public DataTable _LZR_DataTable;
        public DataTable _FBT_OSA_DataTable;
        public DataTable _FBT_ESA_DataTable;
        public DataTable _Active_Align_Coordinates_DataTable;
        public DataTable _FBT_RIN_DataTable;
        public DataTable _FBT_SMSR_DataTable;
        public DataTable _FBT_VOA1_CUR_DataTable;
        public DataTable _SLI_OSA_DataTable;
        public DataTable _SLI_ESA_DataTable;
        public DataTable _SLI_SMSR_DataTable;
        public DataTable _SLI_RIN_DataTable;
        public void InitializeDataTables()
        {
            _LZR_DataTable = new DataTable("LZR Data");
            _VOA1_DataTable = new DataTable("VOA1 Data");
            _VOA2_DataTable = new DataTable("VOA2 Data");
            LZR_labels.ForEach(label =>
            {
                _LZR_DataTable.Columns.Add(label, typeof(double));
                _VOA1_DataTable.Columns.Add(label, typeof(double)); // Same as LIV Label
                _VOA2_DataTable.Columns.Add(label, typeof(double)); // Same as LIV label 
            });
            _FBT_OSA_DataTable = new DataTable("FBT OSA Data");
            _SLI_OSA_DataTable = new DataTable("SLI OSA Data");
            FBTSLI_OSA_labels.ForEach(label =>
            {
                _FBT_OSA_DataTable.Columns.Add(label, typeof(double));
                _SLI_OSA_DataTable.Columns.Add(label, typeof(double));
            });
            _FBT_ESA_DataTable = new DataTable("FBT ESA Data");
            _SLI_ESA_DataTable = new DataTable("SLI ESA Data");
            FBTSLI_ESA_labels.ForEach(label =>
            {
                _FBT_ESA_DataTable.Columns.Add(label, typeof(double));
                _SLI_ESA_DataTable.Columns.Add(label, typeof(double));
            });
            _FBT_VOA1_CUR_DataTable = new DataTable("FBT VOA1 CUR Data");
            FBT_VOA1_CUR_labels.ForEach(label => _FBT_VOA1_CUR_DataTable.Columns.Add(label, typeof(double)));
            _Active_Align_Coordinates_DataTable = new DataTable("Active Align Coordinates Data");
            _Active_Align_Coordinates_labels.ForEach(label => _Active_Align_Coordinates_DataTable.Columns.Add(label, typeof(double)));
            _FBT_SMSR_DataTable = new DataTable("SMSR Data");
            _SLI_SMSR_DataTable = new DataTable("SMSR Data");
            _FBT_SMSR_labels.ForEach(label =>
            {
                _FBT_SMSR_DataTable.Columns.Add(label, typeof(double));
                _SLI_SMSR_DataTable.Columns.Add(label, typeof(double)); // Shares same label as FBT
            });
            _FBT_RIN_DataTable = new DataTable("RIN Data");
            _SLI_RIN_DataTable = new DataTable("RIN Data");
            _FBT_RIN_labels.ForEach(label =>
            {
                _FBT_RIN_DataTable.Columns.Add(label, typeof(double));
                _SLI_RIN_DataTable.Columns.Add(label, typeof(double)); // Shares same label as FBT
            });
        }

        public Dictionary<string, double> Analyze_SLI_Data(Dictionary<string, double> MISC_VARS)
        {
            Dictionary<string, double> param_out = new Dictionary<string, double>();

            param_out["SLI_ESA_REF_MED_DBM"] = MISC_VARS["_ESA_REF_MED_DBM"];
            param_out["SLI_VTIA_PRE_SWEEP"] = MISC_VARS["_VTIA_PRE_SWEEP"];
            param_out["SLI_VTIA_POST_SWEEP"] = MISC_VARS["_VTIA_POST_SWEEP"];
            param_out["SLI_VTIA_PERC_CHANGE"] = MISC_VARS["_VTIA_PERC_CHANGE"];

            // % RIN_OUTPUT
            if (_SLI_RIN_DataTable.Rows.Count != 0)
            {
                List<double> intrin = getArray("intRIN", _SLI_RIN_DataTable);
                List<double> vtia = getArray("Vtia", _SLI_RIN_DataTable);
                param_out.Add("SLI_RIN_MAX_DB", intrin.Max());
                param_out.Add("SLI_RIN_MAX_VTIA", vtia.ElementAt(intrin.IndexOf(intrin.Max())));
                param_out.Add("SLI_RIN_MIN_DB", intrin.Min());
            }

            if ( _SLI_SMSR_DataTable.Rows.Count == 0)
            {
                return param_out;
            }

            List<double> smsr_min_smsr_over_0p2 = getArray("MIN_SMSR_OVER_0P2", _SLI_SMSR_DataTable);
            List<double> smsr_ilzr_in = getArray("Ilzr_IN", _SLI_SMSR_DataTable);
            List<double> smsr_mode_separation = getArray("MODE_SEPARATION", _SLI_SMSR_DataTable);
            List<double> smsr_center_wl = getArray("CENTER_WL", _SLI_SMSR_DataTable);
            param_out.Add("SLI_MIN_SMSR_OVER_0.2", smsr_min_smsr_over_0p2.Min());
            int min_index = smsr_min_smsr_over_0p2.FindIndex(x => x == smsr_min_smsr_over_0p2.Min());
            param_out.Add("SLI_CUR@MIN_SMSR_OVER_0.2", smsr_ilzr_in.ElementAt(min_index));
            param_out.Add("SLI_MODE_SEP@MIN_SMSR_OVER_0.2", smsr_mode_separation.ElementAt(min_index));
            param_out.Add("SLI_INITIAL_WAVELENGTH", smsr_center_wl.First());
            param_out.Add("SLI_FINAL_WAVELENGTH", smsr_center_wl.Last());
            param_out.Add("SLI_MAX_WAVELENGTH", smsr_center_wl.Max());
            param_out.Add("SLI_MIN_WAVELENGTH", smsr_center_wl.Min());

            List<double> smsr_center_dbm = getArray("CENTER_DBM", _SLI_SMSR_DataTable);
            param_out.Add("SLI_OSA_MAX_PWR_DIP", ErrorCodes.INVALID_POST_CALCULATION_VALUE);
            if (smsr_center_dbm.Count == 1)
            {
                param_out["SLI_OSA_MAX_PWR_DIP"] = 0;
            }
            else if (smsr_center_dbm.Count > 1)
            {
                List<double> smsr_center_dbm_delta = new List<double>(smsr_center_dbm.Count - 1);
                for (int i = 0; i < smsr_center_dbm.Count - 1; i++)
                {
                    smsr_center_dbm_delta.Add(smsr_center_dbm.ElementAt(i + 1) - smsr_center_dbm.ElementAt(i));
                }
                param_out["SLI_OSA_MAX_PWR_DIP"] = smsr_center_dbm_delta.Min(); // Most negative
            }
            param_out.Add("SLI_OSA_MAX_PWR", smsr_center_dbm.Max());
            param_out.Add("SLI_OSA_MIN_PWR", smsr_center_dbm.Min());

            param_out.Add("SLI_OSA_MAX_WL_JUMP", ErrorCodes.INVALID_POST_CALCULATION_VALUE);
            if (smsr_center_wl.Count == 0)
            {
                param_out["SLI_OSA_MAX_WL_JUMP"] = 0;
            }
            else
            {
                List<double> smsr_center_wl_absdelta = new List<double>(smsr_center_wl.Count - 1);
                for (int i = 0; i < smsr_center_wl.Count - 1; i++)
                {
                    smsr_center_wl_absdelta.Add(Math.Abs(smsr_center_wl.ElementAt(i + 1) - smsr_center_wl.ElementAt(i)));
                }
                if (smsr_center_wl_absdelta.Count >= 0)
                {
                    param_out["SLI_OSA_MAX_WL_JUMP"] = smsr_center_wl_absdelta.Max();
                }
            }

            // % Report first & last fail current for SMSR levels
            foreach(double smsr_limit in _arSMSR_LIMITS_DB)
            {
                int firstfailindex = smsr_min_smsr_over_0p2.FindIndex(x=> x < smsr_limit);
                int lastfailindex = smsr_min_smsr_over_0p2.FindLastIndex(x=> x < smsr_limit);

                string suffix = "";
                if (Math.Abs(Math.Round(smsr_limit) - smsr_limit) < 0.001)
                {
                    suffix += String.Format("_{0:0}DB", smsr_limit);
                }
                else
                {
                    suffix += String.Format("_{0:0.#}DB", smsr_limit);
                }

                param_out["SLI_SMSR_FIRST_FAIL_VAL" + suffix] = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_out["SLI_SMSR_FIRST_FAIL_CUR" + suffix] = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_out["SLI_SMSR_FIRST_FAIL_SEP" + suffix] = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_out["SLI_SMSR_LAST_FAIL_VAL" + suffix] = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_out["SLI_SMSR_LAST_FAIL_CUR" + suffix] = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_out["SLI_SMSR_LAST_FAIL_SEP" + suffix] = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                if (firstfailindex != -1)
                {
                    param_out["SLI_SMSR_FIRST_FAIL_VAL" + suffix] = smsr_min_smsr_over_0p2.ElementAt(firstfailindex);
                    param_out["SLI_SMSR_FIRST_FAIL_CUR" + suffix] = smsr_ilzr_in.ElementAt(firstfailindex);
                    param_out["SLI_SMSR_FIRST_FAIL_SEP" + suffix] = smsr_mode_separation.ElementAt(firstfailindex);
                }
                if (lastfailindex != -1)
                {
                    param_out["SLI_SMSR_LAST_FAIL_VAL" + suffix] = smsr_min_smsr_over_0p2.ElementAt(lastfailindex);
                    param_out["SLI_SMSR_LAST_FAIL_CUR" + suffix] = smsr_ilzr_in.ElementAt(lastfailindex);
                    param_out["SLI_SMSR_LAST_FAIL_SEP" + suffix] = smsr_mode_separation.ElementAt(lastfailindex);
                }
            }

            return param_out;
        }

        public Dictionary<string, double> Analyze_FBT_Data(Dictionary<string, double> MISC_VARS)
        {
            // TODO add SKIPS if missing FBT oR SMSR Data
            Dictionary<string, double> param_out = new Dictionary<string, double>();
            List<double> ilzr = getArray("Ilzr_IN", _FBT_RIN_DataTable);
            List<double> ivoa2 = getArray("Ivoa2_IN", _FBT_RIN_DataTable);
            List<double> intrin = getArray("intRIN", _FBT_RIN_DataTable);
            List<double> vtia = getArray("Vtia", _FBT_RIN_DataTable);

            param_out["FBT_ESA_REF_MED_DBM"] = MISC_VARS["_ESA_REF_MED_DBM"];
            param_out["FBT_VTIA_PRE_SWEEP"] = MISC_VARS["_VTIA_PRE_SWEEP"];
            param_out["FBT_VTIA_POST_SWEEP"] = MISC_VARS["_VTIA_POST_SWEEP"];
            param_out["FBT_VTIA_PERC_CHANGE"] = MISC_VARS["_VTIA_PERC_CHANGE"];

            // % RIN_OUTPUT
            List<Dictionary<string, double>> fbt_voa1_cur_partial = new List<Dictionary<string, double>>();
            // % Report pass current for FBT levels
            double start = _arFBT_START.ElementAt(0);
            double stop = _arFBT_STOP.ElementAt(0);
            double step = Math.Abs(_arFBT_STEP.ElementAt(0));
            if (stop < start)
            {
                double temp = start;
                start = stop;
                stop = temp;
            }
            int numsteps = (int)Math.Truncate((stop - start) / step) + 1;
            string prefix;
            string suffix;

            if (_FBT_RIN_DataTable.Rows.Count != 0)
            {
                param_out.Add("FBT_RIN_MAX_DB", intrin.Max());
                param_out.Add("FBT_RIN_MAX_VTIA", vtia.ElementAt(intrin.IndexOf(intrin.Max())));
                param_out.Add("FBT_RIN_MIN_DB", intrin.Min());

                prefix = "FBT_VOA1_TRANS_";
                for (double count = start; count < numsteps; count++)
                {
                    double bias_a = count * step;
                    double bias_ma = count * step * 1000.0;

                    foreach (double fbt_threshold in _arREPORT_FBT)
                    {
                        suffix = "";
                        // Append FBT report 
                        if (Math.Abs(Math.Round(fbt_threshold) - fbt_threshold) < 0.001)
                        {
                            suffix += String.Format("@{0:0}DB", fbt_threshold);
                        }
                        else
                        {
                            suffix += String.Format("@{0:0.#}DB", fbt_threshold);
                        }
                        // Append Bias Current Report;
                        if (Math.Abs(Math.Round(bias_ma) - bias_ma) < 0.001)
                        {
                            suffix += String.Format("@{0:0}MA", bias_ma);
                        }
                        else
                        {
                            suffix += String.Format("@{0:0.#}MA", bias_ma);
                        }

                        DataView DataByIlzr = new DataView(_FBT_RIN_DataTable);
                        DataByIlzr.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa2_IN = {1}", bias_a, ivoa2.Min());

                        List<double> ilzr_filtered = getColumnFromDataView(DataByIlzr, "Ilzr_IN");
                        List<double> ivoa1_filtered = getColumnFromDataView(DataByIlzr, "Ivoa1_IN");
                        List<double> ivoa2_filtered = getColumnFromDataView(DataByIlzr, "Ivoa2_IN");
                        List<double> intrin_filtered = getColumnFromDataView(DataByIlzr, "intRIN");
                        List<double> vtia_filtered = getColumnFromDataView(DataByIlzr, "Vtia");

                        if (ilzr_filtered.Count != 0 && ivoa2_filtered.Count != 0)
                        {
                            List<double> analysis_results = IntRin_Current_Calculation(ivoa1_filtered, intrin_filtered, fbt_threshold);
                            if (analysis_results.ElementAt(0) == 0)
                            {
                                param_out.Add(prefix + "CUR" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            }
                            else
                            {
                                param_out.Add(prefix + "CUR" + suffix, analysis_results.ElementAt(0));
                            }
                            param_out.Add(prefix + "RIN_MAX_DB" + suffix, intrin_filtered.Max());
                            param_out.Add(prefix + "RIN_MIN_DB" + suffix, intrin_filtered.Min());
                            param_out.Add(prefix + "VTIA_MIN" + suffix, vtia_filtered.Min());
                            param_out.Add(prefix + "VTIA_MEDIAN" + suffix, vtia_filtered.Median());
                            param_out.Add(prefix + "VTIA_MAX" + suffix, vtia_filtered.Max());
                        }
                        else
                        {
                            param_out.Add(prefix + "CUR" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            param_out.Add(prefix + "RIN_MAX_DB" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            param_out.Add(prefix + "RIN_MIN_DB" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            param_out.Add(prefix + "VTIA_MIN" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            param_out.Add(prefix + "VTIA_MEDIAN" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            param_out.Add(prefix + "VTIA_MAX" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                        }

                        // Make a temporary structure to store all RIN parameters for FBT_VOA1_CUR table
                        Dictionary<string, double> temp_row = new Dictionary<string, double>();
                        temp_row.Add("Ilzr_IN", bias_a);
                        temp_row.Add("FBT_VOA1_TRANS_intRIN", fbt_threshold);
                        temp_row.Add("FBT_VOA1_TRANS_CUR", param_out[prefix + "CUR" + suffix]);
                        temp_row.Add("FBT_VOA1_TRANS_RIN_MAX_DB", param_out[prefix + "RIN_MAX_DB" + suffix]);
                        temp_row.Add("FBT_VOA1_TRANS_RIN_MIN_DB", param_out[prefix + "RIN_MIN_DB" + suffix]);
                        temp_row.Add("FBT_VOA1_TRANS_VTIA_MIN", param_out[prefix + "VTIA_MIN" + suffix]);
                        temp_row.Add("FBT_VOA1_TRANS_VTIA_MEDIAN", param_out[prefix + "VTIA_MEDIAN" + suffix]);
                        temp_row.Add("FBT_VOA1_TRANS_VTIA_MAX", param_out[prefix + "VTIA_MAX" + suffix]);
                        fbt_voa1_cur_partial.Add(temp_row);
                    }
                }
            }

            // % SMSR
            if (_FBT_SMSR_DataTable.Rows.Count != 0)
            {
                List<double> smsr_min_smsr_over_0p2 = getArray("MIN_SMSR_OVER_0P2", _FBT_SMSR_DataTable);
                List<double> smsr_ilzr_in = getArray("Ilzr_IN", _FBT_SMSR_DataTable);
                List<double> smsr_mode_separation = getArray("MODE_SEPARATION", _FBT_SMSR_DataTable);
                List<double> smsr_center_wl = getArray("CENTER_WL", _FBT_SMSR_DataTable);
                List<double> smsr_center_dbm = getArray("CENTER_DBM", _FBT_SMSR_DataTable);
                List<double> smsr_voa2_in = getArray("Ivoa2_IN", _FBT_SMSR_DataTable);

                param_out.Add("FBT_MIN_SMSR_OVER_0.2", smsr_min_smsr_over_0p2.Min());
                int min_index = smsr_min_smsr_over_0p2.FindIndex(x => x == smsr_min_smsr_over_0p2.Min());
                param_out.Add("FBT_CUR@MIN_SMSR_OVER_0.2", smsr_ilzr_in.ElementAt(min_index));
                param_out.Add("FBT_MODE_SEP@MIN_SMSR_OVER_0.2", smsr_mode_separation.ElementAt(min_index));
                param_out.Add("FBT_INITIAL_WAVELENGTH", smsr_center_wl.First());
                param_out.Add("FBT_FINAL_WAVELENGTH", smsr_center_wl.Last());
                param_out.Add("FBT_MAX_WAVELENGTH", smsr_center_wl.Max());
                param_out.Add("FBT_MIN_WAVELENGTH", smsr_center_wl.Min());

                param_out.Add("FBT_OSA_MAX_PWR_DIP", ErrorCodes.INVALID_POST_CALCULATION_VALUE);
                if (smsr_center_dbm.Count == 1)
                {
                    param_out["FBT_OSA_MAX_PWR_DIP"] = 0;
                }
                else if (smsr_center_dbm.Count > 1)
                {
                    List<double> smsr_center_dbm_delta = new List<double>(smsr_center_dbm.Count - 1);
                    for (int i = 0; i < smsr_center_dbm.Count - 1; i++)
                    {
                        smsr_center_dbm_delta.Add(smsr_center_dbm.ElementAt(i + 1) - smsr_center_dbm.ElementAt(i));
                    }
                    param_out["FBT_OSA_MAX_PWR_DIP"] = smsr_center_dbm_delta.Min(); // Most negative
                }

                param_out.Add("FBT_OSA_MAX_PWR", smsr_center_dbm.Max());
                param_out.Add("FBT_OSA_MIN_PWR", smsr_center_dbm.Min());

                param_out.Add("FBT_OSA_MAX_WL_JUMP", ErrorCodes.INVALID_POST_CALCULATION_VALUE);
                if (smsr_center_wl.Count == 0)
                {
                    param_out["FBT_OSA_MAX_WL_JUMP"] = 0;
                }
                else
                {
                    List<double> smsr_center_wl_absdelta = new List<double>(smsr_center_wl.Count - 1);
                    for (int i = 0; i < smsr_center_wl.Count - 1; i++)
                    {
                        smsr_center_wl_absdelta.Add(Math.Abs(smsr_center_wl.ElementAt(i + 1) - smsr_center_wl.ElementAt(i)));
                    }
                    if (smsr_center_wl_absdelta.Count >= 0)
                    {
                        param_out["FBT_OSA_MAX_WL_JUMP"] = smsr_center_wl_absdelta.Max();
                    }
                }

                prefix = "FBT_SPCTR_WDTH_VOA1_TRANS_";
                int fbt_i = 0;
                for (double count = start; count < numsteps; count++)
                {
                    double bias_a = count * step;
                    double bias_ma = count * step * 1000.0;

                    foreach (double sws in _SPECTRA_WIDTH_SETTINGS)
                    {
                        foreach (double stl in _SPECTRA_WIDTH_TRANSITION_LIMIT)
                        {
                            suffix = String.Format("@{0:0}DB@{1:0}PRCNT", sws, stl);
                            if (Math.Abs(Math.Round(bias_ma) - bias_ma) < 0.001)
                            {
                                suffix += String.Format("@{0:0}MA", bias_ma);
                            }
                            else
                            {
                                suffix += String.Format("@{0:0.#}MA", bias_ma);
                            }

                            // Spectra Width Current Calculation
                            // Current which is above percentage of data i.e 80% -> 20%?
                            DataView dv_spectra_width_cc = new DataView(_FBT_SMSR_DataTable);
                            dv_spectra_width_cc.Sort = "Ivoa1_IN ASC";
                            dv_spectra_width_cc.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa2_IN = {1}", bias_a, ivoa2.Min());
                            List<double> spectra_width_cc = getColumnFromDataView(dv_spectra_width_cc, String.Format("SPECTRA_WIDTH_{0:0}DB", sws));
                            int num_invalid_pts = spectra_width_cc.FindAll(x =>
                                x == ErrorCodes.INITIAL_PARAMETER_VALUE || x == ErrorCodes.INVALID_POST_CALCULATION_VALUE).Count;
                            if (num_invalid_pts > 0 || spectra_width_cc.Count == 0)
                            {
                                param_out.Add(prefix + "CUR" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            }
                            else
                            {
                                double filter = (100.0 - stl) / 100.0 *
                                    (spectra_width_cc.Max() - spectra_width_cc.Min()) + spectra_width_cc.Min();
                                dv_spectra_width_cc.RowFilter = String.Format(
                                    "Ilzr_IN = {0} AND Ivoa2_IN = {1} AND {2} >= {3}", bias_a, ivoa2.Min(),
                                    String.Format("SPECTRA_WIDTH_{0:0}DB", sws), filter);
                                List<double> data = getColumnFromDataView(dv_spectra_width_cc, "Ivoa1_IN");
                                if (data.Count != 0)
                                {
                                    param_out.Add(prefix + "CUR" + suffix, data.Max());
                                }
                            }

                            DataView dv_spectra_width = new DataView(_FBT_SMSR_DataTable);
                            dv_spectra_width.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa2_IN = {1}", bias_a, ivoa2.Min());
                            List<double> spectra_wdth = getColumnFromDataView(dv_spectra_width, String.Format("SPECTRA_WIDTH_{0:0}DB", sws));
                            if (spectra_wdth.Count == 0)
                            {
                                param_out.Add(prefix + "WDTH_SPRD" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                            }
                            else
                            {
                                param_out.Add(prefix + "WDTH_SPRD" + suffix, spectra_wdth.Max() - spectra_wdth.Min());
                            }

                            if (fbt_voa1_cur_partial.Count != 0 && fbt_i < fbt_voa1_cur_partial.Count())
                            {
                                DataRow row = _FBT_VOA1_CUR_DataTable.NewRow();
                                row["Ilzr_IN"] = fbt_voa1_cur_partial[fbt_i]["Ilzr_IN"];
                                row["FBT_VOA1_TRANS_intRIN"] = fbt_voa1_cur_partial[fbt_i]["FBT_VOA1_TRANS_intRIN"];
                                row["FBT_VOA1_TRANS_CUR"] = fbt_voa1_cur_partial[fbt_i]["FBT_VOA1_TRANS_CUR"];
                                row["FBT_VOA1_TRANS_RIN_MAX_DB"] = fbt_voa1_cur_partial[fbt_i]["FBT_VOA1_TRANS_RIN_MAX_DB"];
                                row["FBT_VOA1_TRANS_RIN_MIN_DB"] = fbt_voa1_cur_partial[fbt_i]["FBT_VOA1_TRANS_RIN_MIN_DB"];
                                row["FBT_VOA1_TRANS_VTIA_MIN"] = fbt_voa1_cur_partial[fbt_i]["FBT_VOA1_TRANS_VTIA_MIN"];
                                row["FBT_VOA1_TRANS_VTIA_MEDIAN"] = fbt_voa1_cur_partial[fbt_i]["FBT_VOA1_TRANS_VTIA_MEDIAN"];
                                row["FBT_VOA1_TRANS_VTIA_MAX"] = fbt_voa1_cur_partial[fbt_i]["FBT_VOA1_TRANS_VTIA_MAX"];

                                row["SPECTRA_WIDTH_SETTING"] = sws;
                                row["SPECTRA_WIDTH_TRANSITION_LIMIT"] = stl;
                                row["FBT_SPCTR_WDTH_VOA1_TRANS_CUR"] = param_out[prefix + "CUR" + suffix];
                                row["FBT_SPCTR_WDTH_VOA1_TRANS_WDTH_SPRD"] = param_out[prefix + "WDTH_SPRD" + suffix];
                                _FBT_VOA1_CUR_DataTable.Rows.Add(row);
                            }
                        }
                    }
                    fbt_i++;
                }
            }
            return param_out;
        }

        public Dictionary<string, double> Analyze_LIV_Data()
        {
            DataView dv = new DataView(_LZR_DataTable);
            List<double> ivoa1_in = getArray("Ivoa1_IN", _LZR_DataTable); // TODO clean up mix up of getArray and getColumnFromDataView
            List<double> ivoa2_in = getArray("Ivoa2_IN", _LZR_DataTable);
            dv.RowFilter = String.Format("Ivoa1_IN = {0} AND Ivoa2_IN = {1}", ivoa1_in.Max(), ivoa2_in.Min());
            Dictionary<string, double> param_out = new Dictionary<string, double>();
            Dictionary<string, double> local_params;

            // % Probing health Monitor for LZR
            List<double> ilzr_original = getColumnFromDataView(dv, "Ilzr_IN");
            double range = ilzr_original.Max() - ilzr_original.Min();
            double dense_step = Math.Sign(range) * Math.Min((VecMath.absdiff(ilzr_original.ToArray()).Min()), 0.001);
            List<double> ilzr = CreateSweepList(ilzr_original.First(), ilzr_original.Last(), dense_step); // Matlab compatible, slighly less precise laser bias list


            List<double> v2wire_orig = getColumnFromDataView(dv, "Vlzr_2Wire");
            List<double> v2wire = VecMath.interp1(new DenseVector(ilzr_original.ToArray()), new DenseVector(v2wire_orig.ToArray()), new DenseVector(ilzr.ToArray())).ToList();
            decimal max_ilzr_in = (decimal)ilzr.Max();
            Parametric_Analysis pa = new Parametric_Analysis();
            List<double> Analysis_range = new List<double> { (double)((decimal)0.1 * max_ilzr_in), (double)(max_ilzr_in) };
            local_params = pa.Parametric_Analysis_2D(ilzr.ToArray(), v2wire.ToArray(), "V-I", Analysis_range);
            param_out.Add("LZR_VI_MAX_V", local_params["MAX_V"]);
            param_out.Add("LZR_VI_SUM_ABS_DV", local_params["SUM_ABS_DV"]);

            // % Calculate Threshold Current
            Ith_Algorithms la = new Ith_Algorithms();
            List<double> pout_orig = getColumnFromDataView(dv, "Pout_mW");
            List<double> pout = VecMath.interp1(new DenseVector(ilzr_original.ToArray()), new DenseVector(pout_orig.ToArray()), new DenseVector(ilzr.ToArray())).ToList();
            List<double> ipd = new List<double>(pout.Count);
            for (int i = 0; i < pout.Count; i++)
            {
                ipd.Add((double)((decimal)pout[i] / (decimal)1000.0 * (decimal)(0.85)));
            }
            local_params = la.Laser_Ith_Algorithm_3(ipd.ToArray(), ilzr.ToArray()); // Potential issues with mismatch for future engineer, unhandled short
            //if (local_params.ContainsValue(ErrorCodes.INVALID_POST_CALCULATION_VALUE))
            //{
            //    // Abort test if threshold reports -9999
            //    throw new Exception("Monitor photodiode is shorted, unhandled event");
            //}
            local_params.Remove("THRESH_ERROR_CURV");
            local_params.Remove("THRESH_ERROR_RSQ");
            foreach (KeyValuePair<string, double> param in local_params)
            {
                param_out.Add(param.Key, param.Value);
            }

            // % Threshold current should be more than 0mA and less than the maximum laser bias.
            List<double> th = new List<double>(local_params.Count);
            th.Add((double)local_params["THRESH_FIT_CURVE"]);
            th.Add((double)local_params["THRESH_FIT_RSQ"]);
            th.Add((double)local_params["THRESH_CURV"]);
            th.Add((double)local_params["THRESH_RSQ"]);
            double median = th.Median(); // need one that matches
            List<double> thresh_min = new List<double> { median, (double)max_ilzr_in };
            List<double> thresh_max = new List<double> { 0, thresh_min.Min() };
            double threshold = thresh_max.Max();
            param_out.Add("THRESHOLD", threshold);

            // % Calculate laser ideality factor and resistance
            // TODO: Switch to non-static method for NelderMeadSolver
            NelderMeadSolverStatic.Tolerance = .0001;
            NelderMeadSolverStatic.MaxIterations = 100000000;
            NelderMeadSolverStatic.MaxFunctionEvaluations = 10000;
            MyFit myfit = new MyFit();
            int usable_data = ilzr.FindIndex(x=> x>= 0.001);
            MyFit.I = ilzr.GetRange(usable_data, ilzr.Count - usable_data);
            MyFit.V = v2wire.GetRange(usable_data, v2wire.Count - usable_data);
            var coeff = NelderMeadSolverStatic.Solve(myfit.evaluate, new[] { 3, 1.5 });
            param_out.Add("LASER_RS", coeff[0]);
            param_out.Add("LZR_IDEALITY", coeff[1]);

            List<double> pout_dbm_orig = getColumnFromDataView(dv, "Pout_dBm");
            List<double> pout_dbm = VecMath.interp1(new DenseVector(ilzr_original.ToArray()), new DenseVector(pout_dbm_orig.ToArray()), new DenseVector(ilzr.ToArray())).ToList();
            param_out.Add("V_TURNON", v2wire.ElementAt(1));
            param_out.Add("MAXPOWER", pout_dbm.Max());

            // Power L-I
            local_params = pa.Parametric_Analysis_2D(ilzr.ToArray(), pout_dbm.ToArray(), "Power L-I", new List<double>());
            param_out.Add("LZR_LI_MAX_POWER_I", local_params["MAX_POWER_I"]);
            param_out.Add("LZR_LI_ROLLED_OFF_POWER_DB", local_params["ROLLED_OFF_POWER_DB"]);
            param_out.Add("LZR_LI_MAX_DISCONTINUITY_MW", local_params["MAX_DISCONTINUITY_MW"]);


            // % VBIAS/IBIAS for passed in power
            foreach (double report_pwr in _arREPORT_PWR)
            {
                //Target_lbias = Find_x_for_y(LIV_DATA.Ilzr_IN, 10.^ (LIV_DATA.Pout_dBm./ 10), 10.^ (structin.REPORT_PWR(n)./ 10));
                // Potential matching issue here TODo

                double target_lbias = MathLib.ListMath.Find_x_for_y(ilzr, pout, Math.Pow(10, report_pwr / 10.0));
                double target_vbias = MathLib.ListMath.Find_y_for_x(ilzr, v2wire, target_lbias);
                string suffix;
                if (Math.Abs(Math.Round(report_pwr) - report_pwr) < 0.001)
                {
                    suffix = String.Format("{0:0}DBM", report_pwr);
                }
                else
                {
                    suffix = String.Format("{0:0.#}DBM", report_pwr);
                }
                param_out.Add("CUROPT@" + suffix, nanToError(target_lbias));
                param_out.Add("VOLOPT@" + suffix, nanToError(target_vbias));
            }

            foreach (double report_cur_A in _arREPORT_CUR)
            {
                string suffix;
                double report_cur = report_cur_A * 1000.0;
                if (Math.Abs(Math.Round(report_cur) - report_cur) < 0.001)
                {
                    suffix = String.Format("{0:0}MA", report_cur);
                }
                else
                {
                    suffix = String.Format("{0:0.#}MA", report_cur);
                }
                List<double> ipd1_orig = getColumnFromDataView(dv, "Ipd1");
                List<double> ipd1 = VecMath.interp1(new DenseVector(ilzr_original.ToArray()), new DenseVector(ipd1_orig.ToArray()), new DenseVector(ilzr.ToArray())).ToList();
                List<double> ipd2_orig = getColumnFromDataView(dv, "Ipd2");
                List<double> ipd2 = VecMath.interp1(new DenseVector(ilzr_original.ToArray()), new DenseVector(ipd2_orig.ToArray()), new DenseVector(ilzr.ToArray())).ToList();
                double target_pwr = MathLib.ListMath.Find_y_for_x(ilzr, pout_dbm, report_cur_A);
                // Matlab reports 9999 if point is at the end of a sweep
                if (target_pwr == pout_dbm.Last()) target_pwr = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                double target_ipd1 = MathLib.ListMath.Find_y_for_x(ilzr, ipd1, report_cur_A);
                if (target_ipd1 == ipd1.Last()) target_ipd1 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                double target_ipd2 = MathLib.ListMath.Find_y_for_x(ilzr, ipd2, report_cur_A);
                if (target_ipd2 == ipd2.Last()) target_ipd2 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                double target_v = MathLib.ListMath.Find_y_for_x(ilzr, v2wire, report_cur_A);
                if (target_v == v2wire.Last()) target_v = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_out.Add("OPWR@" + suffix, nanToError(target_pwr));
                param_out.Add("IPD1@" + suffix, nanToError(target_ipd1));
                param_out.Add("IPD2@" + suffix, nanToError(target_ipd2));
                param_out.Add("VLZR@" + suffix, nanToError(target_v));
            }
            return param_out;
        }

        public List<double> getArray(string label, DataTable datatable)
        {
            List<double> data;
            var queryobj = datatable.AsEnumerable().Select(s => s.Field<double>(label));
            data = queryobj.ToList<double>();
            return data;
        }

        /// <summary>
        /// If an input is nan, report ErrorCodes.INVALID_POST_CALCULATION_VALUE instead.
        /// </summary>
        /// <param name="input">input to sanitize</param>
        /// <returns></returns>
        public static double nanToError(double input)
        {
            if (Double.IsNaN(input))
            {
                return ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            }
            return input;
        }

        private static List<double> getColumnFromDataView(DataView dv, string column)
        {
            List<double> data = new List<double>();
            foreach (DataRowView drv in dv)
            {
                data.Add((double)drv.Row[column]);
            }
            return data;
        }

        /// <summary>
        /// Used for fminsearch NelderMead solver in Analyze_LIV_Data
        /// </summary>
        public class MyFit
        {
            public static List<double> V;
            public static List<double> I;

            public double evaluate(double[] x)
            {
                double R = x[0];
                double n = x[1];
                double error = 0;
                double vnew = 0;
                for (int i = 0; i < V.Count && i < I.Count; i++)
                {
                    vnew = I.ElementAt(i) * R + n * 1.38e-23 * 293 / 1.602e-19 *
                        Math.Log(I.ElementAt(i) / 1e-10 + 1);
                    error += Math.Pow(vnew - V.ElementAt(i), 2);
                }
                error = Math.Pow(error, 0.5);
                return error;
            }

        }

        /// <summary>
        /// Find the bias current at which the RIN transitions through the intrin_spec
        /// </summary>
        /// <param name="bias">List of bias currents</param>
        /// <param name="intrin">Associated list of RIN measurements for bias currents</param>
        /// <param name="intrin_spec">RIN spec threshold</param>
        /// <returns>Returns a list of 3 values.
        ///     0) The maximal current at which RIN fails spec (greatest current incase of multiple failures)
        ///     1) Interpolate between crossover of spec fail (using log intrin)
        ///     2) Interpolate between crossover of spec fail (using intrin converted to linear scale)
        /// </returns>
        public static List<double> IntRin_Current_Calculation(
            List<double> bias, List<double> intrin, double intrin_spec)
        {
            List<double> results = new List<double>(3);
            results.Add(ErrorCodes.INVALID_POST_CALCULATION_VALUE);
            results.Add(ErrorCodes.INVALID_POST_CALCULATION_VALUE);
            results.Add(ErrorCodes.INVALID_POST_CALCULATION_VALUE);

            // % IntRIN >= IntRIN_SPEC;
            List<double> indices = new List<double>(intrin.Count);
            for (int i = 0; i < intrin.Count; i++)
            {
                if (intrin.ElementAt(i) >= intrin_spec)
                {
                    indices.Add(1);
                }
                else
                {
                    indices.Add(0);
                }
            }
            List<double> intrin_gte_spec = intrin.FindAll(x => x >= intrin_spec);
            List<double> bias_gte_spec = new List<double>(intrin_gte_spec.Count);

            for (int i = 0; i < indices.Count; i++)
            {
                if (indices.ElementAt(i) == 1)
                {
                    bias_gte_spec.Add(bias.ElementAt(i));
                }
            }

            results[0] = bias.Min();
            if (bias_gte_spec.Count != 0)
            {
                // Find the highest current that is after the point where intrin fails the spec
                List<double> current = bias.FindAll(x => x > bias_gte_spec.Max());
                if (current.Count != 0)
                {
                    results[0] = current.Min();
                    if (bias_gte_spec.Count == bias.Count) // If All points are above the spec
                    {
                        results[0] = current.Max();
                    }
                }
            }

            double current2 = results[0]; ;
            List<double> bias_current2 = bias.FindAll(x => x < current2);
            double current1 = current2;
            if (bias_current2.Count != 0)
            {
                current1 = bias_current2.Max();
            }

            double intrin_1 = VecMath.interp1(
                new DenseVector(bias.ToArray()),
                new DenseVector(intrin.ToArray()),
                new DenseVector(new double[] { current1 }), "Linear").ElementAt(0);
            double intrin_2 = VecMath.interp1(
                new DenseVector(bias.ToArray()),
                new DenseVector(intrin.ToArray()),
                new DenseVector(new double[] { current2 }), "Linear").ElementAt(0);

            results[1] = intrin_2;
            results[2] = intrin_2;
            List<double> intrin_pts = new List<double>() { intrin_1, intrin_2 };
            if (intrin_pts.Min() <= intrin_spec && intrin_pts.Max() >= intrin_spec)
            {
                // Logarithmic
                results[1] = VecMath.interp1(
                    new DenseVector(new double[] { intrin_1, intrin_2 }),
                    new DenseVector(new double[] { current1, current2 }),
                    new DenseVector(new double[] { intrin_spec }), "Linear").ElementAt(0);
                // Convert to linear
                results[2] = VecMath.interp1(
                    new DenseVector(new double[] { Math.Pow(10.0, intrin_1 / 10.0), Math.Pow(10.0, intrin_2 / 10.0) }),
                    new DenseVector(new double[] { current1, current2 }),
                    new DenseVector(new double[] { Math.Pow(10.0, intrin_spec / 10.0) }), "Linear").ElementAt(0);
            }

            return results;
        }

        public void Measure_LIV_LZR_VOA1_VOA2(DataTable dt,
            List<double> start,
            List<double> stop,
            List<double> step,
            bool is_aligned = false)
        {

            // Assume switch connected
            if (is_aligned)
            {
                // Manually turn off autoranging and set to 0dBm range since not implemented in ConfigurePWMforSampling
                //_PWM.AutoRange = false; // Matlab POR
                _PWM.AutoRange = true;
                //_PWM.Range = "0"; // -30 db of dynamic range to +3
                Dictionary<string, double> PWM_struct = new Dictionary<string, double>();
                PWM_struct["PowerUnit"] = 0; // PWM unit: 0: dBm; 1: Watt
                PWM_struct["wavelength"] = _CENTER_WL;
                PWM_struct["InternalTrigger"] = 1;
                PWM_struct["AveragingTime"] = (5e-4) * 1;
                PWM_struct["triggerin"] = 0;
                PWM_struct["triggerout"] = 0;
                PWM_struct["Samples"] = 250;
                _PWM.ConfigurePWMforSampling(PWM_struct);  
            }

            if (_VOA != null)
            {
                _VOA.SetWavelength(_CENTER_WL);
                _VOA.SetRawAttenuation(_arATTN_SETTINGS.ElementAt(VOA.FIXED));
            }
            //_OptSwitch.Connect("COMMON", "ToPWM");

            // Inline Apply_DC_Stress
            Apply_DC_Stress(_PD_BIAS, _PD_BIAS, 0, 0, 0, 0);

            List<double> lzr_biases = CreateSweepList(
                start.ElementAt(LZR_POS),
                stop.ElementAt(LZR_POS),
                step.ElementAt(LZR_POS));
            List<double> voa1_biases = CreateSweepList(
                start.ElementAt(VOA1_POS),
                stop.ElementAt(VOA1_POS),
                step.ElementAt(VOA1_POS));
            List<double> voa2_biases = CreateSweepList(
                start.ElementAt(VOA2_POS),
                stop.ElementAt(VOA2_POS),
                step.ElementAt(VOA2_POS));
            CParamLimits<double> LZRSweepLimits = new CParamLimits<double>(-Math.Abs(_fMaxLaserDriveCurrent), Math.Abs(_fMaxLaserDriveCurrent));
            CParamLimits<double> VOASweepLimits = new CParamLimits<double>(-Math.Abs(_fMaxVOADriveCurrent), Math.Abs(_fMaxVOADriveCurrent));
            for(int i=0; i<lzr_biases.Count; i++)
            {
                double bias = lzr_biases.ElementAt(i);
                LZRSweepLimits.CheckAgainstLimits(ref bias);
                lzr_biases[i] = bias;
            }
            for(int i=0; i<voa1_biases.Count; i++)
            {
                double bias = voa1_biases.ElementAt(i);
                VOASweepLimits.CheckAgainstLimits(ref bias); 
                voa1_biases[i] = bias;
            }
            for(int i=0; i<voa2_biases.Count; i++)
            {
                double bias = voa2_biases.ElementAt(i);
                VOASweepLimits.CheckAgainstLimits(ref bias); 
                voa2_biases[i] = bias;
            }

            bool is_tracking = false;
            foreach(double lzr_bias in lzr_biases)
            {
                _spa.ForceI(SPA_CONSTANTS.SMU5, "10e-6", lzr_bias, _LZR_COMP.ToString(), "0", true); 
                foreach(double voa1_bias in voa1_biases)
                {
                    _spa.ForceI(SPA_CONSTANTS.SMU3, "10e-6", voa1_bias, _VOA_COMP.ToString(), "0", true);
                    foreach(double voa2_bias in voa2_biases)
                    {
                        _spa.ForceI(SPA_CONSTANTS.SMU4, "10e-6", voa2_bias, _VOA_COMP.ToString(), "0", true);

                        DataRow newrow = dt.NewRow();

                        Dictionary<string, double> dc_values;
                        Read_Spa_Stress(lzr_bias, voa1_bias, voa2_bias, out dc_values);
                        foreach (KeyValuePair<string, double> line in dc_values)
                        {
                            newrow[line.Key] = line.Value;
                        }

                        double pout_report = -100;
                        if (is_aligned)
                        {
                            // % Inline Read_Power_Meter_Reading
                            List<double> pout_samples = new List<double> ();
                            List<double> invalid_pts;
                            System.DateTime startTime = DateTime.UtcNow;
                            int i = 0;
                            while ((DateTime.UtcNow - startTime).TotalSeconds <= 5.0)
                            { 
                                pout_samples = _PWM.GetData();
                                invalid_pts = pout_samples.FindAll(x => x > 10);
                                if (invalid_pts.Count == 0)
                                {
                                    break;
                                }
                                log("Retry({3}) PWM reading at lzr_bias {0} voa1 {1} voa2 {2} ",lzr_bias, voa1_bias, voa2_bias, ++i);
                            }

                            bool has_voa = StationHardware.Instance().MapInst.ContainsKey(WaferLevelTestLib.Constants.ATTEN);
                            double voa_attenuation =  _loss.loss(_CENTER_WL, "MMSwitchLoss");
                            if (_loss.ApplyVariableLoss && has_voa)
                            {
                                //voa_attenuation += Math.Abs(_arATTN_SETTINGS.ElementAt(VOA.FIXED));
                                double current_attenuation = 0;
                                _VOA.GetRawAttenuation(ref current_attenuation);
                                voa_attenuation += current_attenuation;
                            }
                            pout_report = pout_samples.Max() + voa_attenuation;
                        }
                        newrow["Pout_dBm"] = pout_report;
                        newrow["Pout_mW"] = Math.Pow(10, pout_report/10.0);
                        dt.Rows.Add(newrow);

                        // Enable tracking if optically aligned and power looks good
                        // -30 roughly lasing threshold
                        if (is_aligned && pout_report>-30.0 && !is_tracking) // -30 dbm from matlab, based on fixed range of 0dbm -40,+3 dbm dynamic range.
                        {
                            _alignment.SetTrack(_alignmentType, true);
                            is_tracking = true;
                        }
                    } // End VOA2 Loop
                } // End VOA 1 Loop
            } // End bias loop

            if (is_aligned && is_tracking)
            {
                _alignment.SetTrack(_alignmentType, false);
                is_tracking = false;
            }
            _spa.stop();

            if (_VOA != null)
            {
                _VOA.SetWavelength(_CENTER_WL);
                _VOA.SetAverageTime(0.002);
                _VOA.SetOutputPower(_arATTN_SETTINGS.ElementAt(VOA.RIN_SMSR));
            }

            return;
        }

        // Need to set Param "THRESHOLD"
        public Dictionary<string, double> Measure_RIN_SMSR_LZR_VOA1_VOA2(
            Dictionary<string, double> param,
            DataTable dt_osa,
            DataTable dt_esa,
            DataTable dt_smsr,
            DataTable dt_rin,
            List<double> start,
            List<double> stop,
            List<double> step,
            List<double> SWEEP_WINDOW,
            List<double> SWEEP_SPECS,
            bool SWEEP_OSA = false,
            bool SWEEP_ESA = false,
            bool plot = true)
        {

            GraphEventArgs id1 = null, id2 = null, id3 = null, id4 = null;

            SMSR_Analysis _SMSR = new SMSR_Analysis();
            RIN_Analysis _RIN = new RIN_Analysis();

            //_OptSwitch.Connect("COMMON", "ToRIN");
            if (_VOA != null)
            {
                _VOA.SetWavelength(_CENTER_WL);
                _VOA.SetAverageTime(0.002);
                _VOA.SetOutputPower(_arATTN_SETTINGS.ElementAt(VOA.RIN_SMSR));
            }

            //Switch Matrix for measurement configured in pretest
            List<double> lzr_biases = CreateSweepList(
                start.ElementAt(LZR_POS), stop.ElementAt(LZR_POS), step.ElementAt(LZR_POS));
            List<double> voa1_biases = CreateSweepList(
                start.ElementAt(VOA1_POS), stop.ElementAt(VOA1_POS), step.ElementAt(VOA1_POS));
            List<double> voa2_biases = CreateSweepList(
                start.ElementAt(VOA2_POS), stop.ElementAt(VOA2_POS), step.ElementAt(VOA2_POS));
            CParamLimits<double> LZRSweepLimits = new CParamLimits<double>(-Math.Abs(_fMaxLaserDriveCurrent), Math.Abs(_fMaxLaserDriveCurrent));
            CParamLimits<double> VOASweepLimits = new CParamLimits<double>(-Math.Abs(_fMaxVOADriveCurrent), Math.Abs(_fMaxVOADriveCurrent));
            for(int i=0; i<lzr_biases.Count; i++)
            {
                double bias = lzr_biases.ElementAt(i);
                LZRSweepLimits.CheckAgainstLimits(ref bias);
                lzr_biases[i] = bias;
            }
            for(int i=0; i<voa1_biases.Count; i++)
            {
                double bias = voa1_biases.ElementAt(i);
                VOASweepLimits.CheckAgainstLimits(ref bias); 
                voa1_biases[i] = bias;
            }
            for(int i=0; i<voa2_biases.Count; i++)
            {
                double bias = voa2_biases.ElementAt(i);
                VOASweepLimits.CheckAgainstLimits(ref bias);
                //ester 7/8, voa2_biases is len 2, not 1
                //voa2_biases[i] = bias;
            }

            //% Apply DC stress
            Apply_DC_Stress(_PD_BIAS, _PD_BIAS, 0, 0, 0, 0);

            //Perform_Dark_Measurement (inlined)
            // ester 7/8, commented out
            //double vtia_0 = Measure_Vtia(10, 0, 0);
            
            // % Configure_ESA_For_Reading
            Dictionary<string, string> ESA_settings = new Dictionary<string, string> (); 
            ESA_settings["fa"] = "50e3";
            ESA_settings["fb"] =  _ESA_STOP_FREQ.ToString();
            ESA_settings["rb"] = "1e6";
            ESA_settings["vb"] = "0.1e6";
            ESA_settings["imp"] = "50";
            ESA_settings["gain"] = _PD_TIA_RF_GAIN.ToString();
            if (_O2E != null)
            {
                double tia_gain = _O2E.Gain;
                ESA_settings["gain"] = tia_gain.ToString();
            }
            
            ESA_settings["rl"] = "-10";
            ESA_settings["attenuation"] = "0";
            ESA_settings["binary"] = "1";
            ESA_settings["points"] = (1 + 2 * Math.Floor(1001 * ((double.Parse(ESA_settings["fb"]) - double.Parse(ESA_settings["fa"])) / (10e9 - 50e3) / 2))).ToString();
            if (_ESA_POINTS != -1)
            {
                ESA_settings["points"] = _ESA_POINTS.ToString();
            }
            _ESA.Configure(ESA_settings);
            _ESA.Trigger();
            CESAData ESA_REF = new CESAData();
            _ESA.Get_Data(ref ESA_REF);
            //GraphEvent(new GraphEventArgsAddLineGraph("ESA", "Frequency", "Power"));
            //GraphEvent(new GraphEventArgsPlot("", ESA_REF.frequency.ToList(), ESA_REF.power.ToList(), WpfGraphService.Styles.MatlabStyle("go-")));

            // % Calculate_Center_Bias
            double threshold = param["THRESHOLD"];
            List<double> lzr_biases_above_threshold = lzr_biases.FindAll(x => x >= threshold);
            // Filter lzr biases to only points above threshold
            double lzr_center_bias = lzr_biases.Max();
            if (lzr_biases_above_threshold.Count != 0)
            {
                lzr_center_bias = (lzr_biases_above_threshold.Max() + lzr_biases_above_threshold.Min()) / 2;
            }
            double voa1_center_bias = voa1_biases.Max();
            double voa2_center_bias = voa2_biases.Min();

            Apply_DC_Stress(_PD_BIAS, _PD_BIAS, voa1_center_bias, voa2_center_bias, lzr_center_bias, 0);
            Configure_OSA_For_Reading();

            _alignment.SetTrack(_alignmentType, true);
            
            //ester 7/8
            //double vtia_beginning = Measure_Vtia(_NUM_VTIA_REPEATS,  vtia_0, 1);
          //  vtia_beginning = vtia_beginning < 0 ? 0 : vtia_beginning;

            _alignment.SetTrack(_alignmentType, false);

            bool skip_voa1_bias =false;
            bool skip_laser_bias = false;
            bool is_tracking = false;
            COSAData OSA_DATA_WL = null; // Stores the first wavelength sweep for all subsequent sweeps. Matlab Method.

            foreach (double lzr_bias in lzr_biases)
            {
                //GraphEvent(new GraphEventArgsAddFigure(getDeviceInfoString()));

                //Check the LIV_data to see if there is enough power
                //Find LIV power at the 
                DataView dv_liv = new DataView(_LZR_DataTable);
                List<double> ivoa1_in = getColumnFromDataView(dv_liv, "Ivoa1_IN");
                List<double> ivoa2_in = getColumnFromDataView(dv_liv, "Ivoa2_IN");
                dv_liv.RowFilter = String.Format("Ivoa1_IN = {0} AND Ivoa2_IN = {1} AND Pout_dBm >= {2}", ivoa1_in.Max(), ivoa2_in.Min(), _LIV_MIN_PWR_LIMIT);
                List<double> ilzr_in = getColumnFromDataView(dv_liv, "Ilzr_IN");
                if (ilzr_in.Count==0 || lzr_bias < ilzr_in.Min() || skip_laser_bias) // if lzr bias is not in range or no points exceed power limit
                {
                    continue;
                }
                _spa.ForceI(SPA_CONSTANTS.SMU5, "10e-6", lzr_bias, _LZR_COMP.ToString(), "0", true);
                skip_voa1_bias = false;

                foreach (double voa1_bias in voa1_biases)
                {
                    if (skip_voa1_bias)
                    {
                        continue;
                    }
                    _spa.ForceI(SPA_CONSTANTS.SMU3, "10e-6", voa1_bias, _VOA_COMP.ToString(), "0", true);

                    foreach (double voa2_bias in voa2_biases)
                    {
                        _spa.ForceI(SPA_CONSTANTS.SMU4, "10e-6", voa2_bias, _VOA_COMP.ToString(), "0", true);

                        if (!is_tracking)
                        {
                            _alignment.SetTrack(_alignmentType, true);
                            is_tracking = true;
                        }


                        // read_spa_stress
                        Dictionary<string, double> dc_values;
                        Read_Spa_Stress(lzr_bias, voa1_bias, voa2_bias, out dc_values);  //40 ms
                        // ester 7/8
                        //double vtia = Measure_Vtia(_NUM_VTIA_REPEATS,  vtia_0, 0);

                        if (SWEEP_OSA) // 150ms
                        {
                            _OSA.Run_OSA();
                            COSAData OSA_DATA;
                            if (OSA_DATA_WL == null)
                            {
                                OSA_DATA_WL = _OSA.get_OSA_data(2); // Get the wavelength data as well
                                OSA_DATA = OSA_DATA_WL;
                            }
                            else
                            {
                                OSA_DATA = _OSA.get_OSA_data(1); // Get the wavelength data as well
                                OSA_DATA.wavelength = OSA_DATA_WL.wavelength;
                            }
                            //if (voa1_bias == voa1_biases.First() && voa2_bias == voa2_biases.First())
                            //{
                            //    GraphEvent(new GraphEventArgsAddLineGraph(String.Format("OSA @ {0}",lzr_bias), "Wavelength", "Power"));
                            //    GraphEvent(new GraphEventArgsPlot(String.Format("v1:{0} v2:{1}", voa1_bias.ToString(), voa2_bias.ToString()),
                            //        OSA_DATA.wavelength.ToList(), OSA_DATA.power.ToList(), WpfGraphService.Styles.MatlabStyle("go-")));
                            //}

                            DataRow dr = dt_smsr.NewRow();
                            foreach (KeyValuePair<string, double> line in dc_values)
                            {
                                dr[line.Key] = line.Value;
                            }

                            Dictionary<string, double> SMSR_PARAMS = _SMSR.SMSRByPEAKFINDER(OSA_DATA.wavelength, OSA_DATA.power);
                            foreach(KeyValuePair<string, double> line in SMSR_PARAMS)
                            {
                                dr[line.Key] = line.Value;
                            }

                            // Create processed OSA data table
                            foreach(double width in _SPECTRA_WIDTH_SETTINGS)
                            {
                                Dictionary<string,double> Optical_Spectrum = 
                                    (new Parametric_Analysis()).Parametric_Analysis_2D(
                                        OSA_DATA.wavelength, OSA_DATA.power, "Optical Spectrum", new List<double> {width});
                                dr[String.Format("SPECTRA_WIDTH_{0}DB", width)] = Optical_Spectrum.First().Value;
                            }
                            dt_smsr.Rows.Add(dr);

                            // Create raw OSA data table
                            for (int i = 0; i < OSA_DATA.wavelength.Count(); i++)
                            {
                                dr = dt_osa.NewRow();
                                dr["Ilzr_IN"] = dc_values["Ilzr_IN"];
                                dr["Ivoa1_IN"] = dc_values["Ivoa1_IN"];
                                dr["Ivoa2_IN"] = dc_values["Ivoa2_IN"];
                                dr["wavelength_nm"] = OSA_DATA.wavelength.ElementAt(i);

                                //here
                                dr["Pout_dBm"] = OSA_DATA.power.ElementAt(i);
                                dr["smsr_dB"] = SMSR_PARAMS["MIN_SMSR_OVER_0P2"];
                                dr["peak_nm"] = SMSR_PARAMS["CENTER_WL"];
                                dt_osa.Rows.Add(dr);
                            }
                        }

                        if (SWEEP_ESA) // 150 ms
                        {
                            _ESA.Trigger();
                            CESAData ESA_DATA = new CESAData();
                            _ESA.Get_Data(ref ESA_DATA);
                            //if (voa1_bias == voa1_biases.First() && voa2_bias == voa2_biases.First())
                            //{
                            //    GraphEvent(new GraphEventArgsAddLineGraph(String.Format("ESA @ {0}", lzr_bias), "Frequency", "Power"));
                            //    GraphEvent(new GraphEventArgsPlot(String.Format("v1:{0} v2:{1}", voa1_bias.ToString(), voa2_bias.ToString()),
                            //    ESA_DATA.frequency.ToList(), ESA_DATA.power.ToList(), WpfGraphService.Styles.MatlabStyle("go-")));
                            //}
                            // % calculate_RIN
                            double intRIN;
                            DenseVector RIN;
                            
                            //_RIN.calculate_RIN(ESA_DATA.power, ESA_REF, vtia, ESA_settings, out RIN, out intRIN);

                            // Create the processed integrated RIN data table
                            DataRow dr = dt_rin.NewRow();
                            foreach (KeyValuePair<string, double> line in dc_values)
                            {
                                dr[line.Key] = line.Value;
                            }
                            //ester
                            //dr["vtia"] = vtia;
                            //dr["intRIN"] = intRIN;
                            dt_rin.Rows.Add(dr);
                            
                            // Create the raw data data table
                            //Loop through _ESA_DATA
                            for (var i=0; i<ESA_DATA.frequency.Count(); i++ )
                            {
                                dr = dt_esa.NewRow();
                                dr["Ilzr_IN"] = dc_values["Ilzr_IN"];
                                dr["Ivoa1_IN"] = dc_values["Ivoa1_IN"];
                                dr["Ivoa2_IN"] = dc_values["Ivoa2_IN"];

                                //ester
                                //dr["Vtia"] = vtia;
                                dr["Frequency_Hz"] = ESA_DATA.frequency.ElementAt(i);

                                //ester
                                //dr["RIN_dBpHz"] = RIN.ElementAt(i);
                                dt_esa.Rows.Add(dr);
                            }
                        }

                    } // end for voa2
                    skip_voa1_bias = Should_The_For_Loop_Be_Stopped(dt_smsr, dt_rin, SWEEP_WINDOW, SWEEP_SPECS, "Ivoa1_IN");
                } // end for voa1
                skip_laser_bias = Should_The_For_Loop_Be_Stopped(dt_smsr, dt_rin, SWEEP_WINDOW, SWEEP_SPECS, "Ilzr_IN");

                if (plot)
                {
                    DataView dv_fbt = new DataView(dt_rin);
                    id1 = GraphDataView(dv_fbt, "RIN", "Ilzr_IN", "Ivoa1_IN", "intRIN", true, id1);
                    DataView dv_osa = new DataView(dt_smsr);
                    id2 = GraphDataView(dv_osa, "SMSR", "Ilzr_IN", "Ivoa1_IN", "SMSR", true, id2);
                    id3 = GraphDataView(dv_osa, "CENTER_DBM", "Ilzr_IN", "Ivoa1_IN", "CENTER_DBM", true, id3);
                    id4 = GraphDataView(dv_osa, "SPECTRAL_WIDTH", "Ilzr_IN", "Ivoa1_IN", "SPECTRA_WIDTH_10DB", true, id4);
                }
            } //end for lzrbias

            //Apply dc stress
            Apply_DC_Stress(_PD_BIAS, _PD_BIAS, voa1_center_bias, voa2_center_bias, lzr_center_bias, 0);
            if (!is_tracking)
            {
                _alignment.SetTrack(_alignmentType, true);
                is_tracking = true;
            }

            //ester 7/8
            //double vtia_end = Measure_Vtia(_NUM_VTIA_REPEATS,  vtia_0, 1);
            //vtia_end = vtia_end < 0 ? 0 : vtia_end;

            if (is_tracking)
            {
                _alignment.SetTrack(_alignmentType, false);
                is_tracking = false;
            }
            _spa.stop();

            if (_VOA != null)
            {
                _VOA.SetWavelength(_CENTER_WL);
                _VOA.SetOutputPower(_arATTN_SETTINGS.ElementAt(VOA.FIXED));
            }

            Dictionary<string, double> MISC_VARS = new Dictionary<string, double> ();
            MISC_VARS.Add("_ESA_REF_MED_DBM", ESA_REF.power.Median());
            
            //ester 7/8
            //MISC_VARS.Add("_VTIA_PRE_SWEEP", vtia_beginning);
            
            //MISC_VARS.Add("_VTIA_POST_SWEEP", vtia_end);
            double epsilon = Math.Pow(2, -52); // Matlab definition

            //ester 7/8
            //MISC_VARS.Add("_VTIA_PERC_CHANGE", (vtia_beginning - vtia_end)/(vtia_beginning + epsilon)*100.0);
            //_OptSwitch.Connect("COMMON", "ToPWM");
            return MISC_VARS;
        }

        public const int LZR_POS = 0;
        public const int VOA1_POS = 1;
        public const int VOA2_POS = 2;
        public static class VOA
        {
            public const int FIXED = 0;
            public const int RIN_SMSR = 1;
            public const int APEX_HROSA = 2;
        }
        public List<double> CreateSweepList(double start, double stop, double step)
        {
            if (stop < start)
            {
                double temp = start;
                start = stop;
                stop = temp;
            }
            int numsteps =  step == 0 ? 1 : (int)Math.Truncate((stop - start) / step) + 1;
            List<double> data = new List<double>(numsteps);
            for (int count = 0; count < numsteps; count++)
            {
                data.Add((double)((decimal)count * (decimal) step + (decimal) start));
            }
            return data;
        }

        /// <summary>
        /// Applies bias to device assuming standard smu range, compliance setup
        /// </summary>
        /// <param name="pd1_vbias"></param>
        /// <param name="pd2_vbias"></param>
        /// <param name="voa1_ibias"></param>
        /// <param name="voa2_ibias"></param>
        /// <param name="lzr_ibias"></param>
        /// <param name="tia_ibias"></param>
        public void Apply_DC_Stress(
            double pd1_vbias, double pd2_vbias,
            double voa1_ibias, double voa2_ibias,
            double lzr_ibias, double tia_ibias
            )
        {
            _spa.stop();
            _spa.SetupSMUs(SPA_CONSTANTS.MMode_Spot);
            _spa.ForceV(SPA_CONSTANTS.SMU1, "1e-4", pd1_vbias, _PD_COMP.ToString(), "0", true);
            _spa.ForceV(SPA_CONSTANTS.SMU2, "1e-4", pd2_vbias, _PD_COMP.ToString(), "0", true);
            //string limit_range = "0.20";
            string limit_range = "10e-6"; // Matlab actually set to 10uA Limit range instead of 200uA Limit range.... Potentially not NI compatible?
            _spa.ForceI(SPA_CONSTANTS.SMU3, limit_range, voa1_ibias, _VOA_COMP.ToString(), "0", true); 
            _spa.ForceI(SPA_CONSTANTS.SMU4, limit_range, voa2_ibias, _VOA_COMP.ToString(), "0", true);
            _spa.ForceI(SPA_CONSTANTS.SMU5, limit_range, lzr_ibias, _LZR_COMP.ToString(), "0", true);
            //_spa.ForceI(SPA_CONSTANTS.SMU7, limit_range, tia_ibias, "6", "0", true); // Lab buddy comp
            _spa.SetTriggerIn(SPA_CONSTANTS.TRIGGER_OFF);
            _spa.SetTriggerOut(SPA_CONSTANTS.TRIGGER_OFF);
        }

        public void Read_Spa_Stress(double lzr_bias, double voa1_bias, double voa2_bias, out Dictionary<string, double> data)
        {
            data = new Dictionary<string, double>(15);
            CMValue ivalue, vvalue;
            _spa.MeasureSpotI(SPA_CONSTANTS.SMU5, "10e-6", out ivalue);
            _spa.MeasureSpotV(SPA_CONSTANTS.SMU5, "20", out vvalue);
            data.Add("Ilzr_IN", lzr_bias);
            data.Add("Ilzr", ivalue.mValue);
            data.Add("Vlzr_2Wire", vvalue.mValue);
            _spa.MeasureSpotI(SPA_CONSTANTS.SMU3, "10e-6", out ivalue);
            _spa.MeasureSpotV(SPA_CONSTANTS.SMU3, "20", out vvalue);
            data.Add("Ivoa1_IN", voa1_bias);
            data.Add("Ivoa1", ivalue.mValue);
            data.Add("Vvoa1", vvalue.mValue);
            _spa.MeasureSpotI(SPA_CONSTANTS.SMU4, "10e-6", out ivalue);
            _spa.MeasureSpotV(SPA_CONSTANTS.SMU4, "20", out vvalue);
            data.Add("Ivoa2_IN", voa2_bias);
            data.Add("Ivoa2", ivalue.mValue);
            data.Add("Vvoa2", vvalue.mValue);
            _spa.MeasureSpotI(SPA_CONSTANTS.SMU1, "10e-6", out ivalue);
            _spa.MeasureSpotV(SPA_CONSTANTS.SMU1, "20", out vvalue);
            data.Add("Vpd1_IN", _PD_BIAS);
            data.Add("Ipd1", ivalue.mValue);
            data.Add("Vpd1", vvalue.mValue);
            _spa.MeasureSpotI(SPA_CONSTANTS.SMU2, "10e-6", out ivalue);
            _spa.MeasureSpotV(SPA_CONSTANTS.SMU2, "20", out vvalue);
            data.Add("Vpd2_IN", _PD_BIAS);
            data.Add("Ipd2", ivalue.mValue);
            data.Add("Vpd2", vvalue.mValue);
        }


        /// <summary>
        /// Sets up OSA for measurement. First uses a coarse range to try to find laser peak. Once found, laser peak is set as the center wavelength.
        /// </summary>
        public void Configure_OSA_For_Reading()
        {
            // Copied/Adapted from STX_EO_DC_RIN_MODE 
            COSAData OSA_DATA = new COSAData();
            double coarse_span = 30;
            double maxP;
            int maxP_index;
            int count = 0;
            double result = 0;
            bool found_peak = false;

            do
            {

                _OSA.sensitivity_mode = _OSA_SENS;
                _OSA.resolution_bandwidth = 0.2;
                _OSA.wavelength_step = 0.2 / 5.0; // resolution BW/5
                _OSA.wavelength_center = _CENTER_WL;
                _OSA.wavelength_span = coarse_span;
                _OSA.ConfigureForSpeed(true);     // Different spectrometers are going to have a number of settings to optimize for speed

                _OSA.Run_OSA();
                OSA_DATA = _OSA.get_OSA_data(1);

                maxP = OSA_DATA.power.Max();
                maxP_index = Array.IndexOf(OSA_DATA.power, maxP);

                count++;

                found_peak = maxP >= -30 && maxP_index <= 0.9 * OSA_DATA.power.Length && maxP_index >= 0.1 * OSA_DATA.power.Length;

            } while (false == found_peak && 2 > count);    // Try twice (legacy reasons from MATLAB)

            if (found_peak)
            {
                // The math below rounds the peak to the nearest 0.02 nm
                result = Math.Round(_CENTER_WL + Math.Round(coarse_span * (maxP_index - 0.5 * (OSA_DATA.power.Length - 1)) / OSA_DATA.power.Length / 2, 2) * 2, 2);
                _OSA.wavelength_center = result;
            }

            // Matlab only configures this once
            _OSA.resolution_bandwidth = _OSA_RBW;
            _OSA.wavelength_span = _OSA_SPAN;
            _OSA.wavelength_step = _OSA_RBW / 5.0; // Matlab
            return;
        }

        public DataTable CreateDownSampledOsaDatatable(DataTable dt)
        {
            Dictionary<string, string> structin = new Dictionary<string, string>();
            structin.Add("OSA_CLMP_LMT", _OSA_CLMP_LMT.ToString());
            structin.Add("OSA_DWN_SMPL_FLAG", _OSA_DWN_SMPL_FLAG.ToString());
            structin.Add("OSA_DWN_SMPL_STEP", _OSA_DWN_SMPL_STEP.ToString());
            //// Down sample and save
            DataView dv = new DataView(dt);

            SMSR_Analysis sa = new SMSR_Analysis();
            ArrayList wl_out;
            ArrayList power_out;
            Dictionary<string, ArrayList> OSA_DATA = new Dictionary<string, ArrayList>();
            List<double> lzr_biases = getColumnFromDataView(dv, "Ilzr_IN").Distinct().ToList();
            List<double> voa1_biases = getColumnFromDataView(dv, "Ivoa1_IN").Distinct().ToList();
            List<double> voa2_biases = getColumnFromDataView(dv, "Ivoa2_IN").Distinct().ToList();

            DataTable FBT_OSA_DWN_SAMPLED_DATATABLE = new DataTable();
            foreach (string label in FBTSLI_OSA_labels)
            {
                FBT_OSA_DWN_SAMPLED_DATATABLE.Columns.Add(label, typeof(double));
            }
            
            dv = new DataView(dt);
            OSA_DATA = new Dictionary<string, ArrayList>();
            dv.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa1_IN = {1} AND Ivoa2_IN = {2}", lzr_biases.ElementAt(0), voa1_biases.ElementAt(0), voa2_biases.ElementAt(0));
            int rowcount = dv.Count;
            int rowstart = 0;

            foreach (double lzr_bias in lzr_biases)
            {
                foreach (double voa1_bias in voa1_biases)
                {
                    foreach (double voa2_bias in voa2_biases)
                    {
                        dv.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa1_IN = {1} AND Ivoa2_IN = {2}", lzr_bias, voa1_bias, voa2_bias);
                        rowcount = dv.Count;
                        if (rowcount == 0)
                        {
                            continue;
                        }
                        var rowfilt_dt = dt.AsEnumerable().Skip(rowstart).Take(rowcount).CopyToDataTable();
                        rowstart += rowcount;
                        OSA_DATA = new Dictionary<string, ArrayList>();
                        OSA_DATA.Add("wavelengtharray", new ArrayList() {getArray("wavelength_nm", rowfilt_dt).ToArray()});
                        OSA_DATA.Add("powerarray", new ArrayList() {getArray("Pout_dBm", rowfilt_dt).ToArray()});
                        sa.OSA_Raw_Data_Down_Sampling(OSA_DATA, new List<double>() { lzr_bias }, structin, out wl_out, out power_out);
                        double smsr_db = (double) rowfilt_dt.Rows[0]["smsr_dB"];
                        double peak_nm = (double) rowfilt_dt.Rows[0]["peak_nm"];

                        for (int i = 0; i < ((double[])wl_out[0]).Length; i++)
                        {
                            DataRow dr = FBT_OSA_DWN_SAMPLED_DATATABLE.NewRow();
                            dr["Ilzr_IN"] = lzr_bias;
                            dr["Ivoa1_IN"] = voa1_bias;
                            dr["Ivoa2_IN"] = voa2_bias;
                            dr["wavelength_nm"] = ((double[])wl_out[0])[i];
                            dr["Pout_dBm"] = ((double[])power_out[0])[i];
                            dr["smsr_dB"] = smsr_db;
                            dr["peak_nm"] = peak_nm;
                            FBT_OSA_DWN_SAMPLED_DATATABLE.Rows.Add(dr);
                        }
                    }
                }
            }
            return FBT_OSA_DWN_SAMPLED_DATATABLE;
        }

        public DataTable CreateDownSampledEsaDatatable(DataTable dt)
        {
            RIN_Analysis ra = new RIN_Analysis();
            Dictionary<string, string> structin = new Dictionary<string, string> ();
            structin.Add("RIN_DWN_SMPL_FLAG", _RIN_DWN_SMPL_FLAG.ToString());
            structin.Add("RIN_DWN_SMPL_START_FREQ", _RIN_DWN_SMPL_START_FREQ.ToString());
            structin.Add("RIN_DWN_SMPL_WINDOW", _RIN_DWN_SMPL_WINDOW.ToString()); 
            structin.Add("RIN_DWN_SMPL_THRESHOLD", _RIN_DWN_SMPL_THRESHOLD.ToString());
            structin.Add("RIN_DWN_SMPL_STEP", _RIN_DWN_SMPL_STEP.ToString()); 

            CESAData ESA_DATA_DWN_SMPL;
            DenseVector RIN_DATA_DWN_SMPL;
            DataView dv = new DataView(dt);
            CESAData ESA_DATA = new CESAData();

            List<double> lzr_biases = getColumnFromDataView(dv, "Ilzr_IN").Distinct().ToList();
            List<double> voa1_biases = getColumnFromDataView(dv, "Ivoa1_IN").Distinct().ToList();
            List<double> voa2_biases = getColumnFromDataView(dv, "Ivoa2_IN").Distinct().ToList();

            DataTable FBT_ESA_DWN_SAMPLED_DATATABLE = new DataTable();
            foreach (string label in FBTSLI_ESA_labels)
            {
                FBT_ESA_DWN_SAMPLED_DATATABLE.Columns.Add(label, typeof(double));
            }

            // Query number of rows for 1 set of lzr, voa1, voa2
            dv.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa1_IN = {1} AND Ivoa2_IN = {2}", lzr_biases.ElementAt(0), voa1_biases.ElementAt(0), voa2_biases.ElementAt(0));
            int rowcount = dv.Count;
            int rowstart = 0;

            foreach (double lzr_bias in lzr_biases)
            {
                foreach (double voa1_bias in voa1_biases)
                {
                    foreach (double voa2_bias in voa2_biases)
                    {
                        dv.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa1_IN = {1} AND Ivoa2_IN = {2}", lzr_bias, voa1_bias, voa2_bias);
                        rowcount = dv.Count;
                        if (rowcount == 0)
                        {
                            continue;
                        }
                        var rowfilt_dt = dt.AsEnumerable().Skip(rowstart).Take(rowcount).CopyToDataTable();
                        rowstart += rowcount;
                        List<double> freq = getArray("Frequency_HZ", rowfilt_dt);
                        List<double> rin = getArray("RIN_dBpHz", rowfilt_dt);
                        ESA_DATA.frequency = freq.ToArray();
                        ra.ESA_Raw_Data_Down_Sampling(ESA_DATA, rin.ToArray(), structin, out ESA_DATA_DWN_SMPL, out RIN_DATA_DWN_SMPL);
                        double vtia = (double) rowfilt_dt.Rows[0]["Vtia"];

                        for (int i = 0; i < RIN_DATA_DWN_SMPL.Count; i++)
                        {
                            DataRow dr = FBT_ESA_DWN_SAMPLED_DATATABLE.NewRow();
                            dr["Ilzr_IN"] = lzr_bias;
                            dr["Ivoa1_IN"] = voa1_bias;
                            dr["Ivoa2_IN"] = voa2_bias;
                            dr["Vtia"] = vtia;
                            dr["Frequency_Hz"] = ESA_DATA_DWN_SMPL.frequency[i];
                            dr["RIN_dBpHz"] = RIN_DATA_DWN_SMPL[i];
                            FBT_ESA_DWN_SAMPLED_DATATABLE.Rows.Add(dr);
                        }
                    }
                }
            }
            return FBT_ESA_DWN_SAMPLED_DATATABLE;
        }

        public bool Should_The_For_Loop_Be_Stopped(
            DataTable SMSR_dt,
            DataTable RIN_dt,
            List<double> SWEEP_WINDOW,
            List<double> SWEEP_SPECS,
            string SWEEP_VAR
            )
        {
            bool stop_smsr = false;
            bool stop_rin = false;

            try
            {
                List<double> ilzr_in_orig = getArray("Ilzr_IN", SMSR_dt);

                if (ilzr_in_orig.Count != 0 && SMSR_dt.Rows.Count != 0)
                {
                    if (SWEEP_VAR == "Ilzr_IN")
                    {
                        DataView SMSR_dv = new DataView(SMSR_dt);
                        List<double> ivoa1_in = getColumnFromDataView(SMSR_dv, "Ivoa1_IN");
                        List<double> ivoa2_in = getColumnFromDataView(SMSR_dv, "Ivoa2_IN");
                        SMSR_dv.RowFilter = String.Format("Ivoa1_IN = {0} AND Ivoa2_IN = {1}", ivoa1_in.Max(), ivoa2_in.Min());
                        List<double> ilzr_in = getColumnFromDataView(SMSR_dv, "Ilzr_IN"); // Filtered Ilzr

                        if (ilzr_in.Count != 0)
                        {
                            List<double> min_smsr_over_0p2 = getColumnFromDataView(SMSR_dv, "MIN_SMSR_OVER_0P2");
                            if (ilzr_in.First() > ilzr_in.Last())
                            {
                                stop_smsr = When_to_stop_FBT_SLI(ilzr_in, min_smsr_over_0p2, "SMSR REVERSE MODE 1", SWEEP_WINDOW, SWEEP_SPECS);
                            }
                            else if (ilzr_in.First() < ilzr_in.Last())
                            {
                                stop_smsr = When_to_stop_FBT_SLI(ilzr_in, min_smsr_over_0p2, "SMSR FORWARD MODE 1", SWEEP_WINDOW, SWEEP_SPECS);
                            }

                            if (ilzr_in_orig.Distinct().Count() != ilzr_in.Distinct().Count())
                            {
                                // % Ivoa1_IN is a sweep variable along with Ilzr_IN and TTR enabled. 
                                // % Do not apply TTR.
                                stop_smsr = false; // Untested
                            }
                        }
                    }
                    else if (SWEEP_VAR == "Ivoa1_IN")
                    {
                        DataView SMSR_dv = new DataView(SMSR_dt);
                        List<double> ivoa2_in = getColumnFromDataView(SMSR_dv, "Ivoa2_IN");
                        SMSR_dv.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa2_IN = {1}", ilzr_in_orig.Last(), ivoa2_in.Min());
                        List<double> ivoa1_in = getColumnFromDataView(SMSR_dv, "Ivoa1_IN");
                        List<double> min_smsr_over_0p2 = getColumnFromDataView(SMSR_dv, "MIN_SMSR_OVER_0P2");

                        if (ivoa1_in.Count != 0)
                        {
                            if (ivoa1_in.First() > ivoa1_in.Last())
                            {
                                stop_smsr = When_to_stop_FBT_SLI(ivoa1_in, min_smsr_over_0p2, "SMSR REVERSE MODE 1", SWEEP_WINDOW, SWEEP_SPECS);
                            }
                            else if (ivoa1_in.First() < ivoa1_in.Last())
                            {
                                stop_smsr = When_to_stop_FBT_SLI(ivoa1_in, min_smsr_over_0p2, "SMSR FORWARD MODE 1", SWEEP_WINDOW, SWEEP_SPECS);
                            }
                        }
                    }
                }

                // Set stop RIN
                if (ilzr_in_orig.Count != 0 && RIN_dt.Rows.Count != 0)
                {
                    if (SWEEP_VAR == "Ilzr_IN")
                    {
                        DataView RIN_dv = new DataView(RIN_dt);
                        List<double> ivoa1_in = getColumnFromDataView(RIN_dv, "Ivoa1_IN");
                        List<double> ivoa2_in = getColumnFromDataView(RIN_dv, "Ivoa2_IN");
                        RIN_dv.RowFilter = String.Format("Ivoa1_IN = {0} AND Ivoa2_IN = {1}", ivoa1_in.Max(), ivoa2_in.Min());
                        List<double> ilzr_in = getColumnFromDataView(RIN_dv, "Ilzr_IN"); // Filtered Ilzr

                        if (ilzr_in.Count != 0)
                        {
                            List<double> int_rin = getColumnFromDataView(RIN_dv, "intRIN");
                            if (ilzr_in.First() > ilzr_in.Last())
                            {
                                stop_rin = When_to_stop_FBT_SLI(ilzr_in, int_rin, "RIN REVERSE MODE 2", SWEEP_WINDOW, SWEEP_SPECS);
                            }
                            else if (ilzr_in.First() < ilzr_in.Last())
                            {
                                stop_rin = When_to_stop_FBT_SLI(ilzr_in, int_rin, "RIN FORWARD MODE 2", SWEEP_WINDOW, SWEEP_SPECS);
                            }

                            if (ilzr_in_orig.Distinct().Count() != ilzr_in.Distinct().Count())
                            {
                                // % Ivoa1_IN is a sweep variable along with Ilzr_IN and TTR enabled. 
                                // % Do not apply TTR.
                                stop_rin = false; // Untested
                            }
                        }
                    }
                    else if (SWEEP_VAR == "Ivoa1_IN")
                    {
                        DataView RIN_dv = new DataView(RIN_dt);
                        List<double> ivoa2_in = getColumnFromDataView(RIN_dv, "Ivoa2_IN");
                        RIN_dv.RowFilter = String.Format("Ilzr_IN = {0} AND Ivoa2_IN = {1}", ilzr_in_orig.Last(), ivoa2_in.Min());
                        List<double> ivoa1_in = getColumnFromDataView(RIN_dv, "Ivoa1_IN");
                        List<double> int_rin = getColumnFromDataView(RIN_dv, "intRIN");

                        if (ivoa1_in.Count != 0)
                        {
                            if (ivoa1_in.First() > ivoa1_in.Last())
                            {
                                stop_rin = When_to_stop_FBT_SLI(ivoa1_in, int_rin, "RIN REVERSE MODE 1", SWEEP_WINDOW, SWEEP_SPECS);
                            }
                            else if (ivoa1_in.First() < ivoa1_in.Last())
                            {
                                stop_rin = When_to_stop_FBT_SLI(ivoa1_in, int_rin, "RIN FORWARD MODE 1", SWEEP_WINDOW, SWEEP_SPECS);
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            return stop_smsr | stop_rin;
        }

        static public bool When_to_stop_FBT_SLI(
            List<double> x,
            List<double> y,
            string datatype,
            List<double> sweep_window,
            List<double> sweep_specs
            )
        {
            bool stop = false;

            double current1, current2;
            switch (datatype)
            {
                case "SMSR REVERSE MODE 1":
                    {
                        List<double> x_filtered = new List<double>();
                        for (int i = 0; i < y.Count; i++)
                        {
                            if (y[i] <= sweep_specs.ElementAt(0))
                            {
                                x_filtered.Add(x[i]);
                            }
                        }
                        if (x_filtered.Count == 0) return false;
                        current1 = x_filtered.Max();
                        current2 = x.Min();
                        stop = Math.Abs(current1 - current2) >= sweep_window.ElementAt(0);
                        break;
                    }
                case "SMSR FORWARD MODE 1":
                    {
                        List<double> x_filtered = new List<double>();
                        for (int i = 0; i < y.Count; i++)
                        {
                            if (y[i] <= sweep_specs.ElementAt(0))
                            {
                                x_filtered.Add(x[i]);
                            }
                        }
                        if (x_filtered.Count == 0) return false;
                        current1 = x_filtered.Min();
                        current2 = x.Max();
                        stop = Math.Abs(current1 - current2) >= sweep_window.ElementAt(0);
                        break;
                    }
                case "RIN REVERSE MODE 1":
                    {
                        stop = y.FindAll(z => z >= sweep_specs.ElementAt(1)).Count() > 0; // Bug in matlab POR? No windowing
                        break;
                    }
                case "RIN REVERSE MODE 2":
                    {
                        List<double> x_filtered = new List<double>();
                        for (int i = 0; i < y.Count; i++)
                        {
                            if (y[i] >= sweep_specs.ElementAt(1))
                            {
                                x_filtered.Add(x[i]);
                            }
                        }
                        if (x_filtered.Count == 0) return false;
                        current1 = x_filtered.Max();
                        current2 = x.Min();
                        stop = Math.Abs(current1 - current2) >= sweep_window.ElementAt(1);
                        break;
                    }
                case "RIN FORWARD MODE 1":
                    {
                        List<double> x_filtered = new List<double>();
                        for (int i = 0; i < y.Count; i++)
                        {
                            if (y[i] >= sweep_specs.ElementAt(1))
                            {
                                x_filtered.Add(x[i]);
                            }
                        }
                        if (x_filtered.Count == 0)
                        {
                            current1 = x.Min();
                        }
                        else
                        {
                            current1 = x_filtered.Max();
                        }
                        current2 = x.Max();
                        stop = Math.Abs(current1 - current2) >= sweep_window.ElementAt(1);
                        break;
                    }
                case "RIN FORWARD MODE 2":
                    {
                        List<double> x_filtered = new List<double>();
                        for (int i = 0; i < y.Count; i++)
                        {
                            if (y[i] >= sweep_specs.ElementAt(1))
                            {
                                x_filtered.Add(x[i]);
                            }
                        }
                        if (x_filtered.Count == 0) return false;
                        current1 = x.Min();
                        current2 = x.Max();
                        stop = Math.Abs(current1 - current2) >= sweep_window.ElementAt(1);
                        break;
                    }
                default:
                    {
                        break; // Untested
                    }
            }
            return stop;
        }

        public bool Is_The_Device_Working(double LIV_MIN_PWR_LIMIT)
        {
            DataView dv = new DataView(_LZR_DataTable);
            List<double> ivoa1_in = getArray("Ivoa1_IN", _LZR_DataTable); // TODO clean up mix up of getArray and getColumnFromDataView
            List<double> ivoa2_in = getArray("Ivoa2_IN", _LZR_DataTable);
            dv.RowFilter = String.Format("Ivoa1_IN = {0} AND Ivoa2_IN = {1}", ivoa1_in.Max(), ivoa2_in.Min());

            List<double> ilzr_measured = getColumnFromDataView(dv, "Ilzr");
            var ilzr_diff = VecMath.diff(new DenseVector(ilzr_measured.ToArray()));
            List<double> ilzr_sign = new List<double> ();
            foreach(double bias in ilzr_diff)
            {
                ilzr_sign.Add(Math.Sign(bias));
            }

            if  (ilzr_sign.Max() != ilzr_sign.Min()) // Not monotonic increasing/decreasing
            {
                return false;
            }

            List<double> pout_dbm = getColumnFromDataView(dv, "Pout_dBm");
            if (pout_dbm.Max() < LIV_MIN_PWR_LIMIT)
            {
                return false;
            }

            return true;
        }
        public void Initialize_Scalar_Parameters()
        {
            string suffix;

            _params.Add(MakeStringStructSafe("THRESHOLD"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("MAXPOWER"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("LASER_RS"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("LZR_IDEALITY"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("V_TURNON"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("THRESH_FIT_CURVE"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("THRESH_FIT_RSQ"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("THRESH_CURV"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("THRESH_RSQ"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("LZR_VI_MAX_V"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("LZR_VI_SUM_ABS_DV"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("LZR_LI_MAX_POWER_I"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("LZR_LI_ROLLED_OFF_POWER_DB"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("LZR_LI_MAX_DISCONTINUITY_MW"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("OPT_ALIGN_TIME"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("FBT_ESA_REF_MED_DBM"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_VTIA_PRE_SWEEP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_VTIA_POST_SWEEP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_VTIA_PERC_CHANGE"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_RIN_MAX_DB"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_RIN_MAX_VTIA"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_RIN_MIN_DB"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("FBT_MIN_SMSR_OVER_0.2"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_CUR@MIN_SMSR_OVER_0.2"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_MODE_SEP@MIN_SMSR_OVER_0.2"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_INITIAL_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_FINAL_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_MAX_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_MIN_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_OSA_MAX_PWR_DIP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_OSA_MAX_PWR"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_OSA_MAX_WL_JUMP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("FBT_OSA_MIN_PWR"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("SLI_ESA_REF_MED_DBM"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_VTIA_PRE_SWEEP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_VTIA_POST_SWEEP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_VTIA_PERC_CHANGE"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_RIN_MAX_DB"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_RIN_MAX_VTIA"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_RIN_MIN_DB"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            _params.Add(MakeStringStructSafe("SLI_MIN_SMSR_OVER_0.2"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_CUR@MIN_SMSR_OVER_0.2"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_MODE_SEP@MIN_SMSR_OVER_0.2"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_INITIAL_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_FINAL_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_MAX_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_MIN_WAVELENGTH"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_OSA_MAX_PWR_DIP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_OSA_MAX_PWR"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_OSA_MAX_WL_JUMP"), ErrorCodes.INITIAL_PARAMETER_VALUE);
            _params.Add(MakeStringStructSafe("SLI_OSA_MIN_PWR"), ErrorCodes.INITIAL_PARAMETER_VALUE);

            foreach (double report_pwr in _arREPORT_PWR)
            {
                if (Math.Abs(Math.Round(report_pwr) - report_pwr) < 0.001)
                {
                    suffix = String.Format("{0:0}DBM", report_pwr);
                }
                else
                {
                    suffix = String.Format("{0:0.#}DBM", report_pwr);
                }
                _params.Add("CUROPT@" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                _params.Add("VOLOPT@" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
            }

            foreach (double report_cur_A in _arREPORT_CUR)
            {
                double report_cur = report_cur_A * 1000.0;
                if (Math.Abs(Math.Round(report_cur) - report_cur) < 0.001)
                {
                    suffix = String.Format("{0:0}MA", report_cur);
                }
                else
                {
                    suffix = String.Format("{0:0.#}MA", report_cur);
                }
                _params.Add("OPWR@" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                _params.Add("IPD1@" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                _params.Add("IPD2@" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                _params.Add("VLZR@" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
            }

            double start = _arFBT_START.ElementAt(0);
            double stop = _arFBT_STOP.ElementAt(0);
            double step = Math.Abs(_arFBT_STEP.ElementAt(0));
            if (stop < start)
            {
                double temp = start;
                start = stop;
                stop = temp;
            }
            int numsteps = (int)Math.Truncate((stop - start) / step) + 1;
            string prefix;

            prefix = "FBT_VOA1_TRANS_";
            for (double count = start; count < numsteps; count++)
            {
                double bias_ma = count * step * 1000.0;

                foreach (double fbt_threshold in _arREPORT_FBT)
                {
                    suffix = "";
                    // Append FBT report 
                    if (Math.Abs(Math.Round(fbt_threshold) - fbt_threshold) < 0.001)
                    {
                        suffix += String.Format("@{0:0}DB", fbt_threshold);
                    }
                    else
                    {
                        suffix += String.Format("@{0:0.#}DB", fbt_threshold);
                    }
                    // Append Bias Current Report;
                    if (Math.Abs(Math.Round(bias_ma) - bias_ma) < 0.001)
                    {
                        suffix += String.Format("@{0:0}MA", bias_ma);
                    }
                    else
                    {
                        suffix += String.Format("@{0:0.#}MA", bias_ma);
                    }

                    _params.Add(prefix + "CUR" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                    _params.Add(prefix + "RIN_MAX_DB" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                    _params.Add(prefix + "RIN_MIN_DB" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                    _params.Add(prefix + "VTIA_MIN" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                    _params.Add(prefix + "VTIA_MEDIAN" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                    _params.Add(prefix + "VTIA_MAX" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                }
            }

            prefix = "FBT_SPCTR_WDTH_VOA1_TRANS_";
            for (double count = start; count < numsteps; count++)
            {
                double bias_a = count * step;
                double bias_ma = count * step * 1000.0;

                foreach (double sws in _SPECTRA_WIDTH_SETTINGS)
                {
                    foreach (double stl in _SPECTRA_WIDTH_TRANSITION_LIMIT)
                    {
                        suffix = String.Format("@{0:0}DB@{1:0}PRCNT", sws, stl);
                        if (Math.Abs(Math.Round(bias_ma) - bias_ma) < 0.001)
                        {
                            suffix += String.Format("@{0:0}MA", bias_ma);
                        }
                        else
                        {
                            suffix += String.Format("@{0:0.#}MA", bias_ma);
                        }

                        _params.Add(prefix + "CUR" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                        _params.Add(prefix + "WDTH_SPRD" + suffix, ErrorCodes.INITIAL_PARAMETER_VALUE);
                    }
                }
            }

            foreach (double smsr_limit in _arSMSR_LIMITS_DB)
            {
                suffix = "";
                if (Math.Abs(Math.Round(smsr_limit) - smsr_limit) < 0.001)
                {
                    suffix += String.Format("_{0:0}DB", smsr_limit);
                }
                else
                {
                    suffix += String.Format("_{0:0.#}DB", smsr_limit);
                }

                _params.Add("SLI_SMSR_FIRST_FAIL_VAL" + suffix, ErrorCodes.INVALID_POST_CALCULATION_VALUE);
                _params.Add("SLI_SMSR_FIRST_FAIL_CUR" + suffix, ErrorCodes.INVALID_POST_CALCULATION_VALUE);
                _params.Add("SLI_SMSR_FIRST_FAIL_SEP" + suffix, ErrorCodes.INVALID_POST_CALCULATION_VALUE);
                _params.Add("SLI_SMSR_LAST_FAIL_VAL" + suffix, ErrorCodes.INVALID_POST_CALCULATION_VALUE);
                _params.Add("SLI_SMSR_LAST_FAIL_CUR" + suffix, ErrorCodes.INVALID_POST_CALCULATION_VALUE);
                _params.Add("SLI_SMSR_LAST_FAIL_SEP" + suffix, ErrorCodes.INVALID_POST_CALCULATION_VALUE);
            }
        }

        public double Measure_Vtia(int n_Samples, double Vtia_0, int delay)
        {
            pause(delay*1000);
            List<double> vtia_samples = new List<double>(n_Samples);
            CMValue point;
            for(int i=0; i<n_Samples; i++)
            {
                _spa.MeasureSpotV(SPA_CONSTANTS.SMU7, "20", out point);
                vtia_samples.Add(point.mValue);
            }
            return vtia_samples.Median() - Vtia_0;
        }

        /// <summary>
        /// Plot a series of data from Dataview/Datatable
        /// </summary>
        /// <param name="dv">Dataview</param>
        /// <param name="title">Title of the graph</param>
        /// <param name="series_label">Label for line. If multi_series is enabled, each series will be plotted as separate line. Every unique entry in the column which matches series_label is a separate line.</param>
        /// <param name="x">X data column label</param>
        /// <param name="y">Y data column label</param>
        /// <param name="multi_series">Plot multiple series as separate lines. Set the label with series_label</param>
        /// <param name="id">Id can be provided to reuse a graph id</param>
        /// <returns>Returns the figure id</returns>
        public GraphEventArgs GraphDataView(DataView dv, string title, string series_label, string x, string y, bool multi_series = false, GraphEventArgs id=null)
        {
            GraphEventArgs rc = id;
            try
            {
                if (id == null) // Create a new graph if null or failed to graph
                {
                    rc = GraphEvent(new GraphEventArgsAddLineGraph(title, x, y));
                    id = rc;
                }

                GraphEvent(new GraphEventArgsClearGraph(id.guid));

                List<double> xdata = getColumnFromDataView(dv, x);
                List<double> ydata = getColumnFromDataView(dv, y);
                string original_rowfilter = dv.RowFilter.ToString();

                if (multi_series)
                {
                    List<double> series = getColumnFromDataView(dv, series_label).Distinct().ToList();
                    DataView copy = new DataView(dv.Table);
                    foreach (double label in series)
                    {
                        int distance = series.IndexOf(label);
                        double ratio = (double)distance / (double)series.Count();
                        //LineColor = Color.FromArgb(0xFF, 0xFF, 0x00, 0x00),
                        LineStyle localstyle = Styles.MatlabStyle("bo");
                        int r = (int)Math.Max(0, (int)255 * (ratio));
                        int b = (int)Math.Max(0, (int)255 * (1 - ratio));
                        int g = 255 - b - r;
                        localstyle.LineColor = Color.FromArgb(0xFF, r, g, b);
                        localstyle.MarkerStroke = Color.FromArgb(0xFF, r, g, b);

                        copy.RowFilter = original_rowfilter + String.Format("{0} = {1}", series_label, label);
                        GraphEvent(new GraphEventArgsPlot(id.guid,
                            String.Format("{0} = {1}", series_label, label), getColumnFromDataView(copy, x), getColumnFromDataView(copy, y), localstyle));
                    }
                    series = getColumnFromDataView(dv, series_label).Distinct().ToList();
                }
                else
                {
                    GraphEvent(new GraphEventArgsPlot(id.guid, series_label, xdata, ydata, Styles.MatlabStyle("bo")));
                }
            }
            //catch (Exception ex) // GraphEvent may return exception if invalid id. i.e when graphs are closed
            catch 
            {
            }
            return rc;
        }
    }
}


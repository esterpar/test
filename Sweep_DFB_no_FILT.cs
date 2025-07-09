using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using DataLib;
using Utility;
using InstrumentsLib.Tools.Instruments.SPA;
using InstrumentsLib.Tools.Core;
using WaferLevelTestLib;
using MathLib;
using MathNet.Numerics.LinearAlgebra.Double;

// 19 November 2019 -- these changes are meant to fix a minor deficiency.  In the 
// previous version, one can only assign one pin to PD2N and PD2P.  In this version, 
// one can assign multiple pins, using a semicolon separator.
// -- Srinivasan "Cheenu" Sethuraman

namespace AutoSPANMeasLib
{
    public class Sweep_DFB_no_FILT : SweepBase
    {
        public const string TESTNAME = "Sweep_DFB_NoFilt_V00_01";
        static bool USE_QBD = false;
        private const int MAX_KINKS = 1;

        //Test file parameters
        protected List<double> REPORT_CUR;          // For OPWR@ILZR and IPD2@ILZR
        protected List<double> REPORT_POWER_DBM;    // For CUROPT@DBM and VOLOPT@DBM
        private string COMMON_GND;
        private string PD_COMMON_GND;
        private DeviceCommonConnectionType _Common = DeviceCommonConnectionType.CommonN;
        private DeviceCommonConnectionType _PDCommon = DeviceCommonConnectionType.CommonN;
        private double test_time;
        DateTime start_time = DateTime.UtcNow;
        

        public Sweep_DFB_no_FILT(SetupWLT setup)
            : base(setup)
        {
            this._data.TestName = TESTNAME;
            this._data.TestFlowName = TestConstants.FLOWNAME_DC_SCREEN;
            this._data.testDepHeader.Operation = TestConstants.OPER_UNKNOWN;
            Debug.Print("Sweep_DFB.Sweep_DFB(SetupWLT setup)");
        }

        public Sweep_DFB_no_FILT()
            : base()
        {
            this._data.TestName = TESTNAME;
            this._data.TestFlowName = TestConstants.FLOWNAME_DC_SCREEN;
            this._data.testDepHeader.Operation = TestConstants.OPER_UNKNOWN;
            Debug.Print("Sweep_DFB.Sweep_DFB_no_Filt()");
        }

        protected bool connectMatrix(int QBDNUM, int NUM_PDS, double PD_BIAS,
                                        string QBD1N, string QBD1P,
                                        string QBD2N, string QBD2P,
                                        string LZRN, string LZRP,
                                        string LZRN_SENSE, string LZRP_SENSE,
                                        string PD1N, string PD1P,
                                        string PD2N, string PD2P)
        {
            List<string> arSMU1_Pins = new List<string>();
            List<string> arSMU2_Pins = new List<string>();
            List<string> arSMU3_Pins = new List<string>();
            List<string> arSMU4_Pins = new List<string>();
            List<string> arSMU5_Pins = new List<string>();
            List<string> arSMU6_Pins = new List<string>();
            List<string> arGND2_Pins = new List<string>();
            List<string> arGNDU_Pins = new List<string>();

            _matrix.DisconnectAll();

            if (COMMON_GND == "NONE")
            {
                arSMU1_Pins.Add(PD1N);
                arGND2_Pins.Add(PD1P);

                // PD 2
                if (NUM_PDS == 2)
                {
                    arSMU2_Pins.Add(PD2N);
                    arGND2_Pins.Add(PD2P);
                }

                // Laser
                arSMU5_Pins.Add(LZRP);
                arGNDU_Pins.Add(LZRN);
                arSMU4_Pins.Add(LZRN_SENSE);
                arSMU6_Pins.Add(LZRP_SENSE);
            }
            else 
            {
                arSMU4_Pins.Add(LZRP_SENSE);
                arSMU6_Pins.Add(LZRN_SENSE);
                arSMU5_Pins.Add(LZRP);

                if(PD_COMMON_GND != "NONE")
                {
                    arSMU1_Pins.Add(PD1P);
                    if (NUM_PDS == 2)
                    {
                        arSMU2_Pins.Add(PD2P);
                    }
                }
            }

            // Connect the matrix pins
            connectPinsIfValid(SPA_CONSTANTS.SMU1, arSMU1_Pins);
            connectPinsIfValid(SPA_CONSTANTS.SMU2, arSMU2_Pins);
            connectPinsIfValid(SPA_CONSTANTS.SMU3, arSMU3_Pins);
            connectPinsIfValid(SPA_CONSTANTS.SMU4, arSMU4_Pins);
            connectPinsIfValid(SPA_CONSTANTS.SMU5, arSMU5_Pins);
            connectPinsIfValid(SPA_CONSTANTS.SMU6, arSMU6_Pins);
            connectPinsIfValid(SPA_CONSTANTS.GND2, arGND2_Pins);
            connectPinsIfValid(SPA_CONSTANTS.GNDU, arGNDU_Pins);

            _matrix.executeCachedConnections();
            _matrix.validateConnections();

            return true;
        }

        #region Naming
        public static string get_pname_V_TurnOn(string PD_BIAS)
        {
            return string.Format("V_TURNON@{0}", PD_BIAS);
        }

        public static string get_pname_Laser_RS(string PD_BIAS)
        {
            return string.Format("LASER_RS@{0}", PD_BIAS);
        }

        public static string get_pname_LZR_IDEALITY(string PD_BIAS)
        {
            return string.Format("LZR_IDEALITY@{0}", PD_BIAS);
        }

        public static string get_pname_Laser_RS_PctError(string PD_BIAS)
        {
            return string.Format("LASER_RS_PctError@{0}", PD_BIAS);
        }

        static string pname_ITH_Template = "ITH_{0}@{1}";
        static string pname_ITH_EXTRAP_Template = "ITH_EXTRAP_{0}@{1}";
        static string pname_ITH_ERROR_Template = "ITH_ERROR_{0}@{1}";
        static string pname_ITH_D2_Template = "ITH_CURV_{0}@{1}";
        static string pname_ITH_RSQ_Template = "ITH_RSQ_{0}@{1}";
        static string pname_SlopeEfficiency_V_Template = "SLOPEEFFICIENCY_V_{0}@{1}";

        /***Initialize output variables***/

        // ITH and SLOPEEFFICIENCY for each PD
        public static string get_pname_ITH(string PDNAME, string PD_BIAS)
        {
            return string.Format(pname_ITH_Template, PDNAME, PD_BIAS);
        }

        public static string get_pname_ITH_EXTRAP(string PDNAME, string PD_BIAS)
        {
            return string.Format(pname_ITH_EXTRAP_Template, PDNAME, PD_BIAS);
        }

        public static string get_pname_ITH_ERROR(string PDNAME, string PD_BIAS)
        {
            return string.Format(pname_ITH_ERROR_Template, PDNAME, PD_BIAS);
        }

        public static string get_pname_ITH_D2(string PDNAME, string PD_BIAS)
        {
            return string.Format(pname_ITH_D2_Template, PDNAME, PD_BIAS);
        }

        public static string get_pname_ITH_RSQ(string PDNAME, string PD_BIAS)
        {
            return string.Format(pname_ITH_RSQ_Template, PDNAME, PD_BIAS);
        }

        public static string get_pname_SlopeEfficiency_V(string PDNAME, string PD_BIAS)
        {
            return string.Format(pname_SlopeEfficiency_V_Template, PDNAME, PD_BIAS);
        }

        // VBIAS/IBIAS/PELEC for each PD
        static string pname_VBIAS_Template = "VBIAS_{0}MW_{1}@{2}";
        static string pname_IBIAS_Template = "IBIAS_{0}MW_{1}@{2}";
        static string pname_PELEC_Template = "PELEC_{0}MW_{1}@{2}";

        private static string Round(string numString)
        {
            var num = numString.Split('.');
            if (num.Length == 1)
            {
                return numString;
            }
            return Math.Round(double.Parse(numString), 1, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture);
        }

        public static string get_pname_VBIAS(string REPORT_POWER, string PDNAME, string PD_BIAS)
        {
            REPORT_POWER = Round(REPORT_POWER);
            return string.Format(pname_VBIAS_Template, REPORT_POWER, PDNAME, PD_BIAS);
        }

        public static string get_pname_IBIAS(string REPORT_POWER, string PDNAME, string PD_BIAS)
        {

            REPORT_POWER = Round(REPORT_POWER);
            return string.Format(pname_IBIAS_Template, REPORT_POWER, PDNAME, PD_BIAS);
        }

        public static string get_pname_PELEC(string REPORT_POWER, string PDNAME, string PD_BIAS)
        {
            REPORT_POWER = Round(REPORT_POWER);
            return string.Format(pname_PELEC_Template, REPORT_POWER, PDNAME, PD_BIAS);
        }

        public static string get_pname_POPT_MAX(string PDNAME, string PD_BIAS)
        {
            return string.Format("POPT_MAX_{0}@{1}", PDNAME, PD_BIAS);
        }

        public static string get_pname_VBIASMAX(string PDNAME, string PD_BIAS)
        {
            return string.Format("VBIAS_MAX_{0}@{1}", PDNAME, PD_BIAS);
        }

        public static string get_pname_IBIASMAX(string PDNAME, string PD_BIAS)
        {
            return string.Format("IBIAS_MAX_{0}@{1}", PDNAME, PD_BIAS);
        }

        public static string get_pname_PELECMAX(string PDNAME, string PD_BIAS)
        {
            return string.Format("PELEC_MAX_{0}@{1}", PDNAME, PD_BIAS);
        }

        // Kink
        public static string get_pname_NUMKINKS(string PDNAME, string PD_BIAS)
        {
            return string.Format("NUM_KINKS_{0}@{1}", PDNAME, PD_BIAS);
        }

        public static string get_pname_IKINK(string PDNAME, int nKINKidx, string PD_BIAS)
        {
            return string.Format("IKINK_{0}_{1}@{2}", PDNAME, nKINKidx, PD_BIAS);
        }

        public static string get_pname_VKINK(string PDNAME, int nKINKidx, string PD_BIAS)
        {
            return string.Format("VKINK_{0}_{1}@{2}", PDNAME, nKINKidx, PD_BIAS);
        }

        public static string get_pname_POPTKINK(string PDNAME, int nKINKidx, string PD_BIAS)
        {
            return string.Format("POPTKINK_{0}_{1}@{2}", PDNAME, nKINKidx, PD_BIAS);
        }
        
        static string[] _arPDnames = new string[2] { "BACK", "FRONT" };

        static string getPDNAME(int nPDidx)
        {
            return _arPDnames[nPDidx - 1];
        }

        static string pname_ITH_DELTA_Template = "ITH_DELTA@{0}";
        static string pname_SlopeEfficiency_V_Ratio_Template = "SLOPEEFFICIENCY_V_RATIO@{0}";

        public static string get_pname_ITH_DELTA(string PD_BIAS)
        {
            return string.Format(pname_ITH_DELTA_Template, PD_BIAS);
        }

        public static string get_pname_SlopeEfficiency_V_Ratio(string PD_BIAS)
        {
            return string.Format(pname_SlopeEfficiency_V_Ratio_Template, PD_BIAS);
        }

        #region TOTAL VBIAS/IBIAS/PELEC
        static string pname_VBIAS_TOTAL_Template = "VBIAS_{0}MW_TOTAL@{1}";
        static string pname_IBIAS_TOTAL_Template = "IBIAS_{0}MW_TOTAL@{1}";
        static string pname_PELEC_TOTAL_Template = "PELEC_{0}MW_TOTAL@{1}";

        public static string get_pname_VBIAS_TOTAL(string REPORT_POWER, string PD_BIAS)
        {
            return string.Format(pname_VBIAS_TOTAL_Template, Round(REPORT_POWER), PD_BIAS);
        }

        public static string get_pname_IBIAS_TOTAL(string REPORT_POWER, string PD_BIAS)
        {
            return string.Format(pname_IBIAS_TOTAL_Template, Round(REPORT_POWER), PD_BIAS);
        }

        public static string get_pname_PELEC_TOTAL(string REPORT_POWER, string PD_BIAS)
        {
            return string.Format(pname_PELEC_TOTAL_Template, Round(REPORT_POWER), PD_BIAS);
        }

        #endregion

        static string pname_VBIAS_RATIO_Template = "VBIAS_RATIO_{0}MW@{1}";
        static string pname_IBIAS_RATIO_Template = "IBIAS_RATIO_{0}MW@{1}";
        static string pname_PELEC_RATIO_Template = "PELEC_RATIO_{0}MW@{1}";

        public static string get_pname_VBIAS_RATIO(string REPORT_POWER, string PD_BIAS)
        {
            return string.Format(pname_VBIAS_RATIO_Template, Round(REPORT_POWER), PD_BIAS);
        }

        public static string get_pname_IBIAS_RATIO(string REPORT_POWER, string PD_BIAS)
        {
            return string.Format(pname_IBIAS_RATIO_Template, Round(REPORT_POWER), PD_BIAS);
        }

        public static string get_pname_PELEC_RATIO(string REPORT_POWER, string PD_BIAS)
        {
            return string.Format(pname_PELEC_RATIO_Template, Round(REPORT_POWER), PD_BIAS);
        }

        #endregion Naming

        public static bool computePDparams(DenseVector arIPD1, DenseVector arIPD2, DenseVector arIlzr, DenseVector arVhi, DenseVector arVlo, DenseVector arVlzr,
                                    string PD_BIAS, List<string> REPORT_POWER, List<double> REPORT_CUR, List<double> REPORT_POWER_DBM, double PDresponsivity, string ITH_METHOD, double START, double LZR_COMP, string PD_COMMON_GND, string COMMON_GND,
                                    out Dictionary<string, double> paramMap, out DenseVector Vlzr_2Wire, out DenseVector Vlzr_4Wire)
        {
            paramMap = new Dictionary<string, double>();
            Vlzr_2Wire = null;
            Vlzr_4Wire = null;
            bool dev_good = true;   // Used to short circuit some calculations if preceding data were nonsensical

            string pname_ITH_NAME = "";
            string pname_ITH_EXTRAP_NAME = "";
            string pname_ITH_ERROR_NAME = "";
            string pname_ITH_D2_NAME = "";
            string pname_ITH_RSQ_NAME = "";
            string pname_SlopeEfficiency_V_NAME = "";

            string pname_VBIAS_NAME = "";
            string pname_IBIAS_NAME = "";
            string pname_PELEC_NAME = "";

            string pname_POPT_MAX_NAME = "";
            string pname_VBIASMAX_NAME = "";
            string pname_IBIASMAX_NAME = "";
            string pname_PELECMAX_NAME = "";

            string pname_IKINK_NAME = "";
            string pname_VKINK_NAME = "";
            string pname_POPTKINK_NAME = "";

            double param_VBIAS = 0.0;
            double param_IBIAS = 0.0;
            double param_PELEC = 0.0;

            double param_POPT_MAX = 0.0;
            double param_VBIASMAX = 0.0;
            double param_IBIASMAX = 0.0;
            double param_PELECMAX = 0.0;

            double param_IKINK = 0.0;
            double param_VKINK = 0.0;
            double param_POPTKINK = 0.0;

            double param_V_TurnOn = 0.0;
            double param_Laser_RS = 0.0;
            double param_LZR_IDEALITY = 0.0;
            double param_LZR_PctError = 0.0;

            DenseVector PPD = new DenseVector(1);
            DenseVector PPD1 = new DenseVector(1);
            DenseVector IPD = new DenseVector(1);

            var NUM_PDS = 0;
            if (arIPD1 != null)
            {
                NUM_PDS = 1;
            }
            if (arIPD1 != null && arIPD2 != null)
            {
                NUM_PDS = 2;
            }
            
            InitializeParameters(ref paramMap, NUM_PDS, REPORT_POWER, PD_BIAS);

            // Force code to get to the end of the function.  Not good design but would take several days to rework this code
            // Need to always report OPWR/CUROPT parameters even if there's an exception
            
                try
                {
                    // Check input parameters
                    if (arIPD1 == null || arIlzr == null || arVhi == null || arVlo == null || arVlzr == null)
                    {
                        dev_good = false;
                    }

                    if (true == dev_good)
                    {
                        Vlzr_4Wire = arVhi - arVlo;
                        Vlzr_2Wire = arVlzr;

                        if (COMMON_GND == "N")
                        {
                            Vlzr_4Wire = -Vlzr_4Wire;
                        }
                        if (Math.Abs(Vlzr_2Wire.Last()) > .9 * Math.Abs(LZR_COMP))
                        {
                            dev_good = false;
                        }
                    }

                    if (true == dev_good)
                    {
                        // Voltage params

                        if (START == 0)
                        {
                            param_V_TurnOn = arVhi[1] - arVlo[1];
                        }
                        else
                        {
                            param_V_TurnOn = arVhi[0] - arVlo[0];
                        }

                        paramMap[get_pname_V_TurnOn(PD_BIAS)] = param_V_TurnOn;

                        FitRes(arIlzr.ToList<double>(), Vlzr_4Wire.ToList<double>(), out param_Laser_RS, out param_LZR_IDEALITY,
                            out param_LZR_PctError);

                        paramMap[get_pname_Laser_RS(PD_BIAS)] = param_Laser_RS;
                        paramMap[get_pname_LZR_IDEALITY(PD_BIAS)] = param_LZR_IDEALITY;
                        paramMap[get_pname_Laser_RS_PctError(PD_BIAS)] = param_LZR_PctError;

                        // BAD RS
                        if ((param_Laser_RS < 0) || (param_Laser_RS > 20))
                        {
                            dev_good = false;
                        }
                    }

                    if (true == dev_good)
                    {
                        for (int nPDidx = 1; nPDidx <= NUM_PDS; nPDidx++)
                        {
                            if (nPDidx == 1)
                                IPD = arIPD1;
                            else
                                IPD = arIPD2;

                        // Threshold current and slope efficiency for each PD
                        double tap_perc = .06;
                        if (USE_QBD)
                        {
                            PPD = IPD / PDresponsivity / tap_perc;
                        }
                        else
                        {
                            PPD = IPD / PDresponsivity;
                        }

                        int IPD_5mA_i = -1;
                        var IPD_5mA_idx_col = VecMath.FindIndex(IPD, item => (item >= 0.005));
                        if (IPD_5mA_idx_col.Count <= 0)
                        {
                            IPD_5mA_i = IPD.Count;
                        }
                        else
                        {
                            IPD_5mA_i = IPD_5mA_idx_col[0];
                        }

                        double thresh = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        double thresh_fit = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        double thresh_error = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        double thresh_curv = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        double thresh_rsq = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        double slope_eff = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

                        if (IPD_5mA_i < 4)
                        {
                            Debug.WriteLine("Sweep_DFB.computePDparams(): Device appears to be shorted, IPD hits 5mA early in sweep.");
                        }
                        else
                        {
                            DenseVector IPD_short = (DenseVector)IPD.SubVector(0, Math.Min(IPD_5mA_i + 1, IPD.Count));
                            DenseVector deriv = (DenseVector)VecMath.diff(IPD_short);
                            DenseVector curv = (DenseVector)VecMath.diff(deriv);
                            DenseVector rsqs = (DenseVector)VecMath.ones(IPD_short.Count);

                            // Restrict fitting range and get better fits, avoiding R^2 glitch at low bias current
                            var start_idx_col = VecMath.FindIndex(arIlzr, item => (item >= 0.005));
                            int start_i = start_idx_col[0];
                            DenseVector vecIlzr = DenseVector.OfEnumerable(arIlzr);

                            for (int j = start_i; j < rsqs.Count - 1; j++)
                            {
                                rsqs[j] = VecMath.calc_fit_rsq(DenseVector.OfVector(vecIlzr.SubVector(0, j + 1)),
                                    DenseVector.OfVector(IPD.SubVector(0, j + 1)));
                            }

                            int min_i = curv.MaximumIndex();
                            int thresh_i_curv = min_i;
                            thresh_curv = arIlzr[thresh_i_curv];

                            min_i = rsqs.MinimumIndex();

                            int thresh_i_rsq = 0;
                            thresh_rsq = 0.0;
                            var RSQ_90 = VecMath.FindIndex(rsqs, item => (item < 0.9));
                            if (RSQ_90.Count == 0)
                            {
                                thresh_i_rsq = min_i;
                                thresh_rsq = arIlzr[thresh_i_rsq];
                            }
                            else
                            {
                                thresh_i_rsq = RSQ_90[0];
                                thresh_rsq = arIlzr[thresh_i_rsq];
                            }

                            // Which threshold do we pick?
                            thresh = 0.0;
                            int thresh_i = 0;
                            if (ITH_METHOD.Equals("CURV"))
                            {
                                thresh = thresh_curv;
                                thresh_i = thresh_i_curv;
                            }
                            else
                            {
                                thresh = thresh_rsq;
                                thresh_i = thresh_i_rsq;
                            }

                            var P = MathNet.Numerics.Fit.Polynomial(vecIlzr.SubVector(thresh_i, 6).ToArray(),
                                PPD.SubVector(thresh_i, 6).ToArray(), 1); // polynomial of order 1

                            slope_eff = P[1];
                            thresh_fit = -P[0] / P[1];
                            thresh_error = thresh - thresh_fit;
                        }

                        pname_ITH_NAME = get_pname_ITH(getPDNAME(nPDidx), PD_BIAS);
                        pname_ITH_EXTRAP_NAME = get_pname_ITH_EXTRAP(getPDNAME(nPDidx), PD_BIAS);
                        pname_ITH_ERROR_NAME = get_pname_ITH_ERROR(getPDNAME(nPDidx), PD_BIAS);
                        pname_ITH_D2_NAME = get_pname_ITH_D2(getPDNAME(nPDidx), PD_BIAS);
                        pname_ITH_RSQ_NAME = get_pname_ITH_RSQ(getPDNAME(nPDidx), PD_BIAS);
                        pname_SlopeEfficiency_V_NAME = get_pname_SlopeEfficiency_V(getPDNAME(nPDidx), PD_BIAS);

                        paramMap[pname_ITH_NAME] = thresh;
                        paramMap[pname_ITH_EXTRAP_NAME] = thresh_fit;
                        paramMap[pname_ITH_ERROR_NAME] = thresh_error;
                        paramMap[pname_ITH_D2_NAME] = thresh_curv;
                        paramMap[pname_ITH_RSQ_NAME] = thresh_rsq;
                        paramMap[pname_SlopeEfficiency_V_NAME] = slope_eff;

                        // VBIAS/IBIAS/PELEC for each PD
                        for (int nReportPowerIdx = 0; nReportPowerIdx < REPORT_POWER.Count; nReportPowerIdx++)
                        {
                            pname_VBIAS_NAME = get_pname_VBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(nPDidx), PD_BIAS);
                            pname_IBIAS_NAME = get_pname_IBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(nPDidx), PD_BIAS);
                            pname_PELEC_NAME = get_pname_PELEC(REPORT_POWER[nReportPowerIdx], getPDNAME(nPDidx), PD_BIAS);

                            param_VBIAS = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                            param_IBIAS = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                            param_PELEC = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

                            List<int> P_index = VecMath.FindIndex(PPD,
                                x => x > Convert.ToDouble(REPORT_POWER[nReportPowerIdx]) / 1000.0);
                            if (P_index.Count > 0 && P_index[0] > 0)
                            {
                                param_VBIAS = arVhi[P_index[0]] - arVlo[P_index[0]];
                                param_IBIAS = arIlzr[P_index[0]];
                                param_PELEC = arIlzr[P_index[0]] * (arVhi[P_index[0]] - arVlo[P_index[0]]);
                            }

                            //Save the param
                            paramMap[pname_VBIAS_NAME] = param_VBIAS;
                            paramMap[pname_IBIAS_NAME] = param_IBIAS;
                            paramMap[pname_PELEC_NAME] = param_PELEC;
                        }

                        // Max per PD
                        param_POPT_MAX = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        param_VBIASMAX = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        param_IBIASMAX = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                        param_PELECMAX = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

                        pname_POPT_MAX_NAME = get_pname_POPT_MAX(getPDNAME(nPDidx), PD_BIAS);
                        pname_VBIASMAX_NAME = get_pname_VBIASMAX(getPDNAME(nPDidx), PD_BIAS);
                        pname_IBIASMAX_NAME = get_pname_IBIASMAX(getPDNAME(nPDidx), PD_BIAS);
                        pname_PELECMAX_NAME = get_pname_PELECMAX(getPDNAME(nPDidx), PD_BIAS);

                        param_POPT_MAX = 1000 * PPD[PPD.Count - 1];

                        int nEndIdx = arVhi.Count - 1;
                        param_VBIASMAX = arVhi[nEndIdx] - arVlo[nEndIdx];
                        param_IBIASMAX = arIlzr[nEndIdx];
                        param_PELECMAX = arIlzr[nEndIdx] * (arVhi[nEndIdx] - arVlo[nEndIdx]);

                        paramMap[pname_POPT_MAX_NAME] = param_POPT_MAX;
                        paramMap[pname_VBIASMAX_NAME] = param_VBIASMAX;
                        paramMap[pname_IBIASMAX_NAME] = param_IBIASMAX;
                        paramMap[pname_PELECMAX_NAME] = param_PELECMAX;

                        // Kink parameters
                        Vector dIpd = VecMath.diff(IPD);
                        Vector ddIpd = VecMath.diff(dIpd);
                        Vector dIpd1 = DenseVector.OfVector(VecMath.Abs(ddIpd) * 1000);

                        double param_ITH = thresh;
                        List<int> peakloc;
                        List<double> arPeakMag;
                        peakutils.peakfinder(dIpd1.ToList<double>(), out peakloc, out arPeakMag);

                        List<int> kinkloc = null;
                        FindKinkLoc(arIlzr.ToList<double>(), peakloc, param_ITH, out kinkloc);

                        string pname_NUMKINKS = get_pname_NUMKINKS(getPDNAME(nPDidx), PD_BIAS);

                        paramMap[pname_NUMKINKS] = kinkloc.Count;

                        // Only do first KINK
                        for (int k = 1; k <= MAX_KINKS; k++)
                        {
                            pname_IKINK_NAME = get_pname_IKINK(getPDNAME(nPDidx), k, PD_BIAS);
                            pname_VKINK_NAME = get_pname_VKINK(getPDNAME(nPDidx), k, PD_BIAS);
                            pname_POPTKINK_NAME = get_pname_POPTKINK(getPDNAME(nPDidx), k, PD_BIAS);

                            double kinkcurr = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                            double kinkvolt = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                            double kinkpopt = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                            try
                            {
                                kinkcurr = arIlzr[peakloc[kinkloc[k - 1]]];
                                kinkvolt = Vlzr_4Wire[peakloc[kinkloc[k - 1]]];
                                kinkpopt = PPD[peakloc[kinkloc[k - 1]]] * 1000;
                            }
                            catch (Exception ex)
                            {

                            }

                            param_IKINK = kinkcurr;
                            param_VKINK = kinkvolt;
                            param_POPTKINK = kinkpopt;

                            paramMap[pname_IKINK_NAME] = param_IKINK;
                            paramMap[pname_VKINK_NAME] = param_VKINK;
                            paramMap[pname_POPTKINK_NAME] = param_POPTKINK;
                        }
                    }

                    // Use IPD2 only for determining power and some laser parameters
                    if (NUM_PDS > 1)
                    {
                        computeLZR_TOTALparams(arIPD1, arIPD2, arIlzr, arVhi, arVlo,
                            USE_QBD, PD_BIAS, REPORT_POWER, PDresponsivity,
                            ref paramMap);

                        computeLZR_RATIOparams(PD_BIAS, REPORT_POWER, PDresponsivity,
                            ref paramMap);
                    }
                }
            }
            catch
            {
                dev_good = false;
            }

            Dictionary<string, DenseVector> calculate_against_current = new Dictionary<string, DenseVector>();
            Dictionary<string, DenseVector> calculate_against_power = new Dictionary<string, DenseVector>();

            if (true == dev_good && NUM_PDS > 1)
            {
                double tap_percentage = .06;
                if (USE_QBD)
                {
                    PPD1 = VecMath.MilliwattToDbm(arIPD1 / PDresponsivity / tap_percentage * 1000);
                    PPD = VecMath.MilliwattToDbm(arIPD2 / PDresponsivity / tap_percentage * 1000);
                }
                else
                {
                    PPD1 = VecMath.MilliwattToDbm(arIPD2 / PDresponsivity * 1000);
                    PPD = VecMath.MilliwattToDbm(arIPD2 / PDresponsivity * 1000);
                }
            }


            if (NUM_PDS == 1)
            {
                calculate_against_current = new Dictionary<string, DenseVector>
            {
                { "IPD_BACK", arIPD1 },
                { "OPWR_BACK", PPD1 },
                { "OPWR_FRONT", PPD },
                { "VLZR", Vlzr_4Wire }
            };
            }
            else if (NUM_PDS == 2)
            {
                calculate_against_current = new Dictionary<string, DenseVector>
            {
                { "IPD_BACK", arIPD1 },
                { "IPD_FRONT", arIPD2 },
                { "OPWR_BACK", PPD1 },
                { "OPWR_FRONT", PPD },
                { "VLZR", Vlzr_4Wire }
            };
            }

            calculate_against_power = new Dictionary<string, DenseVector>
            {
                { "CUROPT", arIlzr },
                { "VOLOPT", Vlzr_4Wire }
            };

            paramMap = paramMap.Concat(Parametric_Analysis.OpwrAndCuropt(calculate_against_current, arIlzr, REPORT_CUR,
                calculate_against_power, PPD, REPORT_POWER_DBM, dev_good)).ToDictionary((xx) => xx.Key, (xx) => xx.Value);

            return true;
        }

        private static void InitializeParameters(ref Dictionary<string, double> paramMap, int NUM_PDS, List<string> REPORT_POWER, string PD_BIAS)
        {
            //Initialize parameters
            paramMap.Add(get_pname_V_TurnOn(PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
            paramMap.Add(get_pname_Laser_RS(PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
            paramMap.Add(get_pname_LZR_IDEALITY(PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
            paramMap.Add(get_pname_Laser_RS_PctError(PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);

            for (int nPDidx = 1; nPDidx <= NUM_PDS; nPDidx++)
            {
                paramMap.Add(get_pname_ITH(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_ITH_EXTRAP(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_ITH_ERROR(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_ITH_D2(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_ITH_RSQ(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_SlopeEfficiency_V(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                for (int nReportPowerIdx = 0; nReportPowerIdx < REPORT_POWER.Count; nReportPowerIdx++)
                {
                    paramMap.Add(get_pname_VBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_IBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_PELEC(REPORT_POWER[nReportPowerIdx], getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                }

                paramMap.Add(get_pname_POPT_MAX(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_VBIASMAX(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_IBIASMAX(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_PELECMAX(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);

                paramMap.Add(get_pname_NUMKINKS(getPDNAME(nPDidx), PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);

                for (int k = 1; k <= MAX_KINKS; k++)
                {
                    paramMap.Add(get_pname_IKINK(getPDNAME(nPDidx), k, PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_VKINK(getPDNAME(nPDidx), k, PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_POPTKINK(getPDNAME(nPDidx), k, PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                }
            }
            if (NUM_PDS > 1)
            {
                paramMap.Add(get_pname_ITH_DELTA(PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                paramMap.Add(get_pname_SlopeEfficiency_V_Ratio(PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);

                for (int nReportPowerIdx = 0; nReportPowerIdx < REPORT_POWER.Count; nReportPowerIdx++)
                {
                    paramMap.Add(get_pname_VBIAS_TOTAL(REPORT_POWER[nReportPowerIdx], PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_IBIAS_TOTAL(REPORT_POWER[nReportPowerIdx], PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_PELEC_TOTAL(REPORT_POWER[nReportPowerIdx], PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                }

                for (int nReportPowerIdx = 0; nReportPowerIdx < REPORT_POWER.Count; nReportPowerIdx++)
                {
                    paramMap.Add(get_pname_VBIAS_RATIO(REPORT_POWER[nReportPowerIdx], PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_IBIAS_RATIO(REPORT_POWER[nReportPowerIdx], PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                    paramMap.Add(get_pname_PELEC_RATIO(REPORT_POWER[nReportPowerIdx], PD_BIAS), ErrorCodes.INITIAL_PARAMETER_VALUE);
                }
            }
        }

        static bool FindKinkLoc(List<double> arIlzr, List<int> peakLoc, double param_ITH, out List<int> kinkloc)
        {
            kinkloc = new List<int>();

            for (int n = 0; n < peakLoc.Count; n++)
            {
                if (arIlzr[peakLoc[n]] > param_ITH + 0.005)
                {
                    kinkloc.Add(n);
                    break;
                }
            }

            return true;
        }

        public static bool computeLZR_TOTALparams(DenseVector arIPD1, DenseVector arIPD2, DenseVector arIlzr, DenseVector arVhi, DenseVector arVlo,
                                                    bool USE_QBD, string PD_BIAS, List<string> REPORT_POWER, double PDresponsivity,
                                                    ref Dictionary<string, double> paramMap)
        {
            string pname_ITH_DELTA = get_pname_ITH_DELTA(PD_BIAS);
            string pname_ITH1 = get_pname_ITH(getPDNAME(1), PD_BIAS);
            string pname_ITH2 = get_pname_ITH(getPDNAME(2), PD_BIAS);

            double param_ITH1 = paramMap[pname_ITH1];
            double param_ITH2 = paramMap[pname_ITH2];
            double param_ITH_DELTA = 9999.9;

            if ((isEqualValue(param_ITH1, ErrorCodes.INVALID_POST_CALCULATION_VALUE)) ||
                (isEqualValue(param_ITH2, ErrorCodes.INVALID_POST_CALCULATION_VALUE)))
            {
                param_ITH_DELTA = 9999.9;
            }
            else
            {
                param_ITH_DELTA = param_ITH2 - param_ITH1;
            }
            paramMap[pname_ITH_DELTA] = param_ITH_DELTA;

            string pname_SLOPE_EFF_RATIO = get_pname_SlopeEfficiency_V_Ratio(PD_BIAS);
            string pname_SLOPE1 = get_pname_SlopeEfficiency_V(getPDNAME(1), PD_BIAS);
            string pname_SLOPE2 = get_pname_SlopeEfficiency_V(getPDNAME(2), PD_BIAS);

            double param_SLOPE1 = paramMap[pname_SLOPE1];
            double param_SLOPE2 = paramMap[pname_SLOPE2];
            double param_SLOPE_EFF_RATIO = 9999.9;

            if ((isEqualValue(param_SLOPE1, ErrorCodes.INVALID_POST_CALCULATION_VALUE)) ||
                (isEqualValue(param_SLOPE2, ErrorCodes.INVALID_POST_CALCULATION_VALUE)))
            {
                param_SLOPE_EFF_RATIO = 9999.9;
            }
            else
            {
                param_SLOPE_EFF_RATIO = param_SLOPE2 / param_SLOPE1;
            }
            paramMap[pname_SLOPE_EFF_RATIO] = param_SLOPE_EFF_RATIO;

            DenseVector PPD;
            DenseVector IPD;

            string pname_VBIAS_TOTAL = "";
            string pname_IBIAS_TOTAL = "";
            string pname_PELEC_TOTAL = "";

            double param_VBIAS_TOTAL = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_IBIAS_TOTAL = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_PELEC_TOTAL = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

            for (int nReportPowerIdx = 0; nReportPowerIdx < REPORT_POWER.Count; nReportPowerIdx++)
            {
                IPD = arIPD1 + arIPD2;

                double tap_perc = .06;
                if (USE_QBD) // Assuming this is an STX device
                {
                    PPD = IPD / PDresponsivity / tap_perc;
                }
                else
                {
                    PPD = IPD / PDresponsivity;
                }

                pname_VBIAS_TOTAL = get_pname_VBIAS_TOTAL(REPORT_POWER[nReportPowerIdx], PD_BIAS);
                pname_IBIAS_TOTAL = get_pname_IBIAS_TOTAL(REPORT_POWER[nReportPowerIdx], PD_BIAS);
                pname_PELEC_TOTAL = get_pname_PELEC_TOTAL(REPORT_POWER[nReportPowerIdx], PD_BIAS);

                param_VBIAS_TOTAL = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_IBIAS_TOTAL = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_PELEC_TOTAL = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

                List<int> P_index = VecMath.FindIndex(PPD, x => x > Convert.ToDouble(REPORT_POWER[nReportPowerIdx]) / 1000.0);
                if (P_index.Count > 0)
                {
                    param_VBIAS_TOTAL = arVhi[P_index[0]] - arVlo[P_index[0]];
                    param_IBIAS_TOTAL = arIlzr[P_index[0]];
                    param_PELEC_TOTAL = arIlzr[P_index[0]] * (arVhi[P_index[0]] - arVlo[P_index[0]]);
                }

                //Save the param
                paramMap[pname_VBIAS_TOTAL] = param_VBIAS_TOTAL;
                paramMap[pname_IBIAS_TOTAL] = param_IBIAS_TOTAL;
                paramMap[pname_PELEC_TOTAL] = param_PELEC_TOTAL;
            }

            return true;
        }

        public static bool computeLZR_RATIOparams(string PD_BIAS, List<string> REPORT_POWER, double PDresponsivity,
                                                    ref Dictionary<string, double> paramMap)
        {
            string pname_VBIAS_RATIO = "";
            string pname_IBIAS_RATIO = "";
            string pname_PELEC_RATIO = "";

            double param_VBIAS_RATIO = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_IBIAS_RATIO = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_PELEC_RATIO = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

            string pname_VBIAS1 = "";
            string pname_IBIAS1 = "";
            string pname_PELEC1 = "";

            double param_VBIAS1 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_IBIAS1 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_PELEC1 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

            string pname_VBIAS2 = "";
            string pname_IBIAS2 = "";
            string pname_PELEC2 = "";

            double param_VBIAS2 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_IBIAS2 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
            double param_PELEC2 = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

            for (int nReportPowerIdx = 0; nReportPowerIdx < REPORT_POWER.Count; nReportPowerIdx++)
            {
                // VBIAS/IBIAS/PELEC Ratios
                pname_VBIAS_RATIO = get_pname_VBIAS_RATIO(REPORT_POWER[nReportPowerIdx], PD_BIAS);
                pname_IBIAS_RATIO = get_pname_IBIAS_RATIO(REPORT_POWER[nReportPowerIdx], PD_BIAS);
                pname_PELEC_RATIO = get_pname_PELEC_RATIO(REPORT_POWER[nReportPowerIdx], PD_BIAS);

                pname_VBIAS1 = get_pname_VBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(1), PD_BIAS);
                pname_IBIAS1 = get_pname_IBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(1), PD_BIAS);
                pname_PELEC1 = get_pname_PELEC(REPORT_POWER[nReportPowerIdx], getPDNAME(1), PD_BIAS);

                pname_VBIAS2 = get_pname_VBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(2), PD_BIAS);
                pname_IBIAS2 = get_pname_IBIAS(REPORT_POWER[nReportPowerIdx], getPDNAME(2), PD_BIAS);
                pname_PELEC2 = get_pname_PELEC(REPORT_POWER[nReportPowerIdx], getPDNAME(2), PD_BIAS);

                param_VBIAS1 = paramMap[pname_VBIAS1];
                param_IBIAS1 = paramMap[pname_IBIAS1];
                param_PELEC1 = paramMap[pname_PELEC1];

                param_VBIAS2 = paramMap[pname_VBIAS2];
                param_IBIAS2 = paramMap[pname_IBIAS2];
                param_PELEC2 = paramMap[pname_PELEC2];

                param_VBIAS_RATIO = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_IBIAS_RATIO = ErrorCodes.INVALID_POST_CALCULATION_VALUE;
                param_PELEC_RATIO = ErrorCodes.INVALID_POST_CALCULATION_VALUE;

                if ((param_VBIAS1 != ErrorCodes.INVALID_POST_CALCULATION_VALUE) && (param_VBIAS2 != ErrorCodes.INVALID_POST_CALCULATION_VALUE))
                {
                    param_VBIAS_RATIO = param_VBIAS2 / param_VBIAS1;
                    param_IBIAS_RATIO = param_IBIAS2 / param_IBIAS1;
                    param_PELEC_RATIO = param_PELEC2 / param_PELEC1;
                }

                paramMap[pname_VBIAS_RATIO] = param_VBIAS_RATIO;
                paramMap[pname_IBIAS_RATIO] = param_IBIAS_RATIO;
                paramMap[pname_PELEC_RATIO] = param_PELEC_RATIO;
            }

            return true;
        }

        public static bool isEqualValue(double value, double refValue)
        {
            return (Math.Abs(value - refValue) < 0.01);
        }

        public override bool execute()
        {
            start_time = DateTime.UtcNow;
            
            base.execute();
            log("Sweep_DFB.execute()");

            #region Common ground info
            COMMON_GND = (_myParam.mapParams.ContainsKey("COMMON_GND")) ? (_myParam.mapParams["COMMON_GND"]) : "NONE";
            PD_COMMON_GND = (_myParam.mapParams.ContainsKey("PD_COMMON_GND")) ? _myParam.mapParams["PD_COMMON_GND"] : "NONE";
            
            if (COMMON_GND == "P")
            {
                this._Common = DeviceCommonConnectionType.CommonP;
            }
            else if (COMMON_GND == "N")
            {
                this._Common = DeviceCommonConnectionType.CommonN;
            }

            if (PD_COMMON_GND == "P")
            {
                this._PDCommon = DeviceCommonConnectionType.CommonP;
            }
            else if (PD_COMMON_GND == "N")
            {
                this._PDCommon = DeviceCommonConnectionType.CommonN;
            }
            #endregion Common ground info

            string PD1P = "";
            string PD1N = "";
            if (PD_COMMON_GND == "NONE")
            {
                PD1P = _myParam.mapParams["PD1P"];
                PD1N = _myParam.mapParams["PD1N"];
            }
            else if(PD_COMMON_GND == "P")
            {
                PD1P = _myParam.mapParams["PD1P"]; //PRE PD is not on common P
            }
            else if(PD_COMMON_GND == "N")
            {
                PD1P = _myParam.mapParams["PD1P"];
            }

            string PD2P = ""; //Don't get test param yet because it might NOT exist...
            string PD2N = ""; //Don't get test param yet because it might NOT exist...

            string QBD1P = "";
            string QBD1N = "";
            string QBD2P = "";
            string QBD2N = "";

            string LZRN = ""; 
            string LZRP = ""; 
            string LZRN_SENSE = "";
            string LZRP_SENSE = "";
            string isSense = (_myParam.mapParams.ContainsKey("SENSE")) ? (_myParam.mapParams["SENSE"]) : "Y";
           
            if (COMMON_GND == "NONE")
            {
                LZRN = _myParam.mapParams["LZRN"];
                LZRP = _myParam.mapParams["LZRP"];
                if (isSense == "Y")
                {
                    LZRN_SENSE = _myParam.mapParams["LZRN_SENSE"];
                    LZRP_SENSE = _myParam.mapParams["LZRP_SENSE"];
                }
            }
            else if(COMMON_GND == "P")
            {
                LZRP = _myParam.mapParams["LZRN"];
                if (isSense == "Y")
                {
                    LZRP_SENSE = _myParam.mapParams["LZRN_SENSE"];
                    LZRN_SENSE = _myParam.mapParams["LZRP_SENSE"];
                }
            }
            else if(COMMON_GND == "N")
            {
                LZRP = _myParam.mapParams["LZRP"];
                if (isSense == "Y")
                {
                    LZRN_SENSE = _myParam.mapParams["LZRN_SENSE"];
                    LZRP_SENSE = _myParam.mapParams["LZRP_SENSE"];
                }
            }
            
            double PD_BIAS = Convert.ToDouble(_myParam.mapParams["PD_BIAS"]);
            string PD_COMP = _myParam.mapParams["PD_COMP"];
            string LZR_COMP = _myParam.mapParams["LZR_COMP"];
            double QBD_BIAS = 0.0;
            double Start = Convert.ToDouble(_myParam.mapParams["START"]);
            double Stop = Convert.ToDouble(_myParam.mapParams["STOP"]);
            double Step = Convert.ToDouble(_myParam.mapParams["STEP"]);
            double PDresponsivity = Convert.ToDouble(_myParam.mapParams["PDresponsivity"]);
            int NUM_PDS = 1;

            string[] PDNAMES = new string[2] { "BACK", "FRONT" };
            //string[] arPDBias = new string[1] { "5" };

            // This section is the old version of the if-then.
            //if (isParamFieldValid("PD2N") && isParamFieldValid("PD2P"))
            //{
            //    NUM_PDS = 2;
            //    PDNAMES = new string[2] { "BACK", "FRONT" };
            //    PD2P = _myParam.mapParams["PD2P"];
            //    PD2N = _myParam.mapParams["PD2N"];
            //}
            if (_myParam.mapParams.ContainsKey("PD2P") && _myParam.mapParams.ContainsKey("PD2N")) // This section is new for this version.
            {                                                                                     // Because using TryParse in SweepBase 
                NUM_PDS = 2;                                                                      // does not work for semicolon separated 
                PDNAMES = new string[2] { "BACK", "FRONT" };                                      // lists, just single values.
                if (PD_COMMON_GND == "NONE")
                {
                    PD2P = _myParam.mapParams["PD2P"];
                    PD2N = _myParam.mapParams["PD2N"];
                }
                else if (PD_COMMON_GND == "P")
                {
                    PD2P = _myParam.mapParams["PD2N"];
                }
                else if (PD_COMMON_GND == "N")
                {
                    PD2P = _myParam.mapParams["PD2P"];
                }
            }
            else
            {
                NUM_PDS = 1;
                PDNAMES = new string[1] { "BACK" };
            }

            // Report currents for VLZR/OPWR/etc
            if (false == _myParam.mapParams.ContainsKey("REPORT_CUR")
                || false == ParseListParams(_myParam.mapParams["REPORT_CUR"], out REPORT_CUR))
            {
                REPORT_CUR = new List<double>();
            }

            // Report powers for VOLOPT/CUROPT
            if (false == _myParam.mapParams.ContainsKey("REPORT_PWR_DBM")
                || false == ParseListParams(_myParam.mapParams["REPORT_PWR_DBM"], out REPORT_POWER_DBM))
            {
                REPORT_POWER_DBM = new List<double>();
            }

            // Report powers for IBIAS/VBIAS/PELEC
            List<string> REPORT_POWER = null;

            try
            {
                string REPORT_VAL = this._myParam.mapParams["REPORT_PWR"];
                REPORT_POWER = REPORT_VAL.Split(';').ToList<string>();
            }
            catch (Exception)
            {
                REPORT_POWER = new List<string>();
                REPORT_POWER.Add("5");
                REPORT_POWER.Add("7");
                REPORT_POWER.Add("10");
                REPORT_POWER.Add("12.5");
            }

            int MAX_KINKS = 3;
            if (isParamFieldValid("NUM_KINKS"))
            {
                MAX_KINKS = Convert.ToInt32(_myParam.mapParams["NUM_KINKS"]);
            }

            int EO_QBDarm = 0;
            if (SharedData.Global.ContainsKey("EO_QBDarm"))
            {
                EO_QBDarm = Convert.ToInt32(SharedData.Global["EO_QBDarm"]);
                QBD_BIAS = EO_QBDarm;
            }
            else
            {
                USE_QBD = false;
                QBD_BIAS = 0;
            }

            int QBDNUM = 1;

            connectMatrix(QBDNUM, NUM_PDS, PD_BIAS,
                            QBD1N, QBD1P,
                            QBD2N, QBD2P,
                            LZRN, LZRP,
                            LZRN_SENSE, LZRP_SENSE,
                            PD1N, PD1P,
                            PD2N, PD2P);

            // Configure SMUs
            int nPoints = (int)((Stop - Start) / Step) + 1;

            string strIPD1 = "IPD1";
            string strIPD2 = "IPD2";
            string strVLO = "VLO";
            string strVHI = "VHI";
            string strVLZR = "VLZR";

            bool bSuccess = false;
            // Added ATime and s_Delay for NI SMU.  Value is determined emperically and hard coded based on measurement needs
            string ATime = "0.001";
            string s_Delay = "0.0001";
            string Hold = "0"; 
            for (int nTry = 0; nTry < this._myAppConfig.nMaxDataAcqTries; nTry++)
            {
                bSuccess = false;
                try
                {
                    _spa.SetupSMUs(SPA_CONSTANTS.MMode_StaircaseSweep);

                    if (COMMON_GND == "NONE")
                    {
                        _spa.SetupSweepSource(SPA_CONSTANTS.SMU5, SPA_CONSTANTS.I, SPA_CONSTANTS.SweepMode_LinearSweepSingleStair, "20", Start, Stop, nPoints, ATime, s_Delay, Hold, LZR_COMP, LZR_COMP);
                    }
                    else
                    {
                        _spa.SetupSweepSource(SPA_CONSTANTS.SMU5, SPA_CONSTANTS.I, SPA_CONSTANTS.SweepMode_LinearSweepSingleStair, "20", (double)this._Common*Start, (double)this._Common*Stop, nPoints, ATime, s_Delay, Hold, LZR_COMP, LZR_COMP);
                    }
                    _spa.ForceV(SPA_CONSTANTS.SMU1, PD_BIAS.ToString(), PD_BIAS, PD_COMP, "0"); //POR code takes in + bias on N; if using COMMON then use negative bias in test file
                    _spa.ForceV(SPA_CONSTANTS.SMU2, PD_BIAS.ToString(), PD_BIAS, PD_COMP, "0");
                    _spa.ForceI(SPA_CONSTANTS.SMU4, "2E-8", 0, "10", "0");
                    _spa.ForceI(SPA_CONSTANTS.SMU6, "2E-8", 0, "10", "0");

                    _spa.SetupSweepMeasure(SPA_CONSTANTS.SMU1, SPA_CONSTANTS.I, "1e-4", strIPD1);
                    _spa.SetupSweepMeasure(SPA_CONSTANTS.SMU2, SPA_CONSTANTS.I, "1e-4", strIPD2);
                    _spa.SetupSweepMeasure(SPA_CONSTANTS.SMU5, SPA_CONSTANTS.V, "20", strVLZR);
                    _spa.SetupSweepMeasure(SPA_CONSTANTS.SMU4, SPA_CONSTANTS.V, "20", strVLO);
                    _spa.SetupSweepMeasure(SPA_CONSTANTS.SMU6, SPA_CONSTANTS.V, "20", strVHI);

                    _spa.SetTriggerIn(SPA_CONSTANTS.TRIGGER_OFF);
                    _spa.SetTriggerOut(SPA_CONSTANTS.TRIGGER_OFF);
                    _spa.RunSweepAndAcquire();

                    bSuccess = true;
                }
                catch (Exception ex)
                {
                    log(ex.ToString());
                    InstrumentX myInstrument = (InstrumentX)_spa;
                    myInstrument.stop();

                    if (nTry == (this._myAppConfig.nMaxDataAcqTries - 1))
                    {
                        throw ex;
                    }
                }

                if (bSuccess)
                {
                    log("Sweep_DFB_no_FILT data acq suceeded!");
                    break;
                }
            }

            CSPA_DataSeries arIPD1 = _spa.GetSweepData(strIPD1);
            CSPA_DataSeries arIPD2 = null;
            if (NUM_PDS != 1)
            {
                arIPD2 = _spa.GetSweepData(strIPD2);
            }
            CSPA_DataSeries arVLO = _spa.GetSweepData(strVLO);
            CSPA_DataSeries arVHI = _spa.GetSweepData(strVHI);
            CSPA_DataSeries arILZR = _spa.GetSweepData(SPA_CONSTANTS.SOURCENAME);
            CSPA_DataSeries arVLZR = _spa.GetSweepData(strVLZR);

            // Add in record of which QBD was used
            DenseVector vecIPD1 = DenseVector.OfEnumerable(arIPD1.data);

            DenseVector vecIPD2 = null;
            if (NUM_PDS != 1)
            {
                vecIPD2 = DenseVector.OfEnumerable(arIPD2.data);
            }

            DenseVector vecVLO = DenseVector.OfEnumerable(arVLO.data);
            DenseVector vecVHI = DenseVector.OfEnumerable(arVHI.data);

            DenseVector vecILZR = DenseVector.OfEnumerable(arILZR.data);
            DenseVector vecVLZR = DenseVector.OfEnumerable(arVLZR.data);

            // Flip signs if desired
            if (PD_BIAS < 0.0)
            {
                vecIPD1 = -vecIPD1;
                if (NUM_PDS != 1)
                {
                    vecIPD2 = -vecIPD2;
                }
            }

            DenseVector vecVpd = (DenseVector)VecMath.constVector(vecIPD1.Count, PD_BIAS);

            string ITH_METHOD = "";
            if (_myParam.mapParams.ContainsKey("ITH_METHOD"))
            {
                ITH_METHOD = _myParam.mapParams["ITH_METHOD"];
            }
            else
            {
                ITH_METHOD = "RSQ";
            }

            string strPD_BIAS = _myParam.mapParams["PD_BIAS"];

            //Declare param results
            Dictionary<string, double> paramMap;
            DenseVector vecVlzr_4Wire = null;
            DenseVector vecVlzr_2Wire = null;

            try
            {
                //if (PREBI == "N")
                //{
                    computePDparams(vecIPD1, vecIPD2, vecILZR, vecVHI, vecVLO, vecVLZR,
                                    strPD_BIAS, REPORT_POWER, REPORT_CUR, REPORT_POWER_DBM, PDresponsivity, ITH_METHOD, Start, double.Parse(LZR_COMP), PD_COMMON_GND, COMMON_GND,
                                    out paramMap, out vecVlzr_2Wire, out vecVlzr_4Wire);
                //}
                //else
                //{
                //    computePREBIparams(vecIPD1, vecIPD2, vecILZR, vecVHI, vecVLO, vecVLZR,
                //                    strPD_BIAS, REPORT_POWER, REPORT_CUR, REPORT_POWER_DBM, PDresponsivity, ITH_METHOD, Start, double.Parse(LZR_COMP), PD_COMMON_GND, COMMON_GND, PREBI,
                //                    out paramMap, out vecVlzr_2Wire, out vecVlzr_4Wire);
                //}
                //Add all other wafers params in the data structure...

                
                    foreach (string strParamName in paramMap.Keys)
                    {
                        addWaferTParam(strParamName, paramMap[strParamName], "", "");
                    }
                
                
                
            }
            catch (Exception ex)
            {
                log(ex);
            }

            if (NUM_PDS == 1)
            {
                vecIPD2 = Enumerable.Repeat(9999.99, vecIPD1.Count()).ToArray();
                //arIPD2 = Enumerable.Repeat(9999.99, arIPD1.Count());
            }
            SaveData(arILZR.data, vecVlzr_2Wire.ToList(), vecVlzr_4Wire.ToList(), vecVpd.ToList(), vecIPD1.ToList(), vecIPD2.ToList(), arVHI.data, arVLO.data);
            test_time = (DateTime.UtcNow - start_time).TotalSeconds;

            addWaferTParam("TestTime", test_time);
            //GraphData(arILZR, vecVlzr_2Wire, vecVlzr_4Wire, arIPD1, arIPD2, arVHI, arVLO);
            return true;
        }

        private bool GraphData(
            CSPA_DataSeries arILZR, 
            DenseVector vecVlzr_2Wire, 
            DenseVector vecVlzr_4Wire, 
            CSPA_DataSeries arIPD1, 
            CSPA_DataSeries arIPD2, 
            CSPA_DataSeries arVHI, 
            CSPA_DataSeries arVLO)
        {
            try
            {
                //var figure = GraphEvent(new GraphEventArgsAddFigure(getDeviceInfoString()));
                var graph1 = GraphEvent(new GraphEventArgsAddLineGraph(string.Empty, "ILZR (A)", "VLZR (V)"));
                GraphEvent(new GraphEventArgsPlot("4 Wire Measurement", arILZR.data, vecVlzr_4Wire.ToList(), WpfGraphService.Styles.MatlabStyle("bs-")));
                GraphEvent(new GraphEventArgsPlot("2 Wire Measurement", arILZR.data, vecVlzr_2Wire.ToList(), WpfGraphService.Styles.MatlabStyle("ro-")));

                var graph2 = GraphEvent(new GraphEventArgsAddLineGraph(string.Empty, "ILZR (A)", "IPD (A)"));
                GraphEvent(new GraphEventArgsPlot("IPD1", arILZR.data, arIPD1.data, WpfGraphService.Styles.MatlabStyle("bs-")));
                GraphEvent(new GraphEventArgsPlot("IPD2", arILZR.data, arIPD2.data, WpfGraphService.Styles.MatlabStyle("ro-")));

            }
            catch (NullReferenceException e)
            {
                return false;
            }
            return true;
        }

        public void SaveData(List<double> arILZR, List<double> vecVlzr_2Wire, List<double> vecVlzr_4Wire,
            List<double> vecVpd, List<double> vecIPD1, List<double> vecIPD2, List<double> arVHI, List<double> arVLO)
        {
            string strIlzr = "Ilzr";
            string strVlzr_4Wire = "Vlzr_4Wire";
            string strVlzr_2Wire = "Vlzr_2Wire";
            string strVpd = "Vpd";
            string strIPD1 = "Ipd1";
            string strIPD2 = "Ipd2";
            string strVPsense = "VPsense";
            string strVNsense = "VNsense";

            var myDFBArrayData = new CArrayData();
            myDFBArrayData.AddCol(strIlzr, arILZR);
            myDFBArrayData.AddCol(strVlzr_4Wire, vecVlzr_4Wire);
            myDFBArrayData.AddCol(strVlzr_2Wire, vecVlzr_2Wire);
            myDFBArrayData.AddCol(strVpd, vecVpd);
            myDFBArrayData.AddCol(strIPD1, vecIPD1);
            // Use zeroes if vecIPD2 is null
            myDFBArrayData.AddCol(strIPD2, vecIPD2 ?? VecMath.zeros(vecIPD1.Count).ToList());
            myDFBArrayData.AddCol(strVPsense, arVHI);
            myDFBArrayData.AddCol(strVNsense, arVLO);

            saveArrayData("DFB", myDFBArrayData);
        }
        public override bool posttest()
        {
            _spa.stop();
            _matrix.DisconnectAll();
            base.posttest();
            Debug.Print("Sweep_DFB_no_FILT.posttest()");
            return true;
        }
    }
}

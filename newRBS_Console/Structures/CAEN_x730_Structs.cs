using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace newRBS_Console
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ConnParam
    {
        public int LinkType;
        public int LinkNum;
        public int ConetNode;
        public uint VMEBaseAddress;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public char[] ETHAddress;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_ExtraParameters
    {
        public double decK2; // Deconvolutor k2
        public double decK3; // Deconvolutor k3
        public int deconvolutormode;
        public int trigK; // trigger fast trapezoid rising time
        public int trigm; // trigger fast trapezoid flat top
        public int trigMODE; // 0 threshold on fast trapeziodal
                             // 1 threshold on filtered second deriv of fast trapeziodal
        public int energyFilterMode; // 0 trapezoidal
                                     // 1 peak detector
        public int PD_thrshld1; // peak detector arm threshold 
        public int PD_thrshld2; // peak detector disarm threshold 
        public int PD_winlen; // peak detector inspection window length
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_PHA_Params_t
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] M;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] m;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] k;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] ftd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] a;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] b;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] thr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] nsbl;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] nspk;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] pkho;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] blho;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] trgho;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] dgain;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public float[] enf;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] decimation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] twwdt;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] trgwin;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 16)]
        public CAENDPP_ExtraParameters[] X770_extraparameters;    //parameters for X770 products only 
    }

    // Only for X770
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_TRReset
    {
        public sbyte EnableResetDetector;
        public UInt32 thrhold;
        public UInt32 reslenmin;
        public UInt32 reslenpulse;
    }

    // Only for X770
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_ChannelExtraParameters
    {
        public uint analogPath;
        public uint InputImpedance;
        public uint CRgain;
        public uint PRDSgain;
        public uint SaturationHoldoff;
        public CAENDPP_TRReset ResetDetector;
    }

    // Waveform mode config parameters
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_WaveformParams_t
    {
        public int dualTraceMode; // if true dual trace is enabled
        public int vp1; // First Analog Probe
                        // CAENDPP_PHA_VIRTUALPROBE1_Input             = 0L, // BOTH X724 AND X770
                        // CAENDPP_PHA_VIRTUALPROBE1_Delta             = 1L, // BOTH X724 AND X770
                        // CAENDPP_PHA_VIRTUALPROBE1_Delta2            = 2L, // BOTH X724 AND X770
                        // CAENDPP_PHA_VIRTUALPROBE1_Trapezoid         = 3L, // BOTH X724 AND X770
                        // CAENDPP_PHA_VIRTUALPROBE1_FastTrap          = 4L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE1_TrapBaseline      = 5L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE1_EnergyOut         = 6L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE1_TrapBLCorr        = 7L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE1_None              = 8L,
                        // CAENDPP_PHA_VIRTUALPROBE1_Deconvolved       = 9L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE1_Dev2FastTrap      = 10L, // X770 ONLY
        public int vp2; // Second Analog Probe, ignored if dualTraceMode=false
                        // CAENDPP_PHA_VIRTUALPROBE2_Input             = 0L, // BOTH X724 AND X770
                        // CAENDPP_PHA_VIRTUALPROBE2_S3                = 1L, // X724 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE2_TrapBLCorr        = 2L, // BOTH X724 AND X770
                        // CAENDPP_PHA_VIRTUALPROBE2_TrapBaseline      = 3L, // BOTH X724 AND X770
                        // CAENDPP_PHA_VIRTUALPROBE2_None              = 4L, // X724 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE2_Delta             = 5L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE2_FastTrap          = 6L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE2_Delta2            = 7L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE2_Trapezoid         = 8L, // X770 ONLY
                        // CAENDPP_PHA_VIRTUALPROBE2_EnergyOut         = 9L, // X770 ONLY
        public int dp1; // First Digital probe
                        // CAENDPP_PHA_DigitalProbe1_TrgWin      = 0L,
                        // CAENDPP_PHA_DigitalProbe1_Armed       = 1L,
                        // CAENDPP_PHA_DigitalProbe1_PkRun       = 2L,
                        // CAENDPP_PHA_DigitalProbe1_PURFlag     = 3L,
                        // CAENDPP_PHA_DigitalProbe1_Peaking     = 4L,
                        // CAENDPP_PHA_DigitalProbe1_TVAW        = 5L,
                        // CAENDPP_PHA_DigitalProbe1_BLHoldoff   = 6L,
                        // CAENDPP_PHA_DigitalProbe1_TRGHoldoff  = 7L,
                        // CAENDPP_PHA_DigitalProbe1_TRGVal      = 8L,
                        // CAENDPP_PHA_DigitalProbe1_ACQVeto           = 9L,
                        // CAENDPP_PHA_DigitalProbe1_BFMVeto           = 10L,
                        // CAENDPP_PHA_DigitalProbe1_ExtTRG            = 11L,
                        // CAENDPP_PHA_DigitalProbe1_Trigger           = 12L,
                        // CAENDPP_PHA_DigitalProbe1_None              = 13L,
                        // CAENDPP_PHA_DigitalProbe1_EnergyAccepted    = 14L,
                        // CAENDPP_PHA_DigitalProbe1_Saturation        = 15L,
                        // CAENDPP_PHA_DigitalProbe1_Reset             = 16L,
        public int dp2; // Second Digital probe
                        // CAENDPP_PHA_DigitalProbe2_Trigger   = 0L,
                        // CAENDPP_PHA_DigitalProbe2_None      = 1L,
                        // CAENDPP_PHA_DigitalProbe2_Peaking     = 2L,
                        // CAENDPP_PHA_DigitalProbe2_BLHoldoff   = 3L,
                        // CAENDPP_PHA_DigitalProbe2_PURFlag     = 4L,
                        // CAENDPP_PHA_DigitalProbe2_EnergyAccepted    = 5L,
                        // CAENDPP_PHA_DigitalProbe2_Saturation = 6L,
                        // CAENDPP_PHA_DigitalProbe2_Reset = 7L,

        public int recordLength;
        public int preTrigger;

        // Only for X770
        public int probeTrigger;
        public int probeSelfTriggerVal;
    }

    // List mode config parameters
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_ListParams_t
    {
        public sbyte enabled; // 1 = ListMode Enabled, 0 = ListMode Disabled
        public int saveMode; // CAENDPP_ListSaveMode_Memory     = 0L, // Keep the list events in a memory buffer of maximum size = MAX_LIST_BUFF_NEV
                             // CAENDPP_ListSaveMode_FileBinary = 1L, // Save list events in a binary file.
                             // CAENDPP_ListSaveMode_FileASCII  = 2L, // Save list events in a ASCII file.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 155)]
        public char[] fileName; // the filename used for binary writing (see CAENDPP_ListSaveMode_t)
        public UInt32 maxBuffNumEvents; // the maximum number of events to keep in the buffer if in memory mode (see CAENDPP_ListSaveMode_t)
        public UInt32 saveMask; // The mask of the object to be dumped as defined from 'DUMP_MASK_*' macros.
    }

    // Coincidence parameters
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_CoincParams_t
    {
        public UInt32 CoincChMask;
        public UInt32 MajLevel;
        public UInt32 TrgWin;
        public int CoincOp;
        public int CoincLogic;
    }

    // Only for X770
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_SpectrumControl
    {
        public int SpectrumMode;       // 0 Energy; 1 Time distribution
        public UInt32 TimeScale;          // Scale in time distribution
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct CAENDPP_DgtzParams_t
    {
        // Generic Write
        public int GWn;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000)]
        public uint[] GWaddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000)]
        public uint[] GWdata;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1000)]
        public uint[] GWmask;

        // Channel settings
        public int ChannelMask;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] PulsePolarity;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public int[] DCoffset;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 16)]
        public CAENDPP_ChannelExtraParameters[] ChannelExtraParameters; // Only for X770

        public int EventAggr;
        public CAENDPP_PHA_Params_t DPPParams;
        public int IOlev; //0: NIM; 1: TTL

        // Waveform Mode Settings, they only affect waveforms acquisition mode
        public CAENDPP_WaveformParams_t WFParams;

        // List Mode Settings
        public CAENDPP_ListParams_t ListParams;

        // Parameters for coincidence mode
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 17)]
        public CAENDPP_CoincParams_t[] CoincParams;

        //Spectrum Control setting (Only for X770)
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 16)]
        public CAENDPP_SpectrumControl[] SpectrumControl;

        public void initializeArrays()
        {
            GWaddr = new uint[1000];
            GWdata = new uint[1000];
            GWmask = new uint[1000];

            PulsePolarity = new int[16];
            DCoffset = new int[16];

            ChannelExtraParameters = new CAENDPP_ChannelExtraParameters[16];

            DPPParams.M = new int[16];
            DPPParams.m = new int[16];
            DPPParams.k = new int[16];
            DPPParams.ftd = new int[16];
            DPPParams.a = new int[16];
            DPPParams.b = new int[16];
            DPPParams.thr = new int[16];
            DPPParams.nsbl = new int[16];
            DPPParams.nspk = new int[16];
            DPPParams.pkho = new int[16];
            DPPParams.blho = new int[16];
            DPPParams.trgho = new int[16];
            DPPParams.dgain = new int[16];
            DPPParams.enf = new float[16];
            DPPParams.decimation = new int[16];
            DPPParams.twwdt = new int[16];
            DPPParams.trgwin = new int[16];
            DPPParams.X770_extraparameters = new CAENDPP_ExtraParameters[16];

            ListParams.fileName = new char[155];

            CoincParams = new CAENDPP_CoincParams_t[17];

            SpectrumControl = new CAENDPP_SpectrumControl[16];
        }

        public void setDefaultConfig()
        {
            ChannelMask = 0xFF;
            EventAggr = 0;
            IOlev = 0;

            // Generic Writes to Registers
            GWn = 0;                                    // Number of Generic Writes
            GWaddr = Enumerable.Repeat((UInt32)0, 1000).ToArray();
            GWdata = Enumerable.Repeat((UInt32)0, 1000).ToArray();
            GWmask = Enumerable.Repeat((UInt32)0, 1000).ToArray();
            //memset(dgtzParams.GWaddr, 0, 4000); // List of addresses (length = 'GWn')
            //memset(dgtzParams.GWdata, 0, 4000); // List of datas (length = 'GWn')
            //memset(dgtzParams.GWmask, 0, 4000); // List of masks (length = 'GWn')

            for (int ch = 0; ch < 16; ch++)
            {
                // Channel parameters
                DCoffset[ch] = 58000;//32768; // 0...65535 and 4x value of MC²
                                     // 32768 should be 0V
                PulsePolarity[ch] = 0;

                // DPP Parameters
                DPPParams.M[ch] = 50000;        // Signal Decay Time Constant
                DPPParams.m[ch] = 1000;         // Trapezoid Flat Top
                DPPParams.k[ch] = 3000;         // Trapezoid Rise Time
                DPPParams.ftd[ch] = 500;        // Flat Top Delay ??? in MC² only '%'
                DPPParams.a[ch] = 4;            // Trigger Filter smoothing factor
                DPPParams.b[ch] = 200;          // Input Signal Rise time
                DPPParams.thr[ch] = 100;        // Trigger Threshold
                DPPParams.nsbl[ch] = 4;         // INDEX of Number of Samples for Baseline Mean - 0 = baseline is not evaluated; 1 = 16 samples; 2 = 64 samples; 3 = 256 samples; 4 = 1024 samples; 5 = 4096 samples; 6 = 16384 samples;
                DPPParams.nspk[ch] = 2;         // INDEX of Number of Samples for Peak Mean Calculation - ??? 1 = 1 sample; 2 = 4 samples; 3 = 16 samples; 4 = 64 samples;
                DPPParams.pkho[ch] = 5000;      // Peak Hold Off - 0???
                DPPParams.blho[ch] = 2000;      // Base Line Hold Off
                DPPParams.dgain[ch] = 0;        // Digital Probe Gain - 0x0: Digital Gain = 1; 0x1: Digital Gain = 2; 0x2: Digital Gain = 4; 0x3: Digital Gain = 8;
                DPPParams.enf[ch] = 1;          // Energy Nomralization Factor
                DPPParams.decimation[ch] = 0;   // Decimation of Input Signal - 0x0: Decimation disabled; 0x1: 2 samples (50 MSps); 0x2: 4 samples (25 MSps); 0x3: 8 samples (12.5 MSps)
                DPPParams.trgho[ch] = 1300;     // Trigger Hold Off
                DPPParams.twwdt[ch] = 0;        // Zero crossing acceptance window for the Rise Time Discriminator (Pile-up Rejection), starting from the RC-CR2 overthreshold which arms the acquisition
                DPPParams.trgwin[ch] = 0;       // Trigger acceptance window in coincidence mode
            }

            // Waveform parameters default settings
            WFParams.dualTraceMode = 1;
            WFParams.vp1 = 2;
            // CAENDPP_PHA_VIRTUALPROBE1_Input
            // CAENDPP_PHA_VIRTUALPROBE1_Delta
            // CAENDPP_PHA_VIRTUALPROBE1_Delta2
            // CAENDPP_PHA_VIRTUALPROBE1_Trapezoid
            WFParams.vp2 = 0;
            // CAENDPP_PHA_VIRTUALPROBE2_Input
            // CAENDPP_PHA_VIRTUALPROBE2_S3
            // CAENDPP_PHA_VIRTUALPROBE2_TrapBLCorr
            // CAENDPP_PHA_VIRTUALPROBE2_TrapBaseline
            // CAENDPP_PHA_VIRTUALPROBE2_None
            WFParams.dp1 = 4;
            // CAENDPP_PHA_DigitalProbe1_TrgWin
            // CAENDPP_PHA_DigitalProbe1_Armed 
            // CAENDPP_PHA_DigitalProbe1_PkRun 
            // CAENDPP_PHA_DigitalProbe1_PURFlag 
            // CAENDPP_PHA_DigitalProbe1_Peaking 
            // CAENDPP_PHA_DigitalProbe1_TVAW
            // CAENDPP_PHA_DigitalProbe1_BLHoldoff 
            // CAENDPP_PHA_DigitalProbe1_TRGHoldoff 
            // CAENDPP_PHA_DigitalProbe1_TRGVal 
            // CAENDPP_PHA_DigitalProbe1_ACQVeto 
            // CAENDPP_PHA_DigitalProbe1_BFMVeto 
            // CAENDPP_PHA_DigitalProbe1_ExtTRG
            WFParams.dp2 = 0;
            WFParams.recordLength = 16384; //8192
            WFParams.preTrigger = 1000; //1000
            WFParams.probeSelfTriggerVal = 1482; //150
            WFParams.probeTrigger = 0;
        }
    }
}

namespace newRBS.Models
{
    public enum InputRange
    {
        CAENDPP_InputRange_2_0Vpp = 9,
        CAENDPP_InputRange_0_5Vpp = 10 
    }

        /// <summary>
        /// Enum for the acquisition mode
        /// </summary>
        public enum CAENDPP_AcqMode_t
    {
        CAENDPP_AcqMode_Waveform,
        CAENDPP_AcqMode_Histogram,
    };

    /// <summary>
    /// Enum for the analog probe 1
    /// </summary>
    public enum CAENDPP_PHA_AnalogProbe1_t
    {
        Input,          //CAENDPP_PHA_VIRTUALPROBE1_Input,
        Delta,          //CAENDPP_PHA_VIRTUALPROBE1_Delta,
        Delta2,         //CAENDPP_PHA_VIRTUALPROBE1_Delta2,
        Trapezoid,      //CAENDPP_PHA_VIRTUALPROBE1_Trapezoid,
    };

    /// <summary>
    /// Enum for the analog probe 2
    /// </summary>
    public enum CAENDPP_PHA_AnalogProbe2_t
    {
        Input,          //CAENDPP_PHA_VIRTUALPROBE2_Input,
        S3,             //CAENDPP_PHA_VIRTUALPROBE2_S3,
        TrapBLCorr,     //CAENDPP_PHA_VIRTUALPROBE2_TrapBLCorr, 
        TrapBaseline,   //CAENDPP_PHA_VIRTUALPROBE2_TrapBaseline,
        None,           //CAENDPP_PHA_VIRTUALPROBE2_None, 
    };

    /// <summary>
    /// Enum for the digital probe 1
    /// </summary>
    public enum CAENDPP_PHA_DigitalProbe1_t
    {
        TrgWin,         // CAENDPP_PHA_DigitalProbe1_TrgWin,
        Armed,          // CAENDPP_PHA_DigitalProbe1_Armed,
        PkRun,          // CAENDPP_PHA_DigitalProbe1_PkRun,
        PURFlag,        // CAENDPP_PHA_DigitalProbe1_PURFlag,
        Peaking,        // CAENDPP_PHA_DigitalProbe1_Peaking,
        TVAW,           // CAENDPP_PHA_DigitalProbe1_TVAW,
        BLHoldoff,      // CAENDPP_PHA_DigitalProbe1_BLHoldoff,
        TRGHoldoff,     // CAENDPP_PHA_DigitalProbe1_TRGHoldoff,
        TRGVal,         // CAENDPP_PHA_DigitalProbe1_TRGVal,
        ACQVeto,        // CAENDPP_PHA_DigitalProbe1_ACQVeto,
        BFMVeto,        // CAENDPP_PHA_DigitalProbe1_BFMVeto,
        ExtTRG,         // CAENDPP_PHA_DigitalProbe1_ExtTRG,
    };

    /// <summary>
    /// Enum for the digital probe 2
    /// </summary>
    public enum CAENDPP_PHA_DigitalProbe2_t
    {
        Trigger,// CAENDPP_PHA_DigitalProbe2_Trigger,
    };

}

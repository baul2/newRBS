namespace newRBS.Devices
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
        CAENDPP_PHA_VIRTUALPROBE1_Input,
        CAENDPP_PHA_VIRTUALPROBE1_Delta,
        CAENDPP_PHA_VIRTUALPROBE1_Delta2,
        CAENDPP_PHA_VIRTUALPROBE1_Trapezoid,
        CAENDPP_PHA_VIRTUALPROBE1_FastTrap,
        CAENDPP_PHA_VIRTUALPROBE1_TrapBaseline,
        CAENDPP_PHA_VIRTUALPROBE1_EnergyOut,
        CAENDPP_PHA_VIRTUALPROBE1_TrapBLCorr,
        CAENDPP_PHA_VIRTUALPROBE1_None,
    };

    /// <summary>
    /// Enum for the analog probe 2
    /// </summary>
    public enum CAENDPP_PHA_AnalogProbe2_t
    {
        CAENDPP_PHA_VIRTUALPROBE2_Input,
        CAENDPP_PHA_VIRTUALPROBE2_S3,
        CAENDPP_PHA_VIRTUALPROBE2_TrapBLCorr, 
        CAENDPP_PHA_VIRTUALPROBE2_TrapBaseline,
        CAENDPP_PHA_VIRTUALPROBE2_None, 
        CAENDPP_PHA_VIRTUALPROBE2_Delta, 
        CAENDPP_PHA_VIRTUALPROBE2_FastTrap,
        CAENDPP_PHA_VIRTUALPROBE2_Delta2, 
        CAENDPP_PHA_VIRTUALPROBE2_Trapezoid,
        CAENDPP_PHA_VIRTUALPROBE2_EnergyOut,
    };

    /// <summary>
    /// Enum for the digital probe 1
    /// </summary>
    public enum CAENDPP_PHA_DigitalProbe1_t
    {
        CAENDPP_PHA_DigitalProbe1_TrgWin,
        CAENDPP_PHA_DigitalProbe1_Armed,
        CAENDPP_PHA_DigitalProbe1_PkRun,
        CAENDPP_PHA_DigitalProbe1_PURFlag,
        CAENDPP_PHA_DigitalProbe1_Peaking,
        CAENDPP_PHA_DigitalProbe1_TVAW,
        CAENDPP_PHA_DigitalProbe1_BLHoldoff,
        CAENDPP_PHA_DigitalProbe1_TRGHoldoff,
        CAENDPP_PHA_DigitalProbe1_TRGVal,
        CAENDPP_PHA_DigitalProbe1_ACQVeto,
        CAENDPP_PHA_DigitalProbe1_BFMVeto,
        CAENDPP_PHA_DigitalProbe1_ExtTRG,
        CAENDPP_PHA_DigitalProbe1_Trigger,
        CAENDPP_PHA_DigitalProbe1_None,
        CAENDPP_PHA_DigitalProbe1_EnergyAccepted,
        CAENDPP_PHA_DigitalProbe1_Saturation,
        CAENDPP_PHA_DigitalProbe1_Reset,
    };

    /// <summary>
    /// Enum for the digital probe 2
    /// </summary>
    public enum CAENDPP_PHA_DigitalProbe2_t
    {
        CAENDPP_PHA_DigitalProbe2_Trigger,
        CAENDPP_PHA_DigitalProbe2_None,
        CAENDPP_PHA_DigitalProbe2_Peaking,
        CAENDPP_PHA_DigitalProbe2_BLHoldoff,
        CAENDPP_PHA_DigitalProbe2_PURFlag,
        CAENDPP_PHA_DigitalProbe2_EnergyAccepted,
        CAENDPP_PHA_DigitalProbe2_Saturation,
        CAENDPP_PHA_DigitalProbe2_Reset,
    };

}

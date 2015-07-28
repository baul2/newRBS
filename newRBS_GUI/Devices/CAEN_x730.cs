using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;

namespace newRBS.Devices
{
    /// <summary>
    /// Class that controls the CAEN N6730 device.
    /// </summary>
    class CAEN_x730
    {
        int handle;
        int bID;
        CAENDPP_AcqMode_t acqMode = CAENDPP_AcqMode_t.CAENDPP_AcqMode_Histogram;
        int waveformAutoTrigger = 1;
        CAENDPP_DgtzParams_t dgtzParams = new CAENDPP_DgtzParams_t();
        InputRange[] inputRange = new InputRange[8] { InputRange.CAENDPP_InputRange_0_5Vpp, InputRange.CAENDPP_InputRange_0_5Vpp, InputRange.CAENDPP_InputRange_0_5Vpp, InputRange.CAENDPP_InputRange_0_5Vpp, InputRange.CAENDPP_InputRange_0_5Vpp, InputRange.CAENDPP_InputRange_0_5Vpp, InputRange.CAENDPP_InputRange_0_5Vpp, InputRange.CAENDPP_InputRange_0_5Vpp };
        List<int> activeChannels = new List<int>();

        TraceSource trace = new TraceSource("CAEN_x730");

        const string cAENDPPLib = "CAENDPPLib.dll";
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_InitLibrary(ref int handle);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_AddBoard(int handle, ConnParam connParam, ref int bID);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_SetBoardConfiguration(int handle, int bID, int acqMode, CAENDPP_DgtzParams_t dgtzParams);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_SetInputRange(int handle, int channel, InputRange inputRange);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_GetBoardConfiguration(int handle, int bID, ref int acqMode, ref CAENDPP_DgtzParams_t dgtzParams);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_StartAcquisition(int handle, int channel);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_StopAcquisition(int handle, int channel);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_GetCurrentHistogram(int handle, int channel, UInt32[] h1, ref UInt32 counts, ref UInt64 realTime, ref UInt64 deadTime, ref int acqStatus);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_GetWaveform(int handle, int channel, Int16 Auto, Int16[] AT1, Int16[] AT2, byte[] DT1, byte[] DT2, ref UInt32 numSample, ref double lenSample);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_EndLibrary(int handle);

        /// <summary>
        /// Constructor that initializes the library, adds the board and sends the default configuration. 
        /// </summary>
        public CAEN_x730()
        {
            //Init library
            int ret = CAENDPP_InitLibrary(ref handle);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Information, 0, "Library initialized"); }

            //Add board
            ConnParam connParam = new ConnParam();
            connParam.LinkType = 0;
            connParam.LinkNum = 0;
            connParam.ConetNode = 0;
            ret = CAENDPP_AddBoard(handle, connParam, ref bID);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Information, 0, "Board added"); }

            //Reset board to default parameters
            SetDefaultConfig();

            //Console.WriteLine("CAENDPP_DgtzParams_t {0}", Marshal.SizeOf(typeof(CAENDPP_DgtzParams_t)));
            //Console.WriteLine("CAENDPP_ChannelExtraParameters {0}", Marshal.SizeOf(typeof(CAENDPP_ChannelExtraParameters)));
            //Console.WriteLine("CAENDPP_TRReset {0}", Marshal.SizeOf(typeof(CAENDPP_TRReset)));
            //Console.WriteLine("CAENDPP_PHA_Params_t {0}", Marshal.SizeOf(typeof(CAENDPP_PHA_Params_t)));
            //Console.WriteLine("CAENDPP_WaveformParams_t {0}", Marshal.SizeOf(typeof(CAENDPP_WaveformParams_t)));
            //Console.WriteLine("CAENDPP_ListParams_t {0}", Marshal.SizeOf(typeof(CAENDPP_ListParams_t)));
            //Console.WriteLine("CAENDPP_CoincParams_t {0}", Marshal.SizeOf(typeof(CAENDPP_CoincParams_t)));
            //Console.WriteLine("CAENDPP_SpectrumControl {0}", Marshal.SizeOf(typeof(CAENDPP_SpectrumControl)));
            //Console.WriteLine("dgtzParams.ChannelExtraParameters[3].analogPath {0}", dgtzParams.ChannelExtraParameters[3].analogPath);
        }

        /// <summary>
        /// Function that sends the default configuration. 
        /// </summary>
        public void SetDefaultConfig()
        {
            dgtzParams = new CAENDPP_DgtzParams_t();
            dgtzParams.initializeArrays();
            dgtzParams.setDefaultConfig();
            for (int channel = 0; channel < 8; channel++) inputRange[channel] = InputRange.CAENDPP_InputRange_0_5Vpp;
            SendConfig();
        }

        public void SetChannelConfig(int channel, ChannelParams channelParams)
        {
            if (channelParams.inputRange != 0) inputRange[channel] = channelParams.inputRange;

            if (channelParams.DCoffset != null) dgtzParams.DCoffset[channel] = (int)channelParams.DCoffset;
            if (channelParams.TrapezoidFlatTopTime != null) dgtzParams.DPPParams.m[channel] = (int)channelParams.TrapezoidFlatTopTime;
            if (channelParams.TrapezoidRiseTime != null) dgtzParams.DPPParams.k[channel] = (int)channelParams.TrapezoidRiseTime;
            if (channelParams.TrapezoidPeakingDelay != null) dgtzParams.DPPParams.ftd[channel] = (int)channelParams.TrapezoidPeakingDelay;
            if (channelParams.TriggerFilterSmoothingFactor != null) dgtzParams.DPPParams.a[channel] = (int)channelParams.TriggerFilterSmoothingFactor;
            if (channelParams.InputSignalRiseTime != null) dgtzParams.DPPParams.b[channel] = (int)channelParams.InputSignalRiseTime;
            if (channelParams.TriggerThreshold != null) dgtzParams.DPPParams.thr[channel] = (int)channelParams.TriggerThreshold;
            if (channelParams.NumSamplesBaselineMean != null) dgtzParams.DPPParams.nsbl[channel] = (int)channelParams.NumSamplesBaselineMean;
            if (channelParams.NumSamplesPeakMean != null) dgtzParams.DPPParams.nspk[channel] = (int)channelParams.NumSamplesPeakMean;
            if (channelParams.PeakHoldOff != null) dgtzParams.DPPParams.pkho[channel] = (int)channelParams.PeakHoldOff;
            if (channelParams.BaseLineHoldOff != null) dgtzParams.DPPParams.blho[channel] = (int)channelParams.BaseLineHoldOff;
            if (channelParams.TriggerHoldOff != null) dgtzParams.DPPParams.trgho[channel] = (int)channelParams.TriggerHoldOff;
            if (channelParams.DigitalGain != null) dgtzParams.DPPParams.dgain[channel] = (int)channelParams.DigitalGain;
            if (channelParams.EnergyNormalizationFactor != null) dgtzParams.DPPParams.enf[channel] = (float)channelParams.EnergyNormalizationFactor;
            if (channelParams.InputSignalDecimation != null) dgtzParams.DPPParams.decimation[channel] = (int)channelParams.InputSignalDecimation;

            SendConfig();
        }

        /// <summary>
        /// Function that sends the waveform configuration.
        /// </summary>
        /// <param name="AP1">Enum for analog probe 1.</param>
        /// <param name="AP2">Enum for analog probe 2.</param>
        /// <param name="DP1">Enum for digital probe 1.</param>
        /// <param name="DP2">Enum for digital probe 2.</param>
        /// <param name="AutoTrigger">Sets AutoTrigger to on (true) of off (false).</param>
        public void SetWaveformConfig(CAENDPP_PHA_AnalogProbe1_t AP1, CAENDPP_PHA_AnalogProbe2_t AP2, CAENDPP_PHA_DigitalProbe1_t DP1, CAENDPP_PHA_DigitalProbe2_t DP2, bool AutoTrigger)
        {
            dgtzParams.WFParams.ap1 = AP1;
            dgtzParams.WFParams.ap2 = AP2;
            dgtzParams.WFParams.dp1 = DP1;
            dgtzParams.WFParams.dp2 = DP2;
            waveformAutoTrigger = Convert.ToInt32(AutoTrigger);

            SendConfig();
        }

        /// <summary>
        /// Function that sends the acquisition mode.
        /// </summary>
        /// <param name="acquisitionMode">Acquisition mode (Waveform/Histogram).</param>
        public void SetMeasurementMode(CAENDPP_AcqMode_t acquisitionMode)
        {
            acqMode = acquisitionMode;

            SendConfig();
        }

        /// <summary>
        /// Function that sends the configuration. 
        /// </summary>
        /// <remarks>
        /// The variables int acqMode and <see cref="CAENDPP_DgtzParams_t"/> dgtzParams of the class <see cref="CAEN_x730"/> are used.
        /// </remarks>
        public void SendConfig()
        {
            int ret1, ret2 = 0;
            ret1 = CAENDPP_SetBoardConfiguration(handle, bID, (int)acqMode, dgtzParams);
            for (int channel = 0; channel < 8; channel++) ret2 = CAENDPP_SetInputRange(handle, channel, inputRange[channel]);

            if (ret1 != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret1, GetErrorText(ret1)); }
            if (ret2 != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret2, GetErrorText(ret2)); }
            if (ret1 == 0 & ret2 == 0) { trace.TraceEvent(TraceEventType.Information, 0, "Configuration send"); }
        }

        /// <summary>
        /// Function that starts the measurement for the selected channel.
        /// </summary>
        /// <param name="channel">Channel (0...7) to start the measurement.</param>
        public void StartAcquisition(int channel)
        {
            if (activeChannels.Contains(channel)) // Checks if measurement is already running
            {
                trace.TraceEvent(TraceEventType.Warning, 0, "Acquisition already running for channel {0}, channel");
                return;
            }
            int ret = CAENDPP_StartAcquisition(handle, channel);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else
            {
                trace.TraceEvent(TraceEventType.Information, 0, "Acquisition started for channel {0}", channel);
                activeChannels.Add(channel); // Adds channel to the active channels
            }
        }

        /// <summary>
        /// Function that reads the current histogram from selected channel.
        /// </summary>
        /// <param name="channel">Channel (0...7) to read the histogram from.</param>
        /// <returns>Array of the obtained histogram. Type: UInt32[]. Length: 16384.</returns>
        public int[] GetHistogram(int channel)
        {
            UInt32[] h1 = new UInt32[16384];
            UInt32 counts = 0;
            UInt64 realTime = 0, deadTime = 0;
            int acqStatus = 0;

            int ret = CAENDPP_GetCurrentHistogram(handle, channel, h1, ref counts, ref realTime, ref deadTime, ref acqStatus);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Verbose, 0, "Histogram read on channel {0}", channel); }

            return (int[])(object)h1;
        }

        /// <summary>
        /// Function that reads the waveforms from selected channel.
        /// </summary>
        /// <param name="channel">Channel (0...7) to read the waveforms from.</param>
        /// <returns>Structure <see cref="Waveform"/> that holds the waveforms and number of samples.</returns>
        public Waveform GetWaveform(int channel)
        {
            Waveform waveform = new Waveform();

            Int16[] AT1 = new Int16[dgtzParams.WFParams.recordLength];
            Int16[] AT2 = new Int16[dgtzParams.WFParams.recordLength];
            byte[] DT1 = new byte[dgtzParams.WFParams.recordLength];
            byte[] DT2 = new byte[dgtzParams.WFParams.recordLength];
            UInt32 numSample = 0;
            double lenSample = 0;

            for (int i = 0; i < 100; i++)
            {
                int ret = CAENDPP_GetWaveform(handle, channel, (short)waveformAutoTrigger, AT1, AT2, DT1, DT2, ref numSample, ref lenSample);
                if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); return waveform; }
                else
                {
                    if (numSample == 0) { continue; }
                    trace.TraceEvent(TraceEventType.Verbose, 0, "Waveform read on channel {0}", channel);
                    waveform.AT1 = AT1.ToString().Select(o => Convert.ToInt32(o)).ToArray();
                    waveform.AT2 = AT2.ToString().Select(o => Convert.ToInt32(o)).ToArray();
                    waveform.DT1 = DT1.ToString().Select(o => Convert.ToInt32(o)).ToArray();
                    waveform.DT2 = DT2.ToString().Select(o => Convert.ToInt32(o)).ToArray();
                    waveform.numSamples = (int)numSample;
                    return waveform;
                }
            }
            trace.TraceEvent(TraceEventType.Warning, 0, "Waveform could not be read on channel {0}", channel);
            return waveform;
        }

        /// <summary>
        /// Function that stops the measurement for the selected channel.
        /// </summary>
        /// <param name="channel">Channel (0...7) to stop the measurement.</param>
        public void StopAcquisition(int channel)
        {
            if (!activeChannels.Contains(channel)) // Checks if measurement is not running
            {
                trace.TraceEvent(TraceEventType.Warning, 0, "Acquisition not running for channel {0}, channel");
                return;
            }
            int ret = CAENDPP_StopAcquisition(handle, channel);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else
            {
                trace.TraceEvent(TraceEventType.Information, 0, "Acquisition stopped for channel {0}", channel);
                activeChannels.Remove(channel); // Removes channel from the active channels
            }
        }

        /// <summary>
        /// Function that closes the library.
        /// </summary>
        public void Close()
        {
            int ret = CAENDPP_EndLibrary(handle);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Information, 0, "Library closed"); }
        }

        /// <summary>
        /// Function that returns the error string.
        /// </summary>
        /// <param name="ret">Return value of a library call.</param>
        /// <returns>Error string corresponding to the return value.</returns>
        private string GetErrorText(int ret)
        {
            switch (ret)
            {
                case 0: return "No error";
                case -100: return "Unspecified error";
                case -101: return "Too	many instances";
                case -102: return "Process fail";
                case -103: return "Read fail";
                case -104: return "Write fail";
                case -105: return "Invalid response";
                case -106: return "Invalid library handle";
                case -107: return "Configuration error";
                case -108: return "Board Init failed";
                case -109: return "Timeout error";
                case -110: return "Invalid parameter";
                case -111: return "Not in Waveforms Mode";
                case -112: return "Not in Histogram Mode";
                case -113: return "Not in List Mode";
                case -114: return "Not yet implemented";
                case -115: return "Board not configured";
                case -116: return "Invalid board index";
                case -117: return "Invalid channel index";
                case -118: return "Invalid board firmware";
                case -119: return "No board added";
                case -120: return "Acquisition Status is not compliant with the function called";
                case -121: return "Out of memory";
                case -122: return "Invalid board channel index";
                case -123: return "No valid histogram allocated";
                case -124: return "Error opening the list dumper";
                case -125: return "Error starting acquisition for a board";
                case -126: return "The given channel is not enabled ";
                case -127: return "Invalid command";
                case -128: return "Invalid number of bins";
                case -129: return "Invalid Hitogram Index";
                case -130: return "The feature is not supported by the gve board/channel";
                case -131: return "The given histogram is an invalid state (e.g. 'done' while it shouldnt)";
                case -132: return "Cannot switch to ext histo, no more histograms available";
                case -133: return "The selected board doesnt support HV Channels";
                case -134: return "Invalid HV channel index";
                case -135: return "Error Sending Message through Socket";
                case -136: return "Error Receiving Message from Socket";
                case -137: return "Cannot get Boards acquisition thread";
                case -138: return "Cannot decode waveform from buffer";
                case -139: return "Error Opening the digitizer";
                case -140: return "Requested a feature incompatible with boards Manufacture";
                case -141: return "Autoset Status is not compliant with the requested feature";
                case -142: return "Autoset error looking for signal parameters";
                default: return "Unknown";
            }
        }
    }
}

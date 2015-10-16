﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;

namespace newRBS.Models
{
    /// <summary>
    /// Class that controls the CAEN N6730 device.
    /// </summary>
    public static class CAEN_x730
    {
        public static bool IsInit = false;
        private static int handle;
        private static int bID;
        private static CAENDPP_AcqMode_t acqMode = CAENDPP_AcqMode_t.CAENDPP_AcqMode_Histogram;
        private static int waveformAutoTrigger;
        private static CAENDPP_DgtzParams_t dgtzParams = new CAENDPP_DgtzParams_t();
        private static int[] inputRange = new int[8] { 10, 10, 10, 10, 10, 10, 10, 10 };
        public static List<int> ActiveChannels = new List<int>();
        public static int NumberOfChanels = 16384;

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        const string cAENDPPLib = "CAENDPPLib.dll";
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_InitLibrary(ref int handle);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_AddBoard(int handle, ConnParam connParam, ref int bID);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_SetBoardConfiguration(int handle, int bID, int acqMode, CAENDPP_DgtzParams_t dgtzParams);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_SetInputRange(int handle, int channel, int inputRange);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_GetBoardConfiguration(int handle, int bID, ref int acqMode, ref CAENDPP_DgtzParams_t dgtzParams);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CAENDPP_ClearCurrentHistogram(int handle, int channel);
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
        /// Function that initializes the library, adds the board and sends the default and custom configuration. 
        /// </summary>
        public static bool Init()
        {
            IsInit = false;

            if (MyGlobals.OffLineMode == true)
                return false;

            //Init library
            int ret = CAENDPP_InitLibrary(ref handle);
            if (ret != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "Error " + ret + ": " + GetErrorText(ret)); return false; }
            else { trace.Value.TraceEvent(TraceEventType.Information, 0, "Library initialized"); }

            //Add board
            ConnParam connParam = new ConnParam { LinkType = 0, LinkNum = 0, ConetNode = 0 };

            ret = CAENDPP_AddBoard(handle, connParam, ref bID);
            if (ret != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "Error " + ret + ": " + GetErrorText(ret)); return false; }
            else { trace.Value.TraceEvent(TraceEventType.Information, 0, "Board added"); }

            //Reset board to default parameters
            SetDefaultConfig();

            LoadCustomChannelConfigs();

            SendConfig();

            IsInit = true;
            return true;
        }

        /// <summary>
        /// Function that sets the default configuration. 
        /// </summary>
        public static void SetDefaultConfig()
        {
            dgtzParams = new CAENDPP_DgtzParams_t();
            dgtzParams.initializeArrays();
            dgtzParams.setDefaultConfig();
            for (int channel = 0; channel < 8; channel++) inputRange[channel] = 10; // 0.5Vpp 
        }

        /// <summary>
        /// Function that loads the default channel configurations from the ChannelConfigs\ folder.
        /// </summary>
        public static void LoadCustomChannelConfigs()
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(ChannelParams));
            FileStream ReadFileStream;

            for (int i = 0; i < 8; i++)
            {
                string path = "ConfigurationFiles/Ch" + i + ".xml";
                if (File.Exists(path))
                {
                    ReadFileStream = new FileStream(path, FileMode.Open);
                    SetChannelConfig(i, (ChannelParams)SerializerObj.Deserialize(ReadFileStream), false);
                    ReadFileStream.Close();

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Channel configuration read from file " + path);
                }
                else
                    trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't red channel configuration file " + path);
            }
        }

        /// <summary>
        /// Function that saves the current channel configurations to the ChannelConfigs\ folder.
        /// </summary>
        public static void SaveCustomChannelConfigs()
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(ChannelParams));
            TextWriter WriteFileStream;

            for (int i = 0; i < 8; i++)
            {
                string path = "ConfigurationFiles/Ch" + i + ".xml";

                WriteFileStream = new StreamWriter(path);
                SerializerObj.Serialize(WriteFileStream, GetChannelConfig(i));
                WriteFileStream.Close();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Channel configuration saved to file " + path);
            }
        }

        /// <summary>
        /// Function that sets the configuration of a single channel based on an instance of <see cref="ChannelParams"/>.
        /// </summary>
        /// <param name="channel">The number of the channel to configure.</param>
        /// <param name="channelParams">The instance of <see cref="ChannelParams"/> holding the channel configuration.</param>
        /// <param name="SendToDevice">Determines whether <see cref="SendConfig"/> is called.</param>
        public static void SetChannelConfig(int channel, ChannelParams channelParams, bool SendToDevice)
        {
            if (channelParams.DCoffset != null) dgtzParams.DCoffset[channel] = (int)channelParams.DCoffset;
            if (channelParams.InputRange != 0) inputRange[channel] = (int)channelParams.InputRange;
            if (channelParams.InputSignalDecayTime != null) dgtzParams.DPPParams.M[channel] = (int)channelParams.InputSignalDecayTime;
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

            if (SendToDevice == true)
                SendConfig();
        }

        /// <summary>
        /// Function that returns the current configuration of a specific channel.
        /// </summary>
        /// <param name="channel">The number of the channel to get the configuration.</param>
        /// <returns>An instance of <see cref="ChannelParams"/> holding the current channel configuration.</returns>
        public static ChannelParams GetChannelConfig(int channel)
        {
            ChannelParams channelParams = new ChannelParams();

            channelParams.DCoffset = dgtzParams.DCoffset[channel];
            channelParams.InputRange = inputRange[channel];
            channelParams.InputSignalDecayTime = dgtzParams.DPPParams.M[channel];
            channelParams.TrapezoidFlatTopTime = dgtzParams.DPPParams.m[channel];
            channelParams.TrapezoidRiseTime = dgtzParams.DPPParams.k[channel];
            channelParams.TrapezoidPeakingDelay = dgtzParams.DPPParams.ftd[channel];
            channelParams.TriggerFilterSmoothingFactor = dgtzParams.DPPParams.a[channel];
            channelParams.InputSignalRiseTime = dgtzParams.DPPParams.b[channel];
            channelParams.TriggerThreshold = dgtzParams.DPPParams.thr[channel];
            channelParams.NumSamplesBaselineMean = dgtzParams.DPPParams.nsbl[channel];
            channelParams.NumSamplesPeakMean = dgtzParams.DPPParams.nspk[channel];
            channelParams.PeakHoldOff = dgtzParams.DPPParams.pkho[channel];
            channelParams.BaseLineHoldOff = dgtzParams.DPPParams.blho[channel];
            channelParams.TriggerHoldOff = dgtzParams.DPPParams.trgho[channel];
            channelParams.DigitalGain = dgtzParams.DPPParams.dgain[channel];
            channelParams.EnergyNormalizationFactor = dgtzParams.DPPParams.enf[channel];
            channelParams.InputSignalDecimation = dgtzParams.DPPParams.decimation[channel];

            return channelParams;
        }

        /// <summary>
        /// Function that sets the waveform configuration and calls <see cref="SendConfig"/>.
        /// </summary>
        /// <param name="AP1">Enum for analog probe 1.</param>
        /// <param name="AP2">Enum for analog probe 2.</param>
        /// <param name="DP1">Enum for digital probe 1.</param>
        /// <param name="DP2">Enum for digital probe 2.</param>
        /// <param name="AutoTrigger">Sets AutoTrigger to on (true) of off (false).</param>
        public static void SetWaveformConfig(CAENDPP_PHA_AnalogProbe1_t AP1, CAENDPP_PHA_AnalogProbe2_t AP2, CAENDPP_PHA_DigitalProbe1_t DP1, CAENDPP_PHA_DigitalProbe2_t DP2, bool AutoTrigger)
        {
            dgtzParams.WFParams.ap1 = AP1;
            dgtzParams.WFParams.ap2 = AP2;
            dgtzParams.WFParams.dp1 = DP1;
            dgtzParams.WFParams.dp2 = DP2;
            waveformAutoTrigger = Convert.ToInt32(AutoTrigger);

            SendConfig();
        }

        /// <summary>
        /// Function that sets the acquisition mode and calls <see cref="SendConfig"/>.
        /// </summary>
        /// <param name="acquisitionMode">Acquisition mode (Waveform/Histogram).</param>
        public static void SetMeasurementMode(CAENDPP_AcqMode_t acquisitionMode)
        {
            acqMode = acquisitionMode;

            SendConfig();
        }

        /// <summary>
        /// Function that sends the configuration to the device. 
        /// </summary>
        /// <remarks>
        /// The variables int acqMode (<see cref="CAENDPP_AcqMode_t"/>) and dgtzParams (<see cref="CAENDPP_DgtzParams_t"/>) of the class <see cref="CAEN_x730"/> are used.
        /// </remarks>
        public static void SendConfig()
        {
            int ret1 = 0, ret2 = 0;
            ret1 = CAENDPP_SetBoardConfiguration(handle, bID, (int)acqMode, dgtzParams);
            for (int channel = 0; channel < 8; channel++) ret2 = CAENDPP_SetInputRange(handle, channel, inputRange[channel]);

            if (ret1 != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "CAENDPP_SetBoardConfiguration: Error " + ret1 + ": " + GetErrorText(ret1)); }
            if (ret2 != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "CAENDPP_SetInputRange: Error " + ret2 + ": " + GetErrorText(ret2)); }
            if (ret1 == 0 & ret2 == 0) { trace.Value.TraceEvent(TraceEventType.Information, 0, "Configuration send"); }
        }

        /// <summary>
        /// Function that starts the acquisition for the specified channel.
        /// </summary>
        /// <param name="channel">Channel number (0...7) to start the acquisition.</param>
        public static void StartAcquisition(int channel)
        {
            if (ActiveChannels.Contains(channel)) // Checks if measurement is already running
            {
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Acquisition already running for channel " + channel);
                return;
            }
            int ret2 = CAENDPP_ClearCurrentHistogram(handle, channel);
            int ret = CAENDPP_StartAcquisition(handle, channel);
            if (ret != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "Error " + ret + ": " + GetErrorText(ret)); }
            else
            {
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Acquisition started for channel " + channel);
                ActiveChannels.Add(channel); // Adds channel to the active channels
            }
        }

        /// <summary>
        /// Function that reads the current histogram from the specified channel.
        /// </summary>
        /// <param name="channel">Channel number (0...7) to read the histogram from.</param>
        /// <returns>Array of the obtained histogram. Type: UInt32[]. Length: 16384.</returns>
        public static int[] GetHistogram(int channel)
        {
            UInt32[] h1 = new UInt32[16384];
            UInt32 counts = 0;
            UInt64 realTime = 0, deadTime = 0;
            int acqStatus = 0;

            int ret = CAENDPP_GetCurrentHistogram(handle, channel, h1, ref counts, ref realTime, ref deadTime, ref acqStatus);
            if (ret != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "Error " + ret + ": " + GetErrorText(ret)); }
            else { trace.Value.TraceEvent(TraceEventType.Verbose, 0, "Histogram read on channel " + channel); }

            return (int[])(object)h1;
        }

        /// <summary>
        /// Function that reads the waveforms (analog1/2 & digital1/2) from the specified channel.
        /// </summary>
        /// <param name="channel">Channel number (0...7) to read the waveforms from.</param>
        /// <returns>Structure <see cref="Waveform"/> that holds the waveforms and number of samples.</returns>
        public static Waveform GetWaveform(int channel)
        {
            Waveform waveform = new Waveform();

            waveform.AT1 = new Int16[dgtzParams.WFParams.recordLength];
            waveform.AT2 = new Int16[dgtzParams.WFParams.recordLength];
            waveform.DT1 = new byte[dgtzParams.WFParams.recordLength];
            waveform.DT2 = new byte[dgtzParams.WFParams.recordLength];
            waveform.NumSamples = 0;
            waveform.LenSample = 0;
            waveform.AcquisitionTime = DateTime.Now;
            waveform.AcquisitionChannel = channel;

            DateTime startDateTime = DateTime.Now;

            while ((DateTime.Now - startDateTime).Milliseconds < 300)
            {
                int ret = CAENDPP_GetWaveform(handle, channel, 1, waveform.AT1, waveform.AT2, waveform.DT1, waveform.DT2, ref waveform.NumSamples, ref waveform.LenSample);

                if (ret != 0)
                { trace.Value.TraceEvent(TraceEventType.Error, 0, "Error " + ret + ": " + GetErrorText(ret)); return waveform; }

                if (waveform.NumSamples > 0)
                {
                    trace.Value.TraceEvent(TraceEventType.Verbose, 0, "Waveform read on channel " + channel);
                    return waveform;
                }
            }
            trace.Value.TraceEvent(TraceEventType.Warning, 0, "Waveform could not be read on channel " + channel);
            return waveform;
        }

        /// <summary>
        /// Function that stops the acquisition for the specified channel.
        /// </summary>
        /// <param name="channel">Channel number (0...7) to stop the acquisition.</param>
        public static void StopAcquisition(int channel)
        {
            if (!ActiveChannels.Contains(channel)) // Checks if measurement is not running
            {
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Acquisition not running for channel " + channel);
                return;
            }
            int ret = CAENDPP_StopAcquisition(handle, channel);
            if (ret != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "Error " + ret + ": " + GetErrorText(ret)); }
            else
            {
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Acquisition stopped for channel " + channel);
                ActiveChannels.Remove(channel); // Removes channel from the active channels
            }
        }

        /// <summary>
        /// Function that closes the library.
        /// </summary>
        public static void Close()
        {
            int ret = CAENDPP_EndLibrary(handle);
            if (ret != 0) { trace.Value.TraceEvent(TraceEventType.Error, 0, "Error " + ret + ": " + GetErrorText(ret)); }
            else { trace.Value.TraceEvent(TraceEventType.Information, 0, "Library closed"); }

            IsInit = false;
        }

        /// <summary>
        /// Function that returns the error string of an error code.
        /// </summary>
        /// <param name="ret">Error code of a library call.</param>
        /// <returns>Error string corresponding to the error code.</returns>
        private static string GetErrorText(int ret)
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

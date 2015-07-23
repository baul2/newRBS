using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace newRBS_Console
{
    public struct WaveformParams
    {
        public int AP1;
        public int AP2;
        public int DP1;
        public int DP2;
        public int AUTO;
        public WaveformParams(int AP1, int AP2, int DP1, int DP2, int AUTO)
        {
            this.AP1 = AP1;
            this.AP2 = AP2;
            this.DP1 = DP1;
            this.DP2 = DP2;
            this.AUTO = AUTO;
        }

    }

    /// <summary>
    /// Class that controls the CAEN N6730 device.
    /// </summary>
    class CAEN_x730
    {
        int handle;
        int bID;
        int acqMode = 1; // 0 = Waveform; 1 = Histogram
        CAENDPP_DgtzParams_t dgtzParams = new CAENDPP_DgtzParams_t();
        List<int> activeChannels = new List<int>();
        int[] inputRange = new int[8] { 10, 10, 10, 10, 10, 10, 10, 10 };
        string stopType = "Manual";
        int stopValue = 0;
        WaveformParams waveformParams = new WaveformParams(4, 1, 5, 1, 1);

        TraceSource trace = new TraceSource("CAEN_x730");

        const string cAENDPPLib = "CAENDPPLib.dll";
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_InitLibrary(ref int handle);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_AddBoard(int handle, ConnParam connParam, ref int bID);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_SetBoardConfiguration(int handle, int bID, int acqMode, CAENDPP_DgtzParams_t dgtzParams);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_GetBoardConfiguration(int handle, int bID, ref int acqMode, ref CAENDPP_DgtzParams_t dgtzParams);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_StartAcquisition(int handle, int channel);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_StopAcquisition(int handle, int channel);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_GetCurrentHistogram(int handle, int channel, UInt32[] h1, ref UInt32 counts, ref UInt64 realTime, ref UInt64 deadTime, ref int acqStatus);
        [DllImport(cAENDPPLib, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CAENDPP_EndLibrary(int handle);

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
            SetConfig();
        }

        /// <summary>
        /// Function that sends the configuration. 
        /// </summary>
        /// <remarks>
        /// The variables int acqMode and <see cref="CAENDPP_DgtzParams_t"/> dgtzParams of the class <see cref="CAEN_x730"/> are used.
        /// </remarks>
        public void SetConfig()
        {
            int ret = CAENDPP_SetBoardConfiguration(handle, bID, acqMode, dgtzParams);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Information, 0, "Configuration send"); }
        }

        /// <summary>
        /// Function that starts the measurement for theselected channel.
        /// </summary>
        /// <param name="channel">Channel (0...7) to start the measurement.</param>
        public void StartAcquisition(int channel)
        {
            int ret = CAENDPP_StartAcquisition(handle, channel);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Information, 0, "Acquisition started for channel {0}", channel); }
        }

        /// <summary>
        /// Function that reads the current histogram from selected channel.
        /// </summary>
        /// <param name="channel">Channel (0...7) to read the histogram from.</param>
        /// <returns>Array of the obtained histogram. Type: UInt32[]. Length: 16384.</returns>
        public UInt32[] GetHistogram(int channel)
        {
            UInt32[] h1 = new UInt32[16384];
            UInt32 counts = 0;
            UInt64 realTime = 0, deadTime = 0;
            int acqStatus = 0;

            int ret = CAENDPP_GetCurrentHistogram(handle, channel, h1, ref counts, ref realTime, ref deadTime, ref acqStatus);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Verbose, 0, "Histogram read on channel {0}", channel); }

            return h1;
        }

        /// <summary>
        /// Function that stops the measurement for the selected channel.
        /// </summary>
        /// <param name="channel">Channel (0...7) to start the measurement.</param>
        public void StopAcquisition(int channel)
        {
            int ret = CAENDPP_StopAcquisition(handle, channel);
            if (ret != 0) { trace.TraceEvent(TraceEventType.Error, 0, "Error {0}: {1}", ret, GetErrorText(ret)); }
            else { trace.TraceEvent(TraceEventType.Information, 0, "Acquisition stopped for channel {0}", channel); }
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

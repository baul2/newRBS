using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using GalaSoft.MvvmLight.Ioc;
using System.Data.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace newRBS.Models
{
    /// <summary>
    /// Class responsible for measuring the waveforms of a single event.
    /// </summary>
    public class MeasureWaveform
    {
        private CAEN_x730 cAEN_x730;

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        private Timer waveformTimer = new Timer();

        public List<int> activeChannels = new List<int>();

        public Waveform waveform;

        public delegate void EventHandlerWaveform(Waveform waveform);
        public event EventHandlerWaveform EventWaveform;

        /// <summary>
        /// Constructor of the class. Gets a reference to the instance of <see cref="CAEN_x730"/> from <see cref="ViewModels.ViewModelLocator"/>.
        /// </summary>
        public MeasureWaveform()
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<CAEN_x730>();
        }

        /// <summary>
        /// Function that passes the list of selected waveform types to <see cref="CAEN_x730.SetWaveformConfig(CAENDPP_PHA_AnalogProbe1_t, CAENDPP_PHA_AnalogProbe2_t, CAENDPP_PHA_DigitalProbe1_t, CAENDPP_PHA_DigitalProbe2_t, bool)"/>
        /// </summary>
        /// <param name="AP1">Waveform type for anologe probe 1</param>
        /// <param name="AP2">Waveform type for anologe probe 2</param>
        /// <param name="DP1">Waveform type for digital probe 1</param>
        /// <param name="DP2">Waveform type for digital probe 2</param>
        /// <param name="AUTO">Software trigger if no trigger signal can be found.</param>
        public void SetWaveformConfig(CAENDPP_PHA_AnalogProbe1_t AP1, CAENDPP_PHA_AnalogProbe2_t AP2, CAENDPP_PHA_DigitalProbe1_t DP1, CAENDPP_PHA_DigitalProbe2_t DP2, bool AUTO)
        {
            cAEN_x730.SetWaveformConfig(AP1, AP2, DP1, DP2, AUTO);
            trace.Value.TraceEvent(TraceEventType.Information, 0, "Waveform config was send to the device");
        }

        /// <summary>
        /// Function that gets the channel configuration for the given channel from <see cref="CAEN_x730.GetChannelConfig(int)"/>.
        /// </summary>
        /// <param name="Channel">The channel number to get the configuration from.</param>
        /// <returns>An instance of <see cref="ChannelParams"/> containing the channel configuration.</returns>
        public ChannelParams GetChannelConfig(int Channel)
        {
            ChannelParams parameter = cAEN_x730.GetChannelConfig(Channel);
            trace.Value.TraceEvent(TraceEventType.Information, 0, "Waveform config was read from the device");
            return parameter;
        }

        /// <summary>
        /// Funtion that sends the channel configuration for the given channel to <see cref="CAEN_x730.SetChannelConfig(int, ChannelParams)"/>.
        /// </summary>
        /// <param name="Channel">The channel number to set the configuration.</param>
        /// <param name="channelParams">An instance of <see cref="ChannelParams"/> containing the channel configuration.</param>
        public void SetChannelConfig(int Channel, ChannelParams channelParams)
        {
            if (cAEN_x730.ActiveChannels.Count() == 0)
            {
                cAEN_x730.SetChannelConfig(Channel, channelParams,true);
                return;
            }
            else
            {
                waveformTimer.Enabled = false;
                int activeChannel = cAEN_x730.ActiveChannels.First();
                cAEN_x730.StopAcquisition(activeChannel);
                cAEN_x730.SetChannelConfig(Channel, channelParams,true);
                cAEN_x730.StartAcquisition(Channel);
                waveformTimer.Enabled = true;
            }
            trace.Value.TraceEvent(TraceEventType.Information, 0, "Channel config was send to the device");
        }

        /// <summary>
        /// Function that start the waveform aquisition for the given channel.
        /// </summary>
        /// <param name="Channel">Number of the channel to start the aquisition.</param>
        public void StartAcquisition(int Channel)
        {
            cAEN_x730.SetMeasurementMode(CAENDPP_AcqMode_t.CAENDPP_AcqMode_Waveform);
            cAEN_x730.StartAcquisition(Channel);
            activeChannels.Add(Channel);

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Waveform acquisition was started for channel " + Channel);

            waveformTimer = new Timer(500);
            waveformTimer.Elapsed += delegate { MeasureWaveformWorker(Channel); };
            waveformTimer.Start();
        }

        /// <summary>
        /// Function that stops the waveform acquisition.
        /// </summary>
        public void StopAcquisition()
        {
            if (activeChannels.Count == 0)
                return;

            waveformTimer.Stop();

            cAEN_x730.StopAcquisition(activeChannels.First());

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Waveform acquisition was stopped");

            activeChannels.Clear();
        }

        /// <summary>
        /// Function that gets the new <see cref="Waveform"/>s from <see cref="CAEN_x730.GetWaveform(int)"/> and sends it with an event.
        /// </summary>
        /// <param name="Channel">Number of the channel to get the waveforms.</param>
        public void MeasureWaveformWorker(int Channel)
        {
            waveformTimer.Enabled = false;
            waveform = cAEN_x730.GetWaveform(Channel);
            trace.Value.TraceEvent(TraceEventType.Verbose, 0, "MeasureWaveformWorker Channel = " + Channel);
            waveformTimer.Enabled = true;
            if (EventWaveform != null) { EventWaveform(waveform); }
        }
    }
}

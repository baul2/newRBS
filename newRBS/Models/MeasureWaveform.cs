using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using GalaSoft.MvvmLight.Ioc;
using System.Data.Linq;
using System.Text;
using System.Threading.Tasks;

namespace newRBS.Models
{
    public class MeasureWaveform
    {
        private CAEN_x730 cAEN_x730;

        private Timer waveformTimer = new Timer();

        public List<int> activeChannels = new List<int>();

        public Waveform waveform;

        public delegate void EventHandlerWaveform(Waveform waveform);
        public event EventHandlerWaveform EventWaveform;

        public MeasureWaveform()
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<Models.CAEN_x730>();
        }

        public void SetWaveformConfig(CAENDPP_PHA_AnalogProbe1_t AP1, CAENDPP_PHA_AnalogProbe2_t AP2, CAENDPP_PHA_DigitalProbe1_t DP1, CAENDPP_PHA_DigitalProbe2_t DP2, bool AUTO)
        {
            cAEN_x730.SetWaveformConfig(AP1, AP2, DP1, DP2, AUTO);
        }

        public ChannelParams GetChannelConfig(int channel)
        {
            return cAEN_x730.GetChannelConfig(channel);
        }

        public void SetChannelConfig(int channel, ChannelParams channelParams)
        {
            if (cAEN_x730.activeChannels.Count() == 0)
            {
                cAEN_x730.SetChannelConfig(channel, channelParams);
                return;
            }
            else
            {
                waveformTimer.Enabled = false;
                int activeChannel = cAEN_x730.activeChannels.First();
                cAEN_x730.StopAcquisition(activeChannel);
                cAEN_x730.SetChannelConfig(channel, channelParams);
                cAEN_x730.StartAcquisition(channel);
                waveformTimer.Enabled = true;
            }

        }

        public void StartAcquisition(int channel)
        {
            Console.WriteLine("Waveform measurement will start");

            cAEN_x730.SetMeasurementMode(CAENDPP_AcqMode_t.CAENDPP_AcqMode_Waveform);
            cAEN_x730.StartAcquisition(channel);
            activeChannels.Add(channel);

            waveformTimer = new Timer(500);
            waveformTimer.Elapsed += delegate { WaveformMeasurementWorker(channel); };
            waveformTimer.Start();
        }

        public void StopAcquisition()
        {
            if (activeChannels.Count == 0)
                return;

            waveformTimer.Stop();

            cAEN_x730.StopAcquisition(activeChannels.First());

            activeChannels.Clear();
        }

        private void WaveformMeasurementWorker(int channel)
        {
            waveform = cAEN_x730.GetWaveform(channel);
            Console.WriteLine("Waveform length: {0}", waveform.NumSamples);
            if (EventWaveform != null) { EventWaveform(waveform); } else { Console.WriteLine("EventWaveform null"); }
        }
    }
}

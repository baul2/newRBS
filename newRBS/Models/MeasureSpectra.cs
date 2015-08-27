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
    /// <summary>
    /// Class responsible for simultaneous measurements of spectra on several channels. 
    /// </summary>
    public class MeasureSpectra
    {
        private CAEN_x730 cAEN_x730;
        private DatabaseUtils dataSpectra;

        public int NumOfChannels = 16384;
        public int IncomingIonNumber = 2;
        public int IncomingIonMass = 4;
        public double IncomingIonEnergy = 1400;
        public double IncomingIonAngle = 180;
        public double OutcomingIonAngle = 170;
        public double SolidAngle = 2.45;

        public double[] EnergyCalOffset = new double[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public double[] EnergyCalSlope = new double[8] { 1, 1, 1, 1, 1, 1, 1, 1 };

        public string StopType = "Manual";
        public int StopValue = 0;

        private Timer[] spectraMeasurementTimer = new Timer[8];

        private Dictionary<int, int> activeChannels = new Dictionary<int, int>(); // <Channel,ID>

        /// <summary>
        /// Initializes the class and stores the handled instances of <see cref="CAEN_x730"/> and <see cref="DatabaseUtils"/>
        /// </summary>
        /// <param name="cAEN_x730">Instance of the class responsible for the CAEN N6730</param>
        /// <param name="dataSpectra">Instance of the class responsible for storing the measured spectra</param>
        public MeasureSpectra()
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<Models.CAEN_x730>();
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DatabaseUtils>();
        }

        public bool IsAcquiring()
        {
            if (cAEN_x730.activeChannels.Count > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Starts the measurement for the selected channels (<see cref="selectedChannels"/>).
        /// </summary>
        /// <returns>A list containing the IDs of the started measurements</returns>
        public List<int> StartMeasurements(List<int> selectedChannels)
        {
            List<int> IDs = new List<int>();
            Console.WriteLine("Measurement will start");

            cAEN_x730.SetMeasurementMode(CAENDPP_AcqMode_t.CAENDPP_AcqMode_Waveform);

            foreach (int channel in selectedChannels)
            {
                cAEN_x730.StartAcquisition(channel);
                int ID = dataSpectra.NewSpectrum(channel, IncomingIonNumber, IncomingIonMass, IncomingIonEnergy, IncomingIonAngle, OutcomingIonAngle, SolidAngle, EnergyCalOffset[channel], EnergyCalSlope[channel], StopType, StopValue, true, NumOfChannels);
                IDs.Add(ID);
                activeChannels.Add(channel, ID);

                spectraMeasurementTimer[channel] = new Timer(500);
                spectraMeasurementTimer[channel].Elapsed += delegate { SpectraMeasurementWorker(ID, channel); };
                spectraMeasurementTimer[channel].Start();
            }
            return IDs;
        }

        /// <summary>
        /// Stops the measurement for the selected channels (<see cref="selectedChannels"/>).
        /// </summary>
        public void StopMeasurements()
        {
            foreach (int channel in activeChannels.Keys.ToList())
            {
                int ID = activeChannels[channel];
                cAEN_x730.StopAcquisition(channel);

                spectraMeasurementTimer[channel].Stop();
                Console.WriteLine("ID to stop: {0}", ID);

                dataSpectra.FinishMeasurement(ID);

                activeChannels.Remove(channel);
            }
        }

        /// <summary>
        /// Function that is called by the spectraMeasurementTimer. It reads a spectrum sends it to the <see cref="DatabaseUtils"/> class.
        /// </summary>
        /// <param name="ID">ID of the measurement where the spectra will be send to.</param>
        /// <param name="channel">Channel to read the spectrum from.</param>
        private void SpectraMeasurementWorker(int ID, int channel)
        {
            int[] newSpectrumY = cAEN_x730.GetHistogram(channel);
            Console.WriteLine("MeasurementWorker ID = {0}; Counts = {1} ", ID, newSpectrumY.Sum());
            dataSpectra.UpdateMeasurement(ID, newSpectrumY);
        }
    }
}

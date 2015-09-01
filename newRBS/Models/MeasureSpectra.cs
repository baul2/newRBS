using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using GalaSoft.MvvmLight.Ioc;
using System.Data.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace newRBS.Models
{
    /// <summary>
    /// Class responsible for simultaneous measurements of spectra on several channels. 
    /// </summary>
    public class MeasureSpectra
    {
        private CAEN_x730 cAEN_x730;

        TraceSource trace = new TraceSource("MeasureSpectra");

        public string MeasurementName;
        public int? SampleID;
        public string SampleRemark;
        public string Chamber = "-10°";
        public string Orientation = "(undefined)";
        public int NumOfChannels = 16384;
        public int IncomingIonAtomicNumber = 2;
        public double IncomingIonEnergy = 1400;
        public double IncomingIonAngle = 180;
        public double OutcomingIonAngle = 170;
        public double SolidAngle = 2.45;

        public double[] EnergyCalOffset = new double[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public double[] EnergyCalSlope = new double[8] { 1, 1, 1, 1, 1, 1, 1, 1 };

        public string StopType = "Manual";
        public int? StopValue = 0;

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
        public void StartMeasurements(List<int> selectedChannels)
        {
            List<int> IDs = new List<int>();

            cAEN_x730.SetMeasurementMode(CAENDPP_AcqMode_t.CAENDPP_AcqMode_Histogram);

            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
            {
                foreach (int channel in selectedChannels)
                {
                    cAEN_x730.StartAcquisition(channel);

                    Measurement newSpectrum = new Measurement
                    {
                        MeasurementName = MeasurementName,
                        SampleID = SampleID,
                        Chamber = Chamber,
                        Orientation = Orientation,
                        SampleRemark = SampleRemark,
                        Channel = channel,
                        IncomingIonAtomicNumber = IncomingIonAtomicNumber,
                        IncomingIonEnergy = IncomingIonEnergy,
                        IncomingIonAngle = IncomingIonAngle,
                        OutcomingIonAngle = OutcomingIonAngle,
                        SolidAngle = SolidAngle,
                        EnergyCalOffset = EnergyCalOffset[channel],
                        EnergyCalSlope = EnergyCalSlope[channel],
                        StartTime = DateTime.Now,
                        Duration = new DateTime(2000, 01, 01),
                        StopType = StopType,
                        StopValue = StopValue,
                        Runs = true,
                        NumOfChannels = NumOfChannels,
                        SpectrumY = new byte[] { 0 }
                    };

                    newSpectrum.Sample = Database.Samples.Single(x => x.SampleID == SampleID);

                    Database.Measurements.InsertOnSubmit(newSpectrum);

                    Database.SubmitChanges();
                    activeChannels.Add(channel, newSpectrum.MeasurementID);

                    Console.WriteLine("New measurementID: {0}",newSpectrum.MeasurementID);
                    spectraMeasurementTimer[channel] = new Timer(500);
                    spectraMeasurementTimer[channel].Elapsed += delegate { SpectraMeasurementWorker(newSpectrum.MeasurementID, channel); };
                    spectraMeasurementTimer[channel].Start();
                }
            }
        }

        /// <summary>
        /// Stops the measurement for the selected channels (<see cref="selectedChannels"/>).
        /// </summary>
        public void StopMeasurements()
        {
            foreach (int channel in activeChannels.Keys.ToList())
            {
                int measurementID = activeChannels[channel];
                cAEN_x730.StopAcquisition(channel);

                spectraMeasurementTimer[channel].Stop();
                Console.WriteLine("ID to stop: {0}", measurementID);

                activeChannels.Remove(channel);

                using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
                {
                    Measurement MeasurementToStop = Database.Measurements.FirstOrDefault(x => x.MeasurementID == measurementID);

                    if (MeasurementToStop == null)
                    { trace.TraceEvent(TraceEventType.Warning, 0, "Can't finish Measurement: Measurement with MeasurementID = {0} not found", measurementID); return; }

                    MeasurementToStop.StopTime = DateTime.Now;
                    MeasurementToStop.Runs = false;

                    Database.SubmitChanges();

                    Sample temp = MeasurementToStop.Sample; // To load the sample before the scope of Database ends

                    //if (EventMeasurementFinished != null) EventMeasurementFinished(MeasurementToStop);
                }
            }
        }

        /// <summary>
        /// Function that is called by the spectraMeasurementTimer. It reads a spectrum sends it to the <see cref="DatabaseUtils"/> class.
        /// </summary>
        /// <param name="measurementID">ID of the measurement where the spectra will be send to.</param>
        /// <param name="channel">Channel to read the spectrum from.</param>
        private void SpectraMeasurementWorker(int measurementID, int channel)
        {
            int[] newSpectrumY = cAEN_x730.GetHistogram(channel);
            trace.TraceEvent(TraceEventType.Verbose, 0, "MeasurementWorker ID = {0}; Counts = {1} ", measurementID, newSpectrumY.Sum());

            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
            {
                Measurement MeasurementToUpdate = Database.Measurements.FirstOrDefault(x => x.MeasurementID == measurementID);

                if (MeasurementToUpdate == null)
                { trace.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Measurement with MeasurementID = {0} not found", measurementID); return; }

                if (newSpectrumY.Length != 16384) // TODO!!!
                { trace.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't match"); return; }

                MeasurementToUpdate.SpectrumY = DatabaseUtils.GetByteSpectrumY(newSpectrumY);

                MeasurementToUpdate.Duration = new DateTime(2000, 01, 01) + (DateTime.Now - MeasurementToUpdate.StartTime);

                switch (MeasurementToUpdate.StopType)
                {
                    case "Manual": MeasurementToUpdate.Progress = 0; break;
                    case "Counts":
                        MeasurementToUpdate.Progress = newSpectrumY.Sum() / (int)MeasurementToUpdate.StopValue;
                        break;
                    case "Time":
                        MeasurementToUpdate.Progress = (MeasurementToUpdate.Duration - DateTime.MinValue).TotalMinutes / (int)MeasurementToUpdate.StopValue; break;
                        // TODO: Chopper
                }

                Database.SubmitChanges();

                if (MeasurementToUpdate.Progress >= 1)
                {
                    MeasurementToUpdate.Progress = 1;
                    Database.SubmitChanges();
                    StopMeasurements();
                }
            }
        }
    }
}

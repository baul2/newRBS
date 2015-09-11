using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using GalaSoft.MvvmLight.Ioc;
using System.Data.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using newRBS.Database;

namespace newRBS.Models
{
    /// <summary>
    /// Class responsible for simultaneous measurements of spectra on several channels. 
    /// </summary>
    public class MeasureSpectra
    {
        private CAEN_x730 cAEN_x730;

        TraceSource trace = new TraceSource("MeasureSpectra");

        public double[] EnergyCalOffset = new double[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public double[] EnergyCalSlope = new double[8] { 1, 1, 1, 1, 1, 1, 1, 1 };

        private Timer[] MeasureSpectraTimer = new Timer[8];

        private Dictionary<int, int> activeChannels = new Dictionary<int, int>(); // <Channel,ID>

        /// <summary>
        /// Constructor of the class. Gets a reference to the instance of <see cref="CAEN_x730"/> from <see cref="ViewModels.ViewModelLocator"/>.
        /// </summary>
        public MeasureSpectra()
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<CAEN_x730>();
        }

        /// <summary>
        /// Function that returns the acquisition status of the device.
        /// </summary>
        /// <returns>TRUE if the divice is acquiring, FALS if not.</returns>
        public bool IsAcquiring()
        {
            if (cAEN_x730.ActiveChannels.Count > 0)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Function that starts the acquisitions for the given channels and initiates a new instance of <see cref="Database.Measurement"/> in the database. 
        /// </summary>
        /// <param name="SelectedChannels">The channel numbers to start the acquisitions.</param>
        public void StartAcquisitions(List<int> SelectedChannels, Measurement NewMeasurement, int SampleID)
        {
            List<int> IDs = new List<int>();

            cAEN_x730.SetMeasurementMode(CAENDPP_AcqMode_t.CAENDPP_AcqMode_Histogram);

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                foreach (int channel in SelectedChannels)
                {
                    cAEN_x730.StartAcquisition(channel);

                    NewMeasurement.MeasurementID = 0;
                    NewMeasurement.Channel = channel;
                    NewMeasurement.EnergyCalOffset = EnergyCalOffset[channel];//ToDo: Find latest value in database!
                    NewMeasurement.EnergyCalSlope = EnergyCalSlope[channel];//ToDo: Find latest value in database!
                    NewMeasurement.StartTime = DateTime.Now;
                    NewMeasurement.Sample = Database.Samples.Single(x => x.SampleID == SampleID);
                    NewMeasurement.CurrentDuration = new DateTime(2000, 01, 01);
                    NewMeasurement.CurrentCharge = 0;
                    NewMeasurement.CurrentCounts = 0;
                    NewMeasurement.CurrentChopperCounts = 0;
                    NewMeasurement.NumOfChannels = cAEN_x730.NumberOfChanels;
                    NewMeasurement.SpectrumY = new int[] { 0 };
                    NewMeasurement.Runs = true;

                    Database.Measurements.InsertOnSubmit(NewMeasurement);

                    Database.SubmitChanges();
                    activeChannels.Add(channel, NewMeasurement.MeasurementID);

                    Console.WriteLine("New measurementID: {0}", NewMeasurement.MeasurementID);
                    MeasureSpectraTimer[channel] = new Timer(500);
                    MeasureSpectraTimer[channel].Elapsed += delegate { MeasureSpectraWorker(NewMeasurement.MeasurementID, channel); };
                    MeasureSpectraTimer[channel].Start();
                }
            }
        }

        /// <summary>
        /// Function that stops the acquisition for all active channels and finishes the corresponging instances of <see cref="Database.Measurement"/> in the database.
        /// </summary>
        public void StopAcquisitions()
        {
            foreach (int channel in activeChannels.Keys.ToList())
            {
                int measurementID = activeChannels[channel];
                cAEN_x730.StopAcquisition(channel);

                MeasureSpectraTimer[channel].Stop();
                Console.WriteLine("ID to stop: {0}", measurementID);

                activeChannels.Remove(channel);

                using (DatabaseDataContext Database = MyGlobals.Database)
                {
                    Measurement MeasurementToStop = Database.Measurements.FirstOrDefault(x => x.MeasurementID == measurementID);

                    if (MeasurementToStop == null)
                    { trace.TraceEvent(TraceEventType.Warning, 0, "Can't finish Measurement: Measurement with MeasurementID = {0} not found", measurementID); return; }

                    MeasurementToStop.Runs = false;

                    Database.SubmitChanges();

                    Sample temp = MeasurementToStop.Sample; // To load the sample before the scope of Database ends

                    //if (EventMeasurementFinished != null) EventMeasurementFinished(MeasurementToStop);
                }
            }
        }

        /// <summary>
        /// Function that get the new SpectrumY from <see cref="CAEN_x730.GetHistogram(int)"/> and updates the corresponding <see cref="Measurement"/> instance.
        /// </summary>
        /// <param name="MeasurementID">ID of the measurement where the spectra will be send to.</param>
        /// <param name="Channel">Channel to read the spectrum from.</param>
        private void MeasureSpectraWorker(int MeasurementID, int Channel)
        {
            int[] newSpectrumY = cAEN_x730.GetHistogram(Channel);
            trace.TraceEvent(TraceEventType.Verbose, 0, "MeasurementWorker ID = {0}; Counts = {1} ", MeasurementID, newSpectrumY.Sum());

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                Measurement MeasurementToUpdate = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);

                if (MeasurementToUpdate == null)
                { trace.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Measurement with MeasurementID = {0} not found", MeasurementID); return; }

                if (newSpectrumY.Length != 16384) // TODO!!!
                { trace.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't match"); return; }

                MeasurementToUpdate.SpectrumY = newSpectrumY;

                MeasurementToUpdate.CurrentDuration = new DateTime(2000, 01, 01) + (DateTime.Now - MeasurementToUpdate.StartTime);
                MeasurementToUpdate.CurrentCounts = newSpectrumY.Sum();
                //MeasurementToUpdate.CurrentCharge = GetCharge();                  //TODO
                //MeasurementToUpdate.CurrentChopperCounts = GetChopperCounts();    //TODO

                switch (MeasurementToUpdate.StopType)
                {
                    case "Manual":
                        MeasurementToUpdate.Progress = 0; break;
                    case "Duration":
                        MeasurementToUpdate.Progress = (MeasurementToUpdate.CurrentDuration - new DateTime(2000, 01, 01)).TotalMinutes / ((DateTime)MeasurementToUpdate.FinalDuration - new DateTime(2000, 01, 01)).TotalMinutes; break;
                    case "Charge":
                        MeasurementToUpdate.Progress = MeasurementToUpdate.CurrentCharge / (double)MeasurementToUpdate.FinalCharge; break;
                    case "Counts":
                        MeasurementToUpdate.Progress = (double)MeasurementToUpdate.CurrentCounts / (long)MeasurementToUpdate.FinalCounts; Console.WriteLine(MeasurementToUpdate.Progress); break;
                    case "ChopperCounts":
                        MeasurementToUpdate.Progress = (double)MeasurementToUpdate.CurrentChopperCounts / (long)MeasurementToUpdate.FinalChopperCounts; break;
                }

                Database.SubmitChanges();

                if (MeasurementToUpdate.Progress >= 1)
                {
                    MeasurementToUpdate.Progress = 1;
                    Database.SubmitChanges();
                    StopAcquisitions();
                }
            }
        }
    }
}

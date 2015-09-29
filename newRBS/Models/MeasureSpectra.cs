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
using System.Reflection;
using System.IO;

namespace newRBS.Models
{
    /// <summary>
    /// Class responsible for simultaneous measurements of spectra on several channels. 
    /// </summary>
    public class MeasureSpectra
    {
        private CAEN_x730 cAEN_x730;
        private Coulombo coulombo;

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        public int ChopperStartChannel = 920;
        public int ChopperEndChannel = 980;

        private Timer[] MeasureSpectraTimer = new Timer[8];

        private Dictionary<int, int> activeChannels = new Dictionary<int, int>(); // <Channel,ID>

        /// <summary>
        /// Constructor of the class. Gets a reference to the instance of <see cref="CAEN_x730"/> from <see cref="ViewModels.ViewModelLocator"/>.
        /// </summary>
        public MeasureSpectra()
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<CAEN_x730>();
            coulombo = SimpleIoc.Default.GetInstance<Coulombo>();
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
        public void StartAcquisitions(List<int> SelectedChannels, Measurement NewMeasurement, int SampleID, int IncomingIonIsotopeID)
        {
            List<int> IDs = new List<int>();

            cAEN_x730.SetMeasurementMode(CAENDPP_AcqMode_t.CAENDPP_AcqMode_Histogram);

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                foreach (int channel in SelectedChannels)
                {
                    // Delete test measurements
                    DatabaseUtils.DeleteMeasurements(Database.Measurements.Where(x => x.IsTestMeasurement == true).Select(y=>y.MeasurementID).ToList());

                    cAEN_x730.StartAcquisition(channel);

                    //if (NewMeasurement.StopType == "ChopperCounts")
                    {
                        cAEN_x730.StartAcquisition(7); // Start chopper
                    }

                    var LastMeasurement = Database.Measurements.Where(x => x.Channel == channel).OrderByDescending(y => y.StartTime).FirstOrDefault();
                    if (LastMeasurement != null)
                    {
                        NewMeasurement.EnergyCalOffset = LastMeasurement.EnergyCalOffset;
                        NewMeasurement.EnergyCalLinear = LastMeasurement.EnergyCalLinear;
                    }
                    else
                    {
                        NewMeasurement.EnergyCalOffset = 0;
                        NewMeasurement.EnergyCalLinear = 0.2;
                    }

                    NewMeasurement.MeasurementID = 0;
                    NewMeasurement.Channel = channel;
                    NewMeasurement.StartTime = DateTime.Now;
                    NewMeasurement.Sample = Database.Samples.Single(x => x.SampleID == SampleID);
                    NewMeasurement.Isotope = Database.Isotopes.FirstOrDefault(x => x.IsotopeID == IncomingIonIsotopeID);
                    NewMeasurement.CurrentDuration = new DateTime(2000, 01, 01);
                    NewMeasurement.CurrentCharge = 0;
                    NewMeasurement.CurrentCounts = 0;
                    NewMeasurement.CurrentChopperCounts = 0;
                    NewMeasurement.NumOfChannels = cAEN_x730.NumberOfChanels;
                    NewMeasurement.SpectrumY = new int[] { 0 };
                    NewMeasurement.Runs = true;

                    if (NewMeasurement.StopType == "Charge (µC)")
                    {
                        coulombo.SetCharge(NewMeasurement.StopValue);
                    }
                    else
                    {
                        coulombo.SetCharge(9999);
                    }
                    coulombo.Start();

                    Database.Measurements.InsertOnSubmit(NewMeasurement);

                    Database.SubmitChanges();
                    activeChannels.Add(channel, NewMeasurement.MeasurementID);

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurement " + NewMeasurement.MeasurementID + " started on channel " + NewMeasurement.Channel);
                    MeasureSpectraTimer[channel] = new Timer(1000);
                    MeasureSpectraTimer[channel].Elapsed += delegate { MeasureSpectraWorker(NewMeasurement.MeasurementID, channel); };
                    MeasureSpectraTimer[channel].Start();
                }
            }
        }

        /// <summary>
        /// Function that stops the acquisition for all active channels, finishes the corresponging instances of <see cref="Database.Measurement"/> in the database and exports the measurement to the backup folder.
        /// </summary>
        public void StopAcquisitions()
        {
            foreach (int channel in activeChannels.Keys.ToList())
            {
                int measurementID = activeChannels[channel];
                cAEN_x730.StopAcquisition(channel);

                MeasureSpectraTimer[channel].Stop();

                activeChannels.Remove(channel);

                using (DatabaseDataContext Database = MyGlobals.Database)
                {
                    Measurement MeasurementToStop = Database.Measurements.FirstOrDefault(x => x.MeasurementID == measurementID);

                    if (MeasurementToStop == null)
                    { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't finish Measurement: Measurement with MeasurementID = " + measurementID + " not found"); return; }

                    MeasurementToStop.Runs = false;

                    Database.SubmitChanges();

                    string path = "Backup/" + DateTime.Now.ToString("yyyy'/'MM'/'dd'/'");
                    string user = new string(MyGlobals.ConString.Split(';').FirstOrDefault(x => x.Contains("User ID = ")).Skip(11).ToArray());
                    string file = DateTime.Now.ToString("HH-mm-ss") + "_User-" + user + "_MeasurementID-" + MeasurementToStop.MeasurementID + ".dat";

                    DirectoryInfo di = Directory.CreateDirectory(path);

                    DatabaseUtils.ExportMeasurements(new List<int> { MeasurementToStop.MeasurementID }, path + file);

                    //if (MeasurementToStop.StopType == "ChopperCounts")
                        cAEN_x730.StopAcquisition(7);

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurement " + MeasurementToStop.MeasurementID + " stopped (channel " + MeasurementToStop.Channel + ")");
                }
            }
        }

        /// <summary>
        /// Function that get the new SpectrumY from <see cref="CAEN_x730.GetHistogram(int)"/> and updates the corresponding <see cref="Measurement"/> instance.
        /// </summary>
        /// <param name="MeasurementID">ID of the measurement where the spectra will be send to.</param>
        /// <param name="Channel">Channel to read the spectrum from.</param>
        public void MeasureSpectraWorker(int MeasurementID, int Channel)
        {
            int[] newSpectrumY = cAEN_x730.GetHistogram(Channel);
            long sum = newSpectrumY.Sum();
            trace.Value.TraceEvent(TraceEventType.Verbose, 0, "MeasureSpectraWorker ID = " + MeasurementID + "; Counts = " + sum);

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                Measurement MeasurementToUpdate = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);

                if (MeasurementToUpdate == null)
                { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Measurement with MeasurementID = " + MeasurementID + " not found"); return; }

                if (newSpectrumY.Length != MeasurementToUpdate.NumOfChannels)
                { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't match"); return; }

                MeasurementToUpdate.SpectrumY = newSpectrumY;

                MeasurementToUpdate.CurrentDuration = new DateTime(2000, 01, 01) + (DateTime.Now - MeasurementToUpdate.StartTime);
                MeasurementToUpdate.CurrentCounts = sum;
                MeasurementToUpdate.CurrentCharge = coulombo.GetCharge();

                //if (MeasurementToUpdate.StopType == "ChopperCounts")
                {
                    MeasurementToUpdate.CurrentChopperCounts = cAEN_x730.GetHistogram(7).Take(ChopperEndChannel).Skip(ChopperStartChannel).Sum();
                    Console.WriteLine("CurrentChopperCounts: " + MeasurementToUpdate.CurrentChopperCounts);
                }

                switch (MeasurementToUpdate.StopType)
                {
                    case "Manual":
                        MeasurementToUpdate.Progress = 0; break;
                    case "Duration (min)":
                        MeasurementToUpdate.Progress = (MeasurementToUpdate.CurrentDuration - new DateTime(2000, 01, 01)).TotalMinutes / MeasurementToUpdate.StopValue; break;
                    case "Charge (µC)":
                        MeasurementToUpdate.Progress = MeasurementToUpdate.CurrentCharge / MeasurementToUpdate.StopValue; break;
                    case "Counts":
                        MeasurementToUpdate.Progress = (double)MeasurementToUpdate.CurrentCounts / MeasurementToUpdate.StopValue; break;
                    case "ChopperCounts":
                        MeasurementToUpdate.Progress = (double)MeasurementToUpdate.CurrentChopperCounts / MeasurementToUpdate.StopValue; break;
                }

                if (MeasurementToUpdate.Progress > 0)
                    MeasurementToUpdate.Remaining = new DateTime(2000, 01, 01) + TimeSpan.FromSeconds((new DateTime(2000, 01, 01) - MeasurementToUpdate.CurrentDuration).TotalSeconds * (1 - 1 / MeasurementToUpdate.Progress));

                Database.SubmitChanges();

                if (MeasurementToUpdate.Progress >= 1)
                {
                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurement " + MeasurementToUpdate.MeasurementID + " has been finished (Progress=1)");
                    MeasurementToUpdate.Progress = 1;
                    MeasurementToUpdate.Remaining = new DateTime(2000, 01, 01);
                    Database.SubmitChanges();
                    StopAcquisitions();
                }
            }
        }
    }
}

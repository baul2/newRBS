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
using System.Windows;
using System.Xml.Serialization;
using GalaSoft.MvvmLight;

namespace newRBS.Models
{
    public class ActiveChannel
    {
        public int Channel { get; set; }
        public int MeasurementID { get; set; }
    }

    public class ChopperConfig : ViewModelBase
    {
        private int _LeftIntervalChannel;
        public int LeftIntervalChannel { get { return _LeftIntervalChannel; } set { _LeftIntervalChannel = value; RaisePropertyChanged(); } }

        private int _RightIntervalChannel;
        public int RightIntervalChannel { get { return _RightIntervalChannel; } set { _RightIntervalChannel = value; RaisePropertyChanged(); } }

        private int _IonMassNumber;
        public int IonMassNumber { get { return _IonMassNumber; } set { _IonMassNumber = value; RaisePropertyChanged(); } }

        private double _IonEnergy;
        public double IonEnergy { get { return _IonEnergy; } set { _IonEnergy = value; RaisePropertyChanged(); } }
    }

    /// <summary>
    /// Class responsible for simultaneous measurements of spectra on several channels. 
    /// </summary>
    public class MeasureSpectra
    {
        private Coulombo coulombo;

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        public ChopperConfig chopperConfig;

        private Timer MeasureSpectraTimer;

        private List<ActiveChannel> ActiveChannels = new List<ActiveChannel>(); // <Channel,ID>

        /// <summary>
        /// Constructor of the class. Gets a reference to the instance of <see cref="CAEN_x730"/> from <see cref="ViewModels.ViewModelLocator"/>.
        /// </summary>
        public MeasureSpectra()
        {
            LoadChopperConfig();
        }

        public void LoadChopperConfig()
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(ChopperConfig));
            FileStream ReadFileStream;

            string path = "ConfigurationFiles/Chopper.xml";
            if (File.Exists(path))
            {
                ReadFileStream = new FileStream(path, FileMode.Open);
                chopperConfig = (ChopperConfig)SerializerObj.Deserialize(ReadFileStream);
                ReadFileStream.Close();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Chopper configuration read from file " + path);
            }
            else
            {
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't red Chopper configuration file " + path);

                chopperConfig = new ChopperConfig { LeftIntervalChannel = 0, RightIntervalChannel = 1000, IonMassNumber = 1, IonEnergy = 1400 };
            }
        }

        public void SaveChopperConfig()
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(ChopperConfig));
            TextWriter WriteFileStream;

            string path = "ConfigurationFiles/Chopper.xml";

            WriteFileStream = new StreamWriter(path);
            SerializerObj.Serialize(WriteFileStream, chopperConfig);
            WriteFileStream.Close();

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Chopper configuration saved to file " + path);
        }

        /// <summary>
        /// Function that returns the acquisition status of the device.
        /// </summary>
        /// <returns>TRUE if the divice is acquiring, FALS if not.</returns>
        public bool IsAcquiring()
        {
            if (CAEN_x730.ActiveChannels.Count > 0)
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

            CAEN_x730.SetMeasurementMode(CAENDPP_AcqMode_t.CAENDPP_AcqMode_Histogram);

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                // Delete test measurements
                DatabaseUtils.DeleteMeasurements(Database.Measurements.Where(x => x.IsTestMeasurement == true).Select(y => y.MeasurementID).ToList());

                switch (NewMeasurement.Chamber)
                {
                    case "-10°":
                        {
                            Console.WriteLine(chopperConfig.IonEnergy + " " + NewMeasurement.IncomingIonEnergy);
                            Console.WriteLine(chopperConfig.IonMassNumber + " " + NewMeasurement.Isotope.MassNumber);
                            if (chopperConfig.IonEnergy != NewMeasurement.IncomingIonEnergy || chopperConfig.IonMassNumber != NewMeasurement.Isotope.MassNumber)
                            {
                                if (MessageBox.Show("Chopper was configured for another ion beam. Please update the chopper configuration!\nContinue anyway?", "Error", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                                    return;
                            }
                            CAEN_x730.StartAcquisition(7); // Start chopper
                            break;
                        }
                    case "-30°":
                        {
                            coulombo = SimpleIoc.Default.GetInstance<Coulombo>();
                            if (NewMeasurement.StopType == "Charge (µC)")
                                coulombo.SetCharge(NewMeasurement.StopValue);
                            else
                                coulombo.SetCharge(9999);
                            coulombo.Start();
                            break;
                        }
                }

                foreach (int channel in SelectedChannels)
                {
                    CAEN_x730.StartAcquisition(channel);

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
                    NewMeasurement.NumOfChannels = CAEN_x730.NumberOfChanels;
                    NewMeasurement.SpectrumY = new int[] { 0 };
                    NewMeasurement.Runs = true;

                    Database.Measurements.InsertOnSubmit(NewMeasurement);

                    Database.SubmitChanges();
                    ActiveChannels.Add(new ActiveChannel { Channel = channel, MeasurementID = NewMeasurement.MeasurementID });

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurement " + NewMeasurement.MeasurementID + " started on channel " + NewMeasurement.Channel);
                }

                MeasureSpectraTimer = new Timer(1000);
                MeasureSpectraTimer.Elapsed += delegate { MeasureSpectraWorker(); };
                MeasureSpectraTimer.Start();
            }
        }

        /// <summary>
        /// Function that stops the acquisition for all active channels, finishes the corresponging instances of <see cref="Database.Measurement"/> in the database and exports the measurement to the backup folder.
        /// </summary>
        public void StopAcquisitions()
        {
            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                MeasureSpectraTimer.Stop();

                switch (Database.Measurements.FirstOrDefault(x => x.MeasurementID == ActiveChannels.FirstOrDefault().MeasurementID).Chamber)
                {
                    case "-10°":
                        { CAEN_x730.StopAcquisition(7); break; }
                    case "-30°":
                        { coulombo.Stop(); break; }
                }

                foreach (ActiveChannel activeChannel in ActiveChannels)
                {
                    Measurement MeasurementToStop = Database.Measurements.FirstOrDefault(x => x.MeasurementID == activeChannel.MeasurementID);

                    if (MeasurementToStop == null)
                    { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't finish Measurement: Measurement with MeasurementID = " + activeChannel.MeasurementID + " not found"); return; }

                    CAEN_x730.StopAcquisition(activeChannel.Channel);

                    MeasurementToStop.Runs = false;

                    Database.SubmitChanges();

                    string path = "Backup/" + DateTime.Now.ToString("yyyy'/'MM'/'dd'/'");
                    string user = new string(MyGlobals.ConString.Split(';').FirstOrDefault(x => x.Contains("User ID = ")).Skip(11).ToArray());
                    string file = DateTime.Now.ToString("HH-mm-ss") + "_User-" + user + "_MeasurementID-" + MeasurementToStop.MeasurementID + ".dat";

                    DirectoryInfo di = Directory.CreateDirectory(path);

                    DatabaseUtils.ExportMeasurements(new List<int> { MeasurementToStop.MeasurementID }, path + file);

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurement " + MeasurementToStop.MeasurementID + " stopped (channel " + MeasurementToStop.Channel + ")");
                }

                ActiveChannels.Clear();
            }
        }

        /// <summary>
        /// Function that get the new SpectrumY from <see cref="CAEN_x730.GetHistogram(int)"/> and updates the corresponding <see cref="Measurement"/> instance.
        /// </summary>
        /// <param name="MeasurementID">ID of the measurement where the spectra will be send to.</param>
        /// <param name="Channel">Channel to read the spectrum from.</param>
        public void MeasureSpectraWorker()
        {
            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                long currentChopperCounts = 0;
                double currentCharge = 0;
                double currentValue = 0;

                switch (Database.Measurements.FirstOrDefault(x => x.MeasurementID == ActiveChannels.FirstOrDefault().MeasurementID).Chamber)
                {
                    case "-10°":
                        { currentChopperCounts = CAEN_x730.GetHistogram(7).Take(chopperConfig.RightIntervalChannel).Skip(chopperConfig.LeftIntervalChannel).Sum(); break; }
                    case "-30°":
                        { currentCharge = coulombo.GetCharge(); break; }
                }

                foreach (ActiveChannel activeChannel in ActiveChannels)
                {
                    Measurement MeasurementToUpdate = Database.Measurements.FirstOrDefault(x => x.MeasurementID == activeChannel.MeasurementID);

                    int[] newSpectrumY = CAEN_x730.GetHistogram(activeChannel.Channel);
                    long sum = newSpectrumY.Sum();
                    trace.Value.TraceEvent(TraceEventType.Verbose, 0, "MeasureSpectraWorker ID = " + activeChannel.MeasurementID + "; Counts = " + sum);

                    if (MeasurementToUpdate == null)
                    { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Measurement with MeasurementID = " + activeChannel.MeasurementID + " not found"); return; }

                    if (newSpectrumY.Length != MeasurementToUpdate.NumOfChannels)
                    { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't match"); return; }

                    MeasurementToUpdate.SpectrumY = newSpectrumY;

                    MeasurementToUpdate.CurrentDuration = new DateTime(2000, 01, 01) + (DateTime.Now - MeasurementToUpdate.StartTime);
                    MeasurementToUpdate.CurrentCounts = sum;

                    switch (MeasurementToUpdate.Chamber)
                    {
                        case "-10°":
                            {
                                MeasurementToUpdate.CurrentChopperCounts = currentChopperCounts;
                                currentValue = currentChopperCounts;
                                break;
                            }
                        case "-30°":
                            {
                                MeasurementToUpdate.CurrentCharge = currentCharge;
                                currentValue = currentCharge;
                                break;
                            }
                    }

                    if (MyGlobals.Charge_CountsOverTime.Count() == 0)
                    {
                        MyGlobals.Charge_CountsOverTime.Add(new TimeSeriesEvent { Time = DateTime.Now, Value = 0 });
                    }

                    if ((DateTime.Now - MyGlobals.Charge_CountsOverTime.LastOrDefault().Time).TotalSeconds >= MyGlobals.TimePlotIntervall)
                    {
                        double oldCounts = MyGlobals.Charge_CountsOverTime.Sum(x => x.Value);
                        MyGlobals.Charge_CountsOverTime.Add(new TimeSeriesEvent { Time = DateTime.Now, Value = (currentValue / MyGlobals.TimePlotIntervall - oldCounts) });
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
}

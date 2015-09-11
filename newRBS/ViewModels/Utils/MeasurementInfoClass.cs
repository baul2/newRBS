using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Threading;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Win32;
using System.IO;
using OxyPlot;
using newRBS.Database;

namespace newRBS.ViewModels.Utils
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.Utils.MeasurementInfo"/>. They display/edit the properties of a <see cref="Measurement"/>.
    /// </summary>
    public class MeasurementInfoClass : ViewModelBase
    {
        private DatabaseDataContext Database;

        public ICommand NewSampleCommand { get; set; }

        private Measurement _Measurement;
        public Measurement Measurement
        {
            get { return _Measurement; }
            set { if (value == null) return; _Measurement = value; SelectedStopType = value.StopType;RaisePropertyChanged(); }
        }

        private ObservableCollection<Sample> _Samples;
        public ObservableCollection<Sample> Samples
        { get { return _Samples; } set { _Samples = value; RaisePropertyChanged(); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<ElementClass> Ions { get; set; }

        public ObservableCollection<string> StopTypes { get; set; }
        private string _SelectedStopType;
        public string SelectedStopType
        { get { return _SelectedStopType; } set { _SelectedStopType = value; SelectedStopTypeChanged(); RaisePropertyChanged(); } }

        private string _StopValueLabel = "";
        public string StopValueLabel
        { get { return _StopValueLabel; } set { _StopValueLabel = value; RaisePropertyChanged(); } }

        private string _StopValue = "";
        public string StopValue
        { get { return _StopValue; } set { _StopValue = value; StopValueChanged(); RaisePropertyChanged(); } }

        /// <summary>
        /// Constructor of the class, storing the handled instance of <see cref="DatabaseDataContext"/> and initializing the collections for the Comboboxes.
        /// </summary>
        /// <param name="database"></param>
        public MeasurementInfoClass(DatabaseDataContext database)
        {
            Database = database;
            Samples = new ObservableCollection<Sample>(Database.Samples.ToList());

            NewSampleCommand = new RelayCommand(() => _NewSampleCommand(), () => true);

            Orientations = new ObservableCollection<string> { "(undefined)", "random", "aligned" };
            Chambers = new ObservableCollection<string> { "(undefined)", "-10°", "-30°" };
            StopTypes = new ObservableCollection<string> { "Manual", "Duration", "Charge", "Counts", "ChopperCounts" };
            Ions = new ObservableCollection<ElementClass> { new ElementClass { ShortName = "H", AtomicNumber = 1, AtomicMass = 1 }, new ElementClass { ShortName = "He", AtomicNumber = 2, AtomicMass = 4 }, new ElementClass { ShortName = "Li", AtomicNumber = 3, AtomicMass = 7 } }; 
        }

        private void SelectedStopTypeChanged()
        {
            Measurement.StopType = SelectedStopType;
            switch (SelectedStopType)
            {
                case "Manual":
                    StopValueLabel = "";
                    StopValue = "";
                    break;
                case "Duration":
                    StopValueLabel = "Duration (min):";
                    StopValue = Math.Round(((DateTime)Measurement.FinalDuration - new DateTime(2000, 01, 01)).TotalMinutes,1).ToString();
                    break;
                case "Charge":
                    StopValueLabel = "Charge (µC):";
                    StopValue = Measurement.FinalCharge.ToString();
                    break;
                case "Counts":
                    StopValueLabel = "Counts:";
                    StopValue = Measurement.FinalCounts.ToString();
                    break;
                case "ChopperCounts":
                    StopValueLabel = "ChopperCounts:";
                    StopValue = Measurement.FinalChopperCounts.ToString();
                    break;
            }
        }

        private void StopValueChanged()
        {
            switch (SelectedStopType)
            {
                case "Manual": break;
                case "Duration":
                    if (StopValue != Math.Round(((DateTime)Measurement.FinalDuration - new DateTime(2000, 01, 01)).TotalMinutes, 1).ToString())
                        Measurement.FinalDuration = new DateTime(2000, 01, 01) + TimeSpan.FromMinutes(Convert.ToDouble(StopValue));
                        break;
                case "Charge":
                    if (StopValue != Measurement.FinalCharge.ToString())
                        Measurement.FinalCharge = Convert.ToDouble(StopValue);
                    break;
                case "Counts":
                    if (StopValue != Measurement.FinalCounts.ToString())
                        Measurement.FinalCounts = Convert.ToInt64(StopValue);
                    break;
                case "ChopperCounts":
                    if( StopValue != Measurement.CurrentChopperCounts.ToString())
                        Measurement.FinalChopperCounts = Convert.ToInt64(StopValue);
                    break;
            }
        }

        /// <summary>
        /// Function that creates a new <see cref="Sample"/> instance and attaches it the the current <see cref="Measurement"/>.
        /// </summary>
        private void _NewSampleCommand()
        {
            int? newSampleID = DatabaseUtils.AddNewSample();
            if (newSampleID != null)
            {
                Sample newSample = Database.Samples.FirstOrDefault(x => x.SampleID == newSampleID);
                if (!Samples.Contains(newSample))
                    Samples.Add(newSample);
                Measurement.Sample = newSample;
            }
        }
    }
}

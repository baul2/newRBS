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
using newRBS.ViewModels.Utils;
using newRBS.Database;

namespace newRBS.ViewModels
{
    public class NewMeasurementViewModel : ViewModelBase
    {
        private Models.MeasureSpectra measureSpectra;
        private DatabaseDataContext Database;

        public ICommand NewSampleCommand { get; set; }

        public ICommand StartMeasurementCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ObservableCollection<CheckedListItem<int>> Channels_10 { get; set; }
        public ObservableCollection<CheckedListItem<int>> Channels_30 { get; set; }

        private Measurement _Measurement;
        public Measurement Measurement
        {
            get { return _Measurement; }
            set { _Measurement = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<Sample> _Samples;
        public ObservableCollection<Sample> Samples
        { get { return _Samples; } set { _Samples = value; RaisePropertyChanged(); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<ElementClass> Ions { get; set; }

        public ObservableCollection<string> StopTypes { get; set; }
        private string _SelectedStopType = "Manual";
        public string SelectedStopType
        { get { return _SelectedStopType; } set { _SelectedStopType = value; SelectedStopTypeChanged(); RaisePropertyChanged(); } }

        private string _StopValueLabel= "";
        public string StopValueLabel
        { get { return _StopValueLabel; } set { _StopValueLabel = value; RaisePropertyChanged(); } }
        public string StopValue { get; set; }
        
        private int _SelectedChamberTabIndex = 0;
        public int SelectedChamberTabIndex
        { get { return _SelectedChamberTabIndex; } set { _SelectedChamberTabIndex = value; RaisePropertyChanged(); } }

        private int _SelectedVariablesTabIndex = 0;
        public int SelectedVariablesTabIndex
        { get { return _SelectedVariablesTabIndex; } set { _SelectedVariablesTabIndex = value; RaisePropertyChanged(); } }

        public ObservableCollection<string> VariableParameters { get; set; }

        public NewMeasurementViewModel()
        {
            measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();
            Database = MyGlobals.Database;

            NewSampleCommand = new RelayCommand(() => _NewSampleCommand(), () => true);

            StartMeasurementCommand = new RelayCommand(() => _StartMeasurementCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Channels_10 = new ObservableCollection<CheckedListItem<int>> { new CheckedListItem<int>(0), new CheckedListItem<int>(1), new CheckedListItem<int>(2), new CheckedListItem<int>(3) };
            Channels_30 = new ObservableCollection<CheckedListItem<int>> { new CheckedListItem<int>(4), new CheckedListItem<int>(5) };

            Channels_10[0].IsChecked = true;
            Channels_30[0].IsChecked = true;

            Orientations = new ObservableCollection<string> { "(undefined)", "random", "aligned" };
            Chambers = new ObservableCollection<string> { "(undefined)", "-10°", "-30°" };
            StopTypes = new ObservableCollection<string> { "Manual", "Duration", "Charge", "Counts", "ChopperCounts" };
            Ions = new ObservableCollection<ElementClass> { new ElementClass { ShortName = "H", AtomicNumber = 1, AtomicMass = 1 }, new ElementClass { ShortName = "He", AtomicNumber = 2, AtomicMass = 4 }, new ElementClass { ShortName = "Li", AtomicNumber = 3, AtomicMass = 7 } };

            Samples = new ObservableCollection<Sample>(Database.Samples.ToList());

            Measurement = Database.Measurements.OrderByDescending(x => x.StartTime).First();

            VariableParameters = new ObservableCollection<string> { "x", "y", "Theta", "Phi", "Energy", "Charge" };
        }

        private void SelectedStopTypeChanged()
        {
            Console.WriteLine(Measurement.IncomingIonAtomicNumber);
            switch (SelectedStopType)
            {
                case "Manual": StopValueLabel = ""; break;
                case "Duration": StopValueLabel = "Duration (min):";break;
                case "Charge": StopValueLabel = "Charge (µC):";  break;
                case "Counts": StopValueLabel = "Counts:"; break;
                case "ChopperCounts": StopValueLabel = "ChopperCounts:"; break;
            }
        }

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

        private void _StartMeasurementCommand()
        {
            DialogResult = false;
            _DialogResult = null;

            measureSpectra.MeasurementName = Measurement.MeasurementName;
            measureSpectra.SampleID = Measurement.SampleID;
            measureSpectra.SampleRemark = Measurement.SampleRemark;
            measureSpectra.Orientation = Measurement.Orientation;
            measureSpectra.IncomingIonAtomicNumber = Measurement.IncomingIonAtomicNumber;
            measureSpectra.IncomingIonEnergy = Measurement.IncomingIonEnergy;
            measureSpectra.IncomingIonAngle = Measurement.IncomingIonAngle;
            measureSpectra.SolidAngle = Measurement.SolidAngle;
            measureSpectra.StopType = Measurement.StopType;
            switch (SelectedStopType)
            {
                case "Manual": break;
                case "Duration": measureSpectra.FinalDuration = new DateTime(2000,01,01)+TimeSpan.FromMinutes(Convert.ToDouble(StopValue)); break;
                case "Charge": measureSpectra.FinalCharge = Convert.ToDouble(StopValue); break;
                case "Counts": measureSpectra.FinalCounts = Convert.ToInt64(StopValue); break;
                case "ChopperCounts": measureSpectra.FinalChopperCounts = Convert.ToInt64(StopValue); break;
            }

            switch (SelectedChamberTabIndex)
            {
                case 0: // -10° chamber
                    {
                        measureSpectra.Chamber = "-10°";
                        List<int> selectedChannels = new List<int>(Channels_10.Where(i => i.IsChecked == true).Select(x => x.Item).ToList());
                        measureSpectra.StartAcquisitions(selectedChannels);
                        break;
                    }
                case 1: // -30° chamber
                    {
                        measureSpectra.Chamber = "-30°";
                        List<int> selectedChannels = new List<int>(Channels_30.Where(i => i.IsChecked == true).Select(x => x.Item).ToList());
                        measureSpectra.StartAcquisitions(selectedChannels);
                        break;
                    }
            }
        }

        private void _CancelCommand()
        {
            DialogResult = false;
            _DialogResult = null;
        }
    }
}

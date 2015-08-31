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

namespace newRBS.ViewModels
{
    public class NewMeasurementViewModel : ViewModelBase
    {
        private Models.MeasureSpectra measureSpectra;
        private Models.DatabaseDataContext Database;

        public ICommand NewSampleCommand { get; set; }

        public ICommand StartMeasurementCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ObservableCollection<CheckedListItem<int>> Channels_10 { get; set; }
        public ObservableCollection<CheckedListItem<int>> Channels_30 { get; set; }

        private Models.Measurement _Measurement;
        public Models.Measurement Measurement
        {
            get { return _Measurement; }
            set { _Measurement = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<Models.Sample> _Samples;
        public ObservableCollection<Models.Sample> Samples
        { get { return _Samples; } set { _Samples = value; RaisePropertyChanged(); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<string> StopTypeList { get; set; }
        public ObservableCollection<Ion> Ions { get; set; }

        private int _SelectedTabIndex = 0;
        public int SelectedTabIndex
        { get { return _SelectedTabIndex; } set { _SelectedTabIndex = value; RaisePropertyChanged(); } }

        public NewMeasurementViewModel()
        {
            measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();
            Database = new Models.DatabaseDataContext(MyGlobals.ConString);

            NewSampleCommand = new RelayCommand(() => _NewSampleCommand(), () => true);

            StartMeasurementCommand = new RelayCommand(() => _StartMeasurementCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Channels_10 = new ObservableCollection<CheckedListItem<int>> { new CheckedListItem<int>(0), new CheckedListItem<int>(1), new CheckedListItem<int>(2), new CheckedListItem<int>(3) };
            Channels_30 = new ObservableCollection<CheckedListItem<int>> { new CheckedListItem<int>(4), new CheckedListItem<int>(5) };

            Channels_10[0].IsChecked = true;
            Channels_30[0].IsChecked = true;

            Orientations = new ObservableCollection<string> { "(undefined)", "random", "aligned" };
            Chambers = new ObservableCollection<string> { "(undefined)", "-10°", "-30°" };
            StopTypeList = new ObservableCollection<string> { "(undefined)", "Manual", "Time", "Counts", "Chopper" };
            Ions = new ObservableCollection<Ion> { new Ion("H", 1, 1), new Ion("He", 2, 4), new Ion("Li", 3, 7) };

            Samples = new ObservableCollection<Models.Sample>(Database.Samples.ToList());

            Measurement = Database.Measurements.OrderByDescending(x => x.StartTime).First();
        }

        private void _NewSampleCommand()
        {
            int? newSampleID = Models.DatabaseUtils.AddNewSample();
            if (newSampleID != null)
            {
                Models.Sample newSample = Database.Samples.FirstOrDefault(x => x.SampleID == newSampleID);
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
            measureSpectra.IncomingIonNumber = Measurement.IncomingIonNumber;
            measureSpectra.IncomingIonEnergy = Measurement.IncomingIonEnergy;
            measureSpectra.IncomingIonAngle = Measurement.IncomingIonAngle;
            measureSpectra.SolidAngle = Measurement.SolidAngle;
            measureSpectra.StopType = Measurement.StopType;
            measureSpectra.StopValue = Measurement.StopValue;

            switch (SelectedTabIndex)
            {
                case 0: // -10° chamber
                    {
                        measureSpectra.Chamber = "-10°";
                        List<int> selectedChannels = new List<int>(Channels_10.Where(i => i.IsChecked == true).Select(x => x.Item).ToList());
                        measureSpectra.StartMeasurements(selectedChannels);
                        break;
                    }
                case 1: // -30° chamber
                    {
                        measureSpectra.Chamber = "-30°";
                        List<int> selectedChannels = new List<int>(Channels_30.Where(i => i.IsChecked == true).Select(x => x.Item).ToList());
                        measureSpectra.StartMeasurements(selectedChannels);
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

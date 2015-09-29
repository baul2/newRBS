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
    /// <summary>
    /// Class that is the view model of <see cref="Views.NewMeasurementView"/>. They set the parameter of a new <see cref="Measurement"/> and start it via <see cref="Models.MeasureSpectra.StartAcquisitions(List{int}, Measurement, int, int)"/>.
    /// </summary>
    public class NewMeasurementViewModel : ViewModelBase
    {
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
        /// <summary>
        /// <see cref="Measurement"/> that stores the parameters of the new Measurement.
        /// </summary>
        public Measurement NewMeasurement
        {
            get { return _Measurement; }
            set { _Measurement = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<Sample> _Samples;
        public ObservableCollection<Sample> Samples
        { get { return _Samples; } set { _Samples = value; RaisePropertyChanged(); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<Isotope> Ions { get; set; }

        public ObservableCollection<string> StopTypes { get; set; }

        private int _SelectedChamberTabIndex = 0;
        public int SelectedChamberTabIndex
        { get { return _SelectedChamberTabIndex; } set { _SelectedChamberTabIndex = value; RaisePropertyChanged(); } }

        private int _SelectedVariablesTabIndex = 0;
        public int SelectedVariablesTabIndex
        { get { return _SelectedVariablesTabIndex; } set { _SelectedVariablesTabIndex = value; RaisePropertyChanged(); } }

        public ObservableCollection<string> VariableParameters { get; set; }

        /// <summary>
        /// Constructor of the class. Sets up commands and initializes variables.
        /// </summary>
        public NewMeasurementViewModel()
        {
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
            StopTypes = new ObservableCollection<string> { "Manual", "Duration (min)", "Charge (µC)", "Counts", "ChopperCounts" };
            Ions = new ObservableCollection<Isotope>(Database.Elements.Where(x => x.AtomicNumber <= 3).SelectMany(y => y.Isotopes).Where(z=>z.MassNumber>0).ToList());

            Samples = new ObservableCollection<Sample>(Database.Samples.ToList());

            NewMeasurement = Database.Measurements.Where(y=>y.MeasurementName!= "TestMeasurement").OrderByDescending(x => x.StartTime).First();

            VariableParameters = new ObservableCollection<string> { "x", "y", "Theta", "Phi", "Energy", "Charge" };
        }

        /// <summary>
        /// Function that inserts a new <see cref="Sample"/>.
        /// </summary>
        public void _NewSampleCommand()
        {
            int? newSampleID = DatabaseUtils.AddNewSample();
            if (newSampleID != null)
            {
                Sample newSample = Database.Samples.FirstOrDefault(x => x.SampleID == newSampleID);
                if (!Samples.Contains(newSample))
                    Samples.Add(newSample);
                NewMeasurement.Sample = newSample;
            }
        }

        /// <summary>
        /// Function that starts the measurement.
        /// </summary>
        public void _StartMeasurementCommand()
        {
            Models.MeasureSpectra measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();

            DialogResult = false;

            int IncomingIonIsotopeID =NewMeasurement.IncomingIonIsotopeID;
            int SampleID = NewMeasurement.SampleID;

            MyGlobals.GenericDetach<Measurement>(NewMeasurement);

            NewMeasurement.IsTestMeasurement = false;

            switch (SelectedChamberTabIndex)
            {
                case 0: // -10° chamber
                    {
                        NewMeasurement.Chamber = "-10°";
                        List<int> selectedChannels = new List<int>(Channels_10.Where(i => i.IsChecked == true).Select(x => x.Item).ToList());
                        measureSpectra.StartAcquisitions(selectedChannels, NewMeasurement, SampleID, IncomingIonIsotopeID);//
                        break;
                    }
                case 1: // -30° chamber
                    {
                        NewMeasurement.Chamber = "-30°";
                        List<int> selectedChannels = new List<int>(Channels_30.Where(i => i.IsChecked == true).Select(x => x.Item).ToList());
                        measureSpectra.StartAcquisitions(selectedChannels, NewMeasurement, SampleID, IncomingIonIsotopeID);
                        break;
                    }
            }
        }

        /// <summary>
        /// Function that cancels the new measurement and closes the window.
        /// </summary>
        public void _CancelCommand()
        {
            DialogResult = false;
            _DialogResult = null;
        }
    }
}

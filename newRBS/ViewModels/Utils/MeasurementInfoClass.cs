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

namespace newRBS.ViewModels.Utils
{
    public class Ion
    {
        public string Name { get; set; }
        public int AtomicNumber { get; set; }
        public int AtomicMass { get; set; }

        public Ion(string name, int atomicNumber, int atomicMass)
        {
            Name = name;
            AtomicNumber = atomicNumber;
            AtomicMass = atomicMass;
        }
    }

    public class MeasurementInfoClass : INotifyPropertyChanged
    {
        private Models.DatabaseDataContext Database;

        public ICommand NewSampleCommand { get; set; }

        private Models.Measurement _Measurement;
        public Models.Measurement Measurement
        {
            get { return _Measurement; }
            set { if (value == null) return; _Measurement = value; OnPropertyChanged("Measurement"); }
        }

        private ObservableCollection<Models.Sample> _Samples;
        public ObservableCollection<Models.Sample> Samples
        { get { return _Samples; } set { _Samples = value; OnPropertyChanged("Samples"); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<string> StopTypeList { get; set; }
        public ObservableCollection<Ion> Ions { get; set; }

        public MeasurementInfoClass(Models.DatabaseDataContext database)
        {
            Database = database;
            Samples = new ObservableCollection<Models.Sample>(Database.Samples.ToList());

            NewSampleCommand = new RelayCommand(() => _NewSampleCommand(), () => true);

            Orientations = new ObservableCollection<string> { "(undefined)", "random", "aligned" };
            Chambers = new ObservableCollection<string> { "(undefined)", "-10°", "-30°" };
            StopTypeList = new ObservableCollection<string> { "(undefined)", "Manual", "Time", "Counts", "Chopper" };
            Ions = new ObservableCollection<Ion> { new Ion("H", 1, 1), new Ion("He", 2, 4), new Ion("Li", 3, 7) };
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}

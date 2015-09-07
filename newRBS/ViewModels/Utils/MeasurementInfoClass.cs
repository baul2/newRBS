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
    public class MeasurementInfoClass : INotifyPropertyChanged
    {
        private DatabaseDataContext Database;

        public ICommand NewSampleCommand { get; set; }

        private Measurement _Measurement;
        public Measurement Measurement
        {
            get { return _Measurement; }
            set { if (value == null) return; _Measurement = value; OnPropertyChanged("Measurement"); }
        }

        private ObservableCollection<Sample> _Samples;
        public ObservableCollection<Sample> Samples
        { get { return _Samples; } set { _Samples = value; OnPropertyChanged("Samples"); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<string> StopTypeList { get; set; }
        public ObservableCollection<ElementClass> Ions { get; set; }

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
            StopTypeList = new ObservableCollection<string> { "(undefined)", "Manual", "Time", "Counts", "Chopper" };
            Ions = new ObservableCollection<ElementClass> { new ElementClass { ShortName = "H", AtomicNumber = 1, AtomicMass = 1 }, new ElementClass { ShortName = "He", AtomicNumber = 2, AtomicMass = 4 }, new ElementClass { ShortName = "Li", AtomicNumber = 3, AtomicMass = 7 } };
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

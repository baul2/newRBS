﻿using System;
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
            set { if (value == null) return; _Measurement = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<Sample> _Samples;
        public ObservableCollection<Sample> Samples
        { get { return _Samples; } set { _Samples = value; RaisePropertyChanged(); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<Isotope> Ions { get; set; }

        public ObservableCollection<string> StopTypes { get; set; }

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
            StopTypes = new ObservableCollection<string> { "Manual", "Duration (min)", "Charge (µC)", "Counts", "ChopperCounts" };
            Ions = new ObservableCollection<Isotope>(Database.Elements.Where(x => x.AtomicNumber <= 3).SelectMany(y => y.Isotopes).Where(z=>z.MassNumber>0).ToList());
        }


        /// <summary>
        /// Function that creates a new <see cref="Sample"/> instance and attaches it the the current <see cref="Measurement"/>.
        /// </summary>
        public void _NewSampleCommand()
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

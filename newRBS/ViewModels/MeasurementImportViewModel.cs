using System;
using System.Linq;
using System.Data.Linq;
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
using newRBS.ViewModels.Utils;
using newRBS.Database;
using System.Diagnostics;
using System.Reflection;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.MeasurementImportView"/>. They import <see cref="Measurement"/>s from either '.xml' or '.dat' files and add them to the database.
    /// </summary>
    public class MeasurementImportViewModel : ViewModelBase
    {
        public ICommand AddCurrentMeasurementCommand { get; set; }
        public ICommand AddAllMeasurementsCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        private DatabaseDataContext Database;

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        /// <summary>
        /// The list of loaded <see cref="Measurement"/>s.
        /// </summary>
        public ObservableCollection<Measurement> newMeausurements { get; set; }

        public MeasurementInfoClass MeasurementInfo { get; set; }

        private Measurement _selectedMeasurement = new Measurement();
        /// <summary>
        /// The currently selected <see cref="Measurement"/>.
        /// </summary>
        public Measurement selectedMeasurement
        {
            get { return _selectedMeasurement; }
            set
            {
                if (value == null) return;
                _selectedMeasurement = value;
                NewSelectedMeasurement();
                RaisePropertyChanged("selectedMeasurement");
            }
        }

        private ObservableCollection<AreaData> _areaData = new ObservableCollection<AreaData>();
        public ObservableCollection<AreaData> areaData
        { get { return _areaData; } set { _areaData = value; RaisePropertyChanged(); } }

        public ObservableCollection<int> UpdatePlot { get; set; }

        /// <summary>
        /// Constructor of the class. Sets up the commands an initializes the variables.
        /// </summary>
        /// <param name="FileName">The name of the file containing the <see cref="Measurement"/>s to import.</param>
        public MeasurementImportViewModel(string FileName)
        {
            AddCurrentMeasurementCommand = new RelayCommand(() => _AddCurrentMeasurementCommand(), () => true);
            AddAllMeasurementsCommand = new RelayCommand(() => _AddAllMeasurementsCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            newMeausurements = new ObservableCollection<Measurement>();

            UpdatePlot = new ObservableCollection<int>();

            Database = MyGlobals.Database;
            //Database.Log = Console.Out;

            MeasurementInfo = new MeasurementInfoClass(Database);

            LoadMeasurements(FileName);
        }

        /// <summary>
        /// Function that loads the <see cref="Measurement"/>s from the given file (via <see cref="DatabaseUtils.LoadMeasurementsFromFile(string)"/>).
        /// </summary>
        /// <param name="FileName">The name of the file containing the <see cref="Measurement"/>s to import.</param>
        public void LoadMeasurements(string FileName)
        {
            List<Measurement> importedMeasurements = DatabaseUtils.LoadMeasurementsFromFile(FileName);

            Sample undefinedSample = Database.Samples.First(x => x.SampleName == "(undefined)");

            newMeausurements.Clear();

            for (int i = 0; i < importedMeasurements.Count(); i++)
            {
                importedMeasurements[i].SampleID = undefinedSample.SampleID;
                newMeausurements.Add(importedMeasurements[i]);
            }

            selectedMeasurement = newMeausurements.First();
        }

        /// <summary>
        /// Function that is executed when the selected <see cref="Measurement"/> changes. It loads the <see cref="Measurement"/> parameters and updates the plot.
        /// </summary>
        public void NewSelectedMeasurement()
        {
            // Updating the grid data
            MeasurementInfo.Measurement = selectedMeasurement;

            // Updating the plot data
            areaData.Clear();
            float[] spectrumX = selectedMeasurement.SpectrumXCal;
            int[] spectrumY = selectedMeasurement.SpectrumY;

            for (int i = 0; i < spectrumY.Count(); i++)
            {
                areaData.Add(new AreaData { x1 = spectrumX[i], y1 = spectrumY[i], x2 = spectrumX[i], y2 = 0 });
            }

            UpdatePlot.Add(1);
        }

        /// <summary>
        /// Function that adds the currently selected <see cref="Measurement"/> to the database.
        /// </summary>
        public void _AddCurrentMeasurementCommand()
        {
            Database.Measurements.InsertOnSubmit(selectedMeasurement);
            Database.SubmitChanges();

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Inserted current imported measurement into the database");

            selectedMeasurement = null;

            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().Init();

            newMeausurements.Remove(selectedMeasurement);

            if (newMeausurements.Count() > 0)
                selectedMeasurement = newMeausurements.First();
            else
            {
                DialogResult = false;
                _DialogResult = null;
            }
        }

        /// <summary>
        /// Function that adds all <see cref="Measurement"/>s to the database and closes the window.
        /// </summary>
        public void _AddAllMeasurementsCommand()
        {
            Database.Measurements.InsertAllOnSubmit(newMeausurements.ToList());
            Database.SubmitChanges();

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Inserted all imported measurement into the database");

            DialogResult = false;
            _DialogResult = null;
        }

        /// <summary>
        /// Function that closes the window.
        /// </summary>
        public void _CancelCommand()
        {
            DialogResult = false;
            _DialogResult = null;
        }
    }
}

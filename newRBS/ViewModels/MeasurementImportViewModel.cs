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
using newRBS.ViewModels.Utils;



namespace newRBS.ViewModels
{
    public class MeasurementImportViewModel : ViewModelBase
    {
        public ICommand AddCurrentMeasurementCommand { get; set; }
        public ICommand AddAllMeasurementsCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private Models.DatabaseDataContext Database;

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ObservableCollection<Models.Measurement> newMeausurements { get; set; }

        public MeasurementInfoClass MeasurementInfo { get; set; }

        private Models.Measurement _selectedMeasurement = new Models.Measurement();
        public Models.Measurement selectedMeasurement
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

        public MeasurementImportViewModel(string FileName)
        {
            AddCurrentMeasurementCommand = new RelayCommand(() => _AddCurrentMeasurementCommand(), () => true);
            AddAllMeasurementsCommand = new RelayCommand(() => _AddAllMeasurementsCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            newMeausurements = new ObservableCollection<Models.Measurement>();

            UpdatePlot = new ObservableCollection<int>();

            Database = new Models.DatabaseDataContext(MyGlobals.ConString);
            //Database.Log = Console.Out;

            MeasurementInfo = new MeasurementInfoClass(Database);

            LoadMeasurements(FileName);
        }

        private void LoadMeasurements(string FileName)
        {
            List<Models.Measurement> importedMeasurements = Models.DatabaseUtils.LoadMeasurementsFromFile(FileName);

            Models.Sample undefinedSample = Database.Samples.First(x => x.SampleName == "(undefined)");

            newMeausurements.Clear();

            for (int i = 0; i < importedMeasurements.Count(); i++)
            {
                importedMeasurements[i].SampleID = undefinedSample.SampleID;
                newMeausurements.Add(importedMeasurements[i]);
            }

            selectedMeasurement = newMeausurements.First();
        }

        private void NewSelectedMeasurement()
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


        public void _AddCurrentMeasurementCommand()
        {
            Console.WriteLine("_AddCurrentMeasurementCommand");

            Database.Measurements.InsertOnSubmit(selectedMeasurement);

            Database.SubmitChanges();



            selectedMeasurement = null;

            newMeausurements.Remove(selectedMeasurement);
            Console.WriteLine(newMeausurements.Count());
            if (newMeausurements.Count() > 0)
                selectedMeasurement = newMeausurements.First();
            else
            {
                DialogResult = false;
                _DialogResult = null;
            }
        }

        public void _AddAllMeasurementsCommand()
        {
            Console.WriteLine("_AddAllMeasurementsCommand");
            Database.Measurements.InsertAllOnSubmit(newMeausurements.ToList());
            Database.SubmitChanges();

            DialogResult = false;
            _DialogResult = null;
        }

        public void _CancelCommand()
        {
            Console.WriteLine("_CancelCommand");
            DialogResult = false;
            _DialogResult = null;
        }
    }
}

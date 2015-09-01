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
        public ICommand OpenFileCommand { get; set; }
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

        private string _SelectedPath;
        public string SelectedPath
        { get { return _SelectedPath; } set { _SelectedPath = value; RaisePropertyChanged("SelectedPath"); } }

        private ObservableCollection<AreaData> _areaData = new ObservableCollection<AreaData>();
        public ObservableCollection<AreaData> areaData
        { get { return _areaData; } set { _areaData = value; RaisePropertyChanged(); } }

        private string _FileContent;
        public string FileContent
        { get { return _FileContent; } set { _FileContent = value; RaisePropertyChanged("FileContent"); } }

        public MeasurementImportViewModel()
        {
            OpenFileCommand = new RelayCommand(() => _OpenFileCommand(), () => true);
            AddCurrentMeasurementCommand = new RelayCommand(() => _AddCurrentMeasurementCommand(), () => true);
            AddAllMeasurementsCommand = new RelayCommand(() => _AddAllMeasurementsCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            newMeausurements = new ObservableCollection<Models.Measurement>();

            Database = new Models.DatabaseDataContext(MyGlobals.ConString);
            //Database.Log = Console.Out;

            MeasurementInfo = new MeasurementInfoClass(Database);
        }

        private void NewSelectedMeasurement()
        {
            // Updating the grid data
            MeasurementInfo.Measurement = selectedMeasurement;

            // Updating the plot data
            areaData.Clear();
            float[] spectrumX = Models.DatabaseUtils.GetCalibratedSpectrumX(selectedMeasurement);
            int[] spectrumY = Models.DatabaseUtils.GetIntSpectrumY(selectedMeasurement);
            for (int i = 0; i < spectrumY.Count(); i++)
                areaData.Add(new AreaData { x1 = spectrumX[i], y1 = spectrumY[i], x2 = spectrumX[i], y2 = 0 });
        }

        public void _OpenFileCommand()
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();

            if ((SelectedPath = dialog.FileName) == null) return;
            if (!File.Exists(dialog.FileName)) return;

            List<Models.Measurement> importedMeasurements = Models.DatabaseUtils.LoadMeasurementsFromFile(SelectedPath);

            Models.Sample undefinedSample = Database.Samples.First(x => x.SampleName == "(undefined)");

            newMeausurements.Clear();

            for (int i = 0; i < importedMeasurements.Count(); i++)
            {
                importedMeasurements[i].SampleID = undefinedSample.SampleID;
                newMeausurements.Add(importedMeasurements[i]);
            }

            selectedMeasurement = newMeausurements.First();
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

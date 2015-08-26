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



namespace newRBS.ViewModels
{
    public class AreaData
    {
        public double x1 { get; set; }
        public double y1 { get; set; }
        public double x2 { get; set; }
        public double y2 { get; set; }
    }

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

    public class ImportMeasurementsViewModel : ViewModelBase
    {
        public ICommand OpenFileCommand { get; set; }
        public ICommand AddCurrentMeasurementCommand { get; set; }
        public ICommand AddAllMeasurementsCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private Models.DataSpectra dataSpectra;

        //private Models.Sample noneSample;
        private Models.DatabaseDataContext Database;

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ObservableCollection<Models.Measurement> newMeausurements { get; set; }

        private Models.Measurement _selectedMeasurement = new Models.Measurement();
        public Models.Measurement selectedMeasurement
        {
            get { return _selectedMeasurement; }
            set
            {
                if (value == null) return;
                Console.WriteLine("selectedMeasurement");
                if (_selectedMeasurement != null)
                {
                    // Unsubscribe to events from old selected Measurement
                    _selectedMeasurement.NewSampleToAdd -= new PropertyChangedEventHandler(AddNewSample);
                }

                _selectedMeasurement = value;

                // Hooking up to events from new selected Measurement
                _selectedMeasurement.NewSampleToAdd += new PropertyChangedEventHandler(AddNewSample);

                // Preparing the plot data
                areaData.Clear();
                int[] temp = ArrayConversion.ByteToInt(value.SpectrumY.ToArray());
                for (int i = 0; i < temp.Count(); i++)
                    areaData.Add(new AreaData { x1 = i, y1 = temp[i], x2 = i, y2 = 0 });

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

        private ObservableCollection<Models.Sample> _SampleList;
        public ObservableCollection<Models.Sample> SampleList
        { get { return _SampleList; } set { _SampleList = value; RaisePropertyChanged("SampleList"); } }

        public ObservableCollection<string> Orientations { get; set; }
        public ObservableCollection<string> Chambers { get; set; }
        public ObservableCollection<string> StopTypeList { get; set; }
        public ObservableCollection<Ion> Ions { get; set; }

        public ImportMeasurementsViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            OpenFileCommand = new RelayCommand(() => _OpenFileCommand(), () => true);
            AddCurrentMeasurementCommand = new RelayCommand(() => _AddCurrentMeasurementCommand(), () => true);
            AddAllMeasurementsCommand = new RelayCommand(() => _AddAllMeasurementsCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            newMeausurements = new ObservableCollection<Models.Measurement>();

            Database = new Models.DatabaseDataContext("Data Source = SVRH; User ID = p4mist; Password = testtesttesttest");
            Database.Log = Console.Out;

            SampleList = new ObservableCollection<Models.Sample>((from sample in Database.Samples select sample).ToList());

            Orientations = new ObservableCollection<string> { "(undefined)", "random", "aligned" };
            Chambers = new ObservableCollection<string> { "(undefined)", "-10°", "-30°" };
            StopTypeList = new ObservableCollection<string> { "(undefined)", "Manual", "Time", "Counts", "Chopper" };
            Ions = new ObservableCollection<Ion> { new Ion("H", 1, 1), new Ion("He", 2, 4), new Ion("Li", 3, 7) };
        }

        private void AddNewSample(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName != "Sample") return;
            //var temp = (Models.Sample)sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
            //if (temp.SampleName != "New...") return;

            Console.WriteLine("AddNewSample");

            ViewUtils.InputDialog inputDialog = new ViewUtils.InputDialog("Enter new sample name:", "");
            if (inputDialog.ShowDialog() == true)
            {
                Console.WriteLine(inputDialog.Answer);
                if (inputDialog.Answer == "")
                    return;

                if (SampleList.FirstOrDefault(x => x.SampleName == inputDialog.Answer) != null)
                {
                    Console.WriteLine("Sample already exists!");

                    MessageBoxResult result = MessageBox.Show("Sample already exists in database!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                    selectedMeasurement.Sample = SampleList.First(x => x.SampleName == inputDialog.Answer);
                    Console.WriteLine(selectedMeasurement.Sample.SampleName);
                    return;
                }

                // New sample
                Console.WriteLine("new sample");

                Models.Sample newSample = new Models.Sample();
                newSample.SampleName = inputDialog.Answer;

                Database.Samples.InsertOnSubmit(newSample);
                Database.SubmitChanges();

                SampleList.Add(newSample);
                selectedMeasurement.SampleID = newSample.SampleID;
            }
        }

        public void _OpenFileCommand()
        {
            var dialog = new OpenFileDialog();
            dialog.ShowDialog();

            if ((SelectedPath = dialog.FileName) == null) return;

            List<Models.Measurement> importedMeasurements = dataSpectra.LoadMeasurementsFromFile(SelectedPath);

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

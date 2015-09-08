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
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using newRBS.Database;

namespace newRBS.ViewModels
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public ICommand NewMeasurementCommand { get; set; }
        public ICommand StopMeasurementCommand { get; set; }

        public ICommand ChannelConfigurationCommand { get; set; }

        public ICommand ImportMeasurementsCommand { get; set; }
        public ICommand ExportMeasurementsCommand { get; set; }
        public ICommand DeleteMeasurementsCommand { get; set; }

        public ICommand SaveMeasurementPlotCommand { get; set; }

        public ICommand MaterialEditorCommand { get; set; }
        public ICommand SampleEditorCommand { get; set; }

        public ICommand EnergyCalCommand { get; set; }
        public ICommand SimulateSpectrumCommand { get; set; }
        public ICommand UserEditorCommand { get; set; }
        
        public ICommand LogOutCommand { get; set; }

        TraceSource trace = new TraceSource("MainViewModel");

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            NewMeasurementCommand = new RelayCommand(() => _NewMeasurementCommand(), () => true);
            StopMeasurementCommand = new RelayCommand(() => _StopMeasurementCommand(), () => true);

            ChannelConfigurationCommand = new RelayCommand(() => _ChannelConfigurationCommand(), () => true);

            ImportMeasurementsCommand = new RelayCommand(() => _ImportMeasurementsCommand(), () => true);
            ExportMeasurementsCommand = new RelayCommand(() => _ExportMeasurementsCommand(), () => true);
            DeleteMeasurementsCommand = new RelayCommand(() => _DeleteMeasurementsCommand(), () => true);

            SaveMeasurementPlotCommand = new RelayCommand(() => _SaveMeasurementPlotCommand(), () => true);

            MaterialEditorCommand = new RelayCommand(() => _MaterialEditorCommand(), () => true);
            SampleEditorCommand = new RelayCommand(() => _SampleEditorCommand(), () => true);
            UserEditorCommand = new RelayCommand(() => _UserEditorCommand(), () => true); 

            EnergyCalCommand = new RelayCommand(() => _EnergyCalCommand(), () => true);
            SimulateSpectrumCommand = new RelayCommand(() => _SimulateSpectrumCommand(), () => true);

            LogOutCommand = new RelayCommand(() => _LogOutCommand(), () => true); 
        }

        public void _NewMeasurementCommand()
        {
            NewMeasurementViewModel newMeasurementViewModel = new NewMeasurementViewModel();
            Views.NewMeasurementView newMeasurementView = new Views.NewMeasurementView();
            newMeasurementView.DataContext = newMeasurementViewModel;
            newMeasurementView.ShowDialog();
        }

        public void _StopMeasurementCommand()
        {
            Models.MeasureSpectra measureSpectra;
            measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();
            measureSpectra.StopAcquisitions();
        }

        public void _ImportMeasurementsCommand()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "newRBS file (*.xml)|*.xml|Spektrenverwaltung file (*.dat)|*.dat";
            if (openFileDialog.ShowDialog() == true)
            {
                if (openFileDialog.FileName == null) return;
                if (!File.Exists(openFileDialog.FileName)) return;

                MeasurementImportViewModel importMeasurementsViewModel = new MeasurementImportViewModel(openFileDialog.FileName);
                Views.MeasurementImportView importMeasurementsView = new Views.MeasurementImportView();
                importMeasurementsView.DataContext = importMeasurementsViewModel;
                importMeasurementsView.ShowDialog();
            }
        }

        public void _ExportMeasurementsCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();
            if (selectedMeasurementIDs.Count() == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "newRBS file (*.xml)|*.xml|Spektrenverwaltung file (*.dat)|*.dat";
            if (saveFileDialog.ShowDialog() == true)
                DatabaseUtils.ExportMeasurements(selectedMeasurementIDs, saveFileDialog.FileName);
        }

        public void _DeleteMeasurementsCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();
            if (selectedMeasurementIDs.Count() == 0) return;

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected measurements?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
                DatabaseUtils.DeleteMeasurements(selectedMeasurementIDs);
        }

        public void _SaveMeasurementPlotCommand()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Bitmap file (*.png)|*.png|Vector file (*.pdf)|*.pdf|Vector file (*.svg)|*.svg|Data file (*.dat)|*.dat";
            if (saveFileDialog.ShowDialog() == true)
                DatabaseUtils.SaveMeasurementImage(saveFileDialog.FileName);
        }

        public void _ChannelConfigurationCommand()
        {
            Models.MeasureSpectra measureSpectra;
            measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();

            if (measureSpectra.IsAcquiring() == true)
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't start channel configuration: Board is acquiring"); MessageBox.Show("Can't start channel configuration: Board is acquiring"); return; }

            ChannelConfigurationViewModel channelConfigurationViewModel = new ChannelConfigurationViewModel();
            Views.ChannelConfigurationView channelConfiguration = new Views.ChannelConfigurationView();
            channelConfiguration.DataContext = channelConfigurationViewModel;
            channelConfiguration.ShowDialog();
        }

        public void _MaterialEditorCommand()
        {
            MaterialEditorViewModel materialEditorViewModel = new MaterialEditorViewModel();
            Views.MaterialEditorView materialEditorView = new Views.MaterialEditorView();
            materialEditorView.DataContext = materialEditorViewModel;
            materialEditorView.ShowDialog();
        }

        public void _SampleEditorCommand()
        {
            SampleEditorViewModel sampleEditorViewModel = new SampleEditorViewModel();
            Views.SampleEditorView materialEditorView = new Views.SampleEditorView();
            materialEditorView.DataContext = sampleEditorViewModel;
            materialEditorView.ShowDialog();
        }

        public void _UserEditorCommand()
        {
            Views.Utils.LogInDialog logInDialog = new Views.Utils.LogInDialog("Please enter the admin login data and the connection settings!");

            if (logInDialog.ShowDialog() == true)
            {
                string ConString = "Data Source = " + logInDialog.logIn.IPAdress + "," + logInDialog.logIn.Port + "; Network Library=DBMSSOCN; User ID = " + logInDialog.logIn.UserName + "; Password = " + logInDialog.logIn.Password + "; Initial Catalog = " + logInDialog.logIn.UserName + "_db";
                //var newConnection = new DatabaseDataContext(ConString);
                //newConnection.CommandTimeout = 10;

                //if (newConnection.DatabaseExists())
                {
                    UserEditorViewModel userEditorViewModel = new UserEditorViewModel(logInDialog.logIn);
                    Views.UserEditorView userEditorView = new Views.UserEditorView();
                    userEditorView.DataContext = userEditorViewModel;
                    userEditorView.ShowDialog();
                }
                //else
                    Console.WriteLine("Connection problem");
            }
        }

        public void _SimulateSpectrumCommand()
        {
            SimulateSpectrumViewModel simulateSpectrumViewModel = new SimulateSpectrumViewModel();
            Views.SimulateSpectrumView simulateSpectrumView = new Views.SimulateSpectrumView();
            simulateSpectrumView.DataContext = simulateSpectrumViewModel;
            simulateSpectrumView.ShowDialog();
        }

        public void _EnergyCalCommand()
        {
            EnergyCalibrationViewModel energyCalibrationViewModel = new EnergyCalibrationViewModel();
            if (energyCalibrationViewModel.ValidSelectedMeasurements == false) return;
            Views.EnergyCalibrationView energyCalibrationView = new Views.EnergyCalibrationView();
            energyCalibrationView.DataContext = energyCalibrationViewModel;
            energyCalibrationView.ShowDialog();
        }

        public void _LogOutCommand()
        {
            MyGlobals.ConString = "";
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().filterTree.Items.Clear();
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().Projects.Clear();
            SimpleIoc.Default.GetInstance<MeasurementPlotViewModel>().ClearPlot(new List<int>());
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().ChangeFilter(new List<int>());

            DatabaseDataContext temp = MyGlobals.Database;

            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().Init();
            //var adventurerWindowVM = SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>(System.Guid.NewGuid().ToString());
        }
    }
}
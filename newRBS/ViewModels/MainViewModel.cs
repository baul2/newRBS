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
using System.Reflection;
using OxyPlot;
using System.Globalization;
using newRBS.Models;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.MainView"/>. They provide the main window and contain the commands to start other program parts.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        public ICommand NewMeasurementCommand { get; set; }
        public ICommand NewTestMeasurementCommand { get; set; }
        public ICommand StopMeasurementCommand { get; set; }

        public ICommand ChannelConfigurationCommand { get; set; }

        public ICommand ImportMeasurementsCommand { get; set; }
        public ICommand ExportMeasurementsCommand { get; set; }
        public ICommand DeleteMeasurementsCommand { get; set; }

        public ICommand SaveMeasurementPlotCommand { get; set; }

        public ICommand EnergyCalCommand { get; set; }
        public ICommand SimulateSpectrumCommand { get; set; }
        public ICommand CalculateCommand { get; set; }

        public ICommand MaterialEditorCommand { get; set; }
        public ICommand SampleEditorCommand { get; set; }
        public ICommand UserEditorCommand { get; set; }


        public ICommand LogOutCommand { get; set; }
        public RelayCommand<CancelEventArgs> OnClosingCommand { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        /// <summary>
        /// Constructor of the class. It sets up all the commands.
        /// </summary>
        public MainViewModel()
        {
            trace.Value.TraceEvent(TraceEventType.Information, 0, "Program started");

            NewMeasurementCommand = new RelayCommand(() => _NewMeasurementCommand(), () => true);
            NewTestMeasurementCommand = new RelayCommand(() => _NewTestMeasurementCommand(), () => true);
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
            CalculateCommand = new RelayCommand(() => _CalculateCommand(), () => true);

            LogOutCommand = new RelayCommand(() => _LogOutCommand(), () => true);
            OnClosingCommand = new RelayCommand<CancelEventArgs>(_CloseProgramCommand);

            // Check if the measurement equipment is accessible
            if (CAEN_x730.Init()== true)
            {
                MyGlobals.CanMeasure = true;
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Program is in measurement mode");
                MeasureSpectra.Init();
            }
            else
            {
                MyGlobals.CanMeasure = false;
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Program is in offline mode");
            }
        
            MyGlobals.myController = new PlotController();
            MyGlobals.myController.BindMouseDown(OxyMouseButton.Left, PlotCommands.ZoomRectangle);
            MyGlobals.myController.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.None, 2, PlotCommands.ResetAt);
            MyGlobals.myController.BindMouseDown(OxyMouseButton.Middle, PlotCommands.PointsOnlyTrack);
        }

        /// <summary>
        /// Function that starts a new <see cref="NewMeasurementViewModel"/> instance and binds it to a <see cref="Views.NewMeasurementView"/> instance.
        /// </summary>
        public void _NewMeasurementCommand()
        {
            NewMeasurementViewModel newMeasurementViewModel = new NewMeasurementViewModel();
            Views.NewMeasurementView newMeasurementView = new Views.NewMeasurementView();
            newMeasurementView.DataContext = newMeasurementViewModel;
            newMeasurementView.ShowDialog();
        }

        /// <summary>
        /// Function that starts a new test measurement.
        /// </summary>
        public void _NewTestMeasurementCommand()
        {
            Views.Utils.ChannelDialog channelDialog = new Views.Utils.ChannelDialog();
            if (channelDialog.ShowDialog() == true)
            {
                Measurement measurement = new Measurement();
                measurement.IsTestMeasurement = true;
                measurement.MeasurementName = "TestMeasurement";
                int SampleID, IncomingIonIsotopeID;
                using (DatabaseDataContext Database = MyGlobals.Database)
                {
                    SampleID = Database.Samples.FirstOrDefault(x => x.SampleName == "(undefined)").SampleID;
                    IncomingIonIsotopeID = Database.Isotopes.FirstOrDefault(x => x.MassNumber == 1).IsotopeID;
                }
                MeasureSpectra.StartAcquisitions(new List<int> { channelDialog.SelectedChannel }, measurement, SampleID, IncomingIonIsotopeID);
            }
        }

        /// <summary>
        /// Function that stops the current acquisition.
        /// </summary>
        public void _StopMeasurementCommand()
        {
            MeasureSpectra.StopAcquisitions();
        }

        /// <summary>
        /// Function that gets a filename and starts a new <see cref="MeasurementImportViewModel"/> instance and binds it to a <see cref="Views.MeasurementImportView"/> instance.
        /// </summary>
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

        /// <summary>
        /// Function that gets a filename and sends the selected measurements to <see cref="DatabaseUtils.ExportMeasurements(List{int}, string)"/> in order to export them.
        /// </summary>
        public void _ExportMeasurementsCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();
            if (selectedMeasurementIDs.Count() == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "newRBS file (*.xml)|*.xml|Spektrenverwaltung file (*.dat)|*.dat";
            if (saveFileDialog.ShowDialog() == true)
                DatabaseUtils.ExportMeasurements(selectedMeasurementIDs, saveFileDialog.FileName);
        }

        /// <summary>
        /// Function that sends the selected measurements to <see cref="DatabaseUtils.DeleteMeasurements(List{int})"/> in order to delete them.
        /// </summary>
        public void _DeleteMeasurementsCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();
            if (selectedMeasurementIDs.Count() == 0) return;

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected measurements?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
                DatabaseUtils.DeleteMeasurements(selectedMeasurementIDs);
        }

        /// <summary>
        /// Function that gets a filename and calls <see cref="DatabaseUtils.SaveMeasurementImage(string)"/> in order to save the measurement plot.
        /// </summary>
        public void _SaveMeasurementPlotCommand()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Bitmap file (*.png)|*.png|Vector file (*.pdf)|*.pdf|Vector file (*.svg)|*.svg|Data file (*.dat)|*.dat";
            if (saveFileDialog.ShowDialog() == true)
                DatabaseUtils.SaveMeasurementImage(saveFileDialog.FileName);
        }

        /// <summary>
        /// Function that starts a new <see cref="ChannelConfigurationViewModel"/> instance and binds it to a <see cref="Views.ChannelConfigurationView"/> instance.
        /// </summary>
        public void _ChannelConfigurationCommand()
        {
            if (MeasureSpectra.IsAcquiring() == true)
            { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't start channel configuration: Board is acquiring"); MessageBox.Show("Can't start channel configuration: Board is acquiring"); return; }

            ChannelConfigurationViewModel channelConfigurationViewModel = new ChannelConfigurationViewModel();
            Views.ChannelConfigurationView channelConfiguration = new Views.ChannelConfigurationView();
            channelConfiguration.DataContext = channelConfigurationViewModel;
            channelConfiguration.ShowDialog();
        }

        /// <summary>
        /// Function that starts a new <see cref="MaterialEditorViewModel"/> instance and binds it to a <see cref="Views.MaterialEditorView"/> instance.
        /// </summary>
        public void _MaterialEditorCommand()
        {
            MaterialEditorViewModel materialEditorViewModel = new MaterialEditorViewModel();
            Views.MaterialEditorView materialEditorView = new Views.MaterialEditorView();
            materialEditorView.DataContext = materialEditorViewModel;
            materialEditorView.ShowDialog();
        }

        /// <summary>
        /// Function that starts a new <see cref="SampleEditorViewModel"/> instance and binds it to a <see cref="Views.SampleEditorView"/> instance.
        /// </summary>
        public void _SampleEditorCommand()
        {
            SampleEditorViewModel sampleEditorViewModel = new SampleEditorViewModel();
            Views.SampleEditorView materialEditorView = new Views.SampleEditorView();
            materialEditorView.DataContext = sampleEditorViewModel;
            materialEditorView.ShowDialog();
        }

        /// <summary>
        /// Function that gets an database admin login and starts a new <see cref="UserEditorViewModel"/> instance and binds it to a <see cref="Views.UserEditorView"/> instance.
        /// </summary>
        public void _UserEditorCommand()
        {
            Views.Utils.LogInDialog logInDialog = new Views.Utils.LogInDialog("Please enter the admin login data and the connection settings!");

            if (logInDialog.ShowDialog() == true)
            {
                string ConString = "Data Source = " + logInDialog.logIn.IPAdress + "," + logInDialog.logIn.Port + "; Network Library=DBMSSOCN; User ID = " + logInDialog.logIn.UserName + "; Password = " + logInDialog.logIn.Password + "; Initial Catalog = " + logInDialog.logIn.UserName + "_db";
                var newConnection = new DatabaseDataContext(ConString);
                newConnection.CommandTimeout = 10;

                if (newConnection.DatabaseExists())
                {
                    UserEditorViewModel userEditorViewModel = new UserEditorViewModel(logInDialog.logIn);
                    Views.UserEditorView userEditorView = new Views.UserEditorView();
                    userEditorView.DataContext = userEditorViewModel;
                    userEditorView.ShowDialog();
                }
                else
                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Database connection problem");
            }
        }

        /// <summary>
        /// Function that starts a new <see cref="SimulateSpectrumViewModel"/> instance and binds it to a <see cref="Views.SimulateSpectrumView"/> instance.
        /// </summary>
        public void _SimulateSpectrumCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();
            if (selectedMeasurementIDs.Count() != 1) { MessageBox.Show("Select exactly one measurement", "Error"); return; }

            SimulateSpectrumViewModel simulateSpectrumViewModel = new SimulateSpectrumViewModel(selectedMeasurementIDs.FirstOrDefault());
            Views.SimulateSpectrumView simulateSpectrumView = new Views.SimulateSpectrumView();
            simulateSpectrumView.DataContext = simulateSpectrumViewModel;
            simulateSpectrumView.ShowDialog();
        }

        /// <summary>
        /// Function that starts a new <see cref="CalculateViewModel"/> instance and binds it to a <see cref="Views.CalculateView"/> instance.
        /// </summary>
        public void _CalculateCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();
            if (selectedMeasurementIDs.Count() != 2) { MessageBox.Show("Select exactly two measurements", "Error"); return; }

            CalculateViewModel calculateViewModel = new CalculateViewModel(selectedMeasurementIDs);
            Views.CalculateView calculateView = new Views.CalculateView();
            calculateView.DataContext = calculateViewModel;
            calculateView.ShowDialog();
        }

        /// <summary>
        /// Function that starts a new <see cref="EnergyCalibrationViewModel"/> instance and binds it to a <see cref="Views.EnergyCalibrationView"/> instance.
        /// </summary>
        public void _EnergyCalCommand()
        {
            var selectedMeasurements = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(x => x.Measurement).ToList();

            if (selectedMeasurements.Count() == 0)
            { MessageBox.Show("Select at least one measurement!"); return; }

            if (selectedMeasurements.Select(x => x.Channel).Distinct().ToList().Count > 1)
            { MessageBox.Show("Select only measurements from the same channel!"); return; }

            if (selectedMeasurements.Select(x => x.IncomingIonEnergy).Distinct().ToList().Count > 1 || selectedMeasurements.Select(x => x.IncomingIonIsotopeID).Distinct().ToList().Count > 1)
            { MessageBox.Show("Select only measurements with identical irradiation parameters!"); return; }

            EnergyCalibrationViewModel energyCalibrationViewModel = new EnergyCalibrationViewModel(selectedMeasurements.Select(x => x.MeasurementID).ToList());
            Views.EnergyCalibrationView energyCalibrationView = new Views.EnergyCalibrationView();
            energyCalibrationView.DataContext = energyCalibrationViewModel;
            energyCalibrationView.ShowDialog();
        }

        /// <summary>
        /// Function that logs out the current user and resets everything.
        /// </summary>
        public void _LogOutCommand()
        {
            if (MeasureSpectra.IsAcquiring() == true)
            { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't log out user: Board is acquiring"); MessageBox.Show("Can't log out user: Board is acquiring"); return; }

            string OldUserName = new string(MyGlobals.ConString.Split(';').FirstOrDefault(x => x.Contains("User ID = ")).Skip(11).ToArray());

            MyGlobals.ConString = "";

            trace.Value.TraceEvent(TraceEventType.Information, 0, "User '" + OldUserName + "' logged out");

            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().filterTree.Items.Clear();
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().Projects.Clear();
            SimpleIoc.Default.GetInstance<MeasurementPlotViewModel>().ClearPlot(new List<int>());
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().ChangeFilter(new List<int>());

            DatabaseDataContext temp = MyGlobals.Database;

            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().Init();
        }

        /// <summary>
        /// Function that closes the board and exits the program.
        /// </summary>
        /// <param name="cancelEventArgs">Argument that allows to cancel the closing of the window.</param>
        public void _CloseProgramCommand(CancelEventArgs cancelEventArgs)
        {
            if (MyGlobals.CanMeasure == true)
            {
                if (MeasureSpectra.IsAcquiring() == true)
                {
                    trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't close the program: Board is acquiring");
                    MessageBox.Show("Can't close the program: Board is acquiring");

                    if (cancelEventArgs != null)
                        cancelEventArgs.Cancel = true;

                    return;
                }

                if (MessageBox.Show("Save channel configurations to disk?", "Save channel configurations", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    MeasureSpectra.SaveChopperConfig();

                if (CAEN_x730.IsInit == true)
                {
                    CAEN_x730.Close();
                }

                if (Coulombo.IsInit == true)
                {
                    Coulombo.Close();
                }
            }

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Program closed");

            Environment.Exit(0);
        }
    }
}
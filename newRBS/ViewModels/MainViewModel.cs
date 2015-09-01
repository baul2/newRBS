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

        public ICommand MaterialEditorCommand { get; set; }
        public ICommand SampleEditorCommand { get; set; }

        public ICommand EnergyCalCommand { get; set; }
        public ICommand SimulateSpectrumCommand { get; set; }

        TraceSource trace = new TraceSource("MainViewModel");

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            NewMeasurementCommand = new RelayCommand(() => _NewMeasurementCommand(), () => true);
            StopMeasurementCommand = new RelayCommand(() => _StopMeasurementCommand(), () => true);

            ChannelConfigurationCommand = new RelayCommand(() => _ChannelConfigurationCommand(), () => true);

            ImportMeasurementsCommand = new RelayCommand(() => _ImportMeasurementCommand(), () => true);
            ExportMeasurementsCommand = new RelayCommand(() => _ExportMeasurementsCommand(), () => true);
            DeleteMeasurementsCommand = new RelayCommand(() => _DeleteMeasurementsCommand(), () => true);

            MaterialEditorCommand = new RelayCommand(() => _MaterialEditorCommand(), () => true);
            SampleEditorCommand = new RelayCommand(() => _SampleEditorCommand(), () => true);

            EnergyCalCommand = new RelayCommand(() => _EnergyCalCommand(), () => true); 
            SimulateSpectrumCommand = new RelayCommand(() => _SimulateSpectrumCommand(), () => true);
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
            measureSpectra.StopMeasurements();
        }

        public void _ImportMeasurementCommand()
        {
            MeasurementImportViewModel importMeasurementsViewModel = new MeasurementImportViewModel();
            Views.MeasurementImportView importMeasurementsView = new Views.MeasurementImportView();
            importMeasurementsView.DataContext = importMeasurementsViewModel;
            importMeasurementsView.ShowDialog();
        }

        public void _ExportMeasurementsCommand()
        {
            Console.WriteLine("_ExportMeasurementsCommand not implemented yet");
        }

        public void _DeleteMeasurementsCommand()
        {
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().DeleteSelectedMeasurement();
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
    }
}
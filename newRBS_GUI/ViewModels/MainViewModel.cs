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
using newRBS.ViewModelUtils;

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
        public ICommand ImportMeasurementCommand { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            NewMeasurementCommand = new RelayCommand(() => _NewMeasurementCommand(), () => true);
            StopMeasurementCommand = new RelayCommand(() => _StopMeasurementCommand(), () => true);
            ImportMeasurementCommand = new RelayCommand(() => _ImportMeasurementCommand(), () => true);
        }

        public void _NewMeasurementCommand()
        {
            Console.WriteLine("_NewMeasurementCommand");

            Views.NewMeasurementView newMeasurementView = new Views.NewMeasurementView();
            
            newMeasurementView.ShowDialog();
        }

        public void _StopMeasurementCommand()
        {
            Console.WriteLine("_StopMeasurementCommand");

            Models.MeasureSpectra measureSpectra;
            measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();
            measureSpectra.StopMeasurements();
        }

        public void _ImportMeasurementCommand()
        {
            Console.WriteLine("_ImportMeasurementCommand");

            Views.ImportSpectra importSpectra = new Views.ImportSpectra();

            importSpectra.ShowDialog();
        }
    }
}
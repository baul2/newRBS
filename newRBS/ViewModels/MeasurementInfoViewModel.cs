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
using newRBS.ViewModels.Utils;
using newRBS.Database;
using System.Data.Linq;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.MeasurementInfoView"/>. They show and modify the properties of the selected <see cref="Measurement"/>.
    /// </summary>
    public class MeasurementInfoViewModel : ViewModelBase
    {
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private DatabaseDataContext Database;

        /// <summary>
        /// Instance of the <see cref="MeasurementInfoClass"/>, containing all information of the <see cref="Measurement"/>.
        /// </summary>
        public MeasurementInfoClass MeasurementInfo { get; set; }

        /// <summary>
        /// Constructor of the class. Sets up the commands and initiates <see cref="MeasurementInfo"/>.
        /// </summary>
        /// <param name="MeasurementID"></param>
        public MeasurementInfoViewModel(int MeasurementID)
        {
            SaveCommand = new RelayCommand(() => _SaveCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Database = MyGlobals.Database;
            MeasurementInfo = new MeasurementInfoClass(Database);
            MeasurementInfo.Measurement = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);
        }

        /// <summary>
        /// Function that saves the changes to the database and closes the window.
        /// </summary>
        public void _SaveCommand()
        {
            Database.SubmitChanges(ConflictMode.ContinueOnConflict);

            DialogResult = true;
        }

        /// <summary>
        /// Function that closes the window.
        /// </summary>
        public void _CancelCommand()
        {
            DialogResult = true;
        }
    }
}

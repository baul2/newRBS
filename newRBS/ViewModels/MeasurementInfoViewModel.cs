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

namespace newRBS.ViewModels
{
    public class MeasurementInfoViewModel : ViewModelBase
    {
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private DatabaseDataContext Database;

        public MeasurementInfoClass MeasurementInfo { get; set; }

        public MeasurementInfoViewModel(int MeasurementID)
        {
            SaveCommand = new RelayCommand(() => _SaveCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Database = MyGlobals.Database;
            MeasurementInfo = new MeasurementInfoClass(Database);
            MeasurementInfo.Measurement = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);
        }

        public void _SaveCommand()
        {
            Database.SubmitChanges();
            DialogResult = true;
        }

        public void _CancelCommand()
        {
            DialogResult = true;
        }
    }
}

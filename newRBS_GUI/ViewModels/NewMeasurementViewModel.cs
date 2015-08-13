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
    public class NewMeasurementViewModel : ViewModelBase
    {
        private Models.DataSpectra dataSpectra;
        private Models.MeasureSpectra measureSpectra;

        public ICommand StartMeasurementCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public ObservableCollection<CheckedListItem<int>> Channels { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public NewMeasurementViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();
            measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();

            StartMeasurementCommand = new RelayCommand(() => _StartMeasurementCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Channels = new ObservableCollection<CheckedListItem<int>>();

            Channels.Add(new CheckedListItem<int>(0));
            Channels.Add(new CheckedListItem<int>(1));
            Channels.Add(new CheckedListItem<int>(2));
            Channels.Add(new CheckedListItem<int>(3));

            Channels[0].IsChecked = true;
        }

        private void _StartMeasurementCommand()
        {
            List<int> selectedChannels = new List<int>();
            List<CheckedListItem<int>> c = Channels.Where(i => i.IsChecked == true).ToList();
            for (int i = 0; i < c.Count; i++)
                selectedChannels.Add(c[i].Item);

            List<int> newIDs = measureSpectra.StartMeasurements(selectedChannels);

            DialogResult = false;
            _DialogResult = null;

            //spectrumList = dataSpectra.GetObservableCollection();
        }

        private void _CancelCommand()
        {
            DialogResult = false;
            _DialogResult = null;
        }
    }
}

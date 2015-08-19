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
using Microsoft.Win32;

namespace newRBS.ViewModels
{
    public class MyMeasurement : INotifyPropertyChanged
    {
        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                if (_Selected != value)
                {
                    _Selected = value;
                    OnPropertyChanged();
                }
            }
        }

        private Models.Measurement _Measurement;
        public Models.Measurement Measurement
        {
            get { return _Measurement; }
            set
            {
                _Measurement = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }

    public class SpectraListViewModel
    {
        private Models.DataSpectra dataSpectra { get; set; }

        public delegate void EventHandlerSpectrumID(int SpectrumID);
        public event EventHandlerSpectrumID EventSpectrumToPlot, EventSpectrumNotToPlot;

        public List<MyMeasurement> ModifiedItems { get; set; }
        public AsyncObservableCollection<MyMeasurement> MeasurementList { get; set; }

        public CollectionViewSource viewSource { get; set; }

        private Filter lastFilter;

        public SpectraListViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            // Hooking up to events from DataSpectra
            dataSpectra.EventSpectrumNew += new Models.DataSpectra.EventHandlerSpectrum(SpectrumNew);
            dataSpectra.EventSpectrumRemove += new Models.DataSpectra.EventHandlerSpectrumID(SpectrumRemove);
            dataSpectra.EventSpectrumUpdate += new Models.DataSpectra.EventHandlerSpectrum(SpectrumUpdate);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<SpectraFilterViewModel>().EventNewFilter += new SpectraFilterViewModel.EventHandlerFilter(ChangeFilter);

            ModifiedItems = new List<MyMeasurement>();
            MeasurementList = new AsyncObservableCollection<MyMeasurement>();
            MeasurementList.CollectionChanged += OnCollectionChanged;

            viewSource = new CollectionViewSource();
            viewSource.Source = MeasurementList;
            viewSource.SortDescriptions.Add(new SortDescription("Measurement.StartTime", ListSortDirection.Descending));

            ChangeFilter(new Filter() { Name = "Today", Type = "Date", SubType = "Today" });
        }

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                if (e.NewItems != null)
                {
                    foreach (MyMeasurement newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                if (e.OldItems != null)
                {
                    foreach (MyMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null && e.OldItems != null)
                {
                    foreach (MyMeasurement newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                    foreach (MyMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MyMeasurement myMeasurement = sender as MyMeasurement;

            if (e.PropertyName == "Measurement") return;

            if (myMeasurement.Selected == true)
            { if (EventSpectrumToPlot != null) EventSpectrumToPlot(myMeasurement.Measurement.MeasurementID); }
            else
            { if (EventSpectrumNotToPlot != null) EventSpectrumNotToPlot(myMeasurement.Measurement.MeasurementID); }
        }

        public void ChangeFilter(Filter selectedFilter)
        {
            MeasurementList.Clear();
            Console.WriteLine("FilterType: {0}", selectedFilter.Type);

            switch (selectedFilter.Type)
            {
                case "All":
                    {
                        List<Models.Measurement> temp = dataSpectra.GetSpectra_All();
                        foreach (Models.Measurement spec in temp)
                            MeasurementList.Add(new MyMeasurement() { Selected = false, Measurement = spec });
                        break;
                    }
                case "Date":
                    {
                        List<Models.Measurement> temp = dataSpectra.GetSpectra_Date(selectedFilter);
                        foreach (Models.Measurement spec in temp)
                            MeasurementList.Add(new MyMeasurement() { Selected = false, Measurement = spec });
                        break;
                    }
                case "Sample":
                    {

                        break;
                    }
                case "Channel":
                    {
                        List<Models.Measurement> temp = dataSpectra.GetSpectra_Channel(selectedFilter);
                        foreach (Models.Measurement spec in temp)
                            MeasurementList.Add(new MyMeasurement() { Selected = false, Measurement = spec });
                        break;
                    }
            }
            viewSource.View.Refresh();
            Console.WriteLine("Length of spectraList: {0}", MeasurementList.Count());
            lastFilter = selectedFilter;
        }

        private void SpectrumNew(Models.Measurement measurement)
        {
            Console.WriteLine("SpectrumNew");
            MeasurementList.Add(new MyMeasurement() { Selected = true, Measurement = measurement });
            if (EventSpectrumToPlot != null) EventSpectrumToPlot(measurement.MeasurementID);
        }

        private void SpectrumRemove(int spectrumID)
        {
            Console.WriteLine("SpectrumRemove");
            MyMeasurement delSpectra = MeasurementList.Where(x => x.Measurement.MeasurementID == spectrumID).First();

            if (delSpectra != null)
                MeasurementList.Remove(delSpectra);
        }

        private void SpectrumUpdate(Models.Measurement spectrum)
        {
            var item = MeasurementList.Where(x => x.Measurement.MeasurementID == spectrum.MeasurementID).First();

            if (item != null)
            {
                int index = MeasurementList.IndexOf(item);
                MeasurementList[index].Measurement = spectrum;
            }
        }

        public void DeleteSelectedSpectra()
        {
            List<int> selectedSpectra = MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected spectra?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
                foreach (int ID in selectedSpectra)
                    dataSpectra.DeleteSpectra(selectedSpectra);
        }
    }
}

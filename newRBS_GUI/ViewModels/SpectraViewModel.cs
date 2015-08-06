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
using System.Threading;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;

namespace newRBS.ViewModel
{
    public delegate void SpectrumNewHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumRemoveHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumYHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumInfosHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumFinishedHandler(object o, Models.SpectrumArgs e);


    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        private SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

        public AsyncObservableCollection()
        {
        }

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list)
        {
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the CollectionChanged event on the current thread
                RaiseCollectionChanged(e);
            }
            else
            {
                // Raises the CollectionChanged event on the creator thread
                _synchronizationContext.Send(RaiseCollectionChanged, e);
            }
        }

        private void RaiseCollectionChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs)param);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                RaisePropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                _synchronizationContext.Send(RaisePropertyChanged, e);
            }
        }

        private void RaisePropertyChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnPropertyChanged((PropertyChangedEventArgs)param);
        }
    }

    public class SpectraViewModel : ViewModelBase
    {
        public Models.CAEN_x730 cAEN_x730;
        public Models.DataSpectra dataSpectra { get; set; }
        public Models.MeasureSpectra measureSpectra;

        public ICommand StartMeasurements { get; set; }
        public ICommand StopMeasurements { get; set; }

        public string myString { get; set; }


        private AsyncObservableCollection<Models.Spectrum> _measurementList;
        public AsyncObservableCollection<Models.Spectrum> measurementList
        {
            get { return _measurementList; }
            set { _measurementList = value; }
        }

        //private ObservableCollection<Models.Spectrum> _measurementList;
        //public ObservableCollection<Models.Spectrum> measurementList
        //{ get { return _measurementList; } set { _measurementList = value; } }

        public class CheckedListItem<T> : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private bool isChecked;
            private T item;

            public CheckedListItem()
            { }

            public CheckedListItem(T item, bool isChecked = false)
            {
                this.item = item;
                this.isChecked = isChecked;
            }

            public T Item
            {
                get { return item; }
                set
                {
                    item = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("Item"));
                }
            }

            public string Name
            {
                get { return String.Format("ch{0}", item); }
            }

            public bool IsChecked
            {
                get { return isChecked; }
                set
                {
                    isChecked = value;
                    if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
                }
            }
        }

        public ObservableCollection<CheckedListItem<int>> Channels { get; set; }

        public SpectraViewModel()
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<Models.CAEN_x730>();
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();
            measureSpectra = SimpleIoc.Default.GetInstance<Models.MeasureSpectra>();

            StartMeasurements = new RelayCommand(() => _StartMeasurements(), () => true);
            StopMeasurements = new RelayCommand(() => _StopMeasurements(), () => true);

            // Hooking up to events from DataSpectra
            dataSpectra.EventSpectrumNew += new Models.DataSpectra.ChangedEventHandler(SpectrumNew);
            dataSpectra.EventSpectrumY += new Models.DataSpectra.ChangedEventHandler(SpectrumY);
            dataSpectra.EventSpectrumInfos += new Models.DataSpectra.ChangedEventHandler(SpectrumInfos);

            myString = "SomeString";
            measurementList = dataSpectra.GetObservableCollection();

            Channels = new ObservableCollection<CheckedListItem<int>>();

            Channels.Add(new CheckedListItem<int>(0));
            Channels.Add(new CheckedListItem<int>(1));
            Channels.Add(new CheckedListItem<int>(2));
            Channels.Add(new CheckedListItem<int>(3));

            Channels[0].IsChecked = true;
        }

        private void SpectrumNew(object sender, Models.SpectrumArgs e)
        {
            Console.WriteLine("New Spectra");
            //Models.Spectrum newSpectrum = dataSpectra.spectra[e.ID];

            //_measurementList.Add(new Measurement(newSpectrum));
        }

        private void SpectrumY(object sender, Models.SpectrumArgs e)
        {
            Console.WriteLine("Updated Spectra");
        }

        private void SpectrumInfos(object sender, Models.SpectrumArgs e)
        {
            Console.WriteLine("SpectrumInfos");
            //Models.Spectrum spectrum = dataSpectra.spectra[e.ID];
            //var found = _measurementList.FirstOrDefault(i => i.spectrumID == e.ID);
            //if (found != null)
            {
                //   int i = _measurementList.IndexOf(found);
                //_measurementList[i] = new Measurement(spectrum);
            }
        }

        private void _StartMeasurements()
        {
            Console.WriteLine("Measurement will be starte");
            List<int> selectedChannels = new List<int>();
            List<CheckedListItem<int>> c = Channels.Where(i => i.IsChecked == true).ToList();
            for (int i = 0; i < c.Count; i++)
                selectedChannels.Add(c[i].Item);

            List<int> newIDs = measureSpectra.StartMeasurements(selectedChannels);

            measurementList = dataSpectra.GetObservableCollection();
        }

        private void _StopMeasurements()
        {
            List<int> selectedChannels = new List<int>();
            List<CheckedListItem<int>> c = Channels.Where(i => i.IsChecked == true).ToList();
            for (int i = 0; i < c.Count; i++)
                selectedChannels.Add(c[i].Item);

            measureSpectra.StopMeasurements(selectedChannels);
        }
    }
}

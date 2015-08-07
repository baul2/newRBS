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

    public class SpectraListClass
    {
        private Models.DataSpectra dataSpectra { get; set; }

        private AsyncObservableCollection<Models.Spectrum> _spectraList;
        public AsyncObservableCollection<Models.Spectrum> spectraList
        {
            get { return _spectraList; }
            set { _spectraList = value; }
        }

        public SpectraListClass()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            spectraList = new AsyncObservableCollection<Models.Spectrum>();

            List<Models.Spectrum> temp = dataSpectra.GetSpectra_All();
            foreach (Models.Spectrum spectrum in temp)
                spectraList.Add(spectrum);
        }

        public void ChangeFilter(string filterType, string filter)
        {
            spectraList.Clear();
            Console.WriteLine("FilterType: {0}; Filter: {1}", filterType, filter);
            if (filter == "All")
            {
                List<Models.Spectrum> temp = dataSpectra.GetSpectra_All();
                foreach (Models.Spectrum spectrum in temp)
                    spectraList.Add(spectrum);
            }
            else
            {
                switch (filterType)
                {
                    case "Date":
                        {
                            List<Models.Spectrum> temp = dataSpectra.GetSpectra_Date(filter);
                            foreach (Models.Spectrum spectrum in temp)
                                spectraList.Add(spectrum);
                            break;
                        }
                    case "Sample":
                        {

                            break;
                        }
                    case "Channel":
                        {
                            List<Models.Spectrum> temp = dataSpectra.GetSpectra_Channel(Int32.Parse(filter));
                            foreach (Models.Spectrum spectrum in temp)
                                spectraList.Add(spectrum);
                            break;
                        }
                }
            }
        }
    }

    public class SpectraFilterClass : ViewModelBase
    {
        private Models.DataSpectra dataSpectra { get; set; }
        private SpectraListClass _spectraListClass;

        public ICommand ExpandFilterList { get; set; }

        private bool _spectraFilterPanelVis = true;
        public bool spectraFilterPanelVis
        {
            get { return _spectraFilterPanelVis; }
            set { _spectraFilterPanelVis = value; RaisePropertyChanged(); }
        }

        public AsyncObservableCollection<string> filterTypeList { get; set; }

        private int _filterTypeIndex;
        public int filterTypeIndex
        {
            get
            { return _filterTypeIndex; }
            set
            {
                _filterTypeIndex = value;
                FillFilterList(filterTypeList[value]);
                RaisePropertyChanged();
            }
        }

        public AsyncObservableCollection<string> filterList { get; set; }

        private string _selectedFilter;
        public string selectedFilter
        {
            get
            { return _selectedFilter; }
            set
            {
                _selectedFilter = value;
                _spectraListClass.ChangeFilter(filterTypeList[filterTypeIndex], value);
                RaisePropertyChanged();
            }
        }

        public SpectraFilterClass(SpectraListClass spectraListClass)
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();
            _spectraListClass = spectraListClass;

            ExpandFilterList = new RelayCommand(() => _ExpandFilterList(), () => true);

            filterTypeList = new AsyncObservableCollection<string>();
            filterList = new AsyncObservableCollection<string>();

            filterTypeList.Add("Date");
            filterTypeList.Add("Sample");
            filterTypeList.Add("Channel");

            filterTypeIndex = 0;
        }

        private void _ExpandFilterList()
        {
            spectraFilterPanelVis = !spectraFilterPanelVis;
        }

        private void FillFilterList(string filterType)
        {
            filterList.Clear();
            switch (filterType)
            {
                case "Date":
                    filterList.Add("All");
                    filterList.Add("Today");
                    filterList.Add("This Week");
                    filterList.Add("This Month");
                    filterList.Add("This Year");
                    break;
                case "Channel":
                    filterList.Add("All");
                    List<string> allChannels = dataSpectra.GetAllChannels();
                    foreach (string channel in allChannels)
                        filterList.Add(channel);
                    Console.WriteLine("asdf"); break;
            }
        }
    }

    public class SpectraViewModel : ViewModelBase
    {
        public Models.CAEN_x730 cAEN_x730;
        public Models.DataSpectra dataSpectra { get; set; }
        public Models.MeasureSpectra measureSpectra;

        private SpectraListClass _spectraListClass;
        public SpectraListClass spectraListClass { get { return _spectraListClass; } set { _spectraListClass = value; } }

        private SpectraFilterClass _spectraFilterClass;
        public SpectraFilterClass spectraFilterClass { get { return _spectraFilterClass; } set { _spectraFilterClass = value; } }

        public ICommand StartMeasurements { get; set; }
        public ICommand StopMeasurements { get; set; }
        public ICommand TestButtonClick { get; set; }

        public string myString { get; set; }

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

            spectraListClass = new SpectraListClass();
            spectraFilterClass = new SpectraFilterClass(spectraListClass);

            StartMeasurements = new RelayCommand(() => _StartMeasurements(), () => true);
            StopMeasurements = new RelayCommand(() => _StopMeasurements(), () => true);
            TestButtonClick = new RelayCommand(() => _TestButtonClick(), () => true);

            // Hooking up to events from DataSpectra
            dataSpectra.EventSpectrumNew += new Models.DataSpectra.ChangedEventHandler(SpectrumNew);
            dataSpectra.EventSpectrumY += new Models.DataSpectra.ChangedEventHandler(SpectrumY);
            dataSpectra.EventSpectrumInfos += new Models.DataSpectra.ChangedEventHandler(SpectrumInfos);

            myString = "SomeString";

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

            //spectrumList = dataSpectra.GetObservableCollection();
        }

        private void _StopMeasurements()
        {
            List<int> selectedChannels = new List<int>();
            List<CheckedListItem<int>> c = Channels.Where(i => i.IsChecked == true).ToList();
            for (int i = 0; i < c.Count; i++)
                selectedChannels.Add(c[i].Item);

            measureSpectra.StopMeasurements(selectedChannels);
        }

        private void _TestButtonClick()
        {
            Console.WriteLine("TestButtionClick");
            spectraListClass.spectraList[0].Channel = 99;
        }
    }
}

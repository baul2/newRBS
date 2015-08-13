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
    public class MySpectrum : INotifyPropertyChanged
    {


        private bool _selected;
        public bool selected
        {
            get { return _selected; }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    OnPropertyChanged();
                }
            }
        }

        private Models.Spectrum _spectrum;
        public Models.Spectrum spectrum
        {
            get { return _spectrum; }
            set
            {
                _spectrum = value;
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

        public List<MySpectrum> ModifiedItems { get; set; }
        public AsyncObservableCollection<MySpectrum> spectraList { get; set; }

        public CollectionViewSource viewSource { get; set; }

        private Filter lastFilter;

        public SpectraListViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            // Hooking up to events from DataSpectra
            dataSpectra.EventSpectrumNew += new Models.DataSpectra.EventHandlerSpectrum(SpectrumNew);
            dataSpectra.EventSpectrumRemove += new Models.DataSpectra.EventHandlerSpectrum(SpectrumRemove);
            dataSpectra.EventSpectrumUpdate += new Models.DataSpectra.EventHandlerSpectrum(SpectrumUpdate);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<SpectraFilterViewModel>().EventNewFilter += new SpectraFilterViewModel.EventHandlerFilter(ChangeFilter);

            ModifiedItems = new List<MySpectrum>();
            spectraList = new AsyncObservableCollection<MySpectrum>();
            spectraList.CollectionChanged += OnCollectionChanged;

            viewSource = new CollectionViewSource();
            viewSource.Source = spectraList;
            viewSource.SortDescriptions.Add(new SortDescription("spectrum.StartTime", ListSortDirection.Descending));

            ChangeFilter(new Filter() { Name = "Today", Type = "Date", SubType = "Today" });
        }

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                if (e.NewItems != null)
                {
                    foreach (MySpectrum newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                if (e.OldItems != null)
                {
                    foreach (MySpectrum oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null && e.OldItems != null)
                {
                    foreach (MySpectrum newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                    foreach (MySpectrum oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            MySpectrum mySpectrum = sender as MySpectrum;

            if (e.PropertyName == "spectrum") return;

            if (mySpectrum.selected == true)
            { if (EventSpectrumToPlot != null) EventSpectrumToPlot(mySpectrum.spectrum.SpectrumID); }
            else
            { if (EventSpectrumNotToPlot != null) EventSpectrumNotToPlot(mySpectrum.spectrum.SpectrumID); }
        }

        public void ChangeFilter(Filter selectedFilter)
        {
            spectraList.Clear();
            Console.WriteLine("FilterType: {0}", selectedFilter.Type);

            switch (selectedFilter.Type)
            {
                case "All":
                    {
                        List<Models.Spectrum> temp = dataSpectra.GetSpectra_All();
                        foreach (Models.Spectrum spec in temp)
                            spectraList.Add(new MySpectrum() { selected = false, spectrum = spec });
                        break;
                    }
                case "Date":
                    {
                        List<Models.Spectrum> temp = dataSpectra.GetSpectra_Date(selectedFilter);
                        foreach (Models.Spectrum spec in temp)
                            spectraList.Add(new MySpectrum() { selected = false, spectrum = spec });
                        break;
                    }
                case "Sample":
                    {

                        break;
                    }
                case "Channel":
                    {
                        List<Models.Spectrum> temp = dataSpectra.GetSpectra_Channel(selectedFilter);
                        foreach (Models.Spectrum spec in temp)
                            spectraList.Add(new MySpectrum() { selected = false, spectrum = spec });
                        break;
                    }
            }
            viewSource.View.Refresh();
            Console.WriteLine("Length of spectraList: {0}", spectraList.Count());
            lastFilter = selectedFilter;
        }

        private void SpectrumNew(Models.Spectrum spectrum)
        {
            Console.WriteLine("SpectrumNew");
            ChangeFilter(lastFilter);
        }

        private void SpectrumRemove(Models.Spectrum spectrum)
        {
            Console.WriteLine("SpectrumRemove");
        }

        private void SpectrumUpdate(Models.Spectrum spectrum)
        {
            var item = spectraList.Where(x => x.spectrum.SpectrumID == spectrum.SpectrumID).First();

            if (item != null)
            {
                int index = spectraList.IndexOf(item);
                spectraList[index].spectrum = spectrum;
            }
        }
    }
}

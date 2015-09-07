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
using Microsoft.Win32;
using newRBS.Database;

namespace newRBS.ViewModels
{
    public class MeasurementListViewModel : ViewModelBase
    {
        //public ICommand DataGridDoubleClick { get; set; }

        private RelayCommand<EventArgs> _dataGridDoubleClickCommand;
        public RelayCommand<EventArgs> DataGridDoubleClickCommand
        {
            get
            {
                return _dataGridDoubleClickCommand
                  ?? (_dataGridDoubleClickCommand = new RelayCommand<EventArgs>(
                    eventargs => { _DataGridDoubleClickCommand(eventargs); }));
            }
        }

        public delegate void EventHandlerMeasurement(Measurement measurement);
        public event EventHandlerMeasurement EventMeasurementToPlot, EventMeasurementNotToPlot;

        public List<SelectableMeasurement> ModifiedItems { get; set; }
        public AsyncObservableCollection<SelectableMeasurement> MeasurementList { get; set; }

        public CollectionViewSource MeasurementListViewSource { get; set; }

        private int _SelectedMeasurementID;
        public int SelectedMeasurementID
        {
            get { return _SelectedMeasurementID; }
            set { _SelectedMeasurementID = value; RaisePropertyChanged(); }
        }

        public MeasurementListViewModel()
        {
            // Hooking up to events from DatabaseUtils
            DatabaseUtils.EventMeasurementRemove += new DatabaseUtils.EventHandlerMeasurement(DeleteRemovedMeasurementFromList);
            DatabaseUtils.EventMeasurementNew += new DatabaseUtils.EventHandlerMeasurement(AddNewMeasurementToList);
            DatabaseUtils.EventMeasurementUpdate += new DatabaseUtils.EventHandlerMeasurement(UpdateMeasurementInList);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().EventNewFilter += new MeasurementFilterViewModel.EventHandlerFilter(ChangeFilter);

            ModifiedItems = new List<SelectableMeasurement>();
            MeasurementList = new AsyncObservableCollection<SelectableMeasurement>();
            MeasurementList.CollectionChanged += OnCollectionChanged;

            MeasurementListViewSource = new CollectionViewSource();
            MeasurementListViewSource.Source = MeasurementList;
            MeasurementListViewSource.SortDescriptions.Add(new SortDescription("Measurement.StartTime", ListSortDirection.Descending));
        }

        private void _DataGridDoubleClickCommand(EventArgs eventArgs)
        {
            Console.WriteLine("_DataGridDoubleClick");

            MeasurementInfoViewModel measurementInfoViewModel = new MeasurementInfoViewModel(SelectedMeasurementID);
            Views.MeasurementInfoView measurementInfoView = new Views.MeasurementInfoView();
            measurementInfoView.DataContext = measurementInfoViewModel;
            measurementInfoView.ShowDialog();

            // Update selected row
            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
            {
                SelectableMeasurement myMeasurement = MeasurementList.First(x => x.Measurement.MeasurementID == SelectedMeasurementID);
                myMeasurement.Measurement = Database.Measurements.First(x => x.MeasurementID == SelectedMeasurementID);
                Sample temp = myMeasurement.Measurement.Sample; // To load the sample bevor the scope of db ends
            }
        }

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                if (e.NewItems != null)
                {
                    foreach (SelectableMeasurement newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                if (e.OldItems != null)
                {
                    foreach (SelectableMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null && e.OldItems != null)
                {
                    foreach (SelectableMeasurement newItem in e.NewItems)
                    {
                        ModifiedItems.Add(newItem);
                        newItem.PropertyChanged += this.OnItemPropertyChanged;
                    }
                    foreach (SelectableMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnItemPropertyChanged;
                        ModifiedItems.Remove(oldItem);
                    }
                }
        }

        void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SelectableMeasurement myMeasurement = sender as SelectableMeasurement;

            if (e.PropertyName == "Measurement") return;

            if (myMeasurement.Selected == true)
            { if (EventMeasurementToPlot != null) EventMeasurementToPlot(myMeasurement.Measurement); }
            else
            { if (EventMeasurementNotToPlot != null) EventMeasurementNotToPlot(myMeasurement.Measurement); }
        }

        public void ChangeFilter(List<int> MeasurementIDList)
        {
            MeasurementList.Clear();

            List<Measurement> newMeasurementList = new List<Measurement>();
            Sample tempSample;

            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
            {
                newMeasurementList = Database.Measurements.Where(x => MeasurementIDList.Contains(x.MeasurementID)).ToList();

                foreach (Measurement measurement in newMeasurementList)
                {
                    tempSample = measurement.Sample;
                    // The view will access MeasurementList.Sample, but the Sample will only load when needed and the DataContext doesn't extend to the view
                    MeasurementList.Add(new SelectableMeasurement() { Selected = false, Measurement = measurement });
                }
            }

            MeasurementListViewSource.View.Refresh();
        }

        private void AddNewMeasurementToList(Measurement measurement)
        {
            Console.WriteLine("AddNewMeasurementToList");
            MeasurementList.Add(new SelectableMeasurement() { Selected = true, Measurement = measurement });

            if (EventMeasurementToPlot != null) EventMeasurementToPlot(measurement);
        }


        private void DeleteRemovedMeasurementFromList(Measurement measurement)
        {
            Console.WriteLine("DeleteRemovedMeasurementFromList");
            SelectableMeasurement delMeasurement = MeasurementList.FirstOrDefault(x => x.Measurement.MeasurementID == measurement.MeasurementID);

            if (delMeasurement != null)
                MeasurementList.Remove(delMeasurement);
        }

        private void UpdateMeasurementInList(Measurement measurement)
        {
            SelectableMeasurement updateMeasurement = MeasurementList.FirstOrDefault(x => x.Measurement.MeasurementID == measurement.MeasurementID);

            if (updateMeasurement != null)
            {
                int index = MeasurementList.IndexOf(updateMeasurement);
                MeasurementList[index].Measurement = measurement;
            }
        }
    }
}

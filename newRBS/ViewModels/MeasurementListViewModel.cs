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
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using newRBS.ViewModels.Utils;
using Microsoft.Win32;
using newRBS.Database;
using System.Timers;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.MeasurementListView"/>. They show a list of all <see cref="Measurement"/>s for the selected filter/project.
    /// </summary>
    public class MeasurementListViewModel : ViewModelBase
    {
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

        private static Timer OfflineUpdateTimer = new Timer(MyGlobals.OfflineUpdateWorkerInterval);

        /// <summary>
        /// List of the currently shown <see cref="Measurement"/>s.
        /// </summary>
        public AsyncObservableCollection<SelectableMeasurement> MeasurementList { get; set; }

        /// <summary>
        /// <see cref="CollectionViewSource"/> of <see cref="MeasurementList"/> to which the datagrid binds to. Is used for sorting etc.
        /// </summary>
        public CollectionViewSource MeasurementListViewSource { get; set; }

        private bool _SelectAll = false;
        public bool SelectAll
        {
            get { return this._SelectAll; }
            set
            {
                _SelectAll = value;
                foreach (SelectableMeasurement s in MeasurementList)
                    s.Selected = value;
                RaisePropertyChanged();
            }
        }

        private int _DoubleClickedMeasurementID;
        /// <summary>
        /// <see cref="Measurement.MeasurementID"/> of the double clicked <see cref="Measurement"/> of the datagrid.
        /// </summary>
        public int DoubleClickedMeasurementID
        {
            get { return _DoubleClickedMeasurementID; }
            set { _DoubleClickedMeasurementID = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Constructor of the class. Sets up the events, <see cref="MeasurementList"/> and <see cref="MeasurementListViewSource"/>.
        /// </summary>
        public MeasurementListViewModel()
        {
            // Hooking up to events from DatabaseUtils
            DatabaseUtils.EventMeasurementRemove += new DatabaseUtils.EventHandlerMeasurement(DeleteRemovedMeasurementFromList);
            DatabaseUtils.EventMeasurementNew += new DatabaseUtils.EventHandlerMeasurement(AddNewMeasurementToList);
            DatabaseUtils.EventMeasurementUpdate += new DatabaseUtils.EventHandlerMeasurement(UpdateMeasurementInList);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().EventNewFilter += new MeasurementFilterViewModel.EventHandlerFilter(ChangeFilter);

            MeasurementList = new AsyncObservableCollection<SelectableMeasurement>();
            MeasurementList.CollectionChanged += OnMeasurementListChanged;

            MeasurementListViewSource = new CollectionViewSource();
            MeasurementListViewSource.Source = MeasurementList;
            MeasurementListViewSource.SortDescriptions.Add(new SortDescription("Measurement.StartTime", ListSortDirection.Descending));
        }

        /// <summary>
        /// Function that is executed on a datagrid double click and loads <see cref="MeasurementInfoViewModel"/>/<see cref="Views.MeasurementInfoView"/>.
        /// </summary>
        /// <param name="eventArgs"></param>
        public void _DataGridDoubleClickCommand(EventArgs eventArgs)
        {
            MeasurementInfoViewModel measurementInfoViewModel = new MeasurementInfoViewModel(DoubleClickedMeasurementID);
            Views.MeasurementInfoView measurementInfoView = new Views.MeasurementInfoView();
            measurementInfoView.DataContext = measurementInfoViewModel;
            measurementInfoView.ShowDialog();

            // Update selected row
            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                SelectableMeasurement myMeasurement = MeasurementList.First(x => x.Measurement.MeasurementID == DoubleClickedMeasurementID);
                myMeasurement.Measurement = Database.Measurements.First(x => x.MeasurementID == DoubleClickedMeasurementID);
                Sample temp = myMeasurement.Measurement.Sample; // To load the sample bevor the scope of db ends
            }
        }

        /// <summary>
        /// Function that is executed when an item of <see cref="MeasurementList"/> is added/removed. It attaches <see cref="OnSelectableMeasurementModified(object, PropertyChangedEventArgs)"/> to the new items 'PropertyChanged' event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnMeasurementListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                if (e.NewItems != null)
                {
                    foreach (SelectableMeasurement newItem in e.NewItems)
                    {
                        newItem.PropertyChanged += this.OnSelectableMeasurementModified;
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Remove)
                if (e.OldItems != null)
                {
                    foreach (SelectableMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnSelectableMeasurementModified;
                    }
                }

            if (e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null && e.OldItems != null)
                {
                    foreach (SelectableMeasurement newItem in e.NewItems)
                    {
                        newItem.PropertyChanged += this.OnSelectableMeasurementModified;
                    }
                    foreach (SelectableMeasurement oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= this.OnSelectableMeasurementModified;
                    }
                }
        }

        /// <summary>
        /// Function that is executed when an item in <see cref="MeasurementList"/> is modified. When the <see cref="SelectableMeasurement.Selected"/> status changed, events are send out.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnSelectableMeasurementModified(object sender, PropertyChangedEventArgs e)
        {
            SelectableMeasurement myMeasurement = sender as SelectableMeasurement;

            if (e.PropertyName == "Measurement") return;

            if (myMeasurement.Selected == true)
            { if (EventMeasurementToPlot != null) EventMeasurementToPlot(myMeasurement.Measurement); }
            else
            { if (EventMeasurementNotToPlot != null) EventMeasurementNotToPlot(myMeasurement.Measurement); }
        }

        /// <summary>
        /// Function that is executed when the filter/project in <see cref="MeasurementFilterViewModel"/> changes. It updates <see cref="MeasurementList"/> with the received <see cref="Measurement.MeasurementID"/>s.
        /// </summary>
        /// <param name="MeasurementIDList">The IDs of the <see cref="Measurement"/>s to show in the list.</param>
        public void ChangeFilter(List<int> MeasurementIDList)
        {
            MeasurementList.Clear();

            OfflineUpdateTimer.Stop();

            List<Measurement> measurements = new List<Measurement>();
            Sample tempSample;
            Isotope tempIsotope;
            Element tempElement;

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                measurements = Database.Measurements.Where(x => MeasurementIDList.Contains(x.MeasurementID)).ToList();

                foreach (Measurement measurement in measurements)
                {
                    tempSample = measurement.Sample;
                    tempIsotope = measurement.Isotope;
                    tempElement = measurement.Isotope.Element;
                    // The view will access MeasurementList.Sample, but the Sample will only load when needed and the DataContext doesn't extend to the view
                    MeasurementList.Add(new SelectableMeasurement() { Selected = false, Measurement = measurement });

                    // Check if measurement is running on another computer -> update measurement periodically
                    if (measurement.Runs == true && MyGlobals.CanMeasure == false) 
                    {
                        OfflineUpdateTimer = new Timer(MyGlobals.OfflineUpdateWorkerInterval);
                        OfflineUpdateTimer.Elapsed += delegate { OfflineUpdateWorker(measurement.MeasurementID); };
                        OfflineUpdateTimer.Start();
                    }
                }
            }

            MeasurementListViewSource.View.Refresh();
        }

        /// <summary>
        /// Function that is executed when a new <see cref="Measurement"/> is detected. It adds the <see cref="Measurement"/> to <see cref="MeasurementList"/> and sends an event to plot it.
        /// </summary>
        /// <param name="measurement">New <see cref="Measurement"/>.</param>
        public void AddNewMeasurementToList(Measurement measurement)
        {
            MeasurementList.Add(new SelectableMeasurement() { Selected = true, Measurement = measurement });

            if (EventMeasurementToPlot != null) EventMeasurementToPlot(measurement);
        }

        /// <summary>
        /// Function that is executed when a <see cref="Measurement"/> is deleted. It removes the <see cref="Measurement"/> from <see cref="MeasurementList"/> and sends an event not to plot it.
        /// </summary>
        /// <param name="measurement">Deleted <see cref="Measurement"/>.</param>
        public void DeleteRemovedMeasurementFromList(Measurement measurement)
        {
            SelectableMeasurement delMeasurement = MeasurementList.FirstOrDefault(x => x.Measurement.MeasurementID == measurement.MeasurementID);

            if (delMeasurement != null)
                MeasurementList.Remove(delMeasurement);
        }

        /// <summary>
        /// Function that is executed when a <see cref="Measurement"/> is modified. It updates the <see cref="Measurement"/> in <see cref="MeasurementList"/>.
        /// </summary>
        /// <param name="measurement">Updated <see cref="Measurement"/>.</param>
        public void UpdateMeasurementInList(Measurement measurement)
        {
            SelectableMeasurement updateMeasurement = MeasurementList.FirstOrDefault(x => x.Measurement.MeasurementID == measurement.MeasurementID);

            if (updateMeasurement != null)
            {
                int index = MeasurementList.IndexOf(updateMeasurement);
                MeasurementList[index].Measurement = measurement;
            }
        }

        public void OfflineUpdateWorker(int MeasurementID)
        {
            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                Measurement updateMeasurement = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);

                if (updateMeasurement != null)
                {
                    Sample tempSample= updateMeasurement.Sample;
                    Isotope tempIsotope = updateMeasurement.Isotope;
                    Element tempElement = updateMeasurement.Isotope.Element;
                 
                    UpdateMeasurementInList(updateMeasurement);
                }
            }
        }
    }
}

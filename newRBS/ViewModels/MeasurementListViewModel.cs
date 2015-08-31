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

        public delegate void EventHandlerMeasurement(Models.Measurement measurement);
        public event EventHandlerMeasurement EventMeasurementToPlot, EventMeasurementNotToPlot;

        public List<MyMeasurement> ModifiedItems { get; set; }
        public AsyncObservableCollection<MyMeasurement> MeasurementList { get; set; }

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
            Models.DatabaseUtils.EventMeasurementRemove += new Models.DatabaseUtils.EventHandlerMeasurement(DeleteRemovedMeasurementFromList);
            Models.DatabaseUtils.EventMeasurementNew += new Models.DatabaseUtils.EventHandlerMeasurement(AddNewMeasurementToList);
            Models.DatabaseUtils.EventMeasurementUpdate += new Models.DatabaseUtils.EventHandlerMeasurement(UpdateMeasurementInList);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().EventNewFilter += new MeasurementFilterViewModel.EventHandlerFilter(ChangeFilter);

            ModifiedItems = new List<MyMeasurement>();
            MeasurementList = new AsyncObservableCollection<MyMeasurement>();
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
            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                MyMeasurement myMeasurement = MeasurementList.First(x => x.Measurement.MeasurementID == SelectedMeasurementID);
                myMeasurement.Measurement = Database.Measurements.First(x => x.MeasurementID == SelectedMeasurementID);
                Models.Sample temp = myMeasurement.Measurement.Sample; // To load the sample bevor the scope of db ends
            }
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
            { if (EventMeasurementToPlot != null) EventMeasurementToPlot(myMeasurement.Measurement); }
            else
            { if (EventMeasurementNotToPlot != null) EventMeasurementNotToPlot(myMeasurement.Measurement); }
        }

        public void ChangeFilter(List<int> MeasurementIDList)
        {
            MeasurementList.Clear();

            List<Models.Measurement> newMeasurementList = new List<Models.Measurement>();
            Models.Sample tempSample;

            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                newMeasurementList = Database.Measurements.Where(x => MeasurementIDList.Contains(x.MeasurementID)).ToList();

                foreach (Models.Measurement measurement in newMeasurementList)
                {
                    tempSample = measurement.Sample;
                    // The view will access MeasurementList.Sample, but the Sample will only load when needed and the DataContext doesn't extend to the view
                    MeasurementList.Add(new MyMeasurement() { Selected = false, Measurement = measurement });
                }
            }

            MeasurementListViewSource.View.Refresh();
        }

        private void AddNewMeasurementToList(Models.Measurement measurement)
        {
            Console.WriteLine("AddNewMeasurementToList");
            MeasurementList.Add(new MyMeasurement() { Selected = true, Measurement = measurement });

            if (EventMeasurementToPlot != null) EventMeasurementToPlot(measurement);
        }


        private void DeleteRemovedMeasurementFromList(Models.Measurement measurement)
        {
            Console.WriteLine("DeleteRemovedMeasurementFromList");
            MyMeasurement delMeasurement = MeasurementList.FirstOrDefault(x => x.Measurement.MeasurementID == measurement.MeasurementID);

            if (delMeasurement != null)
                MeasurementList.Remove(delMeasurement);
        }

        private void UpdateMeasurementInList(Models.Measurement measurement)
        {
            MyMeasurement updateMeasurement = MeasurementList.FirstOrDefault(x => x.Measurement.MeasurementID == measurement.MeasurementID);

            if (updateMeasurement != null)
            {
                int index = MeasurementList.IndexOf(updateMeasurement);
                MeasurementList[index].Measurement = measurement;
            }
        }

        public void DeleteSelectedMeasurement()
        {
            List<int> selectedMeasurementIDs = MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();

            if (selectedMeasurementIDs.Count() == 0) return;

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected spectra?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
                using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                {
                    Database.Measurements.DeleteAllOnSubmit(Database.Measurements.Where(x=>selectedMeasurementIDs.Contains(x.MeasurementID)));
                    Database.SubmitChanges();
                }
        }
    }
}

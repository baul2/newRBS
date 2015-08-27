﻿using System;
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

namespace newRBS.ViewModels
{
    public class TreeViewModel : ViewModelBase
    {
        public AsyncObservableCollection<FilterClass> Items { get; set; }
    }

    public class MeasurementFilterViewModel : INotifyPropertyChanged
    {
        private Models.DatabaseUtils dataSpectra { get; set; }
        //private SpectraListViewModel spectraListViewModel;

        public delegate void EventHandlerFilter(FilterClass newFilter);
        public event EventHandlerFilter EventNewFilter;

        public ICommand ExpandFilterList { get; set; }
        public ICommand TestButtonClick { get; set; }

        private bool _measurementFilterPanelVis = true;
        public bool measurementFilterPanelVis
        {
            get { return _measurementFilterPanelVis; }
            set { _measurementFilterPanelVis = value; OnPropertyChanged("measurementFilterPanelVis"); }
        }

        public AsyncObservableCollection<string> filterTypeList { get; set; }

        private int _filterTypeIndex;
        public int filterTypeIndex
        {
            get { return _filterTypeIndex; }
            set
            {
                _filterTypeIndex = value;
                Console.WriteLine("new filter type {0}", filterTypeList[value]);
                FillFilterList(filterTypeList[value]);
            }
        }

        private FilterClass _selectedFilter;
        public FilterClass selectedFilter
        {
            get { return _selectedFilter; }
            set
            {
                Console.WriteLine("new selectedFilter");
                Console.WriteLine(value.Type);
                _selectedFilter = value;

                // Send event (to SpectraListView...)
                if (EventNewFilter != null) EventNewFilter(value);
            }
        }

        public object CurrSelItem { get; set; }

        public RelayCommand<TreeViewHelper.DependencyPropertyEventArgs> MySelItemChgCmd { get; set; }

        private void TreeViewItemSelectedChangedCallBack(TreeViewHelper.DependencyPropertyEventArgs e)
        {
            if (e != null && e.DependencyPropertyChangedEventArgs.NewValue != null)
            {
                FilterClass temp = (FilterClass)e.DependencyPropertyChangedEventArgs.NewValue;
                Console.WriteLine(temp.Name);
                selectedFilter = temp;
            }
        }

        public TreeViewModel filterTree { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public MeasurementFilterViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DatabaseUtils>();

            ExpandFilterList = new RelayCommand(() => _ExpandFilterList(), () => true);
            TestButtonClick = new RelayCommand(() => _TestButtonClick(), () => true);

            filterTypeList = new AsyncObservableCollection<string> { "Date", "Sample", "Channel" };
            filterTree = new TreeViewModel();
            filterTree.Items = new AsyncObservableCollection<FilterClass>();
            selectedFilter = new FilterClass() { Name = "All", Type = "All" };

            MySelItemChgCmd = new RelayCommand<TreeViewHelper.DependencyPropertyEventArgs>(TreeViewItemSelectedChangedCallBack);
            CurrSelItem = new object();

            filterTypeIndex = 0;
        }

        private void _ExpandFilterList()
        {
            Console.WriteLine("Expand");
            measurementFilterPanelVis = !measurementFilterPanelVis;
        }

        private void _TestButtonClick()
        {
            Console.WriteLine("TestButtionClick-measurementFilterViewModel");
        }

        private void FillFilterList(string filterType)
        {
            Console.WriteLine("Update filter list with filterType: {0}", filterType);
            //filterTree.Items.Clear();
            if (filterTree.Items.Count() > 0)
            {
                foreach (FilterClass n in filterTree.Items)
                    Console.WriteLine(n.Name);
                while (filterTree.Items.Count > 0)
                    filterTree.Items.RemoveAt(0);
                foreach (FilterClass n in filterTree.Items)
                    Console.WriteLine(n.Name);
            }
            Console.WriteLine("clear done");

            switch (filterType)
            {
                case "Date":
                    filterTree.Items.Add(new FilterClass() { Name = "All", Type = "All" });
                    filterTree.Items.Add(new FilterClass() { Name = "Today", Type = "Date", SubType = "Today" });
                    filterTree.Items.Add(new FilterClass() { Name = "This Week", Type = "Date", SubType = "ThisWeek" });
                    filterTree.Items.Add(new FilterClass() { Name = "This Month", Type = "Date", SubType = "ThisMonth" });
                    filterTree.Items.Add(new FilterClass() { Name = "This Year", Type = "Date", SubType = "ThisYear" });

                    using (Models.DatabaseDataContext db = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        Console.WriteLine("test: {0}", db.Measurements.First(x => x.SampleID == 1).Runs);
                        Console.WriteLine("All count: {0}", db.Measurements.ToList().Count());
                        List<int> allYears = (from spec in db.Measurements select spec.StartTime.Year).Distinct().ToList();
                        foreach (int Year in allYears)
                        {
                            FilterClass newYearNode = new FilterClass() { Name = Year.ToString(), Type = "Date", SubType = "Year", Year = Year };

                            List<int> allMonths = db.Measurements.Where(x => x.StartTime.Year == Year).Select(x => x.StartTime.Month).Distinct().ToList();
                            if (allMonths.Count > 0)
                            {
                                newYearNode.Children = new AsyncObservableCollection<FilterClass>();
                                foreach (int Month in allMonths)
                                {
                                    FilterClass newMonthNode = new FilterClass() { Name = Month.ToString("D2"), Type = "Date", SubType = "Month", Year = Year, Month = Month };

                                    List<int> allDays = db.Measurements.Where(x => x.StartTime.Year == Year && x.StartTime.Month == Month).Select(x => x.StartTime.Day).Distinct().ToList();
                                    if (allDays.Count > 0)
                                    {
                                        newMonthNode.Children = new AsyncObservableCollection<FilterClass>();
                                        foreach (int Day in allDays)
                                        {
                                            FilterClass newDayNode = new FilterClass() { Name = Day.ToString("D2"), Type = "Date", SubType = "Day", Year = Year, Month = Month, Day = Day };
                                            newMonthNode.Children.Add(newDayNode);
                                        }
                                    }
                                    newYearNode.Children.Add(newMonthNode);
                                }
                            }
                            filterTree.Items.Add(newYearNode);
                        }
                    }
                    break;

                case "Channel":
                    Console.WriteLine("Channel");
                    filterTree.Items.Add(new FilterClass() { Name = "All", Type = "All" });

                    using (Models.DatabaseDataContext db = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        List<int> allChannels = db.Measurements.Select(x => x.Channel).Distinct().ToList();
                        Console.WriteLine("NumChannels {0}", allChannels.Count());

                        foreach (int Channel in allChannels)
                        {
                            Console.WriteLine(Channel);
                            filterTree.Items.Add(new FilterClass() { Name = Channel.ToString(), Type = "Channel", Channel = Channel });
                        }
                    }
                    break;

                case "Sample":
                    Console.WriteLine("Sample");
                    filterTree.Items.Add(new FilterClass() { Name = "All", Type = "All" });

                    using (Models.DatabaseDataContext db = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        //List<string> allSampleNames = (from sample in db.Samples select sample.SampleName).Distinct().ToList();
                        List<string> allSampleNames = db.Samples.Where(x => x.SampleID > 2).Select(x => x.SampleName).ToList();
                        Console.WriteLine("NumSamples {0}", allSampleNames.Count());

                        foreach (string sampleName in allSampleNames)
                        {
                            Console.WriteLine(sampleName);
                            filterTree.Items.Add(new FilterClass() { Name = sampleName, Type = "Sample", SampleName = sampleName });
                        }
                    }
                    break;

                default:
                    Console.WriteLine("No action found for filterType: {0}", filterType);
                    break;
            }
        }
    }
}
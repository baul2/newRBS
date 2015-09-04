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

namespace newRBS.ViewModels
{
    public class TreeViewModel : ViewModelBase
    {
        public AsyncObservableCollection<FilterClass> Items { get; set; }
    }

    public class MeasurementFilterViewModel : ViewModelBase
    {
        public delegate void EventHandlerFilter(List<int> MeasurementIDList);
        public event EventHandlerFilter EventNewFilter;

        public ICommand ExpandFilterList { get; set; }

        public ICommand NewProjectCommand { get; set; }
        public ICommand RenameProjectCommand { get; set; }
        public ICommand DeleteProjectCommand { get; set; }

        public ICommand AddMeasurementCommand { get; set; }
        public ICommand RemoveMeasurementCommand { get; set; }

        private bool _measurementFilterPanelVis = true;
        public bool measurementFilterPanelVis
        {
            get { return _measurementFilterPanelVis; }
            set
            {
                _measurementFilterPanelVis = value;
                switch (value)
                {
                    case true:
                        { VisButtonContent = "\u21D1 Filter Panel \u21D1"; break; }
                    case false:
                        { VisButtonContent = "\u21D3 Filter Panel \u21D3"; break; }
                }
                RaisePropertyChanged();
            }
        }

        private string _VisButtonContent = "\u21D1 Filter Panel \u21D1";
        public string VisButtonContent
        { get { return _VisButtonContent; } set { _VisButtonContent = value; RaisePropertyChanged(); } }

        public AsyncObservableCollection<string> filterTypeList { get; set; }

        private int _filterTypeIndex;
        public int filterTypeIndex
        { get { return _filterTypeIndex; } set { _filterTypeIndex = value; FillFilterList(filterTypeList[value]); } }

        public FilterClass selectedFilter { get; set; }

        public RelayCommand<TreeViewHelper.DependencyPropertyEventArgs> MySelItemChgCmd { get; set; }

        private void TreeViewItemSelectedChangedCallBack(TreeViewHelper.DependencyPropertyEventArgs e)
        {
            if (e != null && e.DependencyPropertyChangedEventArgs.NewValue != null)
            {
                FilterClass temp = (FilterClass)e.DependencyPropertyChangedEventArgs.NewValue;
                Console.WriteLine(temp.Name);
                SelectedProject = null;
                NewFilterSelected(temp);
            }
        }

        public TreeViewModel filterTree { get; set; }

        public ObservableCollection<Models.Project> Projects { get; set; }

        private Models.Project _SelectedProject;
        public Models.Project SelectedProject
        {
            get { return _SelectedProject; }
            set
            {
                if (value != null)
                {
                    Console.WriteLine("SelectedProject");
                    ClearFilterTreeSelectedItem();
                    SelectProject(value);
                }
                _SelectedProject = value;
                RaisePropertyChanged();
            }
        }

        private void ClearFilterTreeSelectedItem()
        {
            foreach (var s in filterTree.Items)
            {
                if (s.IsSelected == true) s.IsSelected = false;
                if (s.Children != null)
                    foreach (var u in s.Children)
                    {
                        if (u.IsSelected == true) u.IsSelected = false;
                        if (u.Children != null)
                            foreach (var t in u.Children)
                                if (t.IsSelected == true) t.IsSelected = false;
                    }
            }
        }

        public MeasurementFilterViewModel()
        {
            ExpandFilterList = new RelayCommand(() => _ExpandFilterList(), () => true);

            NewProjectCommand = new RelayCommand(() => _NewProjectCommand(), () => true);
            RenameProjectCommand = new RelayCommand(() => _RenameProjectCommand(), () => true);
            DeleteProjectCommand = new RelayCommand(() => _DeleteProjectCommand(), () => true);

            AddMeasurementCommand = new RelayCommand(() => _AddMeasurementCommand(), () => true);
            RemoveMeasurementCommand = new RelayCommand(() => _RemoveMeasurementCommand(), () => true);

            filterTypeList = new AsyncObservableCollection<string> { "Date", "Sample", "Channel" };
            filterTree = new TreeViewModel();
            filterTree.Items = new AsyncObservableCollection<FilterClass>();
            selectedFilter = new FilterClass() { Name = "All", Type = "All" };

            MySelItemChgCmd = new RelayCommand<TreeViewHelper.DependencyPropertyEventArgs>(TreeViewItemSelectedChangedCallBack);

            filterTypeIndex = 0;

            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                Projects = new ObservableCollection<Models.Project>(Database.Projects.ToList());
            }
        }

        private void _ExpandFilterList()
        {
            measurementFilterPanelVis = !measurementFilterPanelVis;
        }

        private void FillFilterList(string filterType)
        {
            Console.WriteLine("Update filter list with filterType: {0}", filterType);
            if (filterTree.Items.Count() > 0)
            {
                while (filterTree.Items.Count > 0)
                    filterTree.Items.RemoveAt(0);
            }

            switch (filterType)
            {
                case "Date":
                    filterTree.Items.Add(new FilterClass() { Name = "All", Type = "All" });
                    filterTree.Items.Add(new FilterClass() { Name = "Today", Type = "Date", SubType = "Today" });
                    filterTree.Items.Add(new FilterClass() { Name = "This Week", Type = "Date", SubType = "ThisWeek" });
                    filterTree.Items.Add(new FilterClass() { Name = "This Month", Type = "Date", SubType = "ThisMonth" });
                    filterTree.Items.Add(new FilterClass() { Name = "This Year", Type = "Date", SubType = "ThisYear" });

                    using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        Console.WriteLine("All count: {0}", Database.Measurements.ToList().Count());
                        List<int> allYears = (from spec in Database.Measurements select spec.StartTime.Year).Distinct().ToList();
                        foreach (int Year in allYears)
                        {
                            FilterClass newYearNode = new FilterClass() { Name = Year.ToString(), Type = "Date", SubType = "Year", Year = Year };

                            List<int> allMonths = Database.Measurements.Where(x => x.StartTime.Year == Year).Select(x => x.StartTime.Month).Distinct().ToList();
                            if (allMonths.Count > 0)
                            {
                                newYearNode.Children = new AsyncObservableCollection<FilterClass>();
                                foreach (int Month in allMonths)
                                {
                                    FilterClass newMonthNode = new FilterClass() { Name = Month.ToString("D2"), Type = "Date", SubType = "Month", Year = Year, Month = Month };

                                    List<int> allDays = Database.Measurements.Where(x => x.StartTime.Year == Year && x.StartTime.Month == Month).Select(x => x.StartTime.Day).Distinct().ToList();
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

                    using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        List<int> allChannels = Database.Measurements.Select(x => x.Channel).Distinct().ToList();
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

                    using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        //List<string> allSampleNames = (from sample in Database.Samples select sample.SampleName).Distinct().ToList();
                        List<string> allSampleNames = Database.Samples.Select(x => x.SampleName).ToList();
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
            if (EventNewFilter != null) EventNewFilter(new List<int>());
        }

        public void NewFilterSelected(FilterClass filter)
        {
            if (selectedFilter == null) return;

            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                List<int> MeasurementIDList = new List<int>();

                switch (filter.Type)
                {
                    case "All":
                        { MeasurementIDList = Database.Measurements.Select(x => x.MeasurementID).ToList(); break; }

                    case "Date":
                        {
                            switch (filter.SubType)
                            {
                                case "Today":
                                    { MeasurementIDList = Database.Measurements.Where(x => x.StartTime.Date == DateTime.Today).Select(x => x.MeasurementID).ToList(); break; }

                                case "ThisWeek":
                                    {
                                        int DayOfWeek = (int)DateTime.Today.DayOfWeek;
                                        MeasurementIDList = Database.Measurements.Where(x => x.StartTime.DayOfYear > (DateTime.Today.DayOfYear - DayOfWeek) && x.StartTime.DayOfYear < (DateTime.Today.DayOfYear - DayOfWeek + 7)).Select(x => x.MeasurementID).ToList(); //Todo!!!
                                        break;
                                    }

                                case "ThisMonth":
                                    { MeasurementIDList = Database.Measurements.Where(x => x.StartTime.Date.Month == DateTime.Now.Month).Select(x => x.MeasurementID).ToList(); break; }

                                case "ThisYear":
                                    { MeasurementIDList = Database.Measurements.Where(x => x.StartTime.Date.Year == DateTime.Now.Year).Select(x => x.MeasurementID).ToList(); break; }

                                case "Year":
                                    { MeasurementIDList = Database.Measurements.Where(x => x.StartTime.Date.Year == filter.Year).Select(x => x.MeasurementID).ToList(); break; }

                                case "Month":
                                    { MeasurementIDList = Database.Measurements.Where(x => x.StartTime.Date.Year == filter.Year && x.StartTime.Date.Month == filter.Month).Select(x => x.MeasurementID).ToList(); break; }

                                case "Day":
                                    { MeasurementIDList = Database.Measurements.Where(x => x.StartTime.Date.Year == filter.Year && x.StartTime.Date.Month == filter.Month && x.StartTime.Date.Day == filter.Day).Select(x => x.MeasurementID).ToList(); break; }
                            }
                        }
                        break;

                    case "Sample":
                        { MeasurementIDList = Database.Measurements.Where(x => x.Sample.SampleName == filter.SampleName).Select(x => x.MeasurementID).ToList(); break; }

                    case "Channel":
                        { MeasurementIDList = Database.Measurements.Where(x => x.Channel == filter.Channel).Select(x => x.MeasurementID).ToList(); break; }
                }

                // Send event (to SpectraListView...)
                if (EventNewFilter != null) EventNewFilter(MeasurementIDList);
            }
        }

        private void SelectProject(Models.Project project)
        {
            if (project == null) return;

            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                Console.WriteLine("Load project");
                List<int> MeasurementIDList = Database.Measurement_Projects.Where(x => x.ProjectID == project.ProjectID).Select(x => x.MeasurementID).ToList();
                // Send event (to SpectraListView...)
                if (EventNewFilter != null) EventNewFilter(MeasurementIDList);
            }
        }

        private void _NewProjectCommand()
        {
            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new project name:", "");
            if (inputDialog.ShowDialog() == true)
                if (inputDialog.Answer != "")
                    using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        Models.Project newProject = new Models.Project { ProjectName = inputDialog.Answer };
                        Database.Projects.InsertOnSubmit(newProject);
                        Database.SubmitChanges();
                        Projects.Add(newProject);
                    }
        }

        private void _RenameProjectCommand()
        {
            if (SelectedProject == null) return;

            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new project name:", SelectedProject.ProjectName);
            if (inputDialog.ShowDialog() == true)
                if (inputDialog.Answer != "")
                {
                    using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                    {
                        Models.Project renamedProject = Database.Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID);
                        renamedProject.ProjectName = inputDialog.Answer;
                        Database.SubmitChanges();
                    }

                    Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID).ProjectName = inputDialog.Answer;
                }
        }

        private void _DeleteProjectCommand()
        {
            if (SelectedProject == null) return;

            MessageBoxResult messageBox = MessageBox.Show("Are you shure to delete the selected project?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (messageBox == MessageBoxResult.Yes)
            {
                using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                {
                    Database.Measurement_Projects.DeleteAllOnSubmit(Database.Measurement_Projects.Where(x => x.ProjectID == SelectedProject.ProjectID));
                    Database.Projects.DeleteOnSubmit(Database.Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID));
                    Database.SubmitChanges();
                }

                Projects.Remove(Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID));
                if (EventNewFilter != null) EventNewFilter(new List<int>());
            }
        }

        private void _AddMeasurementCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();

            if (selectedMeasurementIDs.Count() == 0) return;

            Views.Utils.ProjectSelector projectSelector = new Views.Utils.ProjectSelector();
            if (projectSelector.ShowDialog() == true)
            {
                Console.WriteLine(projectSelector.SelectedProject.ProjectName);

                using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                {
                    List<Models.Measurement_Project> newMeasurement_Projects = new List<Models.Measurement_Project>();

                    foreach (int ID in selectedMeasurementIDs)
                    {
                        newMeasurement_Projects.Add(new Models.Measurement_Project { MeasurementID = ID, ProjectID = projectSelector.SelectedProject.ProjectID });
                    }

                    Database.Measurement_Projects.InsertAllOnSubmit(newMeasurement_Projects);
                    Database.SubmitChanges();
                }
            }
        }

        private void _RemoveMeasurementCommand()
        {
            if (SelectedProject == null) return;

            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();

            if (selectedMeasurementIDs.Count() == 0) return;

            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                Database.Measurement_Projects.DeleteAllOnSubmit(Database.Measurement_Projects.Where(x => selectedMeasurementIDs.Contains(x.MeasurementID)));
                Database.SubmitChanges();
            }

            SelectProject(SelectedProject);
        }
    }
}

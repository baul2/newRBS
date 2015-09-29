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
using newRBS.Database;
using System.Diagnostics;
using System.Reflection;
using System.IO;

namespace newRBS.ViewModels
{
    public class TreeViewModel : ViewModelBase
    {
        public AsyncObservableCollection<FilterClass> Items { get; set; }
    }

    /// <summary>
    /// Class that is the view model of <see cref="Views.MeasurementFilterView"/>. They filter the database by date/sample/project... and send the obtained <see cref="Measurement.MeasurementID"/>s as events.
    /// </summary>
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

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

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
                        { VisButtonContent = "<\n<\n<"; break; }
                    case false:
                        { VisButtonContent = ">\n>\n>"; break; }
                }
                RaisePropertyChanged();
            }
        }

        private string _VisButtonContent = "<\n<\n<";
        public string VisButtonContent
        { get { return _VisButtonContent; } set { _VisButtonContent = value; RaisePropertyChanged(); } }

        /// <summary>
        /// The list of available filter types.
        /// </summary>
        public AsyncObservableCollection<string> filterTypeList { get; set; }

        private int _filterTypeIndex;
        public int filterTypeIndex
        { get { return _filterTypeIndex; } set { _filterTypeIndex = value; FillFilterList(filterTypeList[value]); } }

        /// <summary>
        /// The currently selected filter.
        /// </summary>
        public FilterClass selectedFilter { get; set; }

        public RelayCommand<TreeViewHelper.DependencyPropertyEventArgs> SelectedItemChanged { get; set; }

        private void TreeViewItemSelectedChangedCallBack(TreeViewHelper.DependencyPropertyEventArgs e)
        {
            if (e != null && e.DependencyPropertyChangedEventArgs.NewValue != null)
            {
                FilterClass temp = (FilterClass)e.DependencyPropertyChangedEventArgs.NewValue;
                SelectedProject = null;
                NewFilterSelected(temp);
            }
        }

        /// <summary>
        /// The ItemsSource of the TreeView. It contains the available filters (<see cref="FilterClass"/>).
        /// </summary>
        public TreeViewModel filterTree { get; set; }

        public ObservableCollection<Project> Projects { get; set; }

        private Project _SelectedProject;
        /// <summary>
        /// The currently selected <see cref="Project"/>.
        /// </summary>
        public Project SelectedProject
        {
            get { return _SelectedProject; }
            set
            {
                if (value != null)
                {
                    ClearFilterTreeSelectedItem();
                    SelectProject(value);
                }
                _SelectedProject = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Constructor of the class. It sets up the Commands and initiate the <see cref="filterTypeList"/> and <see cref="filterTree"/>.
        /// </summary>
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

            SelectedItemChanged = new RelayCommand<TreeViewHelper.DependencyPropertyEventArgs>(TreeViewItemSelectedChangedCallBack);

            Projects = new ObservableCollection<Project>();

            Init();
        }

        /// <summary>
        /// Function that resets the <see cref="filterTypeList"/> and fills the list of <see cref="Project"/>s.
        /// </summary>
        public void Init()
        {
            filterTypeIndex = 1;
            filterTypeIndex = 0;

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                Projects.Clear();
                foreach (var project in Database.Projects.ToList())
                    Projects.Add(project);
            }
        }

        /// <summary>
        /// Function that iterates over all entries in <see cref="filterTree"/> and sets their 'IsSelected' property to false.
        /// </summary>
        public void ClearFilterTreeSelectedItem()
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

        /// <summary>
        /// Function that expand/collaps the filter panel.
        /// </summary>
        public void _ExpandFilterList()
        {
            measurementFilterPanelVis = !measurementFilterPanelVis;
        }

        /// <summary>
        /// Function that populates the filter tree with the selected filterType from the <see cref="filterTypeList"/>.
        /// </summary>
        /// <param name="filterType">The string of the selected filterType.</param>
        public void FillFilterList(string filterType)
        {
            trace.Value.TraceEvent(TraceEventType.Information, 0, "New selected filterType: '" + filterType + "'");

            ClearFilterTreeSelectedItem();

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

                    using (DatabaseDataContext Database = MyGlobals.Database)
                    {
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
                    filterTree.Items.Add(new FilterClass() { Name = "All", Type = "All" });

                    using (DatabaseDataContext Database = MyGlobals.Database)
                    {
                        List<int> allChannels = Database.Measurements.Select(x => x.Channel).Distinct().ToList();

                        foreach (int Channel in allChannels)
                        {
                            filterTree.Items.Add(new FilterClass() { Name = Channel.ToString(), Type = "Channel", Channel = Channel });
                        }
                    }
                    break;

                case "Sample":
                    filterTree.Items.Add(new FilterClass() { Name = "All", Type = "All" });

                    using (DatabaseDataContext Database = MyGlobals.Database)
                    {
                        //List<string> allSampleNames = (from sample in Database.Samples select sample.SampleName).Distinct().ToList();
                        List<string> allSampleNames = Database.Samples.Select(x => x.SampleName).ToList();

                        foreach (string sampleName in allSampleNames)
                        {
                            filterTree.Items.Add(new FilterClass() { Name = sampleName, Type = "Sample", SampleName = sampleName });
                        }
                    }
                    break;

                default:
                    trace.Value.TraceEvent(TraceEventType.Warning, 0, "No action found for filterType: '" + filterType +"'");
                    break;
            }
            if (EventNewFilter != null) EventNewFilter(new List<int>());
        }

        /// <summary>
        /// Function that sends an event containing a list of <see cref="Measurement.MeasurementID"/>s generated from the selected filter.
        /// </summary>
        /// <param name="filter">The filter which has been selected in the <see cref="filterTree"/>.</param>
        public void NewFilterSelected(FilterClass filter)
        {
            if (selectedFilter == null) return;

            trace.Value.TraceEvent(TraceEventType.Information, 0, "New selected filter: '" + filter.Name + " (" + filter.Type + ", " + filter.SubType + ")'");

            using (DatabaseDataContext Database = MyGlobals.Database)
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

        /// <summary>
        /// Function that sends an event containing a list of <see cref="Measurement.MeasurementID"/>s generated from the selected <see cref="Project"/>.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> which has been selected.</param>
        public void SelectProject(Project project)
        {
            if (project == null) return;

            trace.Value.TraceEvent(TraceEventType.Information, 0, "New selected project: '" + project.ProjectName + "'");

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                List<int> MeasurementIDList = Database.Measurement_Projects.Where(x => x.ProjectID == project.ProjectID).Select(x => x.MeasurementID).ToList();
                // Send event (to SpectraListView...)
                if (EventNewFilter != null) EventNewFilter(MeasurementIDList);
            }
        }

        /// <summary>
        /// Function that adds a new <see cref="Project"/> to the <see cref="Projects"/> list.
        /// </summary>
        public void _NewProjectCommand()
        {
            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new project name:", "");
            if (inputDialog.ShowDialog() == true)
                if (inputDialog.Answer != "")
                    using (DatabaseDataContext Database = MyGlobals.Database)
                    {
                        Project newProject = new Project { ProjectName = inputDialog.Answer };
                        Database.Projects.InsertOnSubmit(newProject);
                        Database.SubmitChanges();
                        Projects.Add(newProject);

                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Created new project: '" + newProject.ProjectName + "'");
                    }
        }

        /// <summary>
        /// Function that renames the selected <see cref="Project"/>.
        /// </summary>
        public void _RenameProjectCommand()
        {
            if (SelectedProject == null) return;

            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new project name:", SelectedProject.ProjectName);
            if (inputDialog.ShowDialog() == true)
                if (inputDialog.Answer != "")
                {
                    using (DatabaseDataContext Database = MyGlobals.Database)
                    {
                        Project renamedProject = Database.Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID);
                        string OldName = renamedProject.ProjectName;
                        renamedProject.ProjectName = inputDialog.Answer;
                        Database.SubmitChanges();

                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Project '" + OldName + "' renamed to '" + renamedProject.ProjectName + "'");
                    }

                    Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID).ProjectName = inputDialog.Answer;
                }
        }

        /// <summary>
        /// Function that deletes the selected <see cref="Project"/>.
        /// </summary>
        public void _DeleteProjectCommand()
        {
            if (SelectedProject == null) return;

            MessageBoxResult messageBox = MessageBox.Show("Are you shure to delete the selected project?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (messageBox == MessageBoxResult.Yes)
            {
                using (DatabaseDataContext Database = MyGlobals.Database)
                {
                    Database.Measurement_Projects.DeleteAllOnSubmit(Database.Measurement_Projects.Where(x => x.ProjectID == SelectedProject.ProjectID));
                    Project deletedProject = Database.Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID);
                    Database.Projects.DeleteOnSubmit(deletedProject);
                    Database.SubmitChanges();

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Project '" + deletedProject.ProjectName + "' was deleted");
                }

                Projects.Remove(Projects.FirstOrDefault(x => x.ProjectID == SelectedProject.ProjectID));
                if (EventNewFilter != null) EventNewFilter(new List<int>());
            }
        }

        /// <summary>
        /// Function that adds the selected <see cref="Measurement"/>s to a specified <see cref="Project"/>.
        /// </summary>
        public void _AddMeasurementCommand()
        {
            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();

            if (selectedMeasurementIDs.Count() == 0) return;

            Views.Utils.ProjectSelector projectSelector = new Views.Utils.ProjectSelector();
            if (projectSelector.ShowDialog() == true)
            {
                using (DatabaseDataContext Database = MyGlobals.Database)
                {
                    List<Measurement_Project> newMeasurement_Projects = new List<Measurement_Project>();

                    foreach (int ID in selectedMeasurementIDs)
                    {
                        newMeasurement_Projects.Add(new Measurement_Project { MeasurementID = ID, ProjectID = projectSelector.SelectedProject.ProjectID });
                    }

                    Database.Measurement_Projects.InsertAllOnSubmit(newMeasurement_Projects);
                    Database.SubmitChanges();

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurements " + string.Join(", ", selectedMeasurementIDs) + " added to Project '" + projectSelector.SelectedProject.ProjectName + "'");
                }
            }
        }

        /// <summary>
        /// Function that removes the selected <see cref="Measurement"/>s from the selected <see cref="Project"/>.
        /// </summary>
        public void _RemoveMeasurementCommand()
        {
            if (SelectedProject == null) return;

            List<int> selectedMeasurementIDs = SimpleIoc.Default.GetInstance<MeasurementListViewModel>().MeasurementList.Where(x => x.Selected == true).Select(y => y.Measurement.MeasurementID).ToList();

            if (selectedMeasurementIDs.Count() == 0) return;

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                Database.Measurement_Projects.DeleteAllOnSubmit(Database.Measurement_Projects.Where(x => selectedMeasurementIDs.Contains(x.MeasurementID)).Where(y => y.ProjectID == SelectedProject.ProjectID));
                Database.SubmitChanges();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurements " + string.Join(", ", selectedMeasurementIDs) + " removed from Project '" + SelectedProject.ProjectName + "'");
            }

            SelectProject(SelectedProject);
        }
    }
}

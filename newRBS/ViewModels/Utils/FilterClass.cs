using GalaSoft.MvvmLight;
using System;

namespace newRBS.ViewModels.Utils
{
    /// <summary>
    /// Class that represents an item in a TreeView. It contains properties for the selection status and the corresponging filter.
    /// </summary>
    public class FilterClass : ViewModelBase
    {
        private bool _IsSelected = false;
        public bool IsSelected { get { return _IsSelected; } set { _IsSelected = value; RaisePropertyChanged(); } }
        public string Name { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Channel { get; set; }
        public string SampleName { get; set; }
        public AsyncObservableCollection<FilterClass> Children { get; set; }
    }
}

using GalaSoft.MvvmLight;
using System;

namespace newRBS.ViewModels.Utils
{
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

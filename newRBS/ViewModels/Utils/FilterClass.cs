using GalaSoft.MvvmLight;

namespace newRBS.ViewModels.Utils
{
    public class FilterClass : ViewModelBase
    {
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

using GalaSoft.MvvmLight;


namespace newRBS.ViewModels.Utils
{
    public class Element : ViewModelBase
    {
        public string DisplayName
        { get { return (AtomicNumber.ToString() + " - " + ShortName + " - " + LongName); } }

        public string LongName { get; set; }
        public string ShortName { get; set; }
        public int AtomicNumber { get; set; }
        public double AtomicMass { get; set; }
    }
}

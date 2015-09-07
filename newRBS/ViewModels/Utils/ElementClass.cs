using GalaSoft.MvvmLight;


namespace newRBS.ViewModels.Utils
{
    /// <summary>
    /// Class that descripes the main properties an element from the periodec system.
    /// </summary>
    public class ElementClass : ViewModelBase
    {
        public string DisplayName
        { get { return (AtomicNumber.ToString() + " - " + ShortName + " - " + LongName); } }

        public string LongName { get; set; }
        public string ShortName { get; set; }
        public int AtomicNumber { get; set; }
        public double AtomicMass { get; set; }
    }
}

using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace newRBS.ViewModels.Utils
{
    /// <summary>
    /// Class providing an item for collections, that consists of a <see cref="Name"/> an integer <see cref="Value"/>.
    /// </summary>
    public class NameValueClass : INotifyPropertyChanged
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { _Name = value; OnPropertyChanged(); }
        }

        private int _Value;
        public int Value
        {
            get { return _Value; }
            set { _Value = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Constructor of the class, initializing both, the <see cref="Name"/> and <see cref="Value"/> of the class.
        /// </summary>
        /// <param name="Name">The name of the property.</param>
        /// <param name="Value">The value (int) of the property.</param>
        public NameValueClass(string Name, int Value)
        {
            this.Name = Name;
            this.Value = Value;
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
}

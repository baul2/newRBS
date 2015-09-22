using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Data;

namespace newRBS.Views
{
    /// <summary>
    /// Interaction logic for SpectraListView.xaml
    /// </summary>
    public partial class SpectraListView : UserControl
    {
        public SpectraListView()
        {
            InitializeComponent();
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    public class AtomicNumberToShortNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                          System.Globalization.CultureInfo culture)
        {
            using (Database.DatabaseDataContext Database = MyGlobals.Database)
            {
                return Database.Elements.FirstOrDefault(x => x.AtomicNumber == (int)value).ShortName;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                        System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}

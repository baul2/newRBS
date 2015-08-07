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
using GalaSoft.MvvmLight.Ioc;

namespace newRBS.GUI
{
    /// <summary>
    /// Interaction logic for Spectra.xaml
    /// </summary>
    public partial class Spectra : UserControl
    {
        public ViewModel.SpectraViewModel spectraViewModel;

        public Spectra()
        {
            InitializeComponent();
            spectraViewModel = SimpleIoc.Default.GetInstance<ViewModel.SpectraViewModel>();
        }

        public void SpectraFilterTreeChanged(object sender, RoutedPropertyChangedEventArgs<Object> e)
        {
            spectraViewModel.spectraFilterClass.selectedFilter = (string) e.NewValue;
        }
    }
}

using System;
using System.IO;
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
using OxyPlot;
using GalaSoft.MvvmLight.Ioc;

namespace newRBS.Views
{
    public partial class MainView : Window
    {
        private Models.CAEN_x730 cAEN_x730;

        public MainView()
        {
            InitializeComponent();
        }
    }
}

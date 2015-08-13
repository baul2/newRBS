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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private Models.CAEN_x730 cAEN_x730;

        public MainView()
        {
            InitializeComponent();
            Closing += MainView_Closing;
        }

        private void MainView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (SimpleIoc.Default.ContainsCreated<Models.CAEN_x730>() == true)
            {
                cAEN_x730 = SimpleIoc.Default.GetInstance<Models.CAEN_x730>();
                cAEN_x730.Close();
            }
        }
    }
}

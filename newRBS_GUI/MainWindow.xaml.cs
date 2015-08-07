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

namespace newRBS.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Models.CAEN_x730 cAEN_x730;

        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<Models.CAEN_x730>();
            cAEN_x730.Close();
        }


        private void ButtonMeasure_Click(object sender, RoutedEventArgs e)
        {
            if (Measure.Visibility == System.Windows.Visibility.Collapsed)
            {
                Measure.Visibility = System.Windows.Visibility.Visible;
                (sender as Button).Content = "\u21D1 Measure Panel \u21D1";
            }
            else
            {
                Measure.Visibility = System.Windows.Visibility.Collapsed;
                (sender as Button).Content = "\u21D3 Measure Panel \u21D3";
            }
        }
    }
}

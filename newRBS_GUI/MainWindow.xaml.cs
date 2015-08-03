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


            foreach (string s in Directory.GetLogicalDrives())
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = s;
                item.Tag = s;
                //item.FontWeight = FontWeights.Normal;
                item.Items.Add(null);
                item.Expanded += new RoutedEventHandler(FolderExpanded);
                FolderTreeView.Items.Add(item);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cAEN_x730 = SimpleIoc.Default.GetInstance<Models.CAEN_x730>();
            cAEN_x730.Close();
        }

        void FolderExpanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == null)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        subitem.Tag = s;
                        //subitem.FontWeight = FontWeights.Normal;
                        subitem.Items.Add(null);
                        subitem.Expanded += new RoutedEventHandler(FolderExpanded);
                        item.Items.Add(subitem);
                    }
                }
                catch (Exception) { }
            }
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

        private void ButtonFolderTree_Click(object sender, RoutedEventArgs e)
        {
            if (FolderTree.Visibility == System.Windows.Visibility.Collapsed)
            {
                FolderTree.Visibility = System.Windows.Visibility.Visible;
                (sender as Button).Content = "\u21D1 Folder Panel \u21D1";
            }
            else
            {
                FolderTree.Visibility = System.Windows.Visibility.Collapsed;
                (sender as Button).Content = "\u21D3  Folder Panel \u21D3 ";
            }
        }
    }


    public class Employee
    {
        public Employee()
        {
            EmployeeDetails1 = new EmployeeDetails();
            EmployeeDetails1.EmpID = 123;
            EmployeeDetails1.EmpName = "ABC";
            EmployeeDetails1.Points = new List<DataPoint>
            {
                new DataPoint(0, 4),
                new DataPoint(10, 13),
                new DataPoint(20, 15),
                new DataPoint(30, 16),
                new DataPoint(40, 12),
                new DataPoint(50, 12)
            };
            EmployeeDetails2 = new EmployeeDetails();
            EmployeeDetails2.EmpID = 456;
            EmployeeDetails2.EmpName = "DEF";
        }

        public EmployeeDetails EmployeeDetails1 { get; set; }
        public EmployeeDetails EmployeeDetails2 { get; set; }
    }
    public class EmployeeDetails
    {
        public List<DataPoint> Points { get; set; }

        private int empID;
        public int EmpID
        {
            get
            {
                return empID;
            }
            set
            {
                empID = value;
            }
        }

        private string empName;
        public string EmpName
        {
            get
            {
                return empName;
            }
            set
            {
                empName = value;
            }
        }
    }
}

using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Threading;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using newRBS.ViewModels.Utils;
using newRBS.Database;

namespace newRBS.Views.Utils
{
    /// <summary>
    /// Interaktionslogik für ProjectSelector.xaml
    /// </summary>
    public partial class ProjectSelector : Window
    {
        public ObservableCollection<Project> Projects { get; set; }

        public ProjectSelector()
        {
            InitializeComponent();

            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
            {
                Projects = new ObservableCollection<Project>(Database.Projects.ToList());
            }

            if (Projects.Count() == 0) this.Close();

            cBox.ItemsSource = Projects;
            cBox.SelectedItem = Projects[0];
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public Project SelectedProject
        {
            get { return (Project)cBox.SelectedItem; }
        }
    }
}

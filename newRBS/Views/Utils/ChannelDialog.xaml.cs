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
using System.Windows.Shapes;
using System.Net;

namespace newRBS.Views.Utils
{
    public partial class ChannelDialog : Window
    {
        public ChannelDialog()
        {
            InitializeComponent();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            ChannelCombo.Focus();
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            this.Activate();
            this.Topmost = true;  // important
            this.Topmost = false; // important
            this.Focus();
        }

        public int SelectedChannel
        {
            get { return ChannelCombo.SelectedIndex; }
        }
    }
}

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

namespace newRBS.Views.Utils
{
    public class LogIn
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public partial class LogInDialog : Window
    {
        public LogInDialog()
        {
            InitializeComponent();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        public LogIn logIn
        {
            get { return new LogIn { UserName = UserNameEdit.Text, Password = PassowrdEdit.Password }; } 
        }
    }
}

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
    public partial class NewLogInDialog : Window
    {
        public NewLogInDialog(string text)
        {
            InitializeComponent();
            textLabel.Content = text;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordEdit.Password == PasswordEdit2.Password)
                this.DialogResult = true;
            else
                MessageBox.Show("Passwords are not identical!","Error");
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            UserNameEdit.Focus();
        }

        public LogIn logIn
        {
            get
            {
                return new LogIn { UserName = UserNameEdit.Text, Password = PasswordEdit.Password };
            }
        }
    }
}

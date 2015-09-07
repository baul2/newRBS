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
    public class LogIn
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string IPAdress { get; set; }
        public string Port { get; set; }
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

        private bool CheckIPValid(string strIP)
        {
            //  Split string by ".", check that array length is 4
            string[] arrOctets = strIP.Split('.');
            if (arrOctets.Length != 4)
                return false;

            //Check each substring checking that parses to byte
            byte obyte = 0;
            foreach (string strOctet in arrOctets)
                if (!byte.TryParse(strOctet, out obyte))
                    return false;

            return true;
        }

        public LogIn logIn
        {
            get
            {
                int dump;
                if (CheckIPValid(IPAddressEdit.Text) && Int32.TryParse(PortEdit.Text, out dump))
                    return new LogIn { UserName = UserNameEdit.Text, Password = PassowrdEdit.Password, IPAdress = IPAddressEdit.Text, Port = PortEdit.Text };
                else
                    return null;
            } 
        }
    }
}

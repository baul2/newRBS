using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;

namespace newRBS
{
    static class MyGlobals
    {
        //public static string ConString = "Data Source = SVRH; User ID = p4mist; Password = testtesttesttest; Initial Catalog = p4mist_db";
        public static string ConString = "";

        public static Database.DatabaseDataContext Database
        {
            get
            {
                Database.DatabaseDataContext newConnection = new Database.DatabaseDataContext(ConString);

                if (ConString == "")
                {
                    Views.Utils.LogInDialog logInDialog = new Views.Utils.LogInDialog();

                    while (logInDialog.ShowDialog() == true)
                    {
                        ConString = "Data Source = SVRH; User ID = " + logInDialog.logIn.UserName + "; Password = " + logInDialog.logIn.Password + "; Initial Catalog = " + logInDialog.logIn.UserName + "_db";
                        newConnection = new Database.DatabaseDataContext(ConString);
                        if (!newConnection.DatabaseExists())
                        {
                            MessageBox.Show("Please enter a valid username/password combination!", "Connection error!");
                            logInDialog = new Views.Utils.LogInDialog();
                        }
                        else
                            break;
                    }
                    if (!newConnection.DatabaseExists())
                    {
                        Console.WriteLine("close");
                        Environment.Exit(0);
                        return null;
                    }
                }
                return newConnection;
            }
        }
    }
}

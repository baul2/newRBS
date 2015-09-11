using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using System.Reflection;

namespace newRBS
{
    static class MyGlobals
    {
        public static string ConString = "";

        public static Database.DatabaseDataContext Database
        {
            get
            {
                Database.DatabaseDataContext newConnection = new Database.DatabaseDataContext(ConString);
                newConnection.CommandTimeout = 10;

                if (ConString == "")
                {
                    Views.Utils.LogInDialog logInDialog = new Views.Utils.LogInDialog("Please enter your login data and the connection settings!");

                    while (logInDialog.ShowDialog() == true)
                    {
                        ConString = "Data Source = " + logInDialog.logIn.IPAdress + "," + logInDialog.logIn.Port + "; Network Library=DBMSSOCN; User ID = " + logInDialog.logIn.UserName + "; Password = " + logInDialog.logIn.Password + "; Initial Catalog = " + logInDialog.logIn.UserName + "_db";
                        newConnection = new Database.DatabaseDataContext(ConString);
                        newConnection.CommandTimeout = 10;

                        if (!newConnection.DatabaseExists())
                        {
                            MessageBox.Show("Please enter a valid username/password combination!", "Connection error!");
                            logInDialog = new Views.Utils.LogInDialog("Please enter your login data and the connection settings!");
                        }
                        else
                            break;
                    }
                    if (!newConnection.DatabaseExists())
                    {
                        Console.WriteLine("close");
                        SimpleIoc.Default.GetInstance<ViewModels.MainViewModel>()._CloseProgramCommand();
                        return null;
                    }
                }
                return newConnection;
            }
        }

        public static void GenericDetach<T>(T entity) where T : class
        {
            foreach (PropertyInfo pi in entity.GetType().GetProperties())
            {
                if (pi.GetCustomAttributes(typeof(System.Data.Linq.Mapping.AssociationAttribute), false).Length > 0)
                {
                    // Property is associated to another entity
                    Type propType = pi.PropertyType;
                    // Invoke Empty contructor (set to default value)
                    ConstructorInfo ci = propType.GetConstructor(new Type[0]);
                    pi.SetValue(entity, ci.Invoke(null), null);
                }
            }
        }
    }
}

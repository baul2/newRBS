using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data;
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
using System.Data.SqlClient;

namespace newRBS.ViewModels
{
    public class MyUser : ViewModelBase
    {
        public string UserName { get; set; }
        public string LoginName { get; set; }
        public string Database { get; set; }
    }

    public class UserEditorViewModel : ViewModelBase
    {
        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ObservableCollection<MyUser> Users { get; set; }

        private MyUser _SelectedUser;
        public MyUser SelectedUser
        {
            get { return _SelectedUser; }
            set { _SelectedUser = value; RaisePropertyChanged(); }
        }

        public ICommand AddUserCommand { get; set; }
        public ICommand RemoveUserCommand { get; set; }

        private Server server;
        private SqlConnection sqlConnection;

        private Views.Utils.LogIn AdminLogIn;

        public UserEditorViewModel(Views.Utils.LogIn adminLogIn)
        {
            AddUserCommand = new RelayCommand(() => _AddUserCommand(), () => true);
            RemoveUserCommand = new RelayCommand(() => _RemoveUserCommand(), () => true);

            AdminLogIn = adminLogIn;

            sqlConnection = new SqlConnection(@"Data Source = " + adminLogIn.IPAdress + ", " + adminLogIn.Port + "; Network Library = DBMSSOCN; User ID = " + adminLogIn.UserName + "; Password = " + adminLogIn.Password + "; ");
            //ServerConnection serverConnection = new ServerConnection(sqlConnection);
            ServerConnection serverConnection = new ServerConnection(adminLogIn.IPAdress + ", " + adminLogIn.Port); 
            server = new Server(serverConnection);
            server.ConnectionContext.LoginSecure = false;
            server.ConnectionContext.Login = adminLogIn.UserName;
            server.ConnectionContext.Password = adminLogIn.Password;

            try
            {
                Console.WriteLine(server.Information.Version);   // connection is established
            }
            catch (ConnectionFailureException e)
            {
                MessageBox.Show("Login error: " + e.Message, "Error");
                DialogResult = false;
                return;
            }

            Users = new ObservableCollection<MyUser>();

            FillUserList();
        }

        public void FillUserList()
        {
            Users.Clear();
            foreach (Microsoft.SqlServer.Management.Smo.Database db in server.Databases)
            {
                if (!db.Name.Contains("_db")) continue; 

                //Run the EnumLoginMappings method and return details of database user-login mappings to a DataTable object variable. 
                DataTable d;
                d = db.EnumLoginMappings();
                foreach (DataRow r in d.Rows)
                {
                    MyUser myUser = new MyUser { Database = db.Name };
                    foreach (DataColumn c in r.Table.Columns)
                    {
                        switch (c.ColumnName)
                        {
                            case "UserName": { myUser.UserName = (string)r[c]; break; }
                            case "LoginName": { myUser.LoginName = (string)r[c]; break; }
                            default: Console.WriteLine("unknown"); break;
                        }
                    }
                    if (!myUser.LoginName.Contains("admin"))
                        Users.Add(myUser);
                }
            }
        }

        private void _AddUserCommand()
        {
            Views.Utils.NewLogInDialog newLogInDialog = new Views.Utils.NewLogInDialog("Please enter the new user login data!");

            if (newLogInDialog.ShowDialog() == true && newLogInDialog.logIn.Password != "" && newLogInDialog.logIn.Password != "")
            {
                // Create database
                Microsoft.SqlServer.Management.Smo.Database db = new Microsoft.SqlServer.Management.Smo.Database(server, newLogInDialog.logIn.UserName + "_db");
                db.Create();

                // Create login & user
                Login login = new Login(server, newLogInDialog.logIn.UserName);
                login.LoginType = LoginType.SqlLogin;
                login.Create(newLogInDialog.logIn.Password);

                User user = new User(db, newLogInDialog.logIn.UserName);
                user.Login = newLogInDialog.logIn.UserName;
                user.Create();

                // Creating database permission Sets
                DatabasePermissionSet databasePermissionSet = new DatabasePermissionSet();
                databasePermissionSet.Add(DatabasePermission.Insert);
                databasePermissionSet.Add(DatabasePermission.Update);
                databasePermissionSet.Add(DatabasePermission.Select);
                databasePermissionSet.Add(DatabasePermission.Delete);

                // Granting Database Permission Sets to Roles
                db.Grant(databasePermissionSet, newLogInDialog.logIn.UserName);

                // Copy database
                Console.WriteLine(AdminLogIn.UserName + "_db");
                Microsoft.SqlServer.Management.Smo.Database adminDB = server.Databases[AdminLogIn.UserName + "_db"];
                Transfer transfer = new Transfer(adminDB);

                transfer.CopyAllTables = true;
                transfer.Options.WithDependencies = true;
                transfer.Options.DriAll = true;
                transfer.DestinationDatabase = newLogInDialog.logIn.UserName + "_db";
                transfer.DestinationServer = server.Name;
                transfer.DestinationLoginSecure = false;
                transfer.DestinationLogin = AdminLogIn.UserName;
                transfer.DestinationPassword = AdminLogIn.Password;
                transfer.CopySchema = true;
                transfer.TransferData();

                FillUserList();
            }
        }

        private void _RemoveUserCommand()
        {
            if (SelectedUser == null) return;

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected user and the corresponding database?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                server.KillDatabase(SelectedUser.UserName + "_db");

                SqlCommand cmd = new SqlCommand("DROP LOGIN ["+SelectedUser.UserName+"];", sqlConnection);
                // In addition, you can use this command:
                // EXEC sp_droplogin 'someuser';

                try
                {
                    sqlConnection.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 15151)
                        Console.WriteLine("Login does not exist.");
                    else if (ex.Number == 15007)
                        Console.WriteLine("Login already logged on.");
                    else
                        Console.WriteLine("{0}: {1}", ex.Number, ex.Message);
                }
                finally
                {
                    FillUserList();
                    sqlConnection.Close();
                }
            }
        }
    }
}

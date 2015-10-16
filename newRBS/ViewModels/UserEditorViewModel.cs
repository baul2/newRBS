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
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.UserEditorView"/>. They can add or remove users and their corresponding databases.
    /// </summary>
    public class UserEditorViewModel : ViewModelBase
    {
        public ICommand AddUserCommand { get; set; }
        public ICommand RemoveUserCommand { get; set; }
        public ICommand DownloadDatabaseCommand { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        /// <summary>
        /// List of <see cref="MyUser"/> for the datagrid.
        /// </summary>
        public ObservableCollection<MyUser> Users { get; set; }

        private MyUser _SelectedUser;
        public MyUser SelectedUser
        {
            get { return _SelectedUser; }
            set { _SelectedUser = value; RaisePropertyChanged(); }
        }

        private Server server;
        private SqlConnection sqlConnection;

        private Views.Utils.LogIn AdminLogIn;

        /// <summary>
        /// Constructor of the class. Sets up the commands, variables and the sql-connection.
        /// </summary>
        /// <param name="adminLogIn">Login data of the admin account.</param>
        public UserEditorViewModel(Views.Utils.LogIn adminLogIn)
        {
            AddUserCommand = new RelayCommand(() => _AddUserCommand(), () => true);
            RemoveUserCommand = new RelayCommand(() => _RemoveUserCommand(), () => true);
            DownloadDatabaseCommand = new RelayCommand(() => _BackupDatabaseCommand(), () => true);

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
                string temp = server.Information.Version.ToString();   // connection is established
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

        /// <summary>
        /// Funtion that retrieves the users from the database and fills <see cref="Users"/>.
        /// </summary>
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
                            default: break;
                        }
                    }
                    if (!myUser.LoginName.Contains("admin"))
                        Users.Add(myUser);
                }
            }
        }

        /// <summary>
        /// Function that adds a new user with a new database (copied from admin account).
        /// </summary>
        public void _AddUserCommand()
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

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Created new User '" + user.Login + "'");

                // Creating database permission Sets
                DatabasePermissionSet databasePermissionSet = new DatabasePermissionSet();
                databasePermissionSet.Add(DatabasePermission.Insert);
                databasePermissionSet.Add(DatabasePermission.Update);
                databasePermissionSet.Add(DatabasePermission.Select);
                databasePermissionSet.Add(DatabasePermission.Delete);

                // Granting Database Permission Sets to Roles
                db.Grant(databasePermissionSet, newLogInDialog.logIn.UserName);

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Granted permissions to User '" + user.Login + "'");

                // Copy database
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

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Copied default database to User '" + user.Login + "'");

                FillUserList();
            }
        }

        /// <summary>
        /// Function that removes a user and the corresponding database.
        /// </summary>
        public void _RemoveUserCommand()
        {
            if (SelectedUser == null) return;

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected user and the corresponding database?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                server.KillDatabase(SelectedUser.UserName + "_db");

                SqlCommand cmd = new SqlCommand("DROP LOGIN [" + SelectedUser.UserName + "];", sqlConnection);
                // In addition, you can use this command:
                // EXEC sp_droplogin 'someuser';

                try
                {
                    sqlConnection.Open();
                    cmd.ExecuteNonQuery();

                    trace.Value.TraceEvent(TraceEventType.Information, 0, "Deleted User '" + SelectedUser.UserName + "'");
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 15151)
                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Can't deleted User '" + SelectedUser.UserName + "' - user does not exist.");
                    else if (ex.Number == 15007)
                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Can't deleted User '" + SelectedUser.UserName + "' - user is still logged in.");
                    else
                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Can't deleted User '" + SelectedUser.UserName + "' - " + ex.Number + ": " + ex.Message);
                }
                finally
                {
                    FillUserList();
                    sqlConnection.Close();
                }
            }
        }

        /// <summary>
        /// Function that performes a backup of the 'test_db' database. Needs still more work.
        /// </summary>
        public void _BackupDatabaseCommand()
        {
            string script = "USE test_db; GO BACKUP DATABASE test_db TO DISK = 'test_db.Bak' WITH FORMAT, MEDIANAME = 'Z_SQLServerBackups', NAME = 'Full Backup of test_db'; GO ";

            try
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand())
                {
                    command.Connection = sqlConnection;

                    var scripts = script.Split(new string[] { " GO " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var splitScript in scripts)
                    {
                        Console.WriteLine(splitScript);
                        command.CommandText = splitScript;
                        command.ExecuteNonQuery();
                    }
                }
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        /// <summary>
        /// Function that splits a 'Microsoft SQL Server Management Studio' script in individual sql commands.
        /// </summary>
        /// <param name="sqlScript">Script that has to be splitted at every 'GO'.</param>
        /// <returns>List of sql commands.</returns>
        private static IEnumerable<string> SplitSqlStatements(string sqlScript)
        {
            // Split by "GO" statements
            var statements = Regex.Split(
                    sqlScript,
                    @"^\s*GO\s* ($ | \-\- .*$)",
                    RegexOptions.Multiline |
                    RegexOptions.IgnorePatternWhitespace |
                    RegexOptions.IgnoreCase);

            // Remove empties, trim, and return
            return statements
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim(' ', '\r', '\n'));
        }
    }
}

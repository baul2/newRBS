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
using System.Diagnostics;
using System.IO;
using OxyPlot;

namespace newRBS
{
    /// <summary>
    /// Class that contains globally needed variables and functions
    /// </summary>
    static class MyGlobals
    {
        public static bool OffLineMode = true;

        /// <summary>
        /// The interval of the <see cref="Models.MeasureSpectra.MeasureSpectraWorker"/>.
        /// </summary>
        public static double MeasurementWorkerInterval = 1000; //ms

        /// <summary>
        /// The interval of the <see cref="Models.MeasureWaveform.MeasureWaveformWorker(int)"/>.
        /// </summary>
        public static double WaveformWorkerInterval = 500; //ms

        /// <summary>
        /// The interval of <see cref="ViewModels.MeasurementListViewModel.OfflineUpdateWorker(int)/> and <see cref="ViewModels.MeasurementPlotViewModel.OfflineUpdateWorker(int)"/>.
        /// </summary>
        public static double OfflineUpdateWorkerInterval = 1000; //ms

        /// <summary>
        /// The time between data points in the plot of the charge/counts per second (<see cref="ViewModels.MeasurementPlotViewModel.TimePlotModel"/>.
        /// </summary>
        public static double TimePlotIntervall = 30; //s

        /// <summary>
        /// The plot data of <see cref="ViewModels.MeasurementPlotViewModel.TimePlotModel"/>.
        /// </summary>
        public static List<TimeSeriesEvent> Charge_CountsOverTime { get; set; }

        /// <summary>
        /// Is 'true' when the measurement equipment is accessible and 'false' in offline mode.
        /// </summary>
        public static bool CanMeasure { get; set; }

        /// <summary>
        /// Determines the actions of mouse buttons in the various OxyPlots.
        /// </summary>
        public static PlotController myController { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        /// <summary>
        /// Stores the SQL connection string for the currently logged in user.
        /// </summary>
        public static string ConString = "";

        /// <summary>
        /// Returns a new instance of <see cref="Database.DatabaseDataContext"/>. If the <see cref="ConString"/> is empty, prompts for new login data.
        /// </summary>
        public static Database.DatabaseDataContext Database
        {
            get
            {
                Database.DatabaseDataContext newConnection = new Database.DatabaseDataContext(ConString);
                newConnection.CommandTimeout = 10;

                if (ConString == "") // Get new login data
                {
                    Views.Utils.LogInDialog logInDialog = new Views.Utils.LogInDialog("Please enter your login data and the connection settings!");

                    while (logInDialog.ShowDialog() == true) 
                    {
                        ConString = "Data Source = " + logInDialog.logIn.IPAdress + "," + logInDialog.logIn.Port + "; Network Library=DBMSSOCN; User ID = " + logInDialog.logIn.UserName + "; Password = " + logInDialog.logIn.Password + "; Initial Catalog = " + logInDialog.logIn.UserName + "_db";
                        newConnection = new Database.DatabaseDataContext(ConString);
                        newConnection.CommandTimeout = 10;

                        if (!newConnection.DatabaseExists()) // User + password combination isn't valid
                        {
                            MessageBox.Show("Please enter a valid username/password combination!", "Connection error!");
                            logInDialog = new Views.Utils.LogInDialog("Please enter your login data and the connection settings!");
                        }
                        else
                            break;
                    }
                    if (!newConnection.DatabaseExists())
                    {
                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Connection problem");
                        SimpleIoc.Default.GetInstance<ViewModels.MainViewModel>()._CloseProgramCommand(null);
                        return null;
                    }
                    trace.Value.TraceEvent(TraceEventType.Information, 0, "User '" + logInDialog.logIn.UserName + "' logged in");
                }
                return newConnection;
            }
        }

        /// <summary>
        /// Function that detaches an entity of the DataContext.
        /// </summary>
        /// <typeparam name="T">Type of the entity.</typeparam>
        /// <param name="entity">The entity which shall be detached.</param>
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

        /// <summary>
        /// Function that calculates the kinematic factor k.
        /// </summary>
        /// <param name="IncomingIonMass">Mass of the incoming ion in [u].</param>
        /// <param name="TargetAtomMass">Mass of the target atom in [u].</param>
        /// <param name="ThetaDegree">Angle of the scatterin process in [°].</param>
        /// <returns></returns>
        public static double KineFak(double IncomingIonMass, double TargetAtomMass, double ThetaDegree)
        {
            double Theta = ThetaDegree / 360.0 * 2.0 * Math.PI;

            double k = Math.Pow((Math.Pow(1.0 - Math.Pow(IncomingIonMass * Math.Sin(Theta) / TargetAtomMass, 2.0), 0.5) + IncomingIonMass * Math.Cos(Theta) / TargetAtomMass) / (1.0 + IncomingIonMass / TargetAtomMass), 2.0);

            return k;
        }
    }
}

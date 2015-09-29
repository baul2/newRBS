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
    class MyTextWriterTraceListener : TextWriterTraceListener
    {
        public MyTextWriterTraceListener(string fileName) : base(fileName)
        {
        }

        public override void Write(string message)
        {
            base.Write(String.Format("[{0}]:{1}", DateTime.Now, message));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            WriteLine(string.Format("{0}, {1}, {2}, {3}", DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]"), eventType, source, message.Replace("\r", string.Empty).Replace("\n", string.Empty)));
        }
    }

    public static class TraceSources
    {
        private static MyTextWriterTraceListener textWriterTraceListener = new MyTextWriterTraceListener("Logs/LogStart_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        public static TraceSource Create(string sourceName)
        {
            var source = new TraceSource(sourceName);

            // Console listemer
            Essential.Diagnostics.ColoredConsoleTraceListener listener1 = new Essential.Diagnostics.ColoredConsoleTraceListener();
            listener1.Template = "{DateTime:'['HH':'mm':'ss'.'fff']'}, {EventType}, " + sourceName + ", {Message}{Data}";
            listener1.ConvertWriteToEvent = true;
            listener1.Filter = new EventTypeFilter(SourceLevels.All);
            source.Listeners.Add(listener1);

            // Log file listener
            string path = "Logs/";
            DirectoryInfo di = Directory.CreateDirectory(path);
            MyTextWriterTraceListener listener2 = textWriterTraceListener;
            listener2.Filter = new EventTypeFilter(SourceLevels.Information);
            Trace.AutoFlush = true;
            source.Listeners.Add(listener2);

            source.Switch.Level = SourceLevels.All;
            return source;
        }
    }

    static class MyGlobals
    {
        public static PlotController myController { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

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
                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Connection problem");
                        SimpleIoc.Default.GetInstance<ViewModels.MainViewModel>()._CloseProgramCommand();
                        return null;
                    }
                    trace.Value.TraceEvent(TraceEventType.Information, 0, "User '" + logInDialog.logIn.UserName + "' logged in");
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

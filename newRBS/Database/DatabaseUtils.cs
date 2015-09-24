using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Globalization;
using Microsoft.Win32;
using System.Windows;
using System.Xml.Serialization;
using OxyPlot.Wpf;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;
using System.Reflection;

namespace newRBS.Database
{
    /// <summary>
    /// Class providing utilities to manage the <see cref="Measurement"/>s inside the MS SQL Server database (<see cref="DatabaseDataContext"/>).
    /// </summary>
    public static class DatabaseUtils
    {
        public delegate void EventHandlerMeasurement(Measurement measurement);
        public static event EventHandlerMeasurement EventMeasurementNew;
        public static event EventHandlerMeasurement EventMeasurementUpdate;
        public static event EventHandlerMeasurement EventMeasurementRemove;

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        /// <summary>
        /// Function that sends an event (<see cref="EventMeasurementNew"/>) if new <see cref="Measurement"/> has been added to the database. The event argument is the new measurement.
        /// </summary>
        /// <param name="measurement">The measurement which has been added to the database.</param>
        public static void SendMeasurementNewEvent(Measurement measurement)
        {
            if (EventMeasurementNew != null)
            {
                EventMeasurementNew(measurement);
            }
        }

        /// <summary>
        /// Function that sends an event (<see cref="EventMeasurementUpdate"/>) if a <see cref="Measurement"/> in the database has been modified. The event argument is the modified measurement.
        /// </summary>
        /// <param name="measurement">The measurement which has been modified.</param>
        public static void SendMeasurementUpdateEvent(Measurement measurement)
        {
            if (EventMeasurementUpdate != null)
            {
                EventMeasurementUpdate(measurement);
            }
        }

        /// <summary>
        /// Function that sends an event (<see cref="EventMeasurementRemove"/>) if a <see cref="Measurement"/> in the database has been removed. The event argument is the removed measurement.
        /// </summary>
        /// <param name="measurement">The measurement which has been removed.</param>
        public static void SendMeasurementRemoveEvent(Measurement measurement)
        {
            if (EventMeasurementRemove != null)
            {
                EventMeasurementRemove(measurement);
            }
        }

        /// <summary>
        /// Funtion that asks for a new sample name and adds it to the database.
        /// </summary>
        /// <returns>The SampleID of the sample added to the database.</returns>
        public static int? AddNewSample()
        {
            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new sample name:", "");
            if (inputDialog.ShowDialog() == true)
            {
                if (inputDialog.Answer == "") return null;

                return AddNewSample(inputDialog.Answer);
            }
            else
                return null;
        }

        /// <summary>
        /// Function that adds a new sample to the database.
        /// </summary>
        /// <param name="SampleName">The name of the new sample.</param>
        /// <returns>The SampleID of the sample added to the database.</returns>
        public static int? AddNewSample(string SampleName)
        {
            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                Sample sample = Database.Samples.FirstOrDefault(x => x.SampleName == SampleName);

                if (sample != null)
                {
                    trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't create new sample: sample already exists");

                    MessageBoxResult result = MessageBox.Show("Sample already exists in database!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                    return sample.SampleID;
                }

                // New sample
                Sample newSample = new Sample();
                newSample.SampleName = SampleName;
                newSample.MaterialID = 1;

                Database.Samples.InsertOnSubmit(newSample);
                Database.SubmitChanges();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "New sample '" + newSample.SampleName + "' created");

                return newSample.SampleID;
            }
        }

        /// <summary>
        /// Function that exports several <see cref="Measurement"/>s to a file.
        /// </summary>
        /// <remarks>
        /// Can save files as ".xml" (newRBS file) or as ".dat" (Spektrenverwaltung file).
        /// </remarks>
        /// <param name="measurementIDs">IDs of the <see cref="Measurement"/>s to export.</param>
        /// <param name="FileName">Filename to export to.</param>
        public static void ExportMeasurements(List<int> measurementIDs, string FileName)
        {
            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                switch (Path.GetExtension(FileName))
                {
                    case ".xml":
                        using (TextWriter WriteFileStream = new StreamWriter(FileName))
                        {
                            XmlAttributeOverrides xOver = new XmlAttributeOverrides();

                            XmlAttributes attrs = new XmlAttributes();
                            attrs.XmlIgnore = true;

                            xOver.Add(typeof(Measurement), "Sample", attrs);
                            xOver.Add(typeof(Measurement), "SampleID", attrs);
                            xOver.Add(typeof(Measurement), "SampleRemark", attrs);
                            xOver.Add(typeof(Measurement), "SpectrumYByte", attrs);
                            xOver.Add(typeof(Measurement), "SpectrumYCalculatedByte", attrs);

                            XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Measurement>), xOver);
                            SerializerObj.Serialize(WriteFileStream, Database.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList());

                            trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurements " + string.Join(", ", measurementIDs) + " exported to a newRBS data file (.xml)");
                            break;
                        }
                    case ".dat":
                        using (TextWriter tw = new StreamWriter(FileName))
                        {

                            // Header
                            string strName = "";
                            string strData = "Date";
                            string strRemark = "Remark";
                            string strProjectile = "Projectile";
                            string strEnergy = "Energy";
                            string strScatteringAngle = "Scattering angle";
                            string strIncidentAngle = "Incident angle";
                            string strExitAngle = "Exit angle";
                            string strEnergyChannel = "Energy / Channel";
                            string strOffset = "Offset";
                            string strSolidAngle = "Solid angle";
                            string strCharge = "Charge";
                            string strRealTime = "Real time";
                            string strLiveTime = "Live time";
                            string strFWHM = "FWHM";
                            string strChannel = "Channel";

                            List<Measurement> MeasurementsToExport = Database.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList();

                            if (!MeasurementsToExport.Any())
                            { trace.Value.TraceEvent(TraceEventType.Warning, 0, "Can't save Measurement: MeasurementIDs not found"); tw.Close(); return; }

                            NumberFormatInfo point = new NumberFormatInfo { NumberDecimalSeparator = "." };

                            foreach (var measurement in MeasurementsToExport)
                            {
                                strName += "\t" + measurement.MeasurementName;
                                strData += "\t" + String.Format("{0:dd.MM.yyyy HH:mm}", measurement.StartTime); ;
                                strRemark += "\t" + measurement.Sample.SampleName;
                                strProjectile += "\t" + measurement.Isotope.AtomicNumber.ToString(point);
                                strEnergy += "\t" + measurement.IncomingIonEnergy.ToString(point);
                                strScatteringAngle += "\t" + measurement.OutcomingIonAngle.ToString(point);
                                strIncidentAngle += "\t 0.00";
                                strExitAngle += "\t" + (180 - measurement.OutcomingIonAngle).ToString(point);
                                strEnergyChannel += "\t" + measurement.EnergyCalLinear.ToString(point);
                                strOffset += "\t" + (-measurement.EnergyCalOffset / measurement.EnergyCalLinear).ToString(point);
                                strSolidAngle += "\t" + measurement.SolidAngle.ToString(point);
                                strCharge += "\t" + measurement.CurrentCharge.ToString(point);
                                strRealTime += "\t" + (measurement.CurrentDuration - new DateTime(2000, 01, 01)).TotalSeconds.ToString(point);
                                strLiveTime += "\t";
                                strFWHM += "\t";
                                strChannel += "\t" + measurement.MeasurementName;
                            }

                            tw.WriteLine(strName);
                            tw.WriteLine(strData);
                            tw.WriteLine(strRemark);
                            tw.WriteLine(strProjectile);
                            tw.WriteLine(strEnergy);
                            tw.WriteLine(strScatteringAngle);
                            tw.WriteLine(strIncidentAngle);
                            tw.WriteLine(strExitAngle);
                            tw.WriteLine(strEnergyChannel);
                            tw.WriteLine(strOffset);
                            tw.WriteLine(strSolidAngle);
                            tw.WriteLine(strCharge);
                            tw.WriteLine(strRealTime);
                            tw.WriteLine(strLiveTime);
                            tw.WriteLine(strFWHM);
                            tw.WriteLine(strChannel);

                            List<int[]> spectrumYList = MeasurementsToExport.Select(x => x.SpectrumY).ToList();

                            int maxChannelNumber = spectrumYList.Select(x => x.Count()).ToList().Max();
                            string dataLine;

                            for (int i = 0; i < maxChannelNumber; i++)
                            {
                                dataLine = i.ToString();
                                foreach (int[] spectrumY in spectrumYList)
                                {
                                    if (i < (spectrumY.Count()))
                                        dataLine += "\t" + spectrumY[i].ToString();
                                    else
                                        dataLine += "\t";
                                }
                                tw.WriteLine(dataLine);
                            }

                            trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurements " + string.Join(", ", measurementIDs) + " exported to a Spektrenverwaltung data file (.dat)");

                            break;
                        }
                }
            }
        }

        /// <summary>
        /// Loads <see cref="Measurement"/>s from a given file an returns them in a List.
        /// </summary>
        /// <remarks>
        /// Can load ".xml" (newRBS file) or ".dat" (Spektrenverwaltung file) files.
        /// </remarks>
        /// <param name="FileName">Filename of the file to load the measurements from.</param>
        /// <returns>A List of <see cref="Measurement"/>s containing the loaded measurements.</returns>
        public static List<Measurement> LoadMeasurementsFromFile(string FileName)
        {
            if (!File.Exists(FileName)) return null;

            List<Measurement> newMeasurements = new List<Measurement>();
            List<List<int>> spectraY = new List<List<int>>();

            switch (Path.GetExtension(FileName))
            {
                case ".xml":
                    {
                        XmlAttributeOverrides xOver = new XmlAttributeOverrides();

                        XmlAttributes attrs = new XmlAttributes();
                        attrs.XmlIgnore = true;

                        // Define the fields that shall not be imported
                        xOver.Add(typeof(Measurement), "Sample", attrs);
                        xOver.Add(typeof(Measurement), "SampleID", attrs);
                        xOver.Add(typeof(Measurement), "SampleRemark", attrs);
                        xOver.Add(typeof(Measurement), "SpectrumYByte", attrs);
                        xOver.Add(typeof(Measurement), "SpectrumYCalculatedByte", attrs);

                        XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Measurement>), xOver);
                        using (FileStream ReadFileStream = new FileStream(FileName, FileMode.Open))
                        {
                            newMeasurements = (List<Measurement>)SerializerObj.Deserialize(ReadFileStream);
                        }
                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurements loaded from .xml file");
                        break;
                    }
                case ".dat":
                    {
                        using (TextReader textReader = new StreamReader(FileName))
                        {
                            string line;

                            string[] lineParts = textReader.ReadLine().Split('\t');

                            int numSpectra = lineParts.Count() - 1;

                            for (int i = 0; i < numSpectra; i++)
                            {
                                newMeasurements.Add(new Measurement());
                                spectraY.Add(new List<int>());
                                newMeasurements[i].MeasurementName = lineParts[i + 1];
                            }

                            while ((line = textReader.ReadLine()) != null)
                            {
                                lineParts = line.Split('\t');

                                for (int i = 0; i < numSpectra; i++)
                                {
                                    switch (lineParts[0])
                                    {
                                        case "Date":
                                            { newMeasurements[i].StartTime = DateTime.ParseExact(lineParts[i + 1], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture); break; }
                                        case "Remark":
                                            { newMeasurements[i].SampleRemark = lineParts[i + 1]; break; }
                                        case "Projectile":
                                            {
                                                using (DatabaseDataContext Database = MyGlobals.Database)
                                                {
                                                    newMeasurements[i].IncomingIonIsotopeID = Database.Isotopes.FirstOrDefault(x=>x.AtomicNumber == Int32.Parse(lineParts[i + 1])).IsotopeID; break; }
                                            }
                                        case "Energy":
                                            { newMeasurements[i].IncomingIonEnergy = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Scattering angle":
                                            { newMeasurements[i].OutcomingIonAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Incident angle":
                                            { newMeasurements[i].IncomingIonAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Exit angle":
                                            { break; }
                                        case "Energy / Channel":
                                            { newMeasurements[i].EnergyCalLinear = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Offset":
                                            { newMeasurements[i].EnergyCalOffset = -newMeasurements[i].EnergyCalLinear * Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Solid angle":
                                            { newMeasurements[i].SolidAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Charge":
                                            { newMeasurements[i].CurrentCharge = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); newMeasurements[i].StopValue = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                                        case "Real time":
                                            { newMeasurements[i].CurrentDuration = new DateTime(2000, 01, 01) + TimeSpan.FromSeconds(Convert.ToDouble(lineParts[i + 1].Replace(".", ","))); newMeasurements[i].CurrentDuration = new DateTime(2000, 01, 01) + TimeSpan.FromSeconds(Convert.ToDouble(lineParts[i + 1].Replace(".", ","))); break; }
                                        case "Live time":
                                            { break; }
                                        case "FWHM":
                                            { break; }
                                        case "Channel":
                                            { break; }
                                        case "":
                                            { break; }
                                        default:
                                            { Console.WriteLine(line); spectraY[i].Add(Int32.Parse(lineParts[i + 1].Replace(" ", ""))); break; }
                                    }
                                }
                            }

                            for (int i = 0; i < numSpectra; i++)
                            {
                                newMeasurements[i].IsTestMeasurement = false;
                                newMeasurements[i].SpectrumY = spectraY[i].ToArray();
                                newMeasurements[i].NumOfChannels = spectraY[i].Count();
                                newMeasurements[i].Orientation = "(undefined)";
                                newMeasurements[i].StopType = "Charge (µC)";
                                newMeasurements[i].Chamber = "(undefined)";
                                newMeasurements[i].Progress = 1;
                                newMeasurements[i].EnergyCalQuadratic = 0;
                            }
                        }
                        trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurements loaded from .dat file");
                        break;
                    }
            }
            return newMeasurements;
        }

        /// <summary>
        /// Function that deletes <see cref="Measurement"/>s from the database.
        /// </summary>
        /// <param name="MeasurementIDs">IDs of the measurements to be deleted.</param>
        public static void DeleteMeasurements(List<int> MeasurementIDs)
        {
            if (MeasurementIDs.Count() == 0) return;

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                var projects = Database.Measurement_Projects.Where(x => MeasurementIDs.Contains(x.MeasurementID));
                if (projects.ToList().Count > 0)
                    if (MessageBox.Show("Selected measurements belong to projects. Delete them nevertheless?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                        return;
                Database.Measurement_Projects.DeleteAllOnSubmit(projects);
                Database.Measurements.DeleteAllOnSubmit(Database.Measurements.Where(x => MeasurementIDs.Contains(x.MeasurementID)));
                Database.SubmitChanges();
            }

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurements " + string.Join(", ", MeasurementIDs) + " deleted from the database");
        }

        /// <summary>
        /// Function that saves the current content of the MeasurementPlot to a file.
        /// </summary>
        /// <remarks>
        /// Can save a bitmap file (".png"), a vector file (".pdf" or ".svg") or data file (".dat").
        /// </remarks>
        /// <param name="FileName">File name of the file to save to.</param>
        public static void SaveMeasurementImage(string FileName)
        {
            using (FileStream fileStream = File.Create(FileName))
            {
                var plotModel = SimpleIoc.Default.GetInstance<ViewModels.MeasurementPlotViewModel>().plotModel;
                switch (Path.GetExtension(FileName))
                {
                    case ".png":
                        {
                            PngExporter.Export(plotModel, fileStream, 1000, 600, OxyColors.White);
                            break;
                        }
                    case ".pdf":
                        {
                            PdfExporter.Export(plotModel, fileStream, 1000, 600);
                            break;
                        }
                    case ".svg":
                        {
                            OxyPlot.SvgExporter.Export(plotModel, fileStream, 1000, 600, true);
                            break;
                        }
                    case ".dat":
                        {
                            if (!plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title.Contains("interval"))
                            { MessageBox.Show("Only data sets with data binning cas be saved this way. Use export instead!", "Error"); return; }

                            using (StreamWriter streamWriter = new StreamWriter(fileStream))
                            {
                                List<List<DataPoint>> pointsList = new List<List<DataPoint>>();

                                // Header
                                string LongName = "Energy";
                                string Unit = "keV";
                                string Comment = "";

                                foreach (var s in plotModel.Series)
                                {
                                    LongName += "\t Counts";
                                    Unit += "\t" + (plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title).Replace("Counts per ", "");
                                    Comment += "\t" + s.Title;

                                    pointsList.Add(((OxyPlot.Series.AreaSeries)s).Points);
                                }
                                streamWriter.WriteLine(LongName);
                                streamWriter.WriteLine(Unit);
                                streamWriter.WriteLine(Comment);

                                // Data
                                List<int> xAxis = new List<int>();

                                foreach (List<DataPoint> dataPointList in pointsList)
                                    foreach (DataPoint dataPoint in dataPointList)
                                        xAxis.Add((int)dataPoint.X);

                                xAxis = xAxis.Distinct().OrderBy(x => x).ToList();

                                string dataString;
                                NumberFormatInfo point = new NumberFormatInfo();
                                point.NumberDecimalSeparator = ".";

                                foreach (int x in xAxis)
                                {
                                    dataString = x.ToString();
                                    foreach (List<DataPoint> dataPointList in pointsList)
                                    {
                                        DataPoint d = dataPointList.FirstOrDefault(i => i.X == x);
                                        if (d.X != 0)
                                            dataString += "\t" + d.Y.ToString(point);
                                        else
                                            dataString += "\t";
                                    }
                                    streamWriter.WriteLine(dataString);
                                }
                                streamWriter.Flush(); // Added
                            }
                            break;
                        }
                }
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Plot of selected measurements exported");
            }
        }
    }
}

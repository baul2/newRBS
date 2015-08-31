using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Globalization;

namespace newRBS.Models
{
    /// <summary>
    /// Class responsible for managing a spectrum dictionary (<see cref="spectra"/>) with items \<ID, <see cref="DataSpectrum"/>\>.
    /// </summary>
    public static class DatabaseUtils
    {
        public delegate void EventHandlerMeasurement(Measurement measurement);
        public static event EventHandlerMeasurement EventMeasurementNew, EventMeasurementUpdate, EventMeasurementFinished, EventMeasurementRemove;

        private static TraceSource trace = new TraceSource("DatabaseUtils");

        public static void SendMeasurementNewEvent(Measurement measurement)
        {
            if (EventMeasurementNew != null)
            {
                EventMeasurementNew(measurement);
            }
        }

        public static void SendMeasurementUpdateEvent(Measurement measurement)
        {
            if (EventMeasurementUpdate != null)
            {
                EventMeasurementUpdate(measurement);
            }
        }

        public static void SendMeasurementRemoveEvent(Measurement measurement)
        {
            if (EventMeasurementRemove != null)
            {
                EventMeasurementRemove(measurement);
            }
        }

        /// <summary>
        /// Function that saves spectra to a file
        /// </summary>
        /// <param name="IDs">Array of IDs of the spectra to save.</param>
        /// <param name="file">Filename of the file to save the spectra to.</param>
        public static void ExportMeasurements(int[] measurementIDs, string file)
        {
            using (DatabaseDataContext db = new DatabaseDataContext(MyGlobals.ConString))
            {
                using (TextWriter tw = new StreamWriter(file))
                {

                    // Header
                    string strChannel = "Channel";
                    string strID = "ID";
                    string strStart = "StartTime";
                    string strStop = "StopTime";
                    string strECalOffset = "EnergyCalOffset";
                    string strECalSlope = "EnergyCalSlope";
                    string strName = "Name";

                    List<Measurement> MeasurementsToExport = db.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList();

                    if (!MeasurementsToExport.Any())
                    { trace.TraceEvent(TraceEventType.Warning, 0, "Can't save Measurement: MeasurementIDs not found"); tw.Close(); return; }

                    foreach (var expSpectrum in MeasurementsToExport)
                    {
                        strChannel += String.Format("\t {0}", expSpectrum.Channel);
                        strID += String.Format("\t {0}", expSpectrum.MeasurementID);
                        strStart += String.Format("\t {0}", expSpectrum.StartTime);
                        strStop += String.Format("\t {0}", expSpectrum.StopTime);
                        //strECalOffset += String.Format("\t {0:yyyy-MM-dd_HH:mm:ss}", expSpectrum.energyCalibration_.energyCalOffset);
                        //strECalSlope += String.Format("\t {0:yyyy-MM-dd_HH:mm:ss}", expSpectrum.energyCalibration_.energyCalSlope);
                        strName += String.Format("\t {0}", expSpectrum.Sample);
                    }

                    tw.WriteLine(strChannel);
                    tw.WriteLine(strID);
                    tw.WriteLine(strStart);
                    tw.WriteLine(strStop);
                    tw.WriteLine(strECalOffset);
                    tw.WriteLine(strECalSlope);
                    tw.WriteLine(strName);

                    // TODO: Write data              
                }
            }
        }

        public static List<Measurement> LoadMeasurementsFromFile(string fileName)
        {
            List<Measurement> newMeasurements = new List<Measurement>();
            List<List<int>> spectraY = new List<List<int>>();

            using (TextReader textReader = new StreamReader(fileName))
            {
                string line;

                string[] lineParts = textReader.ReadLine().Split('\t');

                int numSpectra = lineParts.Count() - 1;
                Console.WriteLine("Number of spectra: {0}", numSpectra);

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
                                { newMeasurements[i].IncomingIonNumber = Int32.Parse(lineParts[i + 1]); break; }
                            case "Energy":
                                { newMeasurements[i].IncomingIonEnergy = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                            case "Scattering angle":
                                { newMeasurements[i].OutcomingIonAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                            case "Incident angle":
                                { newMeasurements[i].IncomingIonAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                            case "Exit angle":
                                { break; }
                            case "Energy / Channel":
                                { newMeasurements[i].EnergyCalSlope = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                            case "Offset":
                                { newMeasurements[i].EnergyCalOffset = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                            case "Solid angle":
                                { newMeasurements[i].SolidAngle = Convert.ToDouble(lineParts[i + 1].Replace(".", ",")); break; }
                            case "Charge":
                                { break; }
                            case "Real time":
                                { newMeasurements[i].Duration = new DateTime(2000, 01, 01) + TimeSpan.FromSeconds(Convert.ToDouble(lineParts[i + 1].Replace(".", ","))); break; }
                            case "Live time":
                                { break; }
                            case "FWHM":
                                { break; }
                            case "Channel":
                                { break; }
                            case "":
                                { break; }
                            default:
                                { spectraY[i].Add(Int32.Parse(lineParts[i + 1].Replace(" ", ""))); break; }
                        }
                    }
                }

                for (int i = 0; i < numSpectra; i++)
                {
                    newMeasurements[i].SpectrumY = ArrayConversion.IntToByte(spectraY[i].ToArray());
                    newMeasurements[i].NumOfChannels = spectraY[i].Count();
                    newMeasurements[i].Orientation = "(undefined)";
                    newMeasurements[i].StopType = "(undefined)";
                    newMeasurements[i].StopTime = newMeasurements[i].StartTime + (newMeasurements[i].Duration - new DateTime(2000, 01, 01));
                    newMeasurements[i].Chamber = "(undefined)";
                }

                return newMeasurements;
            }
        }
    }
}

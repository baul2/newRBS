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

        public static float[] GetCalibratedSpectrumX(Measurement measurement)
        {
            float[] SpectrumX = new float[measurement.NumOfChannels];
            for (int i = 0; i < measurement.NumOfChannels; i++)
                SpectrumX[i] = (float)measurement.EnergyCalOffset + i * (float)measurement.EnergyCalSlope;
            return SpectrumX;
        }

        public static int[] GetIntSpectrumY(Measurement measurement)
        {
            int[] intArray = new int[measurement.SpectrumY.Length / sizeof(int)];
            Buffer.BlockCopy(measurement.SpectrumY.ToArray(), 0, intArray, 0, intArray.Length * sizeof(int));
            return intArray;
        }

        public static byte[] GetByteSpectrumY(int[] intArray)
        {
            byte[] byteArray = new byte[intArray.Length * sizeof(int)];
            Buffer.BlockCopy(intArray, 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }

        public static int? AddNewSample()
        {
            Console.WriteLine("AddNewSample");

            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new sample name:", "");
            if (inputDialog.ShowDialog() == true)
            {
                Console.WriteLine(inputDialog.Answer);
                if (inputDialog.Answer == "")
                    return null;

                using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
                {
                    Sample sample = Database.Samples.FirstOrDefault(x => x.SampleName == inputDialog.Answer);

                    if (sample != null)
                    {
                        Console.WriteLine("Sample already exists!");

                        MessageBoxResult result = MessageBox.Show("Sample already exists in database!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);

                        return sample.SampleID;
                    }

                    // New sample
                    Console.WriteLine("new sample");

                    Sample newSample = new Sample();
                    newSample.SampleName = inputDialog.Answer;
                    newSample.MaterialID = 1;

                    Database.Samples.InsertOnSubmit(newSample);
                    Database.SubmitChanges();

                    return newSample.SampleID;
                }
            }
            else return null;
        }

        /// <summary>
        /// Function that saves spectra to a file
        /// </summary>
        /// <param name="measurementIDs">Array of IDs of the spectra to save.</param>
        /// <param name="FileName">Filename of the file to save the spectra to.</param>
        public static void ExportMeasurements(List<int> measurementIDs, string FileName)
        {
            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
            {
                Console.WriteLine(Path.GetExtension(FileName));
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

                            XmlSerializer SerializerObj = new XmlSerializer(typeof(List<Measurement>), xOver);
                            SerializerObj.Serialize(WriteFileStream, Database.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList());
                            break;
                        }
                    case ".dat":
                        using (TextWriter tw = new StreamWriter(FileName))
                        {

                            // Header
                            string strChannel = "Channel";
                            string strID = "ID";
                            string strStart = "StartTime";
                            string strStop = "StopTime";
                            string strECalOffset = "EnergyCalOffset";
                            string strECalSlope = "EnergyCalSlope";
                            string strName = "Name";

                            List<Measurement> MeasurementsToExport = Database.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList();

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
                            break;
                        }
                }
            }
        }

        public static List<Measurement> LoadMeasurementsFromFile(string fileName)
        {
            if (!File.Exists(fileName)) return null;

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
                                { newMeasurements[i].IncomingIonAtomicNumber = Int32.Parse(lineParts[i + 1]); break; }
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
                    newMeasurements[i].SpectrumY = GetByteSpectrumY(spectraY[i].ToArray());
                    newMeasurements[i].NumOfChannels = spectraY[i].Count();
                    newMeasurements[i].Orientation = "(undefined)";
                    newMeasurements[i].StopType = "(undefined)";
                    newMeasurements[i].StopTime = newMeasurements[i].StartTime + (newMeasurements[i].Duration - new DateTime(2000, 01, 01));
                    newMeasurements[i].Chamber = "(undefined)";
                }

                return newMeasurements;
            }
        }

        public static void DeleteMeasurements(List<int> MeasurementIDs)
        {
            if (MeasurementIDs.Count() == 0) return;

            MessageBoxResult rsltMessageBox = MessageBox.Show("Are you shure to delete the selected measurements?", "Confirm deletion", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (rsltMessageBox == MessageBoxResult.Yes)
                using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
                {
                    Database.Measurements.DeleteAllOnSubmit(Database.Measurements.Where(x => MeasurementIDs.Contains(x.MeasurementID)));
                    Database.SubmitChanges();
                }
        }
    }
}

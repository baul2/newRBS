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
    public class DataSpectra
    {

        public delegate void EventHandlerMeasurement(Measurement spectrum);
        public event EventHandlerMeasurement EventMeasurementNew, EventMeasurementUpdate, EventMeasurementFinished;

        public delegate void EventHandlerMeasurementID(int measurementID);
        public event EventHandlerMeasurementID EventMeasurementRemove;

        TraceSource trace = new TraceSource("DataSpectra");

        public void AddSpectrum(Measurement measurement)
        {
            using (DatabaseDataContext db = new DatabaseDataContext(MyGlobals.ConString))
            {
                db.Measurements.InsertOnSubmit(measurement);

                db.SubmitChanges();

                if (EventMeasurementNew != null) { EventMeasurementNew(measurement); } else { Console.WriteLine("EventSpectrumNew null"); }
            }
        }

        public void DeleteSpectra(List<int> measurementIDs)
        {
            using (DatabaseDataContext db = new DatabaseDataContext(MyGlobals.ConString))
            {
                List<Measurement> MeasurementsToDelete = db.Measurements.Where(x => measurementIDs.Contains(x.MeasurementID)).ToList();
                //from spec in DatabaseDataContext.Measurements where measurementIDs.Contains(spec.MeasurementID) select spec;

                db.Measurements.DeleteAllOnSubmit(MeasurementsToDelete);
                db.SubmitChanges();

                foreach (int measurementID in measurementIDs)
                    if (EventMeasurementRemove != null) { EventMeasurementRemove(measurementID); } else { Console.WriteLine("EventSpectrumRemove null"); }
            }
        }

        /// <summary>
        /// Function that adds a new item (\<ID, <see cref="DataSpectrum"/>\>) to the dictionary of spectra.
        /// </summary>
        /// <param name="channel">Channel on which the spectrum is obtained</param>
        /// <remarks>Other parameters (expDetails, energyCalibration) is taken from the class definition.</remarks>
        /// <returns>ID of the new spectrum.</returns>
        public int NewSpectrum(int channel, int incomingIonNumber, int incomingIonMass, double incomingIonEnergy, double incomingIonAngle, double outcomingIonAngle, double solidAngle, double energyCalOffset, double energyCalSlope, string stopType, int stopValue, bool runs, int numOfChannels)
        {
            using (DatabaseDataContext db = new DatabaseDataContext(MyGlobals.ConString))
            {
                Measurement newSpectrum = new Measurement
                {
                    Channel = channel,
                    IncomingIonNumber = incomingIonNumber,
                    IncomingIonMass = incomingIonMass,
                    IncomingIonEnergy = incomingIonEnergy,
                    IncomingIonAngle = incomingIonAngle,
                    OutcomingIonAngle = outcomingIonAngle,
                    SolidAngle = solidAngle,
                    EnergyCalOffset = energyCalOffset,
                    EnergyCalSlope = energyCalSlope,
                    StopType = stopType,
                    StopValue = stopValue,
                    Runs = runs,
                    NumOfChannels = numOfChannels
                };

                newSpectrum.Sample = db.Samples.Single(x => x.SampleName == "(undefined)");

                db.Measurements.InsertOnSubmit(newSpectrum);
                db.SubmitChanges();

                if (EventMeasurementNew != null) { EventMeasurementNew(newSpectrum); } else { Console.WriteLine("EventSpectrumNew null"); }

                return newSpectrum.MeasurementID;
            }
        }

        /// <summary>
        /// Function that saves spectra to a file
        /// </summary>
        /// <param name="IDs">Array of IDs of the spectra to save.</param>
        /// <param name="file">Filename of the file to save the spectra to.</param>
        public void ExportMeasurements(int[] measurementIDs, string file)
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

        public void UpdateMeasurement(int measurementID, int[] spectrumY)
        {
            using (DatabaseDataContext db = new DatabaseDataContext(MyGlobals.ConString))
            {
                Measurement MeasurementToUpdate = db.Measurements.FirstOrDefault(x => x.MeasurementID == measurementID);

                if (MeasurementToUpdate == null)
                { trace.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Measurement with MeasurementID = {0} not found", measurementID); return; }

                if (spectrumY.Length != 16384) // TODO!!!
                { trace.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't match"); return; }

                MeasurementToUpdate.SpectrumY = ArrayConversion.IntToByte(spectrumY);

                MeasurementToUpdate.Duration = new DateTime(2000, 01, 01) + (DateTime.Now - MeasurementToUpdate.StartTime);

                switch (MeasurementToUpdate.StopType)
                {
                    case "Manual": MeasurementToUpdate.Progress = 0; break;
                    case "Counts":
                        int counts = 0;
                        for (int i = 0; i < spectrumY.Length; i++)
                        { counts += spectrumY[i]; }
                        MeasurementToUpdate.Progress = counts / (int)MeasurementToUpdate.StopValue;
                        break;
                    case "Time":
                        MeasurementToUpdate.Progress = (MeasurementToUpdate.Duration - DateTime.MinValue).TotalMinutes / (int)MeasurementToUpdate.StopValue; break;
                        // TODO: Chopper
                }

                db.SubmitChanges();

                if (MeasurementToUpdate.Progress >= 1)
                {
                    MeasurementToUpdate.Progress = 1;
                    if (EventMeasurementFinished != null) EventMeasurementFinished(MeasurementToUpdate);
                }

                if (EventMeasurementUpdate != null) EventMeasurementUpdate(MeasurementToUpdate);
            }
        }

        /// <summary>
        /// Function that stops the Spectrum: It sets stopTime = now and runs = false.
        /// </summary>
        /// <param name="ID">ID of the spectrum to be stopped.</param>
        public void FinishMeasurement(int measurementID)
        {
            using (DatabaseDataContext db = new DatabaseDataContext(MyGlobals.ConString))
            {
                Measurement MeasurementToStop = db.Measurements.FirstOrDefault(x => x.MeasurementID == measurementID);

                if (MeasurementToStop == null)
                { trace.TraceEvent(TraceEventType.Warning, 0, "Can't finish Measurement: Measurement with MeasurementID = {0} not found", measurementID); return; }

                MeasurementToStop.StopTime = DateTime.Now;
                MeasurementToStop.Runs = false;

                db.SubmitChanges();

                if (EventMeasurementUpdate != null) EventMeasurementUpdate(MeasurementToStop);
            }
        }

        public List<Measurement> LoadMeasurementsFromFile(string fileName)
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
                    newMeasurements[i].Name = lineParts[i + 1];
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
                    newMeasurements[i].StopTime = newMeasurements[i].StartTime + (newMeasurements[i].Duration - new DateTime(2000, 01, 01));
                }

                return newMeasurements;
            }
        }
    }
}

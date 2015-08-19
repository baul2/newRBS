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
    [Database(Name = "p4mist_db")]
    public class SpectraDB : DataContext
    {
        public Table<Measurement> Spectra;

        public SpectraDB(string connection) : base(connection) { }
    }

    /// <summary>
    /// Class responsible for managing a spectrum dictionary (<see cref="spectra"/>) with items \<ID, <see cref="DataSpectrum"/>\>.
    /// </summary>
    public class DataSpectra
    {

        public delegate void EventHandlerSpectrum(Measurement spectrum);
        public event EventHandlerSpectrum EventSpectrumNew, EventSpectrumUpdate, EventSpectrumFinished;

        public delegate void EventHandlerSpectrumID(int spectrumID);
        public event EventHandlerSpectrumID EventSpectrumRemove;

        TraceSource trace = new TraceSource("DataSpectra");

        //private string ConnectionString = "Data Source = SVRH; Initial Catalog = p4mist_db; User ID = p4mist; Password = testtesttesttest";
        private string ConnectionString = "Data Source = SVRH; User ID = p4mist; Password = testtesttesttest";

        public List<int> GetAllChannels()
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            var channels = from spec in spectraDB.Spectra select spec.Channel;

            List<int> noDupes = channels.Distinct().ToList();

            return noDupes;
        }

        public List<int> GetAllYears()
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            var years = from spec in spectraDB.Spectra select spec.StartTime.Year;

            List<int> noDupes = years.Distinct().ToList();

            return noDupes;
        }

        public List<int> GetAllMonths(int year)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            var months = from spec in spectraDB.Spectra where spec.StartTime.Year == year select spec.StartTime.Month;

            List<int> noDupes = months.Distinct().ToList();

            return noDupes;
        }

        public List<int> GetAllDays(int year, int month)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            var days = from spec in spectraDB.Spectra where spec.StartTime.Year == year && spec.StartTime.Month == month select spec.StartTime.Day;

            List<int> noDupes = days.Distinct().ToList();

            return noDupes;
        }


        public List<Measurement> GetSpectra_All()
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Measurement> Spec = from spec in spectraDB.Spectra select spec;

            Console.WriteLine("Num Spectra: {0}", Spec.Count());

            return Spec.ToList();
        }

        public List<Measurement> GetSpectra_Date(ViewModels.Filter selectedFilter)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Measurement> Spec = null;

            switch (selectedFilter.SubType)
            {
                case "Today":
                    { Spec = from spec in spectraDB.Spectra where spec.StartTime.Date == DateTime.Today select spec; break; }
                case "ThisWeek":
                    {
                        int dayofweek = (int)DateTime.Today.DayOfWeek;
                        Console.WriteLine(dayofweek);
                        Spec = from spec in spectraDB.Spectra where (DateTime.Today.DayOfYear - spec.StartTime.DayOfYear) < dayofweek select spec;
                        break;
                    }
                case "ThisMonth":
                    { Spec = from spec in spectraDB.Spectra where spec.StartTime.Date.Month == DateTime.Today.Month select spec; break; }
                case "ThisYear":
                    { Spec = from spec in spectraDB.Spectra where spec.StartTime.Date.Year == DateTime.Today.Year select spec; break; }
                case "Year":
                    { Spec = from spec in spectraDB.Spectra where spec.StartTime.Date.Year == selectedFilter.year select spec; break; }
                case "Month":
                    { Spec = from spec in spectraDB.Spectra where spec.StartTime.Date.Year == selectedFilter.year && spec.StartTime.Date.Month == selectedFilter.month select spec; break; }
                case "Day":
                    { Spec = from spec in spectraDB.Spectra where spec.StartTime.Date.Year == selectedFilter.year && spec.StartTime.Date.Month == selectedFilter.month && spec.StartTime.Date.Day == selectedFilter.day select spec; break; }
            }

            return Spec.ToList();

        }
        public List<Measurement> GetSpectra_Channel(ViewModels.Filter selectedFilter)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Measurement> Spec = from spec in spectraDB.Spectra where spec.Channel == selectedFilter.channel select spec;

            return Spec.ToList();
        }

        public Measurement GetSpectrum_SpectrumID(int spectrumID)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            return (from spec in spectraDB.Spectra where spec.MeasurementID == spectrumID select spec).First();
        }

        public void AddSpectrum(Measurement spectrum)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            spectraDB.Spectra.InsertOnSubmit(spectrum);

            spectraDB.SubmitChanges();

            if (EventSpectrumNew != null) { EventSpectrumNew(spectrum); } else { Console.WriteLine("EventSpectrumNew null"); }
        }

        public void DeleteSpectra(List<int> spectraIDs)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            var deleteSpectra = from spec in spectraDB.Spectra where spectraIDs.Contains(spec.MeasurementID) select spec;

            spectraDB.Spectra.DeleteAllOnSubmit(deleteSpectra);

            spectraDB.SubmitChanges();

            foreach (int spectrumID in spectraIDs)
                if (EventSpectrumRemove != null) { EventSpectrumRemove(spectrumID); } else { Console.WriteLine("EventSpectrumRemove null"); }
        }

        /// <summary>
        /// Function that adds a new item (\<ID, <see cref="DataSpectrum"/>\>) to the dictionary of spectra.
        /// </summary>
        /// <param name="channel">Channel on which the spectrum is obtained</param>
        /// <remarks>Other parameters (expDetails, energyCalibration) is taken from the class definition.</remarks>
        /// <returns>ID of the new spectrum.</returns>
        public int NewSpectrum(int channel, int incomingIonNumber, int incomingIonMass, double incomingIonEnergy, double incomingIonAngle, double outcomingIonAngle, double solidAngle, double energyCalOffset, double energyCalSlope, string stopType, int stopValue, bool runs, int numOfChannels)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            Measurement newSpectrum = new Measurement(channel, incomingIonNumber, incomingIonMass, incomingIonEnergy, incomingIonAngle, outcomingIonAngle, solidAngle, energyCalOffset, energyCalSlope, stopType, stopValue, runs, numOfChannels);
            spectraDB.Spectra.InsertOnSubmit(newSpectrum);
            spectraDB.SubmitChanges();

            if (EventSpectrumNew != null) { EventSpectrumNew(newSpectrum); } else { Console.WriteLine("EventSpectrumNew null"); }

            return newSpectrum.MeasurementID;
        }

        /// <summary>
        /// Function that saves spectra to a file
        /// </summary>
        /// <param name="IDs">Array of IDs of the spectra to save.</param>
        /// <param name="file">Filename of the file to save the spectra to.</param>
        public void ExportSpectra(int[] spectrumIDs, string file)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            TextWriter tw = new StreamWriter(file);

            // Header
            string strChannel = "Channel";
            string strID = "ID";
            string strStart = "StartTime";
            string strStop = "StopTime";
            string strECalOffset = "EnergyCalOffset";
            string strECalSlope = "EnergyCalSlope";
            string strName = "Name";

            var expSpectra = from spec in spectraDB.Spectra where spectrumIDs.Contains(spec.MeasurementID) select spec;

            if (!expSpectra.Any())
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't save Spectra: No spectrum not found"); tw.Close(); return; }

            foreach (var expSpectrum in expSpectra)
            {
                strChannel += String.Format("\t {0}", expSpectrum.Channel);
                strID += String.Format("\t {0}", expSpectrum.MeasurementID);
                strStart += String.Format("\t {0}", expSpectrum.StartTime);
                strStop += String.Format("\t {0}", expSpectrum.StopTime);
                //strECalOffset += String.Format("\t {0:yyyy-MM-dd_HH:mm:ss}", expSpectrum.energyCalibration_.energyCalOffset);
                //strECalSlope += String.Format("\t {0:yyyy-MM-dd_HH:mm:ss}", expSpectrum.energyCalibration_.energyCalSlope);
                strName += String.Format("\t {0}", expSpectrum.SampleID);
            }

            tw.WriteLine(strChannel);
            tw.WriteLine(strID);
            tw.WriteLine(strStart);
            tw.WriteLine(strStop);
            tw.WriteLine(strECalOffset);
            tw.WriteLine(strECalSlope);
            tw.WriteLine(strName);

            // Data
            //for (int x = 0; x < expSpectrum.SpectrumX.Length; x++)
            {
                // TODO: Write data
            }
            tw.Close();
        }

        public void UpdateSpectrum(int spectrumID, int[] spectrumY)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = null;

            var updateSpectrum = (from spec in spectraDB.Spectra where spec.MeasurementID == spectrumID select spec).First();

            if (updateSpectrum == null)
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Spectrum with SpectrumID={0} not found", spectrumID); return; }

            if (spectrumY.Length != 16384)
            { trace.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't match"); return; }

            updateSpectrum.SpectrumY = spectrumY;

            updateSpectrum.Duration = DateTime.Now - updateSpectrum.StartTime;

            switch (updateSpectrum.StopType)
            {
                case "Manual": updateSpectrum.Progress = 0; break;
                case "Counts":
                    int counts = 0;
                    for (int i = 0; i < updateSpectrum.SpectrumY.Length; i++)
                    { counts += updateSpectrum.SpectrumY[i]; }
                    updateSpectrum.Progress = counts / updateSpectrum.StopValue;
                    break;
                case "Time":
                    updateSpectrum.Progress = updateSpectrum.Duration.TotalMinutes / updateSpectrum.StopValue; break;
                    // TODO: Chopper
            }

            spectraDB.SubmitChanges();

            if (updateSpectrum.Progress >= 1)
            {
                updateSpectrum.Progress = 1;
                if (EventSpectrumFinished != null) EventSpectrumFinished(updateSpectrum);
            }

            if (EventSpectrumUpdate != null) EventSpectrumUpdate(updateSpectrum);
        }

        /// <summary>
        /// Function that stops the Spectrum: It sets stopTime = now and runs = false.
        /// </summary>
        /// <param name="ID">ID of the spectrum to be stopped.</param>
        public void StopSpectrum(int spectrumID)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            Measurement stopSpectrum = (from spec in spectraDB.Spectra where spec.MeasurementID == spectrumID select spec).First();

            if (stopSpectrum == null)
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't stop Spectrum: Spectrum with SpectrumID={0} not found", spectrumID); return; }

            stopSpectrum.StopTime = DateTime.Now;
            stopSpectrum.Runs = false;

            spectraDB.SubmitChanges();

            if (EventSpectrumUpdate != null) EventSpectrumUpdate(stopSpectrum);
        }

        /// <summary>
        /// Function that updates the metadata of the spectrum (duration, progress, ...)
        /// </summary>
        /// <param name="ID">ID of the spectrum to be updated</param>
        public void UpdateSpectrumInfos(int spectrumID)
        {

        }

        public List<Measurement> ImportSpectra(string fileName)
        {
            List<Measurement> newSpectra = new List<Measurement>();
            List<List<int>> spectraY = new List<List<int>>();

            using (TextReader textReader = new StreamReader(fileName))
            {
                string line;

                string[] lineParts = textReader.ReadLine().Split('\t');

                int numSpectra = lineParts.Count() - 1;
                Console.WriteLine("Number of spectra: {0}", numSpectra);

                for (int i = 0; i < numSpectra; i++)
                {
                    newSpectra.Add(new Models.Measurement());
                    spectraY.Add(new List<int>());
                    newSpectra[i].Name = lineParts[i + 1];
                }

                while ((line = textReader.ReadLine()) != null)
                {
                    lineParts = line.Split('\t');

                    for (int i = 0; i < numSpectra; i++)
                    {
                        switch (lineParts[0])
                        {
                            case "Date":
                                { newSpectra[i].StartTime = DateTime.ParseExact(lineParts[i + 1], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture); break; }
                            case "Remark":
                                { break; }
                            case "Projectile":
                                { break; }
                            case "Energy":
                                { break; }
                            case "Scattering angle":
                                { break; }
                            case "Incident angle":
                                { break; }
                            case "Exit angle":
                                { break; }
                            case "Energy / Channel":
                                { break; }
                            case "Offset":
                                { break; }
                            case "Solid angle":
                                { break; }
                            case "Charge":
                                { break; }
                            case "Real time":
                                { newSpectra[i].Duration = TimeSpan.FromSeconds(Convert.ToDouble(lineParts[i + 1].Replace(".", ","))); break; }
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
                    newSpectra[i].SpectrumY = spectraY[i].ToArray();

                return newSpectra;
            }
        }
    }
}

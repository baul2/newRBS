using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Data.Linq;
using System.Globalization;

namespace newRBS.Models
{
    public class SpectraDB : DataContext
    {
        public Table<Spectrum> Spectra;

        public SpectraDB(string connection) : base(connection) { }
    }

    /// <summary>
    /// Class responsible for managing a spectrum dictionary (<see cref="spectra"/>) with items \<ID, <see cref="DataSpectrum"/>\>.
    /// </summary>
    public class DataSpectra
    {

        public delegate void EventHandlerSpectrum(Spectrum spectrum);
        public event EventHandlerSpectrum EventSpectrumNew, EventSpectrumRemove, EventSpectrumUpdate, EventSpectrumFinished;

        TraceSource trace = new TraceSource("DataSpectra");

        private string ConnectionString = "Data Source = SVRH; Initial Catalog = p4mist_db; User ID = p4mist; Password = testtesttesttest";

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

        public List<int> GetAllMonths( int year)
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


        public List<Spectrum> GetSpectra_All()
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Spectrum> Spec = from spec in spectraDB.Spectra select spec;

            Console.WriteLine("Num Spectra: {0}", Spec.Count());

            return Spec.ToList();
        }

        public List<Spectrum> GetSpectra_Date(ViewModels.Filter selectedFilter)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Spectrum> Spec = null;

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
        public List<Spectrum> GetSpectra_Channel(ViewModels.Filter selectedFilter)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Spectrum> Spec = from spec in spectraDB.Spectra where spec.Channel == selectedFilter.channel select spec;

            return Spec.ToList();
        }

        public Spectrum GetSpectrum_SpectrumID(int spectrumID)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            return (from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec).First();
        }

        /// <summary>
        /// Function that adds a new item (\<ID, <see cref="DataSpectrum"/>\>) to the dictionary of spectra.
        /// </summary>
        /// <param name="channel">Channel on which the spectrum is obtained</param>
        /// <remarks>Other parameters (expDetails, energyCalibration) is taken from the class definition.</remarks>
        /// <returns>ID of the new spectrum.</returns>
        public int NewSpectrum(int channel, ExpDetails expDetails, EnergyCalibration energyCalibration, string stopType, int stopValue, bool runs)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            Spectrum newSpectrum = new Spectrum(channel, expDetails, energyCalibration, stopType, stopValue, runs);
            spectraDB.Spectra.InsertOnSubmit(newSpectrum);
            spectraDB.SubmitChanges();

            if (EventSpectrumNew != null) { EventSpectrumNew(newSpectrum); } else { Console.WriteLine("EventSpectrumNew null"); }

            return newSpectrum.SpectrumID;
        }

        /// <summary>
        /// Function that removes an item (\<ID, <see cref="DataSpectrum"/>\>) from the dictionary of spectra.
        /// </summary>
        /// <param name="spectrumID">ID of the spectrum to remove.</param>
        public void RemoveSpectrum(int spectrumID)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            var delSpectra = from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec;

            if (!delSpectra.Any())
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't remove Spectrum: Spectrum with SpectrumID={0} not found", spectrumID); return; }

            Spectrum delSpectrum = delSpectra.First();

            spectraDB.Spectra.DeleteOnSubmit(delSpectra.First());

            spectraDB.SubmitChanges();

            if (EventSpectrumRemove != null) EventSpectrumRemove(delSpectrum);
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

            var expSpectra = from spec in spectraDB.Spectra where spectrumIDs.Contains(spec.SpectrumID) select spec;

            if (!expSpectra.Any())
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't save Spectra: No spectrum not found"); tw.Close(); return; }

            foreach (var expSpectrum in expSpectra)
            {
                strChannel += String.Format("\t {0}", expSpectrum.Channel);
                strID += String.Format("\t {0}", expSpectrum.SpectrumID);
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

            var updateSpectrum = (from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec).First();

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

            Spectrum stopSpectrum = (from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec).First();

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
    }
}

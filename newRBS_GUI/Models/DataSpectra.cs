using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Data.Linq;

namespace newRBS.Models
{

    public class SpectrumArgs : EventArgs
    {
        public readonly int ID;
        public SpectrumArgs(int id) { ID = id; }
    }

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

        public delegate void ChangedEventHandler(object sender, SpectrumArgs e);
        public event ChangedEventHandler EventSpectrumNew, EventSpectrumRemove, EventSpectrumY, EventSpectrumInfos, EventSpectrumFinished;

        TraceSource trace = new TraceSource("DataSpectra");

        private string ConnectionString = "Data Source = SVRH; Initial Catalog = p4mist_db; User ID = p4mist; Password = testtesttesttest";

        public DataSpectra()
        {

        }

        public ViewModel.AsyncObservableCollection<Spectrum> GetObservableCollection()
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Spectrum> Spec = from spec in spectraDB.Spectra select spec;

            //foreach (Spectrum spec in Spec)
            //{ Console.WriteLine("Spectrum {0}", spec.SpectrumID); }

            Console.WriteLine("Num Spectra: {0}", Spec.Count());

            return new ViewModel.AsyncObservableCollection<Spectrum>(Spec.ToList());
        }

        public Spectrum GetSpectrum(int spectrumID)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = Console.Out;

            IQueryable<Spectrum> Spec = from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec;

            if (!Spec.Any())
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't find Spectrum with SpectrumID={0}", spectrumID); return null; }

            return Spec.First();
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

            SpectrumArgs e1 = new SpectrumArgs(newSpectrum.SpectrumID);
            if (EventSpectrumNew != null) { EventSpectrumNew(this, e1); } else { Console.WriteLine("EventSpectrumNew null"); }

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

            foreach (var delSpectrum in delSpectra)
                spectraDB.Spectra.DeleteOnSubmit(delSpectrum);

            spectraDB.SubmitChanges();

            SpectrumArgs e1 = new SpectrumArgs(spectrumID);
            if (EventSpectrumRemove != null) EventSpectrumRemove(this, e1);
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

        public void SetSpectrumY(int spectrumID, int[] spectrumY)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);
            spectraDB.Log = null;

            var modSpectra = from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec;

            if (!modSpectra.Any())
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Spectrum with SpectrumID={0} not found", spectrumID); return; }

            if (spectrumY.Length != 16384)
            { trace.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't match"); return; }

            foreach (var modSpectrum in modSpectra)
                modSpectrum.SpectrumY = spectrumY;

            spectraDB.SubmitChanges();

            SpectrumArgs e1 = new SpectrumArgs(spectrumID);
            if (EventSpectrumY != null) EventSpectrumY(this, e1);

            UpdateSpectrumInfos(spectrumID);
        }

        /// <summary>
        /// Function that stops the Spectrum: It sets stopTime = now and runs = false.
        /// </summary>
        /// <param name="ID">ID of the spectrum to be stopped.</param>
        public void StopSpectrum(int spectrumID)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            IQueryable<Spectrum> stopSpectra = from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec;
            Console.WriteLine("stopSpectra.Count() {0}", stopSpectra.Count());
            if (stopSpectra.Count() == 0)
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't stop Spectrum: Spectrum with SpectrumID={0} not found", spectrumID); return; }

            foreach (Spectrum stopSpectrum in stopSpectra)
            {
                stopSpectrum.StopTime = DateTime.Now;
                stopSpectrum.Runs = false;
                Console.WriteLine("NumElements: {0}", stopSpectrum.SpectrumY.Length);
            }

            spectraDB.SubmitChanges();

            SpectrumArgs e1 = new SpectrumArgs(spectrumID);
            if (EventSpectrumInfos != null) EventSpectrumInfos(this, e1);
        }

        /// <summary>
        /// Function that updates the metadata of the spectrum (duration, progress)
        /// </summary>
        /// <param name="ID">ID of the spectrum to be updated</param>
        public void UpdateSpectrumInfos(int spectrumID)
        {
            SpectraDB spectraDB = new SpectraDB(ConnectionString);

            SpectrumArgs e1 = new SpectrumArgs(spectrumID);

            var updateSpectra = from spec in spectraDB.Spectra where spec.SpectrumID == spectrumID select spec;

            if (!updateSpectra.Any())
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't update Spectrum: Spectrum with SpectrumID={0} not found", spectrumID); return; }

            foreach (var updateSpectrum in updateSpectra)
            {
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
                    case "Time": updateSpectrum.Progress = updateSpectrum.Duration.TotalMinutes / updateSpectrum.StopValue; break;
                        // TODO: Chopper
                }

                if (updateSpectrum.Progress >= 1)
                {
                    updateSpectrum.Progress = 1;
                    if (EventSpectrumFinished != null) EventSpectrumFinished(this, e1);
                }

                if (EventSpectrumInfos != null) EventSpectrumInfos(this, e1);
            }

            spectraDB.SubmitChanges();
        }
    }
}

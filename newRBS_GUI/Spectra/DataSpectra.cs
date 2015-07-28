using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace newRBS.Spectra
{

    public class SpectrumArgs : EventArgs
    {
        public readonly int ID;
        public readonly int Channel;
        public SpectrumArgs(int id, int channel) { ID = id; Channel = channel; }
    }

    /// <summary>
    /// Class responsible for managing a spectrum dictionary (<see cref="spectra"/>) with items \<ID, <see cref="DataSpectrum"/>\>.
    /// </summary>
    class DataSpectra
    {
        private int spectrumIndex = 0;
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<int, DataSpectrum> spectra = new Dictionary<int, DataSpectrum>();

        public static event SpectrumNewHandler EventSpectrumNew;
        public static event SpectrumRemoveHandler EventSpectrumRemove;
        public static event SpectrumYHandler EventSpectrumY;
        public static event SpectrumInfosHandler EventSpectrumInfos;
        public static event SpectrumFinishedHandler EventSpectrumFinished;

        TraceSource trace = new TraceSource("DataSpectra");

        /// <summary>
        /// Function that adds a new item (\<ID, <see cref="DataSpectrum"/>\>) to the dictionary of spectra.
        /// </summary>
        /// <param name="channel">Channel on which the spectrum is obtained</param>
        /// <remarks>Other parameters (expDetails, energyCalibration) is taken from the class definition.</remarks>
        /// <returns>ID of the new spectrum.</returns>
        public int NewSpectrum(int channel, ExpDetails expDetails, EnergyCalibration energyCalibration, Stop stop)
        {
            int ID = spectrumIndex;
            spectra.Add(ID, new DataSpectrum(ID, channel, expDetails, energyCalibration, stop));
            spectrumIndex += 1;

            SpectrumArgs e1 = new SpectrumArgs(ID, channel);
            if (EventSpectrumNew != null) EventSpectrumNew(this, e1);

            return ID;
        }

        /// <summary>
        /// Function that removes an item (\<ID, <see cref="DataSpectrum"/>\>) from the dictionary of spectra.
        /// </summary>
        /// <param name="ID">ID of the spectrum to remove.</param>
        public void RemoveSpectrum(int ID)
        {
            if (!spectra.ContainsKey(ID))
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't remove Spectrum: Spectrum with ID={0} not found", ID); return; }

            spectra.Remove(ID);

            SpectrumArgs e1 = new SpectrumArgs(ID, -1);
            if (EventSpectrumRemove != null) EventSpectrumRemove(this, e1);
        }


        /// <summary>
        /// Function that saves spectra to a file
        /// </summary>
        /// <param name="IDs">Array of IDs of the spectra to save.</param>
        /// <param name="file">Filename of the file to save the spectra to.</param>
        public void SaveSpectra(int[] IDs, string file)
        {
            DataSpectrum spectrum = null;
            TextWriter tw = new StreamWriter(file);

            // Header
            string strChannel = "Channel";
            string strID = "ID";
            string strStart = "StartTime";
            string strStop = "StopTime";
            string strECalOffset = "EnergyCalOffset";
            string strECalSlope = "EnergyCalSlope";
            string strName = "Name";

            foreach (int ID in IDs)
            {
                if (!spectra.ContainsKey(ID))
                {
                    trace.TraceEvent(TraceEventType.Warning, 0, "Spectrum with ID={0} not found", ID);
                    continue;
                }
                spectrum = spectra[ID];
                strChannel += String.Format("\t {0}", spectrum.channel);
                strID += String.Format("\t {0}", spectrum.ID);
                strStart += String.Format("\t {0}", spectrum.startTime);
                strStop += String.Format("\t {0}", spectrum.stopTime);
                strECalOffset += String.Format("\t {0:yyyy-MM-dd_HH:mm:ss}", spectrum.energyCalibration_.energyCalOffset);
                strECalSlope += String.Format("\t {0:yyyy-MM-dd_HH:mm:ss}", spectrum.energyCalibration_.energyCalSlope);
                strName += String.Format("\t {0}", spectrum.name);
            }

            if (spectrum == null)
            {
                trace.TraceEvent(TraceEventType.Warning, 0, "No spectra to save");
                tw.Close();
                return;
            }

            tw.WriteLine(strChannel);
            tw.WriteLine(strID);
            tw.WriteLine(strStart);
            tw.WriteLine(strStop);
            tw.WriteLine(strECalOffset);
            tw.WriteLine(strECalSlope);
            tw.WriteLine(strName);

            // Data
            for (int x = 0; x < spectrum.SpectrumX.Length; x++)
            {
                // TODO: Write data
            }
            tw.Close();

        }

        public void SetSpectrumY(int ID, int[] spectrumY)
        {
            if (!spectra.ContainsKey(ID))
            { trace.TraceEvent(TraceEventType.Warning, 0, "Can't update SpectrumY: Spectrum with ID={0} not found", ID); return; }
            if (spectrumY.Length != spectra[ID].SpectrumY.Length)
            { trace.TraceEvent(TraceEventType.Warning, 0, "Length of spectrumY doesn't macht"); return; }

            spectra[ID].SpectrumY = spectrumY;

            SpectrumArgs e1 = new SpectrumArgs(ID, spectra[ID].channel);
            if (EventSpectrumY != null) EventSpectrumY(this, e1);

            UpdateSpectrumInfos(ID);
        }

        /// <summary>
        /// Function that stops the Spectrum: It sets stopTime = now and runs = false.
        /// </summary>
        /// <param name="ID">ID of the spectrum to be stopped.</param>
        public void StopSpectrum(int ID)
        {
            spectra[ID].stopTime = DateTime.Now;
            spectra[ID].runs = false;

            SpectrumArgs e1 = new SpectrumArgs(ID, spectra[ID].channel);
            if (EventSpectrumInfos != null) EventSpectrumInfos(this, e1);
        }

        /// <summary>
        /// Function that updates the metadata of the spectrum (duration, progress)
        /// </summary>
        /// <param name="ID">ID of the spectrum to be updated</param>
        public void UpdateSpectrumInfos(int ID)
        {
            spectra[ID].duration = DateTime.Now - spectra[ID].startTime;

            switch (spectra[ID].stop_.type)
            {
                case "Manual": spectra[ID].progress = 0; break;
                case "Counts":
                    int counts = 0;
                    for (int i = 0; i < spectra[ID].SpectrumY.Length; i++)
                    { counts += spectra[ID].SpectrumY[i]; }
                    spectra[ID].progress = counts / spectra[ID].stop_.value;
                    break;
                case "Time": spectra[ID].progress = spectra[ID].duration.TotalMinutes / spectra[ID].stop_.value; break;
                // TODO: Chopper
            }

            SpectrumArgs e1 = new SpectrumArgs(ID, spectra[ID].channel);

            if (spectra[ID].progress >= 1)
            {
                spectra[ID].progress = 1;
                if (EventSpectrumFinished != null) EventSpectrumFinished(this, e1);
            }

            if (EventSpectrumInfos != null) EventSpectrumInfos(this, e1);
        }
    }
}

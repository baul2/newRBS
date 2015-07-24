using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace newRBS
{
    public enum EnumIon
    {
        H,
        He,
        Li,
    };

    /// <summary>
    /// Experimental details
    /// </summary>
    public struct ExpDetails
    {
        public EnumIon ion;
        public int ionEnergy;
        public int theta;
    }

    public struct EnergyCalibration
    {
        public float energyCalOffset;
        public float energyCalSlope;
    }

    public struct Stop
    {
        public string type;
        public double value;
    }

    /// <summary>
    /// Class responsible for storing a single spectrum.
    /// </summary>
    class DataSpectrum
    {
        public int ID;
        public int channel;
        public string name;
        public ExpDetails expDetails_;
        public EnergyCalibration energyCalibration_;
        public Stop stop_;
        public DateTime startTime, stopTime;
        public TimeSpan duration;
        public double progress;
        public bool runs;
        public string chamber;
        public float x, y, z, phi, psi;

        public readonly int[] SpectrumX = new int[16384];
        public float[] SpectrumCalX
        {
            get
            {
                float[] temp = new float[SpectrumX.Length];
                for (int i = 0; i < SpectrumX.Length; i++)
                    temp[i] = energyCalibration_.energyCalSlope * SpectrumX[i] + energyCalibration_.energyCalOffset;
                return temp;
            }
        }
        private int[] spectrumY = new int[16384];

        public int[] SpectrumY
        {
            get { return spectrumY; }
            set
            {
                spectrumY = value;

            }
        }

        /// <summary>
        /// Initiate a new spectrum with the given parameters. 
        /// </summary>
        /// <param name="id">ID of the spectrum</param>
        /// <param name="Channel">Channel where the spectrum is measured</param>
        /// <param name="expDetails">Experimental details</param>
        /// <param name="energyCalibration">Energy calibration</param>
        /// <param name="stop">Stop condition</param>
        public DataSpectrum(int id, int Channel, ExpDetails expDetails, EnergyCalibration energyCalibration, Stop stop)
        {
            ID = id;
            channel = Channel;
            expDetails_ = expDetails;
            energyCalibration_ = energyCalibration;
            stop_ = stop;
            startTime = DateTime.Now;
            for (int i = 0; i < 16384; i++) { SpectrumX[i] = i; }
        }
    }
}

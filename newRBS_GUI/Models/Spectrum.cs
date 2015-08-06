using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace newRBS.Models
{
    public enum Chamber
    {
        Minus10,
        Minus30,
    }

    public enum RandomAligned
    {
        random,
        aligned,
        unknown,
    }

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
    [global::System.Data.Linq.Mapping.TableAttribute(Name = "dbo.Spectra")]
    public class Spectrum : INotifyPropertyChanged
    {
        private int _SpectrumID = 0;
        private int _Channel;
        private int _SampleID;
        private DateTime _StartTime;
        private DateTime? _StopTime;
        private TimeSpan _Duration = TimeSpan.Zero;
        private double _Progress;
        private bool _Runs;
        [Column(Name = "SpectrumY")]
        private byte[] _SpectrumY = new byte[] { 0};
        private string _StopType;
        private int _StopValue;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, Storage = "_SpectrumID", DbType = "Int IDENTITY(1,1)")]
        public int SpectrumID
        { get { return _SpectrumID; } }

        [Column(CanBeNull = true, Storage = "_Channel")]
        public int Channel
        { get { return _Channel; } set { _Channel = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_SampleID")]
        public int SampleID
        { get { return _SampleID; } set { _SampleID = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_StartTime")]
        public DateTime StartTime
        { get { return _StartTime; } set { _StartTime = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_StopTime")]
        public DateTime? StopTime
        { get { return _StopTime; } set { _StopTime = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_Duration", DbType = "time")]
        public TimeSpan Duration
        { get { return _Duration; } set { _Duration = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_Progress")]
        public double Progress
        { get { return _Progress; } set { _Progress = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_Runs", DbType = "bit")]
        public bool Runs
        { get { return _Runs; } set { _Runs = value; OnPropertyChanged(); } }

        public int[] SpectrumY
        {
            get
            {
                int[] temp = new int[_SpectrumY.Length / sizeof(int)];
                Buffer.BlockCopy(_SpectrumY, 0, temp, 0, temp.Length);
                return temp;
            }
            set
            {
                byte[] temp = new byte[value.Length * sizeof(int)];
                Buffer.BlockCopy(value, 0, temp, 0, temp.Length);
                _SpectrumY = temp; OnPropertyChanged();
            }
        }

        [Column(CanBeNull = true, Storage = "_StopType")]
        public string StopType
        { get { return _StopType; } set { _StopType = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_StopValue")]
        public int StopValue
        { get { return _StopValue; } set { _StopValue = value; OnPropertyChanged(); } }

        public ExpDetails expDetails_;
        public EnergyCalibration energyCalibration_;

        public Chamber chamber;
        public RandomAligned randomAligned;
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

        /// <summary>
        /// Initiate a new spectrum with the given parameters. 
        /// </summary>
        /// <param name="id">ID of the spectrum</param>
        /// <param name="Channel">Channel where the spectrum is measured</param>
        /// <param name="expDetails">Experimental details</param>
        /// <param name="energyCalibration">Energy calibration</param>
        /// <param name="stop">Stop condition</param>
        public Spectrum(int channel, ExpDetails expDetails, EnergyCalibration energyCalibration, string stopType, int stopValue, bool runs)
        {
            //Console.WriteLine("");
            //SpectrumID = id;
            Channel = channel;
            expDetails_ = expDetails;
            energyCalibration_ = energyCalibration;
            StopType = stopType;
            StopValue = stopValue;
            Runs = runs;
            StartTime = DateTime.Now;
            for (int i = 0; i < 16384; i++) { SpectrumX[i] = i; }
        }

        public Spectrum()
        {

        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}

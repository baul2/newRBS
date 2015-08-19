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

    public struct Stop
    {
        public string type;
        public double value;
    }

    /// <summary>
    /// Class responsible for storing a single spectrum.
    /// </summary>
    [Table(Name = "dbo.Spectra")]
    public class Measurement : INotifyPropertyChanged
    {
        private int _MeasurementID = 0;
        private string _Name;
        private int _Channel;
        private int _SampleID;
        private bool? _RandomAligned;
        private DateTime _StartTime;
        private DateTime? _StopTime;
        private TimeSpan _Duration = TimeSpan.Zero;
        private bool _Runs;
        private double? _Charge;
        private double _Progress;
        private int _NumOfChannels;
        [Column(Name = "SpectrumY")]
        private byte[] _SpectrumY = new byte[] { 0 };
        [Column(Name = "SpectrumYCalculated")]
        private byte[] _SpectrumYCalculated = new byte[] { 0 };
        private string _StopType;
        private int _StopValue;
        private double _EnergyCalOffset;
        private double _EnergyCalSlope;
        private int _IncomingIonNumber;
        private int _IncomingIonMass;
        private double _IncomingIonEnergy;
        private double _IncomingIonAngle;
        private double _OutcomingIonAngle;
        private double _SolidAngle;
        private double? _X;
        private double? _Y;
        private double? _Phi;
        private double? _Theta;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, Storage = "_MeasurementID", DbType = "Int IDENTITY(1,1)")]
        public int MeasurementID
        { get { return _MeasurementID; } }

        [Column(CanBeNull = true, Storage = "_Name")]
        public string Name
        { get { return _Name; } set { _Name = value; OnPropertyChanged(); } }

        [Column(Storage = "_Channel")]
        public int Channel
        { get { return _Channel; } set { _Channel = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_SampleID")]
        public int SampleID
        { get { return _SampleID; } set { _SampleID = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_RandomAligned")]
        public bool? RandomAligned
        { get { return _RandomAligned; } set { _RandomAligned = value; OnPropertyChanged(); } }

        [Column( Storage = "_StartTime")]
        public DateTime StartTime
        { get { return _StartTime; } set { _StartTime = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_StopTime")]
        public DateTime? StopTime
        { get { return _StopTime; } set { _StopTime = value; OnPropertyChanged(); } }

        [Column( Storage = "_Duration", DbType = "time")]
        public TimeSpan Duration
        { get { return _Duration; } set { _Duration = value; OnPropertyChanged(); } }

        [Column( Storage = "_Runs", DbType = "bit")]
        public bool Runs
        { get { return _Runs; } set { _Runs = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_Charge")]
        public double? Charge
        { get { return _Charge; } set { _Charge = value; OnPropertyChanged(); } }

        [Column( Storage = "_Progress")]
        public double Progress
        { get { return _Progress; } set { _Progress = value; OnPropertyChanged(); } }

        [Column(Storage = "_NumOfChannels")]
        public int NumOfChannels
        { get { return _NumOfChannels; } set { _NumOfChannels = value; OnPropertyChanged(); } }
        
        public int[] SpectrumY
        {
            get
            {
                int[] temp = new int[_SpectrumY.Length / sizeof(int)];
                Buffer.BlockCopy(_SpectrumY, 0, temp, 0, temp.Length * sizeof(int));
                return temp;
            }
            set
            {
                byte[] temp = new byte[value.Length * sizeof(int)];
                Buffer.BlockCopy(value, 0, temp, 0, temp.Length);
                _SpectrumY = temp; OnPropertyChanged();
            }
        }

        public int[] SpectrumYCalculated
        {
            get
            {
                int[] temp = new int[_SpectrumY.Length / sizeof(int)];
                Buffer.BlockCopy(_SpectrumYCalculated, 0, temp, 0, temp.Length * sizeof(int));
                return temp;
            }
            set
            {
                byte[] temp = new byte[value.Length * sizeof(int)];
                Buffer.BlockCopy(value, 0, temp, 0, temp.Length);
                _SpectrumYCalculated = temp; OnPropertyChanged();
            }
        }

        [Column(CanBeNull = true, Storage = "_StopType")]
        public string StopType
        { get { return _StopType; } set { _StopType = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_StopValue")]
        public int StopValue
        { get { return _StopValue; } set { _StopValue = value; OnPropertyChanged(); } }

        [Column(Storage = "_EnergyCalOffset")]
        public double EnergyCalOffset
        { get { return _EnergyCalOffset; } set { _EnergyCalOffset = value; OnPropertyChanged(); } }

        [Column(Storage = "_EnergyCalSlope")]
        public double EnergyCalSlope
        { get { return _EnergyCalSlope; } set { _EnergyCalSlope = value; OnPropertyChanged(); } }

        [Column(Storage = "_IncomingIonNumber")]
        public int IncomingIonNumber
        { get { return _IncomingIonNumber; } set { _IncomingIonNumber = value; OnPropertyChanged(); } }

        [Column(Storage = "_IncomingIonMass")]
        public int IncomingIonMass
        { get { return _IncomingIonMass; } set { _IncomingIonMass = value; OnPropertyChanged(); } }

        [Column(Storage = "_IncomingIonEnergy")]
        public double IncomingIonEnergy
        { get { return _IncomingIonEnergy; } set { _IncomingIonEnergy = value; OnPropertyChanged(); } }

        [Column(Storage = "_IncomingIonAngle")]
        public double IncomingIonAngle
        { get { return _IncomingIonAngle; } set { _IncomingIonAngle = value; OnPropertyChanged(); } }

        [Column(Storage = "_OutcomingIonAngle")]
        public double OutcomingIonAngle
        { get { return _OutcomingIonAngle; } set { _OutcomingIonAngle = value; OnPropertyChanged(); } }

        [Column(Storage = "_SolidAngle")]
        public double SolidAngle
        { get { return _SolidAngle; } set { _SolidAngle = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_X")]
        public double? X
        { get { return _X; } set { _X = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_Y")]
        public double? Y
        { get { return _Y; } set { _Y = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_Phi")]
        public double? Phi
        { get { return _Phi; } set { _Phi = value; OnPropertyChanged(); } }

        [Column(CanBeNull = true, Storage = "_Theta")]
        public double? Theta
        { get { return _Theta; } set { _Theta = value; OnPropertyChanged(); } }

        public Chamber chamber;

        public readonly int[] SpectrumX ;
        public double[] SpectrumCalX
        {
            get
            {
                double[] temp = new double[SpectrumX.Length];
                for (int i = 0; i < SpectrumX.Length; i++)
                    temp[i] = EnergyCalSlope * SpectrumX[i] + EnergyCalOffset;
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
        public Measurement(int channel, int incomingIonNumber, int incomingIonMass, double incomingIonEnergy, double incomingIonAngle, double outcomingIonAngle, double solidAngle, double energyCalOffset, double energyCalSlope, string stopType, int stopValue, bool runs, int numOfChannels)
        {
            Channel = channel;
            StartTime = DateTime.Now;
            IncomingIonNumber = incomingIonNumber;
            IncomingIonMass = incomingIonMass;
            IncomingIonEnergy = incomingIonEnergy;
            IncomingIonAngle = incomingIonAngle;
            OutcomingIonAngle = outcomingIonAngle;
            SolidAngle = solidAngle;
            EnergyCalOffset = energyCalOffset;
            EnergyCalSlope = energyCalSlope;
            StopType = stopType;
            StopValue = stopValue;
            Runs = runs;
            NumOfChannels = numOfChannels;
            SpectrumX = new int[numOfChannels];
            for (int i = 0; i < numOfChannels; i++) { SpectrumX[i] = i; }
        }

        public Measurement()
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

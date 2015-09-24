using System;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Threading;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using newRBS.ViewModels.Utils;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;
using System.Globalization;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.Reflection;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.ChannelConfigurationView"/>. They set the channel configurations and display waveforms.
    /// </summary>
    public class ChannelConfigurationViewModel : ViewModelBase
    {
        private Models.MeasureWaveform measureWaveform;

        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand SaveWaveformCommand { get; set; }

        public ICommand SendToDeviceCommand { get; set; }
        public ICommand SaveToFileCommand { get; set; }
        public ICommand LoadFromFileCommand { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        /// <summary>
        /// Contains all the plot data and the plot style of the OxyPlot in <see cref="Views.ChannelConfigurationView"/>.
        /// </summary>
        public PlotModel WaveformPlot { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ObservableCollection<int> channels { get; set; }

        public ObservableCollection<string> AP1Items { get; set; }
        public ObservableCollection<string> AP2Items { get; set; }
        public ObservableCollection<string> DP1Items { get; set; }
        public ObservableCollection<string> DP2Items { get; set; }

        private Models.ChannelParams _channelParams;
        public Models.ChannelParams channelParams
        {
            get { return _channelParams; }
            set { _channelParams = value; RaisePropertyChanged(); }
        }

        private string _selectedAP1; public string selectedAP1 { get { return _selectedAP1; } set { _selectedAP1 = value; RaisePropertyChanged(); } }
        private string _selectedAP2; public string selectedAP2 { get { return _selectedAP2; } set { _selectedAP2 = value; RaisePropertyChanged(); } }
        private string _selectedDP1; public string selectedDP1 { get { return _selectedDP1; } set { _selectedDP1 = value; RaisePropertyChanged(); } }
        private string _selectedDP2; public string selectedDP2 { get { return _selectedDP2; } set { _selectedDP2 = value; RaisePropertyChanged(); } }

        private int _selectedChannel;
        public int selectedChannel
        {
            get { return _selectedChannel; }
            set { _selectedChannel = value; channelParams = measureWaveform.GetChannelConfig(_selectedChannel); RaisePropertyChanged(); }
        }

        public ObservableCollection<NameValueClass> InputRange { get; set; }
        public ObservableCollection<NameValueClass> Decimation { get; set; }
        public ObservableCollection<NameValueClass> DigitalProbeGain { get; set; }
        public ObservableCollection<NameValueClass> TriggerFilterSmoothing { get; set; }
        public ObservableCollection<NameValueClass> BaselineMean { get; set; }
        public ObservableCollection<NameValueClass> PeakMean { get; set; }

        /// <summary>
        /// Constructor of the class. Sets up the commands, hooks up to events and sets the collections and selected items of the view.
        /// </summary>
        public ChannelConfigurationViewModel()
        {
            StartCommand = new RelayCommand(() => _StartCommand(), () => true);
            StopCommand = new RelayCommand(() => _StopCommand(), () => true);
            SaveWaveformCommand = new RelayCommand(() => _SaveWaveformCommand(), () => true);

            SendToDeviceCommand = new RelayCommand(() => _SendToDeviceCommand(), () => true);
            SaveToFileCommand = new RelayCommand(() => _SaveToFileCommand(), () => true);
            LoadFromFileCommand = new RelayCommand(() => _LoadFromFileCommand(), () => true);

            measureWaveform = SimpleIoc.Default.GetInstance<Models.MeasureWaveform>();

            // Hooking up to events from MeasureWaveform
            measureWaveform.EventWaveform += new Models.MeasureWaveform.EventHandlerWaveform(WaveformUpdate);

            AP1Items = new ObservableCollection<string>(Enum.GetNames(typeof(Models.CAENDPP_PHA_AnalogProbe1_t)).ToList());
            AP2Items = new ObservableCollection<string>(Enum.GetNames(typeof(Models.CAENDPP_PHA_AnalogProbe2_t)).ToList());
            DP1Items = new ObservableCollection<string>(Enum.GetNames(typeof(Models.CAENDPP_PHA_DigitalProbe1_t)).ToList());
            DP2Items = new ObservableCollection<string>(Enum.GetNames(typeof(Models.CAENDPP_PHA_DigitalProbe2_t)).ToList());

            selectedAP1 = Enum.GetName(typeof(Models.CAENDPP_PHA_AnalogProbe1_t), Models.CAENDPP_PHA_AnalogProbe1_t.Delta2);
            selectedAP2 = Enum.GetName(typeof(Models.CAENDPP_PHA_AnalogProbe2_t), Models.CAENDPP_PHA_AnalogProbe2_t.Input);
            selectedDP1 = Enum.GetName(typeof(Models.CAENDPP_PHA_DigitalProbe1_t), Models.CAENDPP_PHA_DigitalProbe1_t.Peaking);
            selectedDP2 = Enum.GetName(typeof(Models.CAENDPP_PHA_DigitalProbe2_t), Models.CAENDPP_PHA_DigitalProbe2_t.Trigger);

            channels = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            selectedChannel = 0;

            InputRange = new ObservableCollection<NameValueClass>() { new NameValueClass("2.0", 9), new NameValueClass("0.5", 10) };
            Decimation = new ObservableCollection<NameValueClass>() { new NameValueClass("1", 0), new NameValueClass("2", 1), new NameValueClass("4", 2), new NameValueClass("8", 3) };
            DigitalProbeGain = new ObservableCollection<NameValueClass>() { new NameValueClass("1", 0), new NameValueClass("2", 1), new NameValueClass("4", 2), new NameValueClass("8", 3) };
            TriggerFilterSmoothing = new ObservableCollection<NameValueClass>() { new NameValueClass("4", 4), new NameValueClass("8", 8), new NameValueClass("16", 16), new NameValueClass("32", 32) };
            BaselineMean = new ObservableCollection<NameValueClass>() { new NameValueClass("0", 0), new NameValueClass("16", 1), new NameValueClass("64", 2), new NameValueClass("256", 3), new NameValueClass("1024", 4), new NameValueClass("4096", 5), new NameValueClass("16384", 6) };
            PeakMean = new ObservableCollection<NameValueClass>() { new NameValueClass("1", 0), new NameValueClass("4", 1), new NameValueClass("16", 2), new NameValueClass("64", 3) };

            channelParams = measureWaveform.GetChannelConfig(_selectedChannel);

            WaveformPlot = new PlotModel();
            SetUpModel();
        }

        /// <summary>
        /// Function that configures the OxyPlot <see cref="PlotModel"/> <see cref="WaveformPlot"/>.
        /// </summary>
        public void SetUpModel()
        {
            WaveformPlot.LegendOrientation = LegendOrientation.Vertical;
            WaveformPlot.LegendPlacement = LegendPlacement.Inside;
            WaveformPlot.LegendPosition = LegendPosition.TopRight;
            WaveformPlot.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
            WaveformPlot.LegendBorder = OxyColors.Black;

            var xAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Time (µs)", TitleFontSize = 16, AxisTitleDistance = 8, Minimum = 0 };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, Title = "LSB", TitleFontSize = 16, AxisTitleDistance = 12 };

            WaveformPlot.Axes.Add(xAxis);
            WaveformPlot.Axes.Add(yAxis);

            var AP1Series = new LineSeries
            { Tag = "AP1Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            var AP2Series = new LineSeries
            { Tag = "AP2Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            var DP1Series = new LineSeries
            { Tag = "DP1Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            var DP2Series = new LineSeries
            { Tag = "DP2Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            WaveformPlot.Series.Add(AP1Series);
            WaveformPlot.Series.Add(AP2Series);
            WaveformPlot.Series.Add(DP1Series);
            WaveformPlot.Series.Add(DP2Series);
        }

        /// <summary>
        /// Updates the y-data of the four line plots in <see cref="WaveformPlot"/> with the data of in <see cref="Models.Waveform"/> instance.
        /// </summary>
        /// <param name="Waveform">A <see cref="Models.Waveform"/> containing the data for the four waveform plots.</param>
        public void WaveformUpdate(Models.Waveform Waveform)
        {
            double x;

            if (Waveform.NumSamples == 0)
                return;

            (WaveformPlot.Series[0] as LineSeries).Points.Clear();
            (WaveformPlot.Series[1] as LineSeries).Points.Clear();
            (WaveformPlot.Series[2] as LineSeries).Points.Clear();
            (WaveformPlot.Series[3] as LineSeries).Points.Clear();

            for (int i = 0; i < Waveform.NumSamples; i++)
            {
                x = i * Waveform.LenSample / 1000;
                (WaveformPlot.Series[0] as LineSeries).Points.Add(new DataPoint(x, Waveform.AT1[i]));
                (WaveformPlot.Series[1] as LineSeries).Points.Add(new DataPoint(x, Waveform.AT2[i]));
                (WaveformPlot.Series[2] as LineSeries).Points.Add(new DataPoint(x, 1000 * Waveform.DT1[i]));
                (WaveformPlot.Series[3] as LineSeries).Points.Add(new DataPoint(x, 1000 * Waveform.DT2[i]));
            }
            WaveformPlot.InvalidatePlot(true);
        }

        /// <summary>
        /// Function that starts the waveform acquisition for the <see cref="selectedChannel"/>.
        /// </summary>
        public void _StartCommand()
        {
            WaveformPlot.Series[0].Title = selectedAP1;
            WaveformPlot.Series[1].Title = selectedAP2;
            WaveformPlot.Series[2].Title = selectedDP1;
            WaveformPlot.Series[3].Title = selectedDP2;

            Models.CAENDPP_PHA_AnalogProbe1_t AP1 = (Models.CAENDPP_PHA_AnalogProbe1_t)Enum.Parse(typeof(Models.CAENDPP_PHA_AnalogProbe1_t), selectedAP1, true);
            Models.CAENDPP_PHA_AnalogProbe2_t AP2 = (Models.CAENDPP_PHA_AnalogProbe2_t)Enum.Parse(typeof(Models.CAENDPP_PHA_AnalogProbe2_t), selectedAP2, true);
            Models.CAENDPP_PHA_DigitalProbe1_t DP1 = (Models.CAENDPP_PHA_DigitalProbe1_t)Enum.Parse(typeof(Models.CAENDPP_PHA_DigitalProbe1_t), selectedDP1, true);
            Models.CAENDPP_PHA_DigitalProbe2_t DP2 = (Models.CAENDPP_PHA_DigitalProbe2_t)Enum.Parse(typeof(Models.CAENDPP_PHA_DigitalProbe2_t), selectedDP2, true);
            measureWaveform.SetWaveformConfig(AP1, AP2, DP1, DP2, false);
            measureWaveform.StartAcquisition(selectedChannel);
        }

        /// <summary>
        /// Function that stops the waveform acquisition.
        /// </summary>
        public void _StopCommand()
        {
            measureWaveform.StopAcquisition();
        }

        /// <summary>
        /// Function that opens a save file dialog and saves the current <see cref="Models.Waveform"/> as an ASCII file.
        /// </summary>
        public void _SaveWaveformCommand()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "ASCII file (*.dat)|*.dat";
            if (saveFileDialog.ShowDialog() == true)
            {
                XmlSerializer SerializerObj = new XmlSerializer(typeof(Models.ChannelParams));

                TextWriter WriteFileStream = new StreamWriter(saveFileDialog.FileName.Replace(".dat", "-ChannelConfiguration.xml"));
                SerializerObj.Serialize(WriteFileStream, channelParams);
                WriteFileStream.Close();

                WriteFileStream = new StreamWriter(saveFileDialog.FileName);
                WriteFileStream.WriteLine("Acquisition time\t{0}", measureWaveform.waveform.AcquisitionTime.ToString("yyyy-MM-dd HH:mm:ss"));
                WriteFileStream.WriteLine("Acquisition channel\t{0}", measureWaveform.waveform.AcquisitionChannel);
                WriteFileStream.WriteLine("Number of samples\t{0}", measureWaveform.waveform.NumSamples);
                WriteFileStream.WriteLine("Length of a sample (ns)\t{0}", measureWaveform.waveform.LenSample);
                WriteFileStream.WriteLine("Sample number\t{0}\t{1}\t{2}\t{3}", selectedAP1, selectedAP2, selectedDP1, selectedDP2);
                for (int i = 0; i < measureWaveform.waveform.AT1.Count(); i++)
                    WriteFileStream.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}", i, measureWaveform.waveform.AT1[i], measureWaveform.waveform.AT2[i], measureWaveform.waveform.DT1[i], measureWaveform.waveform.DT2[i]);

                WriteFileStream.Close();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Waveform saved to file");
            }
        }

        /// <summary>
        /// Function that sends the <see cref="channelParams"/> and the <see cref="selectedChannel"/> to <see cref="MeasureWaveform.SetChannelConfig"/>
        /// </summary>
        public void _SendToDeviceCommand()
        {
            measureWaveform.SetChannelConfig(_selectedChannel, channelParams);
        }

        /// <summary>
        /// Function that saves the current <see cref="channelParams"/> instance to an '.xml' file.
        /// </summary>
        public void _SaveToFileCommand()
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(Models.ChannelParams));

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML file (*.xml)|*.xml";
            if (saveFileDialog.ShowDialog() == true)
            {
                TextWriter WriteFileStream = new StreamWriter(saveFileDialog.FileName);
                SerializerObj.Serialize(WriteFileStream, channelParams);
                WriteFileStream.Close();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Channel configuration saved to file");
            }
        }

        /// <summary>
        /// Function that loads the current <see cref="channelParams"/> instance from an '.xml' file.
        /// </summary>
        public void _LoadFromFileCommand()
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(Models.ChannelParams));

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML file (*.xml)|*.xml";
            if (openFileDialog.ShowDialog() == true)
            {
                FileStream ReadFileStream = new FileStream(openFileDialog.FileName, FileMode.Open);
                channelParams = (Models.ChannelParams)SerializerObj.Deserialize(ReadFileStream);
                ReadFileStream.Close();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Channel configuration read from file");
            }
        }
    }
}

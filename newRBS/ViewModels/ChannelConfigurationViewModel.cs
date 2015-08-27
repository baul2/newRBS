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

namespace newRBS.ViewModels
{
    public class ChannelConfigurationViewModel : ViewModelBase
    {
        private Models.MeasureWaveform measureWaveform;

        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand SaveWaveformCommand { get; set; }

        public ICommand SendToDeviceCommand { get; set; }
        public ICommand SaveToFileCommand { get; set; }
        public ICommand LoadFromFileCommand { get; set; }

        public PlotModel plotModel { get; set; }

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
            set
            {
                _selectedChannel = value;
                channelParams = measureWaveform.GetChannelConfig(_selectedChannel);
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<NameValueClass> InputRange { get; set; }
        public ObservableCollection<NameValueClass> Decimation { get; set; }
        public ObservableCollection<NameValueClass> DigitalProbeGain { get; set; }
        public ObservableCollection<NameValueClass> TriggerFilterSmoothing { get; set; }
        public ObservableCollection<NameValueClass> BaselineMean { get; set; }
        public ObservableCollection<NameValueClass> PeakMean { get; set; }

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

            plotModel = new PlotModel();
            SetUpModel();
        }

        private void SetUpModel()
        {
            plotModel.LegendOrientation = LegendOrientation.Vertical;
            plotModel.LegendPlacement = LegendPlacement.Inside;
            plotModel.LegendPosition = LegendPosition.TopRight;
            plotModel.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
            plotModel.LegendBorder = OxyColors.Black;

            var xAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Time (µs)", TitleFontSize = 16, AxisTitleDistance = 8, Minimum = 0 };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, Title = "LSB", TitleFontSize = 16, AxisTitleDistance = 12 };

            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);

            var AP1Series = new LineSeries
            { Tag = "AP1Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            var AP2Series = new LineSeries
            { Tag = "AP2Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            var DP1Series = new LineSeries
            { Tag = "DP1Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            var DP2Series = new LineSeries
            { Tag = "DP2Series", StrokeThickness = 2, MarkerSize = 3, CanTrackerInterpolatePoints = false, Smooth = false, };

            plotModel.Series.Add(AP1Series);
            plotModel.Series.Add(AP2Series);
            plotModel.Series.Add(DP1Series);
            plotModel.Series.Add(DP2Series);
        }

        private void WaveformUpdate(Models.Waveform waveform)
        {
            double x;

            if (waveform.NumSamples == 0)
                return;

            (plotModel.Series[0] as LineSeries).Points.Clear();
            (plotModel.Series[1] as LineSeries).Points.Clear();
            (plotModel.Series[2] as LineSeries).Points.Clear();
            (plotModel.Series[3] as LineSeries).Points.Clear();

            for (int i = 0; i < waveform.NumSamples; i++)
            {
                x = i * waveform.LenSample / 1000;
                (plotModel.Series[0] as LineSeries).Points.Add(new DataPoint(x, waveform.AT1[i]));
                (plotModel.Series[1] as LineSeries).Points.Add(new DataPoint(x, waveform.AT2[i]));
                (plotModel.Series[2] as LineSeries).Points.Add(new DataPoint(x, 1000 * waveform.DT1[i]));
                (plotModel.Series[3] as LineSeries).Points.Add(new DataPoint(x, 1000 * waveform.DT2[i]));
            }
            plotModel.InvalidatePlot(true);
        }

        private void _StartCommand()
        {
            plotModel.Series[0].Title = selectedAP1;
            plotModel.Series[1].Title = selectedAP2;
            plotModel.Series[2].Title = selectedDP1;
            plotModel.Series[3].Title = selectedDP2;

            Models.CAENDPP_PHA_AnalogProbe1_t AP1 = (Models.CAENDPP_PHA_AnalogProbe1_t)Enum.Parse(typeof(Models.CAENDPP_PHA_AnalogProbe1_t), selectedAP1, true);
            Models.CAENDPP_PHA_AnalogProbe2_t AP2 = (Models.CAENDPP_PHA_AnalogProbe2_t)Enum.Parse(typeof(Models.CAENDPP_PHA_AnalogProbe2_t), selectedAP2, true);
            Models.CAENDPP_PHA_DigitalProbe1_t DP1 = (Models.CAENDPP_PHA_DigitalProbe1_t)Enum.Parse(typeof(Models.CAENDPP_PHA_DigitalProbe1_t), selectedDP1, true);
            Models.CAENDPP_PHA_DigitalProbe2_t DP2 = (Models.CAENDPP_PHA_DigitalProbe2_t)Enum.Parse(typeof(Models.CAENDPP_PHA_DigitalProbe2_t), selectedDP2, true);
            measureWaveform.SetWaveformConfig(AP1, AP2, DP1, DP2, false);
            measureWaveform.StartAcquisition(selectedChannel);
        }

        private void _StopCommand()
        {
            measureWaveform.StopAcquisition();
        }

        private void _SaveWaveformCommand()
        {
            Console.WriteLine("_SaveWaveformCommand");

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
            }
        }

        private void _SendToDeviceCommand()
        {
            measureWaveform.SetChannelConfig(_selectedChannel, channelParams);
        }

        private void _SaveToFileCommand()
        {
            XmlSerializer SerializerObj = new XmlSerializer(typeof(Models.ChannelParams));

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "XML file (*.xml)|*.xml";
            if (saveFileDialog.ShowDialog() == true)
            {
                TextWriter WriteFileStream = new StreamWriter(saveFileDialog.FileName);
                SerializerObj.Serialize(WriteFileStream, channelParams);
                WriteFileStream.Close();
            }
        }

        private void _LoadFromFileCommand()
        {
            Console.WriteLine("_LoadFromFileCommand");

            XmlSerializer SerializerObj = new XmlSerializer(typeof(Models.ChannelParams));

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML file (*.xml)|*.xml";
            if (openFileDialog.ShowDialog() == true)
            {
                FileStream ReadFileStream = new FileStream(openFileDialog.FileName, FileMode.Open);
                channelParams = (Models.ChannelParams)SerializerObj.Deserialize(ReadFileStream);
                ReadFileStream.Close();
            }
        }
    }
}

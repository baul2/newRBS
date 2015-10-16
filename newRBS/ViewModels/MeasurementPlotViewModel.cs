﻿using System;
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
using System.Timers;
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
using System.Diagnostics;
using newRBS.Database;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is the view model of <see cref="Views.MeasurementPlotView"/>. They plot the selected <see cref="Measurement"/>s.
    /// </summary>
    public class MeasurementPlotViewModel : ViewModelBase
    {
        public ICommand ExpandConfigPanel { get; set; }

        private RelayCommand<EventArgs> _plotSizeChangedCommand;
        public RelayCommand<EventArgs> PlotSizeChangedCommand
        {
            get
            {
                return _plotSizeChangedCommand
                  ?? (_plotSizeChangedCommand = new RelayCommand<EventArgs>(
                    eventargs => { YAxisUpdated(); }));
            }
        }

        private bool _ConfigPanelVis = true;
        public bool ConfigPanelVis
        {
            get { return _ConfigPanelVis; }
            set
            {
                _ConfigPanelVis = value;
                switch (value)
                {
                    case true:
                        { VisButtonContent = ">\n>\n>"; break; }
                    case false:
                        { VisButtonContent = "<\n<\n<"; break; }
                }
                RaisePropertyChanged();
            }
        }

        private string _VisButtonContent = ">\n>\n>";
        public string VisButtonContent
        { get { return _VisButtonContent; } set { _VisButtonContent = value; RaisePropertyChanged(); } }

        /// <summary>
        /// List of <see cref="Measurement.MeasurementID"/> of the plottet <see cref="Measurement"/>s.
        /// </summary>
        private List<int> MeasurementIDList;

        public PlotModel MeasurementsPlotModel { get; set; }

        public PlotModel TimePlotModel { get; set; }

        private List<OxyColor> LineColors { get; set; }

        private static Timer OfflineUpdateTimer = new Timer(MyGlobals.OfflineUpdateWorkerInterval);

        TraceSource trace = new TraceSource("MeasurementPlotViewModel");

        public List<NameValueClass> DataBindingIntervals { get; set; }

        private int _SelectedDataBindingInterval = 2;
        public int SelectedDataBindingInterval
        { get { return _SelectedDataBindingInterval; } set { _SelectedDataBindingInterval = value; UpdateAllPlots(); RaisePropertyChanged(); UpdateYAxisTitle(); } }

        public List<string> YAxisScale { get; set; }

        private string _SelectedYAxisScale = "linear";
        public string SelectedYAxisScale
        { get { return _SelectedYAxisScale; } set { _SelectedYAxisScale = value; RaisePropertyChanged(); UpdateYAxisScale(); UpdateYAxisTitle(); } }

        public List<string> LegendCaptions { get; set; }

        private string _SelectedLegendCaption = "Measurement IDs";
        public string SelectedLegendCaption
        { get { return _SelectedLegendCaption; } set { _SelectedLegendCaption = value; RaisePropertyChanged(); UpdateLegend(); } }

        private double _CutOffCountsPercent = 1;
        public double CutOffCountsPercent
        { get { return _CutOffCountsPercent; } set { _CutOffCountsPercent = value; RaisePropertyChanged(); UpdateAllPlots(); } }

        private bool _ShowSimulatedSpectra = true;
        public bool ShowSimulatedSpectra
        { get { return _ShowSimulatedSpectra; } set { _ShowSimulatedSpectra = value; RaisePropertyChanged(); UpdateAllPlots(); } }

        private bool _ShowElementPositions = false;
        public bool ShowElementPositions
        { get { return _ShowElementPositions; } set { _ShowElementPositions = value; RaisePropertyChanged(); UpdateElementPositions(); } }

        /// <summary>
        /// Constructor of the class. Hooks up to events, sets up commands and initialises variables.
        /// </summary>
        public MeasurementPlotViewModel()
        {
            // Hooking up to events from DatabaseUtils 
            DatabaseUtils.EventMeasurementRemove += new DatabaseUtils.EventHandlerMeasurement(MeasurementNotToPlot);
            DatabaseUtils.EventMeasurementUpdate += new DatabaseUtils.EventHandlerMeasurement(UpdatePlot);

            // Hooking up to events from SpectraList
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().EventMeasurementToPlot += new MeasurementListViewModel.EventHandlerMeasurement(MeasurementToPlot);
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().EventMeasurementNotToPlot += new MeasurementListViewModel.EventHandlerMeasurement(MeasurementNotToPlot);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().EventNewFilter += new MeasurementFilterViewModel.EventHandlerFilter(ClearPlot);

            ExpandConfigPanel = new RelayCommand(() => _ExpandConfigPanel(), () => true);

            //SetYOriginTo0Command = new RelayCommand(() => _SetYOriginTo0Command(), () => true); 

            MeasurementIDList = new List<int>();

            LineColors = new List<OxyColor>
            {
                OxyColor.FromRgb(0x4E, 0x9A, 0x06),
                OxyColor.FromRgb(0xC8, 0x8D, 0x00),
                OxyColor.FromRgb(0xCC, 0x00, 0x00),
                OxyColor.FromRgb(0x20, 0x4A, 0x87),
                OxyColors.Red,
                OxyColors.Orange,
                OxyColors.Yellow,
                OxyColors.Green,
                OxyColors.Blue,
                OxyColors.Indigo,
                OxyColors.Violet
            };

            MyGlobals.Charge_CountsOverTime = new List<TimeSeriesEvent>();

            SetUpMeasurementsPlotModel();
            SetUpTimePlotModel();

            DataBindingIntervals = new List<NameValueClass> { new NameValueClass("none", 0), new NameValueClass("1keV", 1), new NameValueClass("2keV", 2), new NameValueClass("5keV", 5), new NameValueClass("10keV", 10) };

            YAxisScale = new List<string> { "linear", "logarithmic" };

            LegendCaptions = new List<string> { "Measurement IDs", "Measurement names", "Sample names", "Sample remarks", "Sample names + remarks" };
        }

        /// <summary>
        /// Function that toggles the ConfigPanel visibility.
        /// </summary>
        public void _ExpandConfigPanel()
        {
            ConfigPanelVis = !ConfigPanelVis;
        }

        /// <summary>
        /// Function that sets up the <see cref="MeasurementsPlotModel"/>.
        /// </summary>
        public void SetUpMeasurementsPlotModel()
        {
            MeasurementsPlotModel = new PlotModel
            {
                LegendOrientation = LegendOrientation.Vertical,
                LegendPlacement = LegendPlacement.Inside,
                LegendPosition = LegendPosition.TopRight,
                LegendBackground = OxyColor.FromAColor(200, OxyColors.White),
                LegendBorder = OxyColors.Black
            };

            var xAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Energy (keV)", TitleFontSize = 16, AxisTitleDistance = 8, AbsoluteMinimum = 0 };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, TitleFontSize = 16, AxisTitleDistance = 18, Minimum = 0, AbsoluteMinimum = 0 };
            yAxis.AxisChanged += (o, e) => YAxisUpdated();

            MeasurementsPlotModel.Axes.Add(xAxis);
            MeasurementsPlotModel.Axes.Add(yAxis);
            UpdateYAxisTitle();
        }

        /// <summary>
        /// Function that sets up the <see cref="TimePlotModel"/>.
        /// </summary>
        public void SetUpTimePlotModel()
        {
            TimePlotModel = new PlotModel();

            var xAxis = new DateTimeAxis() { Position = AxisPosition.Bottom, IntervalLength = 40, StringFormat = "HH:mm" };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, IntervalLength = 30 };

            TimePlotModel.Axes.Add(xAxis);
            TimePlotModel.Axes.Add(yAxis);

            var lineSeries = new LineSeries { TrackerFormatString = "Time: {2:HH:mm}\n{3}: {4:0.###}" };

            TimePlotModel.Series.Add(lineSeries);

            TimePlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Function that is executed whenever a <see cref="Measurement"/> has to be plotted.
        /// </summary>
        /// <param name="measurement"><see cref="Measurement"/> to be plotted.</param>
        public void MeasurementToPlot(Measurement measurement)
        {
            if (MeasurementIDList.Contains(measurement.MeasurementID)) return;

            MeasurementIDList.Add(measurement.MeasurementID);
            PlotMeasurement(measurement);
        }

        /// <summary>
        /// Function that is executed whenever a <see cref="Measurement"/> has not to be plotted.
        /// </summary>
        /// <param name="measurement"><see cref="Measurement"/> not to be plotted.</param>
        public void MeasurementNotToPlot(Measurement measurement)
        {
            MeasurementIDList.Remove(measurement.MeasurementID);

            var delSerie = MeasurementsPlotModel.Series.Where(x => ((Measurement)x.Tag).MeasurementID == measurement.MeasurementID).FirstOrDefault();
            if (delSerie != null)
            {
                MeasurementsPlotModel.Series.Remove(delSerie);

                MeasurementsPlotModel.InvalidatePlot(true);
            }

            delSerie = MeasurementsPlotModel.Series.Where(x => ((Measurement)x.Tag).MeasurementID == measurement.MeasurementID).FirstOrDefault();
            if (delSerie != null)
            {
                MeasurementsPlotModel.Series.Remove(delSerie);

                MeasurementsPlotModel.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Function that cleas the <see cref="MeasurementsPlotModel"/>.
        /// </summary>
        /// <param name="dump"></param>
        public void ClearPlot(List<int> dump)
        {
            MeasurementsPlotModel.Series.Clear();
            MeasurementIDList.Clear();
            MeasurementsPlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Function that plots a <see cref="Measurement"/> to the <see cref="MeasurementsPlotModel"/>.
        /// </summary>
        /// <param name="measurement"><see cref="Measurement"/> to be plotted.</param>
        public void PlotMeasurement(Measurement measurement)
        {
            if (SelectedDataBindingInterval > 0 && measurement.EnergyCalLinear > SelectedDataBindingInterval)
            { MessageBox.Show("Selected data binding interval is smaller than the actual channel spacing!", "Error"); SelectedDataBindingInterval = 0; return; }

            OfflineUpdateTimer.Stop();

            var MeassuredPlot = new AreaSeries
            {
                Tag = measurement,
                StrokeThickness = 2,
                MarkerSize = 3,
                Color = LineColors[measurement.MeasurementID % LineColors.Count],
                CanTrackerInterpolatePoints = false,
                Title = GetMeasurementTitle(measurement),
                Smooth = false,
            };

            var SimulatedPlot = new AreaSeries
            {
                Tag = measurement,
                StrokeThickness = 2,
                MarkerSize = 3,
                Color = LineColors[measurement.MeasurementID % LineColors.Count],
                CanTrackerInterpolatePoints = false,
                Title = GetMeasurementTitle(measurement) + " (Sim.)",
                Smooth = false,
            };

            float[] spectrumX = measurement.SpectrumXCal;
            int[] spectrumY = measurement.SpectrumY;
            int[] spectrumYCalculated = measurement.SpectrumYSimulated;

            if (spectrumYCalculated == null || ShowSimulatedSpectra == false)
            {
                spectrumYCalculated = new int[measurement.NumOfChannels];
            }

            // Remove "Counts<CutOffCounts" data points from start/end of the spectra
            int BorderOffset = 200;

            double CutOffCounts = spectrumY.Max() * CutOffCountsPercent / 100;
            if (CutOffCounts == 0) CutOffCounts = 1;

            int rightBorderIndex = spectrumY.Count();
            while (spectrumY[rightBorderIndex - 1] < CutOffCounts && rightBorderIndex > 2) rightBorderIndex--;

            int leftBorderIndex = 0;
            while (spectrumY[leftBorderIndex] < CutOffCounts && leftBorderIndex < spectrumY.Count() - 2) leftBorderIndex++;

            if (rightBorderIndex < 5) rightBorderIndex = spectrumY.Count();
            if (leftBorderIndex > spectrumY.Count() - 5) leftBorderIndex = 0;

            if (rightBorderIndex + BorderOffset < spectrumY.Count()) rightBorderIndex += BorderOffset; else rightBorderIndex = spectrumY.Count();
            if (leftBorderIndex - BorderOffset > 0) leftBorderIndex -= BorderOffset; else leftBorderIndex = 0;

            // Add points to plot
            switch (SelectedDataBindingInterval)
            {
                case 0: // Average undistinguishable points in spectrumX/spectrumY
                    {
                        var XAxis = MeasurementsPlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Bottom);

                        int XPlotWidth = (int)(XAxis.ScreenMax.X - XAxis.ScreenMin.X);

                        int XDataWidth = rightBorderIndex - leftBorderIndex;

                        int AverageCount = (int)Math.Floor((double)XDataWidth / XPlotWidth / 2);
                        if (AverageCount == 0) AverageCount = 1;

                        int Count = 0;
                        int newY = 0;
                        int newYCalculated = 0;

                        for (int i = leftBorderIndex; i < rightBorderIndex; i++)
                        {
                            newY += spectrumY[i];
                            newYCalculated = +spectrumYCalculated[i];

                            if (Count < AverageCount - 1)
                            {
                                Count++;
                            }
                            else
                            {
                                if (newY == 0) newY = 1;
                                if (newYCalculated == 0) newYCalculated = 1;

                                MeassuredPlot.Points.Add(new DataPoint(spectrumX[i], (double)newY / AverageCount));
                                MeassuredPlot.Points2.Add(new DataPoint(spectrumX[i], (float)0.0001));

                                SimulatedPlot.Points.Add(new DataPoint(spectrumX[i], (double)newYCalculated / AverageCount));
                                SimulatedPlot.Points2.Add(new DataPoint(spectrumX[i], (float)0.0001));

                                Count = 0;
                                newY = 0;
                                newYCalculated = 0;
                            }
                        }
                        break;
                    }
                default: // Bind points inside SelectedDataBinding intervall
                    {
                        float x, y = 0, yCalculated = 0;
                        float intervalStart = SelectedDataBindingInterval * ((int)(spectrumX[leftBorderIndex] / SelectedDataBindingInterval) + (float)0.5); ;
                        int numOfPoints = 0;

                        for (int i = leftBorderIndex; i < rightBorderIndex; i++)
                        {
                            if (spectrumX[i] - intervalStart > SelectedDataBindingInterval)
                            {
                                x = intervalStart + (float)SelectedDataBindingInterval / 2;

                                if (y == 0) y = (float)0.0001;
                                if (yCalculated == 0) yCalculated = (float)0.0001;

                                MeassuredPlot.Points.Add(new DataPoint(x, y / numOfPoints * (SelectedDataBindingInterval / measurement.EnergyCalLinear)));
                                MeassuredPlot.Points2.Add(new DataPoint(x, (float)0.0001));

                                SimulatedPlot.Points.Add(new DataPoint(x, yCalculated / numOfPoints * (SelectedDataBindingInterval / measurement.EnergyCalLinear)));
                                SimulatedPlot.Points2.Add(new DataPoint(x, (float)0.0001));

                                y = 0;
                                yCalculated = 0;
                                numOfPoints = 0;
                                intervalStart += SelectedDataBindingInterval;
                            }
                            y += spectrumY[i];
                            yCalculated += spectrumYCalculated[i];
                            numOfPoints++;
                        }
                        break;
                    }
            }
            MeasurementsPlotModel.Series.Add(MeassuredPlot);
            if (measurement.SpectrumYSimulated != null && ShowSimulatedSpectra == true)
                MeasurementsPlotModel.Series.Add(SimulatedPlot);
            MeasurementsPlotModel.InvalidatePlot(true);

            // Check if measurement is running on another computer -> update measurement periodically
            if (measurement.Runs == true && MyGlobals.CanMeasure == false)
            {
                OfflineUpdateTimer = new Timer(MyGlobals.OfflineUpdateWorkerInterval);
                OfflineUpdateTimer.Elapsed += delegate { OfflineUpdateWorker(measurement.MeasurementID); };
                OfflineUpdateTimer.Start();
            }

            // Update TimePlotModel
            var lineSeries = (LineSeries)TimePlotModel.Series.FirstOrDefault();

            if (lineSeries.Points.Count() != MyGlobals.Charge_CountsOverTime.Count() && MyGlobals.Charge_CountsOverTime.Count() >= 2)
            {
                lineSeries.Points.Clear();
                foreach (var temp in MyGlobals.Charge_CountsOverTime)
                {
                    if (temp.Value > 0)
                    {
                        lineSeries.Points.Add(new DataPoint(DateTimeAxis.ToDouble(temp.Time), temp.Value));
                    }
                }
                TimePlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Minimum = 0.7 * MyGlobals.Charge_CountsOverTime.Where(x => x.Value > 0).Min(x => x.Value);
                TimePlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Maximum = 1.3 * MyGlobals.Charge_CountsOverTime.Max(x => x.Value);

                TimePlotModel.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Function that is executed whenever a <see cref="Measurement"/> was updated. It updates the corresponding plot in <see cref="MeasurementsPlotModel"/>.
        /// </summary>
        /// <param name="measurement"></param>
        public void UpdatePlot(Measurement measurement)
        {
            if (!MeasurementIDList.Contains(measurement.MeasurementID))
                return;

            List<Series> updateSeries = MeasurementsPlotModel.Series.Where(x => ((Measurement)x.Tag).MeasurementID == measurement.MeasurementID).ToList();
            foreach (Series updateSerie in updateSeries)
            {
                MeasurementsPlotModel.Series.Remove(updateSerie);
            }
            PlotMeasurement(measurement);
        }

        /// <summary>
        /// Function that sets the y-axis of the plot to either 'linear' or 'logarithmic'.
        /// </summary>
        public void UpdateYAxisScale()
        {
            switch (SelectedYAxisScale)
            {
                case "linear":
                    {
                        MeasurementsPlotModel.Axes.Remove(MeasurementsPlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left));
                        var yAxis = new LinearAxis() { Position = AxisPosition.Left, TitleFontSize = 16, AxisTitleDistance = 18, Minimum = 0 };
                        MeasurementsPlotModel.Axes.Add(yAxis);
                        break;
                    }
                case "logarithmic":
                    {
                        MeasurementsPlotModel.Axes.Remove(MeasurementsPlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left));
                        var yAxis = new LogarithmicAxis() { Position = AxisPosition.Left, TitleFontSize = 16, AxisTitleDistance = 18, Minimum = 1 };
                        MeasurementsPlotModel.Axes.Add(yAxis);
                        break;
                    }
            }
        }

        /// <summary>
        /// Function that sets the y-axis title to either 'Counts per channel' or 'Counts per interval'.
        /// </summary>
        public void UpdateYAxisTitle()
        {
            switch (SelectedDataBindingInterval)
            {
                case 0:
                    {
                        MeasurementsPlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title = "Counts per channel";
                        break;
                    }
                default:
                    {
                        MeasurementsPlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title = string.Format("Counts per {0}keV interval", SelectedDataBindingInterval);
                        break;
                    }
            }
        }

        /// <summary>
        /// Function that determines the title of a <see cref="Measurement"/> plot according to the <see cref="SelectedLegendCaption"/>.
        /// </summary>
        /// <param name="measurement"><see cref="Measurement"/> for which the title is determined.</param>
        /// <returns></returns>
        private string GetMeasurementTitle(Measurement measurement)
        {
            if (measurement == null) return "";

            switch (SelectedLegendCaption)
            {
                case "Measurement IDs":
                    return "MeasurementID " + measurement.MeasurementID;
                case "Measurement names":
                    return measurement.MeasurementName;
                case "Sample names":
                    return measurement.Sample.SampleName;
                case "Sample remarks":
                    return measurement.SampleRemark;
                case "Sample names + remarks":
                    return measurement.Sample.SampleName + " " + measurement.SampleRemark;
                default:
                    return "MeasurementID " + measurement.MeasurementID;
            }
        }

        /// <summary>
        /// Function that updates the plot titles.
        /// </summary>
        public void UpdateLegend()
        {
            foreach (var plot in MeasurementsPlotModel.Series)
            {
                plot.Title = GetMeasurementTitle((Measurement)plot.Tag);
            }
        }

        /// <summary>
        /// Function that redraws the whole plot.
        /// </summary>
        public void UpdateAllPlots()
        {
            if (MeasurementIDList.Count() == 0) return;

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                List<Measurement> measurements = Database.Measurements.Where(x => MeasurementIDList.Contains(x.MeasurementID)).ToList();

                MeasurementsPlotModel.Series.Clear();

                foreach (Measurement measurement in measurements)
                {
                    PlotMeasurement(measurement);
                }
            }
        }

        /// <summary>
        /// Function that draws or clears the annotation for the element surface channel position.
        /// </summary>
        public void UpdateElementPositions()
        {
            if (ShowElementPositions == true)
            {
                var yAxis = MeasurementsPlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left);
                var xAxis = MeasurementsPlotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Bottom);

                double currentPixelPerXAxisUnit = MeasurementsPlotModel.PlotArea.Width / (xAxis.ActualMaximum - xAxis.ActualMinimum);
                double currentPixelPerYAxisUnit = MeasurementsPlotModel.PlotArea.Height / (yAxis.ActualMaximum - yAxis.ActualMinimum);

                double currentMax = yAxis.ActualMaximum;

                using (DatabaseDataContext Database = MyGlobals.Database)
                {
                    Measurement measurement = Database.Measurements.FirstOrDefault(x => MeasurementIDList.Contains(x.MeasurementID));
                    double lastXPosition = 0;

                    var elements = Database.Isotopes
                        .Where(x => x.MassNumber == 0)
                        .ToList()
                        .Select(x => new { ElementName = x.Element.ShortName, ElementEnergy = measurement.IncomingIonEnergy * MyGlobals.KineFak(measurement.Isotope.Mass, x.Mass, measurement.OutcomingIonAngle) })
                        .OrderBy(x => x.ElementEnergy);

                    foreach (var element in elements)
                    {
                        ArrowAnnotation arrowAnnotation = new ArrowAnnotation
                        {
                            StartPoint = new DataPoint(element.ElementEnergy, currentMax - 20 / currentPixelPerYAxisUnit),
                            EndPoint = new DataPoint(element.ElementEnergy, currentMax - 40 / currentPixelPerYAxisUnit),
                            HeadLength = 5,
                            HeadWidth = 2,
                            Color = OxyColor.FromHsv(0.7, 0.3, 1),
                            Layer = AnnotationLayer.BelowSeries,
                        };

                        if (element.ElementEnergy * currentPixelPerXAxisUnit > lastXPosition + 20)
                        {
                            arrowAnnotation.Text = element.ElementName;
                            arrowAnnotation.Color = OxyColor.FromHsv(0.7, 1, 1);
                            arrowAnnotation.Layer = AnnotationLayer.AboveSeries;
                            lastXPosition = element.ElementEnergy * currentPixelPerXAxisUnit;
                        }
                        MeasurementsPlotModel.Annotations.Add(arrowAnnotation);
                    }
                }
                MeasurementsPlotModel.IsLegendVisible = false;
            }
            else
            {
                MeasurementsPlotModel.Annotations.Clear();
                MeasurementsPlotModel.IsLegendVisible = true;
            }
            MeasurementsPlotModel.InvalidatePlot(true);
        }

        /// <summary>
        /// Function that is executed whenever the plot size or axis range is changed. It updates the element surface channel positions.
        /// </summary>
        public void YAxisUpdated()
        {
            if (ShowElementPositions == true)
            {
                MeasurementsPlotModel.Annotations.Clear();

                UpdateElementPositions();
            }
        }

        public void OfflineUpdateWorker(int MeasurementID)
        {
            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                Measurement updateMeasurement = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);

                if (updateMeasurement != null)
                {
                    UpdatePlot(updateMeasurement);
                }
            }
        }
    }
}

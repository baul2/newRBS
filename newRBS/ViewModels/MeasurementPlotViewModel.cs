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
using System.Diagnostics;
using newRBS.Database;

namespace newRBS.ViewModels
{
    public class MeasurementPlotViewModel : ViewModelBase
    {
        public ICommand ExpandConfigPanel { get; set; }

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
                        { VisButtonContent = "\u21D3 Plot Configuration \u21D3"; break; }
                    case false:
                        { VisButtonContent = "\u21D1 Plot Configuration \u21D1"; break; }
                }
                RaisePropertyChanged();
            }
        }

        private string _VisButtonContent = "\u21D3 Plot Configuration \u21D3";
        public string VisButtonContent
        { get { return _VisButtonContent; } set { _VisButtonContent = value; RaisePropertyChanged(); } }

        private List<int> MeasurementIDList;

        public PlotModel plotModel { get; set; }

        private List<OxyColor> LineColors { get; set; }

        TraceSource trace = new TraceSource("MeasurementPlotViewModel");

        public List<NameValueClass> DataBindingIntervals { get; set; }

        private int _SelectedDataBindingInterval = 0;
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

            plotModel = new PlotModel();

            SetUpModel();

            DataBindingIntervals = new List<NameValueClass> { new NameValueClass("none", 0), new NameValueClass("1keV", 1), new NameValueClass("2keV", 2), new NameValueClass("5keV", 5), new NameValueClass("10keV", 10) };

            YAxisScale = new List<string> { "linear", "logarithmic" };

            LegendCaptions = new List<string> { "Measurement IDs", "Measurement names", "Sample names", "Sample remarks", "Sample names + remarks" };
        }

        private void _ExpandConfigPanel()
        {
            ConfigPanelVis = !ConfigPanelVis;
        }

        private void SetUpModel()
        {
            plotModel.LegendOrientation = LegendOrientation.Vertical;
            plotModel.LegendPlacement = LegendPlacement.Inside;
            plotModel.LegendPosition = LegendPosition.TopRight;
            plotModel.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
            plotModel.LegendBorder = OxyColors.Black;

            var xAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Energy (keV)", TitleFontSize = 16, AxisTitleDistance = 8, AbsoluteMinimum = 0 };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, TitleFontSize = 16, AxisTitleDistance = 18, Minimum = 0, AbsoluteMinimum = 0 };
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
            UpdateYAxisTitle();
        }

        private void MeasurementToPlot(Measurement measurement)
        {
            if (MeasurementIDList.Contains(measurement.MeasurementID)) return;

            MeasurementIDList.Add(measurement.MeasurementID);
            PlotMeasurement(measurement);
        }

        private void MeasurementNotToPlot(Measurement measurement)
        {
            MeasurementIDList.Remove(measurement.MeasurementID);

            var delSerie = plotModel.Series.Where(x => ((Measurement)x.Tag).MeasurementID == measurement.MeasurementID).FirstOrDefault();
            if (delSerie != null)
            {
                plotModel.Series.Remove(delSerie);

                plotModel.InvalidatePlot(true);
            }

            delSerie = plotModel.Series.Where(x => ((Measurement)x.Tag).MeasurementID == measurement.MeasurementID).FirstOrDefault();
            if (delSerie != null)
            {
                plotModel.Series.Remove(delSerie);

                plotModel.InvalidatePlot(true);
            }
        }

        public void ClearPlot(List<int> dump)
        {
            plotModel.Series.Clear();
            MeasurementIDList.Clear();
            plotModel.InvalidatePlot(true);
        }

        private void PlotMeasurement(Measurement measurement)
        {
            if (SelectedDataBindingInterval > 0 && measurement.EnergyCalLinear > SelectedDataBindingInterval)
            { MessageBox.Show("Selected data binding interval is smaller than the actual channel spacing!", "Error"); SelectedDataBindingInterval = 0; return; }

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
            int[] spectrumYCalculated = measurement.SpectrumYCalculated;

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
                        var XAxis = plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Bottom);

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
            plotModel.Series.Add(MeassuredPlot);
            if (measurement.SpectrumYCalculated != null && ShowSimulatedSpectra == true)
                plotModel.Series.Add(SimulatedPlot);
            plotModel.InvalidatePlot(true);
        }

        private void UpdatePlot(Measurement measurement)
        {
            if (!MeasurementIDList.Contains(measurement.MeasurementID))
                return;

            Series updateSerie = plotModel.Series.Where(x => ((Measurement)x.Tag).MeasurementID == measurement.MeasurementID).FirstOrDefault();
            if (updateSerie != null)
            {
                plotModel.Series.Remove(updateSerie);
                PlotMeasurement(measurement);
            }
        }

        private void UpdateYAxisScale()
        {
            switch (SelectedYAxisScale)
            {
                case "linear":
                    {
                        plotModel.Axes.Remove(plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left));
                        var yAxis = new LinearAxis() { Position = AxisPosition.Left, TitleFontSize = 16, AxisTitleDistance = 18, Minimum = 0 };
                        plotModel.Axes.Add(yAxis);
                        break;
                    }
                case "logarithmic":
                    {
                        plotModel.Axes.Remove(plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left));
                        var yAxis = new LogarithmicAxis() { Position = AxisPosition.Left, TitleFontSize = 16, AxisTitleDistance = 18, Minimum = 1 };
                        plotModel.Axes.Add(yAxis);
                        break;
                    }
            }
        }

        private void UpdateYAxisTitle()
        {
            switch (SelectedDataBindingInterval)
            {
                case 0:
                    {
                        plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title = "Counts per channel";
                        break;
                    }
                default:
                    {
                        plotModel.Axes.FirstOrDefault(x => x.Position == AxisPosition.Left).Title = string.Format("Counts per {0}keV interval", SelectedDataBindingInterval);
                        break;
                    }
            }
        }

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

        private void UpdateLegend()
        {
            foreach (var plot in plotModel.Series)
            {
                plot.Title = GetMeasurementTitle((Measurement)plot.Tag);
            }
        }

        private void UpdateAllPlots()
        {
            if (MeasurementIDList.Count() == 0) return;

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                List<Measurement> measurements = Database.Measurements.Where(x => MeasurementIDList.Contains(x.MeasurementID)).ToList();

                plotModel.Series.Clear();

                foreach (Measurement measurement in measurements)
                {
                    PlotMeasurement(measurement);
                }
            }
        }
    }
}

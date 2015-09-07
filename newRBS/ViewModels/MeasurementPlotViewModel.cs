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
        { get { return _SelectedDataBindingInterval; } set { _SelectedDataBindingInterval = value; UpdateAllPlots(); RaisePropertyChanged(); UpdateYAxisTitle(); }  }

        public List<string> YAxisScale { get; set; }

        private string _SelectedYAxisScale = "linear";
        public string SelectedYAxisScale
        { get { return _SelectedYAxisScale; } set { _SelectedYAxisScale = value; RaisePropertyChanged(); UpdateYAxisScale(); UpdateYAxisTitle(); } }

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

            var xAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Energy (keV)", TitleFontSize = 16, AxisTitleDistance = 8 };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, TitleFontSize = 16, AxisTitleDistance = 18, Minimum = 0 };
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
            UpdateYAxisTitle();
        }

        private void MeasurementToPlot(Measurement measurement)
        {
            if (MeasurementIDList.Contains(measurement.MeasurementID))
            { Console.WriteLine("Measurement is already in MeasurementIDList!"); return; }

            MeasurementIDList.Add(measurement.MeasurementID);

            PlotMeasurement(measurement);
        }

        private void MeasurementNotToPlot(Measurement measurement)
        {
            MeasurementIDList.Remove(measurement.MeasurementID);

            Series delSerie = plotModel.Series.Where(x => (int)x.Tag == measurement.MeasurementID).FirstOrDefault();
            if (delSerie != null)
            {
                plotModel.Series.Remove(delSerie);
                plotModel.InvalidatePlot(true);
            }
        }

        private void ClearPlot(List<int> dump)
        {
            plotModel.Series.Clear();
            MeasurementIDList.Clear();
            plotModel.InvalidatePlot(true);
        }

        private void PlotMeasurement(Measurement measurement)
        {
            if (SelectedDataBindingInterval>0 && measurement.EnergyCalSlope>SelectedDataBindingInterval)
            { MessageBox.Show("Selected data binding interval is smaller than the actual channel spacing!", "Error"); SelectedDataBindingInterval = 0; return; }
            
            var areaSeries = new AreaSeries
            {
                Tag = measurement.MeasurementID,
                StrokeThickness = 2,
                MarkerSize = 3,
                Color = LineColors[measurement.MeasurementID % LineColors.Count],
                CanTrackerInterpolatePoints = false,
                Title = string.Format("MeasurementID {0}", measurement.MeasurementID),
                Smooth = false,
            };

            float[] spectrumX = measurement.SpectrumXCal;
            int[] spectrumY = measurement.SpectrumY;

            // Remove "Counts<3" data points from start/end of the spectra
            int BorderOffset = 200;

            int rightBorderIndex = spectrumY.Count();
            while (spectrumY[rightBorderIndex - 1] < 3 && rightBorderIndex > 2) rightBorderIndex--;

            int leftBorderIndex = 0;
            while (spectrumY[leftBorderIndex] < 3 && leftBorderIndex < spectrumY.Count() - 2) leftBorderIndex++;

            if (rightBorderIndex < 5) rightBorderIndex = spectrumY.Count();
            if (leftBorderIndex > spectrumY.Count() - 5) leftBorderIndex = 0;

            if (rightBorderIndex + BorderOffset < spectrumY.Count()) rightBorderIndex += BorderOffset; else rightBorderIndex = spectrumY.Count();
            if (leftBorderIndex - BorderOffset > 0) leftBorderIndex -= BorderOffset; else leftBorderIndex = 0;

            // Add points to plot
            switch (SelectedDataBindingInterval)
            {
                case 0: // All points in spectrumX/spectrumY
                    {
                        float y;
                        for (int i = leftBorderIndex; i < rightBorderIndex; i++)
                        {
                            if (spectrumY[i] == 0) y = (float)0.0001; else y = spectrumY[i];
                            areaSeries.Points.Add(new DataPoint(spectrumX[i], y));
                            areaSeries.Points2.Add(new DataPoint(spectrumX[i], (float)0.0001));
                        }
                        break;
                    }
                default: // Bind points inside SelectedDataBinding intervall
                    {
                        float x, y = 0;
                        float intervalStart = SelectedDataBindingInterval * ((int)(spectrumX[leftBorderIndex] / SelectedDataBindingInterval) + (float)0.5); ;
                        int numOfPoints = 0;

                        for (int i = leftBorderIndex; i < rightBorderIndex; i++)
                        {
                            if (spectrumX[i] - intervalStart > SelectedDataBindingInterval)
                            {
                                x = intervalStart + (float)SelectedDataBindingInterval / 2;
                                if (y == 0) y = (float)0.0001;
                                areaSeries.Points.Add(new DataPoint(x, y / numOfPoints * (SelectedDataBindingInterval / measurement.EnergyCalSlope)));
                                areaSeries.Points2.Add(new DataPoint(x, (float)0.0001));

                                y = 0;
                                numOfPoints = 0;
                                intervalStart += SelectedDataBindingInterval;
                            }
                            y += spectrumY[i];
                            numOfPoints++;
                        }
                        break;
                    }
            }
            plotModel.Series.Add(areaSeries);
            plotModel.InvalidatePlot(true);
        }

        private void UpdatePlot(Measurement measurement)
        {
            if (!MeasurementIDList.Contains(measurement.MeasurementID))
                return;

            Series updateSerie = plotModel.Series.Where(x => (int)x.Tag == measurement.MeasurementID).FirstOrDefault();
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

        private void UpdateAllPlots( )
        {
            if (MeasurementIDList.Count() == 0) return;

            using (DatabaseDataContext Database = new DatabaseDataContext(MyGlobals.ConString))
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

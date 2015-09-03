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

        public List<NameValueClass> DataBinding { get; set; }

        private int _SelectedDataBinding = 0;
        public int SelectedDataBinding
        { get { return _SelectedDataBinding; } set { _SelectedDataBinding = value; UpdateAllPlots(); RaisePropertyChanged(); } }

        public MeasurementPlotViewModel()
        {
            // Hooking up to events from DatabaseUtils 
            Models.DatabaseUtils.EventMeasurementRemove += new Models.DatabaseUtils.EventHandlerMeasurement(MeasurementNotToPlot);
            Models.DatabaseUtils.EventMeasurementUpdate += new Models.DatabaseUtils.EventHandlerMeasurement(UpdatePlot);

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

            DataBinding = new List<NameValueClass> { new NameValueClass("none", 0), new NameValueClass("1keV", 1), new NameValueClass("2keV", 2), new NameValueClass("5keV", 5), new NameValueClass("10keV", 10) };
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
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, Title = "Counts", TitleFontSize = 16, AxisTitleDistance = 12, Minimum = 0 };
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
        }

        private void MeasurementToPlot(Models.Measurement measurement)
        {
            if (MeasurementIDList.Contains(measurement.MeasurementID))
            { Console.WriteLine("Measurement is already in MeasurementIDList!"); return; }

            MeasurementIDList.Add(measurement.MeasurementID);

            PlotMeasurement(measurement);
        }

        private void MeasurementNotToPlot(Models.Measurement measurement)
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

        private void PlotMeasurement(Models.Measurement measurement)
        {
            var areaSeries = new AreaSeries
            {
                Tag = measurement.MeasurementID,
                StrokeThickness = 2,
                MarkerSize = 3,
                Color = LineColors[measurement.MeasurementID % LineColors.Count],
                CanTrackerInterpolatePoints = false,
                Title = string.Format("SpectrumID {0}", measurement.MeasurementID),
                Smooth = false,
            };

            float[] spectrumX = measurement.SpectrumXCal;
            int[] spectrumY = measurement.SpectrumY;

            // Remove "0 Count" data points from start/end of the spectra
            int BorderOffset = 200;

            int rightBorderIndex = spectrumY.Count();
            while (spectrumY[rightBorderIndex - 1] < 3) rightBorderIndex--;

            int leftBorderIndex = 0;
            while (spectrumY[leftBorderIndex] < 3) leftBorderIndex++;

            if (rightBorderIndex + BorderOffset < spectrumY.Count()) rightBorderIndex += BorderOffset; else rightBorderIndex = spectrumY.Count();
            if (leftBorderIndex - BorderOffset > 0) leftBorderIndex -= BorderOffset; else leftBorderIndex = 0;

            // Add points to plot
            switch (SelectedDataBinding)
            {
                case 0: // All points in spectrumX/spectrumY
                    {
                        for (int i = leftBorderIndex; i < rightBorderIndex; i++)
                        {
                            areaSeries.Points.Add(new DataPoint(spectrumX[i], spectrumY[i]));
                            areaSeries.Points2.Add(new DataPoint(spectrumX[i], 0));
                        }
                        break;
                    }
                default: // Bind points inside SelectedDataBinding intervall
                    {
                        if (spectrumX[1] - spectrumX[0] > SelectedDataBinding) { MessageBox.Show("Channel spacing is larger than data binding interval", "Error"); }
                        float x, y = 0;
                        float intervalStart = SelectedDataBinding * ((int)(spectrumX[leftBorderIndex] / SelectedDataBinding) + (float)0.5); ;
                        int numOfPoints = 0;

                        for (int i = leftBorderIndex; i < rightBorderIndex; i++)
                        {
                            if (spectrumX[i] - intervalStart > SelectedDataBinding)
                            {
                                x = intervalStart + (float)SelectedDataBinding / 2;
                                areaSeries.Points.Add(new DataPoint(x, y / numOfPoints));
                                areaSeries.Points2.Add(new DataPoint(x, 0));

                                y = 0;
                                numOfPoints = 0;
                                intervalStart += SelectedDataBinding;
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

        private void UpdatePlot(Models.Measurement measurement)
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

        private void UpdateAllPlots()
        {
            plotModel.Series.Clear();

            using (Models.DatabaseDataContext Database = new Models.DatabaseDataContext(MyGlobals.ConString))
            {
                List<Models.Measurement> measurements = Database.Measurements.Where(x => MeasurementIDList.Contains(x.MeasurementID)).ToList();
                foreach (Models.Measurement measurement in measurements)
                {
                    PlotMeasurement(measurement);
                }
            }
        }
    }
}

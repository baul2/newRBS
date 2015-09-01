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
    public class MeasurementPlotViewModel
    {
        private List<int> MeasurementIDList;

        public PlotModel plotModel { get; set; }

        private List<OxyColor> LineColors { get; set; }

        TraceSource trace = new TraceSource("MeasurementPlotViewModel");

        public MeasurementPlotViewModel()
        {
            // Hooking up to events from DatabaseUtils 
            Models.DatabaseUtils.EventMeasurementRemove += new Models.DatabaseUtils.EventHandlerMeasurement(MeasurementNotToPlot);
            Models.DatabaseUtils.EventMeasurementUpdate += new Models.DatabaseUtils.EventHandlerMeasurement(MeasurementUpdate);

            // Hooking up to events from SpectraList
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().EventMeasurementToPlot += new MeasurementListViewModel.EventHandlerMeasurement(MeasurementToPlot);
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().EventMeasurementNotToPlot += new MeasurementListViewModel.EventHandlerMeasurement(MeasurementNotToPlot);

            // Hooking up to events from SpectraFilter
            SimpleIoc.Default.GetInstance<MeasurementFilterViewModel>().EventNewFilter += new MeasurementFilterViewModel.EventHandlerFilter(ClearPlot);

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
            Console.WriteLine("MeasurementToPlot");

            if (MeasurementIDList.Contains(measurement.MeasurementID))
            { Console.WriteLine("Measurement is already in MeasurementIDList!"); return; }

            MeasurementIDList.Add(measurement.MeasurementID);

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

            //Stopwatch stopWatch = new Stopwatch();
            //stopWatch.Start();
            float[] spectrumX = Models.DatabaseUtils.GetCalibratedSpectrumX(measurement);
            int[] spectrumY = Models.DatabaseUtils.GetIntSpectrumY(measurement);
            for (int i = 0; i < spectrumY.Count(); i++)
            {
                areaSeries.Points.Add(new DataPoint(spectrumX[i], spectrumY[i]));
                areaSeries.Points2.Add(new DataPoint(spectrumX[i], 0));
            }
            plotModel.Series.Add(areaSeries);
            plotModel.InvalidatePlot(true);
            //stopWatch.Stop();
            //Console.WriteLine("Time for points: {0}", stopWatch.Elapsed.ToString());
            Console.WriteLine("Series added");
        }

        private void MeasurementUpdate(Models.Measurement measurement)
        {
            if (!MeasurementIDList.Contains(measurement.MeasurementID))
                return;

            Series updateSerie = plotModel.Series.Where(x => (int)x.Tag == measurement.MeasurementID).FirstOrDefault();
            if (updateSerie != null)
            {
                int index = plotModel.Series.IndexOf(updateSerie);

                (plotModel.Series[index] as AreaSeries).Points.Clear();

                float[] spectrumX = Models.DatabaseUtils.GetCalibratedSpectrumX(measurement);
                int[] spectrumY = Models.DatabaseUtils.GetIntSpectrumY(measurement);
                for (int i = 0; i < spectrumY.Count(); i++)
                {
                    (plotModel.Series[index] as AreaSeries).Points.Add(new DataPoint(spectrumX[i], spectrumY[i]));
                }
                plotModel.InvalidatePlot(true);
            }
        }

        private void MeasurementNotToPlot(Models.Measurement measurement)
        {
            Console.WriteLine("MeasurementNotToPlot");
            MeasurementIDList.Remove(measurement.MeasurementID);

            Series delSerie = plotModel.Series.Where(x => (int)x.Tag == measurement.MeasurementID).FirstOrDefault();
            if (delSerie != null)
            {
                plotModel.Series.Remove(delSerie);
                plotModel.InvalidatePlot(true);
                Console.WriteLine("Series removed");
            }
        }

        private void ClearPlot(List<int> dump)
        {
            Console.WriteLine("ClearPlot");

            plotModel.Series.Clear();
            MeasurementIDList.Clear();
            plotModel.InvalidatePlot(true);
        }
    }
}

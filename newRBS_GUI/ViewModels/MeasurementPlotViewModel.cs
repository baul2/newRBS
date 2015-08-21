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
using newRBS.ViewModelUtils;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;
using System.Diagnostics;

namespace newRBS.ViewModels
{
    public class MeasurementPlotViewModel
    {
        private Models.DataSpectra dataSpectra { get; set; }

        public List<int> MeasurementIDList;

        public PlotModel plotModel { get; set; }

        private List<OxyColor> LineColors { get; set; }

        TraceSource trace = new TraceSource("MeasurementPlotViewModel");

        public MeasurementPlotViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            // Hooking up to events from DataSpectra 
            SimpleIoc.Default.GetInstance<Models.DataSpectra>().EventMeasurementUpdate += new Models.DataSpectra.EventHandlerMeasurement(MeasurementUpdate);
            SimpleIoc.Default.GetInstance<Models.DataSpectra>().EventMeasurementRemove += new Models.DataSpectra.EventHandlerMeasurementID(MeasurementNotToPlot);

            // Hooking up to events from SpectraList
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().EventMeasurementToPlot += new MeasurementListViewModel.EventHandlerMeasurementID(MeasurementToPlot);
            SimpleIoc.Default.GetInstance<MeasurementListViewModel>().EventMeasurementNotToPlot += new MeasurementListViewModel.EventHandlerMeasurementID(MeasurementNotToPlot);

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

            var xAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Energy (keV)", TitleFontSize = 16, AxisTitleDistance = 8, Minimum = 0 };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, Title = "Counts", TitleFontSize = 16, AxisTitleDistance = 12, Minimum = 0 };
            plotModel.Axes.Add(xAxis);
            plotModel.Axes.Add(yAxis);
        }

        private void MeasurementToPlot(int measurementID)
        {
            Console.WriteLine("MeasurementToPlot");

            Models.Measurement measurement;

            using (Models.RBS_Database db = new Models.RBS_Database(MyGlobals.ConString))   
                measurement = db.Measurements.SingleOrDefault(x => x.MeasurementID == measurementID);

            if (measurement == null) { trace.TraceEvent(TraceEventType.Error, 0, "Failed to load data for SpectrumID: {0}", measurementID); return; }

            MeasurementIDList.Add(measurementID);

            var areaSeries = new AreaSeries
            {
                Tag = measurementID,
                StrokeThickness = 2,
                MarkerSize = 3,
                Color = LineColors[measurementID % LineColors.Count],
                CanTrackerInterpolatePoints = false,
                Title = string.Format("SpectrumID {0}", measurementID),
                Smooth = false,
            };

            //Stopwatch stopWatch = new Stopwatch();
            //stopWatch.Start();
            for (int x = 0; x < measurement.SpectrumY.Count(); x++)
            {
                areaSeries.Points.Add(new DataPoint(x, measurement.SpectrumY[x]));
                areaSeries.Points2.Add(new DataPoint(x, 0));
            }
            plotModel.Series.Add(areaSeries);
            plotModel.InvalidatePlot(true);
            //stopWatch.Stop();
            //Console.WriteLine("Time for points: {0}", stopWatch.Elapsed.ToString());
            Console.WriteLine("Series added");
        }

        private void MeasurementUpdate(Models.Measurement spectrum)
        {
            if (!MeasurementIDList.Contains(spectrum.MeasurementID))
                return;

            Console.WriteLine("MeasurementUpdate");

            Series updateSerie = plotModel.Series.Where(x => (int)x.Tag == spectrum.MeasurementID).FirstOrDefault();
            if (updateSerie != null)
            {
                int index = plotModel.Series.IndexOf(updateSerie);

                (plotModel.Series[index] as AreaSeries).Points.Clear();
                for (int x = 0; x < spectrum.SpectrumY.Count(); x++)
                {
                    (plotModel.Series[index] as AreaSeries).Points.Add(new DataPoint(x, spectrum.SpectrumY[x]));
                }
                plotModel.InvalidatePlot(true);
            }
        }

        private void MeasurementNotToPlot(int spectrumID)
        {
            Console.WriteLine("MeasurementNotToPlot");
            MeasurementIDList.Remove(spectrumID);

            Series delSerie = plotModel.Series.Where(x => (int)x.Tag == spectrumID).First();
            if (delSerie != null)
            {
                plotModel.Series.Remove(delSerie);
                plotModel.InvalidatePlot(true);
                Console.WriteLine("Series removed");
            }
        }

        private void ClearPlot(Filter newFilter)
        {
            Console.WriteLine("ClearPlot");

            plotModel.Series.Clear();
            MeasurementIDList.Clear();
            plotModel.InvalidatePlot(true);
        }
    }
}

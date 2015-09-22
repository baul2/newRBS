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
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Annotations;
using OxyPlot.Series;
using MathNet.Numerics;
using newRBS.Database;

namespace newRBS.ViewModels
{
    /// <summary>
    /// Class that is an item of a datagrid.
    /// </summary>
    public class EnergyCalListItem : ViewModelBase
    {
        public string Name { get; set; }
        public int Channel { get; set; }
        public Isotope Isotope { get; set; }

        private double? _CalibratedEnergy;
        public double? CalibratedEnergy
        { get { return _CalibratedEnergy; } set { _CalibratedEnergy = value; RaisePropertyChanged(); } }
    }

    /// <summary>
    /// Class that is the view model of <see cref="Views.EnergyCalibrationView"/>. They calculate the energy calibration for a given set of <see cref="Measurement"/>s.
    /// </summary>
    public class EnergyCalibrationViewModel : ViewModelBase
    {
        public ICommand AddToListCommand { get; set; }
        public ICommand ClearListCommand { get; set; }

        public ICommand CalculateEnergyCalCommand { get; set; }

        public ICommand SaveEnergyCalCommand { get; set; }
        public ICommand CancelCalCommand { get; set; }

        private DatabaseDataContext Database;

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private List<Measurement> selectedMeasurements;

        public PlotModel SelectedMeasurementsPlot { get; set; }

        private List<OxyColor> LineColors { get; set; }

        public ObservableCollection<NameValueClass> SelectedMeasurements { get; set; }

        private int _Channel;
        public int Channel { get { return _Channel; } set { _Channel = value; RaisePropertyChanged(); } }

        public ObservableCollection<Database.Element> ElementList { get; set; }

        public ObservableCollection<Isotope> IsotopeList { get; set; }

        private Database.Element _SelectedElement;
        public Database.Element SelectedElement
        { get { return _SelectedElement; } set { _SelectedElement = value; SelectedElementChanged(); RaisePropertyChanged(); } }

        private Isotope _SelectedIsotope;
        public Isotope SelectedIsotope
        { get { return _SelectedIsotope; } set { _SelectedIsotope = value; RaisePropertyChanged(); } }

        public ObservableCollection<EnergyCalListItem> EnergyCalList { get; set; }

        public ObservableCollection<string> FitType { get; set; }

        private string _SelectedFitType = "linear";
        public string SelectedFitType
        { get { return _SelectedFitType; } set { _SelectedFitType = value; RaisePropertyChanged(); } }

        private double? _ECalOffset;
        public double? ECalOffset { get { return _ECalOffset; } set { _ECalOffset = value; RaisePropertyChanged(); } }

        private double? _ECalLinear;
        public double? ECalLinear { get { return _ECalLinear; } set { _ECalLinear = value; RaisePropertyChanged(); } }

        private double? _ECalQuadratic;
        public double? ECalQuadratic { get { return _ECalQuadratic; } set { _ECalQuadratic = value; RaisePropertyChanged(); } }

        /// <summary>
        /// Constructor of the class. Sets up the commands, collections and selected items of the view.
        /// </summary>
        public EnergyCalibrationViewModel(List<Measurement> SelectedMeasurements)
        {
            AddToListCommand = new RelayCommand(() => _AddToListCommand(), () => true);
            ClearListCommand = new RelayCommand(() => _ClearListCommand(), () => true);

            CalculateEnergyCalCommand = new RelayCommand(() => _CalculateEnergyCalCommand(), () => true);

            SaveEnergyCalCommand = new RelayCommand(() => _SaveEnergyCalCommand(), () => true);
            CancelCalCommand = new RelayCommand(() => _CancelCalCommand(), () => true);

            Database = MyGlobals.Database;

            IsotopeList = new ObservableCollection<Isotope>();

            ElementList = new ObservableCollection<Database.Element>(Database.Elements);

            SelectedElement = ElementList.FirstOrDefault();

            selectedMeasurements = SelectedMeasurements;

            EnergyCalList = new ObservableCollection<EnergyCalListItem>();

            FitType = new ObservableCollection<string> { "linear", "quadratic" };

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

            SelectedMeasurementsPlot = new PlotModel();

            SetUpModel();

            PlotMeasurements();
        }

        /// <summary>
        /// Function that configures the plot of the selected <see cref="Measurement"/>s.
        /// </summary>
        private void SetUpModel()
        {
            SelectedMeasurementsPlot.LegendOrientation = LegendOrientation.Vertical;
            SelectedMeasurementsPlot.LegendPlacement = LegendPlacement.Inside;
            SelectedMeasurementsPlot.LegendPosition = LegendPosition.TopRight;
            SelectedMeasurementsPlot.LegendBackground = OxyColor.FromAColor(200, OxyColors.White);
            SelectedMeasurementsPlot.LegendBorder = OxyColors.Black;

            var xAxis = new LinearAxis() { Position = AxisPosition.Bottom, Title = "Channel", TitleFontSize = 16, AxisTitleDistance = 8, Minimum = 0 };
            var yAxis = new LinearAxis() { Position = AxisPosition.Left, Title = "Counts", TitleFontSize = 16, AxisTitleDistance = 12, Minimum = 0 };
            SelectedMeasurementsPlot.Axes.Add(xAxis);
            SelectedMeasurementsPlot.Axes.Add(yAxis);
            SelectedMeasurementsPlot.MouseUp += (s, e) =>
            {
                var XY = Axis.InverseTransform(e.Position, xAxis, yAxis);
                Channel = (int)Math.Round(XY.X);
            };
        }

        /// <summary>
        /// Function that plots all selected <see cref="Measurement"/>s over their channel number.
        /// </summary>
        private void PlotMeasurements()
        {
            foreach (Measurement measurement in selectedMeasurements)
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

                int[] spectrumY = measurement.SpectrumY;
                for (int i = 0; i < spectrumY.Count(); i++)
                {
                    areaSeries.Points.Add(new DataPoint(i, spectrumY[i]));
                    areaSeries.Points2.Add(new DataPoint(i, 0));
                }
                SelectedMeasurementsPlot.Series.Add(areaSeries);
            }
            SelectedMeasurementsPlot.InvalidatePlot(true);
        }

        private void SelectedElementChanged()
        {
            IsotopeList.Clear();
            foreach (var i in SelectedElement.Isotopes.OrderBy(x => x.MassNumber))
            {
                IsotopeList.Add(i);
            }
            SelectedIsotope = IsotopeList.FirstOrDefault();
        }

        /// <summary>
        /// Function that adds the selected channel and element to the <see cref="EnergyCalList"/>.
        /// </summary>
        private void _AddToListCommand()
        {
            if (Channel == 0) return;
            if (EnergyCalList.FirstOrDefault(x => x.Isotope == SelectedIsotope) != null) return;
            if (EnergyCalList.FirstOrDefault(x => x.Channel == Channel) != null) return;

            EnergyCalList.Add(new EnergyCalListItem { Channel = Channel, Isotope = SelectedIsotope });
        }

        /// <summary>
        /// Function that clears the <see cref="EnergyCalList"/>.
        /// </summary>
        private void _ClearListCommand()
        {
            EnergyCalList.Clear();
        }

        /// <summary>
        /// Function that calculates the energy calibration using the items in <see cref="EnergyCalList"/>. The results are displayed in the view.
        /// </summary>
        private void _CalculateEnergyCalCommand()
        {
            if (EnergyCalList.Count() < 1)
            { MessageBox.Show("Add at least one point to the list!", "Error"); return; }

            Measurement m = selectedMeasurements[0];

            foreach (EnergyCalListItem e in EnergyCalList)
                e.CalibratedEnergy = Math.Round(m.IncomingIonEnergy * KineFak(m.Isotope.Mass, e.Isotope.Mass, m.OutcomingIonAngle), 1);

            ECalOffset = 0;
            ECalLinear = 0;
            ECalQuadratic = 0;

            if (SelectedFitType == "linear")
                if (EnergyCalList.Count() < 2) // One Point
                {
                    ECalLinear = Math.Round((double)EnergyCalList.Select(x => x.CalibratedEnergy).FirstOrDefault() / EnergyCalList.Select(x => x.Channel).FirstOrDefault(), 4);
                }
                else // Linear Fit
                {
                    Tuple<double, double> result = Fit.Line(EnergyCalList.Select(x => (double)x.Channel).ToArray(), EnergyCalList.Select(x => (double)x.CalibratedEnergy).ToArray());
                    ECalOffset = Math.Round(result.Item1, 1);
                    ECalLinear = Math.Round(result.Item2, 4);
                }

            if (SelectedFitType == "quadratic")
                if (EnergyCalList.Count() > 2)
                {
                    double[] result = Fit.Polynomial(EnergyCalList.Select(x => (double)x.Channel).ToArray(), EnergyCalList.Select(x => (double)x.CalibratedEnergy).ToArray(), 2);
                    ECalOffset = Math.Round(result[0], 1);
                    ECalLinear = Math.Round(result[1], 4);
                    ECalQuadratic = Math.Round(result[2], 4);
                }
                else
                {
                    MessageBox.Show("Add at least 3 points to the element list!", "Error");
                }
        }

        /// <summary>
        /// Function that saves the values of <see cref="ECalOffset"/> and <see cref="ECalLinear"/> in the selected measurements and closes the dialog.
        /// </summary>
        private void _SaveEnergyCalCommand()
        {
            if (ECalOffset == null || ECalLinear == null || ECalQuadratic == null) return;
            if (ECalOffset == 0 && ECalLinear == 0 && ECalQuadratic == 0) return;

            using (DatabaseDataContext Database = MyGlobals.Database)
            {
                var m = Database.Measurements.Where(x => selectedMeasurements.Select(y => y.MeasurementID).ToList().Contains(x.MeasurementID));
                foreach (Measurement measurement in m)
                {
                    measurement.EnergyCalOffset = (double)ECalOffset;
                    measurement.EnergyCalLinear = (double)ECalLinear;
                    measurement.EnergyCalQuadratic = (double)ECalQuadratic;
                }
                Database.SubmitChanges();
            }
            DialogResult = false;
        }

        /// <summary>
        /// Function that cancels the dialog.
        /// </summary>
        private void _CancelCalCommand()
        {
            DialogResult = false;
        }

        /// <summary>
        /// Function that calculates the kinematic factor k.
        /// </summary>
        /// <param name="IncomingIonMass">Mass of the incoming ion in [u].</param>
        /// <param name="TargetAtomMass">Mass of the target atom in [u].</param>
        /// <param name="ThetaDegree">Angle of the scatterin process in [°].</param>
        /// <returns></returns>
        private double KineFak(double IncomingIonMass, double TargetAtomMass, double ThetaDegree)
        {
            double Theta = ThetaDegree / 360.0 * 2.0 * Math.PI;

            double k = Math.Pow((Math.Pow(1.0 - Math.Pow(IncomingIonMass * Math.Sin(Theta) / TargetAtomMass, 2.0), 0.5) + IncomingIonMass * Math.Cos(Theta) / TargetAtomMass) / (1.0 + IncomingIonMass / TargetAtomMass), 2.0);

            return k;
        }
    }
}

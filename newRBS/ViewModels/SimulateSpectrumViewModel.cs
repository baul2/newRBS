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
using GLib;
using Epsara;

namespace newRBS.ViewModels
{
    public class SimulateSpectrumViewModel : ViewModelBase
    {
        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ICommand StartSimulationCommand { get; set; }

        private Models.DatabaseDataContext Database;

        private ObservableCollection<AreaData> _MeasuredSpectrumData = new ObservableCollection<AreaData>();
        public ObservableCollection<AreaData> MeasuredSpectrumData
        { get { return _MeasuredSpectrumData; } set { _MeasuredSpectrumData = value; RaisePropertyChanged(); } }

        private ObservableCollection<AreaData> _SimulatedSpectrumData = new ObservableCollection<AreaData>();
        public ObservableCollection<AreaData> SimulatedSpectrumData
        { get { return _SimulatedSpectrumData; } set { _SimulatedSpectrumData = value; RaisePropertyChanged(); } }

        public ObservableCollection<Models.Sample> Samples { get; set; }
        private Models.Sample _SelectedSample;
        public Models.Sample SelectedSample
        {
            get { return _SelectedSample; }
            set { _SelectedSample = value; SelectedSampleChanged(); RaisePropertyChanged(); }
        }

        public ObservableCollection<Models.Measurement> Measurements { get; set; }
        private Models.Measurement _SelectedMeasurement;
        public Models.Measurement SelectedMeasurement
        {
            get { return _SelectedMeasurement; }
            set { _SelectedMeasurement = value; SelectedMeasurementChanged(); RaisePropertyChanged(); }
        }

        public SimulateSpectrumViewModel()
        {
            StartSimulationCommand = new RelayCommand(() => _StartSimulationCommand(), () => true);

            Database = new Models.DatabaseDataContext(MyGlobals.ConString);

            Measurements = new ObservableCollection<Models.Measurement>();

            Samples = new ObservableCollection<Models.Sample>(Database.Samples.ToList());
            Samples.Remove(Samples.First(x => x.SampleName == "(undefined)"));
            //SelectedSample = Samples.FirstOrDefault();
        }

        private void SelectedSampleChanged()
        {
            Measurements.Clear();

            List<Models.Measurement> measurementList = Database.Measurements.Where(x => x.SampleID == SelectedSample.SampleID).ToList();

            if (measurementList == null) return;

            foreach (Models.Measurement measurement in measurementList)
                Measurements.Add(measurement);
        }

        private void SelectedMeasurementChanged()
        {
            MeasuredSpectrumData.Clear();
            float[] spectrumX = SelectedMeasurement.SpectrumXCal;
            int[] spectrumY = SelectedMeasurement.SpectrumY;
            for (int i = 0; i < spectrumY.Count(); i++)
                MeasuredSpectrumData.Add(new AreaData { x1 = spectrumX[i], y1 = spectrumY[i], x2 = spectrumX[i], y2 = 0 });

            //plotModel.InvalidatePlot(true);
        }

        private void _StartSimulationCommand()
        {
            DataSimpleMeasurement simpleMeasurement = new DataSimpleMeasurement();
            simpleMeasurement.AtomicNoIncIon = SelectedMeasurement.IncomingIonAtomicNumber;
            simpleMeasurement.MassNoIncIon = (int)ElementData.AtomicMass[SelectedMeasurement.IncomingIonAtomicNumber - 1];
            simpleMeasurement.IonEnergy = SelectedMeasurement.IncomingIonEnergy;
            simpleMeasurement.IonFluence = 6e13;
            simpleMeasurement.SolidAngle = SelectedMeasurement.SolidAngle;
            simpleMeasurement.IncAngleTheta = SelectedMeasurement.IncomingIonAngle;
            simpleMeasurement.IncAnglePhi = 0.0;
            simpleMeasurement.OutAngleTheta = 180 - SelectedMeasurement.OutcomingIonAngle;
            simpleMeasurement.OutAnglePhi = 0.0;
            simpleMeasurement.CalculateNoise = false;
            simpleMeasurement.ConstEloss = 1;
            simpleMeasurement.ChannelMin = 0;
            simpleMeasurement.ChannelMax = (uint)(SelectedMeasurement.NumOfChannels - 1);
            simpleMeasurement.CaliEnergyPerChannel = SelectedMeasurement.EnergyCalSlope;
            simpleMeasurement.CaliEnergyPerChannelSquare = 0.0;
            simpleMeasurement.CaliEnergyPerChannelCube = 0.0;
            simpleMeasurement.CaliEnergyOffset = SelectedMeasurement.EnergyCalOffset;
            simpleMeasurement.OdeInInitPrec = 1e-8;
            simpleMeasurement.OdeInMaxPrec = 1e-10;
            simpleMeasurement.OdeOutInitPrec = 1e-8;
            simpleMeasurement.OdeOutMaxPrec = 1e-10;

            double layerStart = 0;
            Console.WriteLine(SelectedSample.Material.Layers.Count);
            for (int i = 0; i < SelectedSample.Material.Layers.Count; i++)
            {
                Models.Layer layer = SelectedSample.Material.Layers.FirstOrDefault(x => x.LayerIndex == i);
                Console.WriteLine("Try to add layer: {0}", layer.LayerName);
                foreach (Models.Element element in layer.Elements)
                {
                    Console.WriteLine("Try to add element: {0}", element.ElementName);
                    DataSimpleMaterial newSimpleMaterial = new DataSimpleMaterial();
                    newSimpleMaterial.AtomicNoInitialTarget = (int)element.AtomicNumber;
                    newSimpleMaterial.MassNoInitialTarget = (int)element.MassNumber;
                    newSimpleMaterial.LayerBegin = layerStart;
                    newSimpleMaterial.LayerEnd = layerStart + layer.Thickness;
                    newSimpleMaterial.AtomicDensity = 2.1937e22; // TODO: calculate actual atomic density
                    newSimpleMaterial.QValue = 0.0;
                    newSimpleMaterial.AtomicNoRemainTarget = (int)element.AtomicNumber;
                    newSimpleMaterial.MassNoRemainTarget = (int)element.MassNumber;
                    newSimpleMaterial.AtomicNoDetIon = SelectedMeasurement.IncomingIonAtomicNumber;
                    newSimpleMaterial.MassNoDetIon = (int)ElementData.AtomicMass[SelectedMeasurement.IncomingIonAtomicNumber - 1];
                    newSimpleMaterial.RbsActive = true;
                    newSimpleMaterial.NraActive = false;

                    simpleMeasurement.Add((GLib.Object)newSimpleMaterial);
                }
                layerStart += layer.Thickness;
            }

            simpleMeasurement.Calculate();

            DataMatrix resultMatrix = simpleMeasurement.CalcedSpectrum;

            SimulatedSpectrumData.Clear();
            for (int i = 0; i < SelectedMeasurement.NumOfChannels; i++)
            {
                SimulatedSpectrumData.Add(new AreaData { x1 = i, y1 = resultMatrix[2, i], x2 = i, y2 = 0 });
                //Console.WriteLine(resultMatrix[2, i]);
            }

            MeasuredSpectrumData.Add(new AreaData());
            MeasuredSpectrumData.RemoveAt(MeasuredSpectrumData.Count() - 1);
        }
    }
}

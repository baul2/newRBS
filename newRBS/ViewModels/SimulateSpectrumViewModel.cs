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
using newRBS.Database;

namespace newRBS.ViewModels
{
    public class SimulateSpectrumViewModel : ViewModelBase
    {
        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ICommand StartSimulationCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private DatabaseDataContext Database;

        public Measurement SelectedMeasurement { get; set; }

        public Sample SelectedSample { get; set; }

        public Material SelectedMaterial { get; set; }

        public double IonFluence { get; set; }

        public SimulateSpectrumViewModel(int MeasurementID)
        {
            StartSimulationCommand = new RelayCommand(() => _StartSimulationCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true); 

            Database = MyGlobals.Database;

            SelectedMeasurement = Database.Measurements.FirstOrDefault(x => x.MeasurementID == MeasurementID);
            SelectedSample = SelectedMeasurement.Sample;
            SelectedMaterial = SelectedSample.Material;

            if (SelectedSample.SampleName == "(undefined)" || SelectedMaterial.MaterialName == "(undefined)")
            {
                MessageBox.Show("A sample (with a material) must be assigned to the measurement!", "Error");
                DialogResult = false;
            }

            IonFluence = 1E14;
        }

        private double CalculateAtomicDensity(Layer layer, LayerElement layerElement)
        {
            double MassOfMolecule = 0;
            foreach (LayerElement l in layer.LayerElements)
                MassOfMolecule += l.Isotope.Mass * l.StoichiometricFactor;

            double NumberOfMolecules = layer.Density / MassOfMolecule / 1.66053904E-24;

            double NumberOfAtomsInMolecule = layer.LayerElements.Select(x => x.StoichiometricFactor).ToList().Sum();

            double AtomicDensityOfElement = NumberOfMolecules * layerElement.StoichiometricFactor;
            Console.WriteLine("Atomic density of " + layerElement.Isotope.Element.LongName+ ": " + AtomicDensityOfElement);
            return AtomicDensityOfElement;
        }

        private void _StartSimulationCommand()
        {
            DataSimpleMeasurement simpleMeasurement = new DataSimpleMeasurement();
            simpleMeasurement.AtomicNoIncIon = SelectedMeasurement.Isotope.AtomicNumber;
            simpleMeasurement.MassNoIncIon = (int)SelectedMeasurement.Isotope.Mass;
            simpleMeasurement.IonEnergy = SelectedMeasurement.IncomingIonEnergy;
            simpleMeasurement.IonFluence = IonFluence;
            simpleMeasurement.SolidAngle = SelectedMeasurement.SolidAngle;
            simpleMeasurement.IncAngleTheta = SelectedMeasurement.IncomingIonAngle;
            simpleMeasurement.IncAnglePhi = 0.0;
            simpleMeasurement.OutAngleTheta = 180 - SelectedMeasurement.OutcomingIonAngle;
            simpleMeasurement.OutAnglePhi = 0.0;
            simpleMeasurement.CalculateNoise = false;
            simpleMeasurement.ConstEloss = 1;
            simpleMeasurement.ChannelMin = 0;
            simpleMeasurement.ChannelMax = (uint)(SelectedMeasurement.NumOfChannels - 1);
            simpleMeasurement.CaliEnergyPerChannel = SelectedMeasurement.EnergyCalLinear;
            simpleMeasurement.CaliEnergyPerChannelSquare = 0.0;
            simpleMeasurement.CaliEnergyPerChannelCube = 0.0;
            simpleMeasurement.CaliEnergyOffset = -SelectedMeasurement.EnergyCalOffset / SelectedMeasurement.EnergyCalLinear; // My E-Cal: ECal=Offset+x*Slope; Emanuels E-Cal: ECal=(x-Offset)*Slope
            simpleMeasurement.OdeInInitPrec = 1e-8;
            simpleMeasurement.OdeInMaxPrec = 1e-10;
            simpleMeasurement.OdeOutInitPrec = 1e-8;
            simpleMeasurement.OdeOutMaxPrec = 1e-10;

            double layerStart = 0;
            Console.WriteLine("Material count: " + SelectedSample.Material.Layers.Count);
            for (int i = 0; i < SelectedSample.Material.Layers.Count; i++)
            {
                Layer layer = SelectedSample.Material.Layers.FirstOrDefault(x => x.LayerIndex == i);
                Console.WriteLine("Try to add layer: {0}", layer.LayerName);
                foreach (LayerElement layerElement in layer.LayerElements)
                {
                    Console.WriteLine("Try to add element: {0}", layerElement.Isotope.Element.ShortName);
                    DataSimpleMaterial newSimpleMaterial = new DataSimpleMaterial();
                    newSimpleMaterial.AtomicNoInitialTarget = layerElement.Isotope.AtomicNumber;
                    newSimpleMaterial.MassNoInitialTarget = (int)layerElement.Isotope.Mass;
                    newSimpleMaterial.LayerBegin = layerStart;
                    newSimpleMaterial.LayerEnd = layerStart + layer.Thickness;
                    newSimpleMaterial.AtomicDensity = CalculateAtomicDensity(layer, layerElement);
                    newSimpleMaterial.QValue = 0.0;
                    newSimpleMaterial.AtomicNoRemainTarget = layerElement.Isotope.AtomicNumber;
                    newSimpleMaterial.MassNoRemainTarget = (int)layerElement.Isotope.Mass;
                    newSimpleMaterial.AtomicNoDetIon = SelectedMeasurement.Isotope.AtomicNumber;
                    newSimpleMaterial.MassNoDetIon = (int)SelectedMeasurement.Isotope.Mass;
                    newSimpleMaterial.RbsActive = true;
                    newSimpleMaterial.NraActive = false;

                    simpleMeasurement.Add((GLib.Object)newSimpleMaterial);
                }
                layerStart += layer.Thickness;
            }

            DialogResult = false;

            simpleMeasurement.Calculate();

            DataMatrix resultMatrix = simpleMeasurement.CalcedSpectrum;

            int[] spectrumYCalc = new int[SelectedMeasurement.NumOfChannels];

            for (int i = 0; i < SelectedMeasurement.NumOfChannels; i++)
                spectrumYCalc[i] = (int)resultMatrix[2, i];

            SelectedMeasurement.SpectrumYSimulated = spectrumYCalc;

            Database.SubmitChanges();
        }

        private void _CancelCommand()
        {
            DialogResult = false;
        }
    }
}

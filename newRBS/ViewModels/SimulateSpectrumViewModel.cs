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
    /// <summary>
    /// Class that is the view model of <see cref="Views.SimulateSpectrumView"/>. They calculate the simulate spectra (<see cref="Measurement.SpectrumYSimulated"/>) based on the corresponding <see cref="Sample.Material"/>.
    /// </summary>
    public class SimulateSpectrumViewModel : ViewModelBase
    {
        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ICommand StartSimulationCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private DatabaseDataContext Database;

        /// <summary>
        /// The selected <see cref="Measurement"/>.
        /// </summary>
        public Measurement SelectedMeasurement { get; set; }

        public Sample SelectedSample { get; set; }

        public Material SelectedMaterial { get; set; }

        /// <summary>
        /// The ion fluence of the simulation.
        /// </summary>
        public double IonFluence { get; set; }

        /// <summary>
        /// Constructor of the class. Sets up commands, initializes variables and checks whether a <see cref="Sample"/> and <see cref="Material"/> belongs to the <see cref="SelectedMeasurement"/>.
        /// </summary>
        /// <param name="MeasurementID"></param>
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

        /// <summary>
        /// Function that calculates the atomic density of an <see cref="LayerElement"/> inside a <see cref="Layer"/>.
        /// </summary>
        /// <param name="layer">The <see cref="Layer"/> containing the <see cref="LayerElement"/>.</param>
        /// <param name="layerElement">The <see cref="LayerElement"/> which atomic density is calulated.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Function that populates <see cref="DataSimpleMeasurement"/> and <see cref="DataSimpleMeasurement"/> and start the simulation.
        /// </summary>
        public void _StartSimulationCommand()
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

        /// <summary>
        /// Function that closes the window.
        /// </summary>
        public void _CancelCommand()
        {
            DialogResult = false;
        }
    }
}

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
using newRBS.Database;
using System.Diagnostics;
using System.Reflection;

namespace newRBS.ViewModels
{
    public class SampleEditorViewModel : ViewModelBase
    {
        public ICommand AddSampleCommand { get; set; }
        public ICommand RemoveSampleCommand { get; set; }
        public ICommand RenameSampleCommand { get; set; }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private DatabaseDataContext Database;

        public ObservableCollection<Sample> Samples { get; set; }
        private Sample _SelectedSample;
        public Sample SelectedSample
        {
            get { return _SelectedSample; }
            set { _SelectedSample = value; SelectedSampleChanged(); RaisePropertyChanged(); }
        }

        public ObservableCollection<Material> Materials { get; set; }
        private Material _SelectedMaterials;
        public Material SelectedMaterial
        {
            get { return _SelectedMaterials; }
            set { _SelectedMaterials = value; SelectedMaterialChanged(); RaisePropertyChanged(); }
        }

        public ObservableCollection<string> Layers { get; set; }

        public SampleEditorViewModel()
        {
            Database = MyGlobals.Database;

            AddSampleCommand = new RelayCommand(() => _AddSampleCommand(), () => true);
            RemoveSampleCommand = new RelayCommand(() => _RemoveSampleCommand(), () => true);
            RenameSampleCommand = new RelayCommand(() => _RenameSampleCommand(), () => true);

            SaveCommand = new RelayCommand(() => _SaveCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Layers = new ObservableCollection<string>();

            Materials = new ObservableCollection<Material>(Database.Materials.ToList());
            SelectedMaterial = new Material();

            Samples = new ObservableCollection<Sample>(Database.Samples.ToList());
            Samples.Remove(Samples.First(x => x.SampleName == "(undefined)"));
            SelectedSample = Samples.FirstOrDefault();
        }

        public void SelectedSampleChanged()
        {
            if (SelectedSample != null)
                SelectedMaterial = Materials.FirstOrDefault(x => x.MaterialID == SelectedSample.MaterialID);
        }

        public void SelectedMaterialChanged()
        {
            if (SelectedSample == null || SelectedMaterial == null) return;

            SelectedSample.MaterialID = SelectedMaterial.MaterialID;

            Layers.Clear();

            foreach (Layer layer in SelectedMaterial.Layers)
            {
                string newLayerString = string.Format("{0} ({1}nm", layer.LayerName, layer.Thickness);
                foreach (LayerElement layerElement in layer.LayerElements)
                    newLayerString += string.Format(", {0}", layerElement.Isotope.Element.LongName);
                newLayerString += ")";
                Layers.Add(newLayerString);
            }
        }

        public void _AddSampleCommand()
        {
            int? newSampleID = DatabaseUtils.AddNewSample();

            if (newSampleID == null) return;

            Sample newSample = Database.Samples.FirstOrDefault(x => x.SampleID == newSampleID);
            Samples.Add(newSample);
            SelectedSample = newSample;
        }

        public void _RemoveSampleCommand()
        {
            var measurements = Database.Measurements.Where(x => x.SampleID == SelectedSample.SampleID);
            foreach (Measurement measurement in measurements)
                measurement.Sample = Database.Samples.FirstOrDefault(x => x.SampleName == "(undefined)");

            Database.Samples.DeleteOnSubmit(SelectedSample);
            Samples.Remove(SelectedSample);
        }

        public void _RenameSampleCommand()
        {
            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new sample name:", SelectedSample.SampleName);
            if (inputDialog.ShowDialog() == true)
            {
                Database.Samples.FirstOrDefault(x => x.SampleID == SelectedSample.SampleID).SampleName = inputDialog.Answer;
            }
        }

        public void _SaveCommand()
        {
            Database.SubmitChanges();

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Saved samples changes in the database");

            DialogResult = false;
        }

        public void _CancelCommand()
        {
            DialogResult = false;
        }
    }
}

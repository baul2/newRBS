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

namespace newRBS.ViewModels
{
    public class SampleEditorViewModel : ViewModelBase
    {
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private Models.DatabaseDataContext Database;

        public ObservableCollection<Models.Sample> Samples { get; set; }
        private Models.Sample _SelectedSample;
        public Models.Sample SelectedSample
        {
            get { return _SelectedSample; }
            set { _SelectedSample = value; SelectedSampleChanged(); RaisePropertyChanged(); }
        }

        public ObservableCollection<Models.Material> Materials { get; set; }
        private Models.Material _SelectedMaterials;
        public Models.Material SelectedMaterial
        {
            get { return _SelectedMaterials; }
            set { _SelectedMaterials = value; SelectedMaterialChanged(); RaisePropertyChanged(); }
        }

        public ObservableCollection<string> Layers { get; set; }

        public SampleEditorViewModel()
        {
            Database = new Models.DatabaseDataContext(MyGlobals.ConString);

            SaveCommand = new RelayCommand(() => _SaveCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            Layers = new ObservableCollection<string>();

            Materials = new ObservableCollection<Models.Material>(Database.Materials.ToList());
            SelectedMaterial = new Models.Material();

            Samples = new ObservableCollection<Models.Sample>(Database.Samples.ToList());
            Samples.Remove(Samples.First(x => x.SampleName == "New..."));
            Samples.Remove(Samples.First(x => x.SampleName == "(undefined)"));
            SelectedSample = Samples.FirstOrDefault();
        }

        public void SelectedSampleChanged()
        {
            Console.WriteLine("SelectedSampleChanged");

            Console.WriteLine(SelectedSample.MaterialID);
            foreach (var m in Materials)
                Console.WriteLine("{0} {1}", m.MaterialID, m.MaterialName);
            Console.WriteLine(Materials.FirstOrDefault(x => x.MaterialID == SelectedSample.MaterialID).MaterialName);
            SelectedMaterial = Materials.FirstOrDefault(x => x.MaterialID == SelectedSample.MaterialID);
        }

        public void SelectedMaterialChanged()
        {
            Console.WriteLine("SelectedMaterialChanged");

            if (SelectedSample == null || SelectedMaterial == null) return;

            SelectedSample.MaterialID = SelectedMaterial.MaterialID;

            Layers.Clear();

            foreach (Models.Layer layer in SelectedMaterial.Layers)
            {
                string newLayerString = string.Format("{0} ({1}nm", layer.LayerName, layer.Thickness);
                foreach (Models.Element element in layer.Elements)
                    newLayerString += string.Format(", {0}", element.ElementName);
                newLayerString += ")";
                Layers.Add(newLayerString);
            }
        }

        public void _SaveCommand()
        {
            Console.WriteLine("_SaveCommand");

            Database.SubmitChanges();

            DialogResult = false;
        }

        public void _CancelCommand()
        {
            DialogResult = false;
        }
    }
}

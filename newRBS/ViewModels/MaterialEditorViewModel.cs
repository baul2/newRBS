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
    public class MaterialEditorViewModel : ViewModelBase
    {
        public ICommand AddMaterialCommand { get; set; }
        public ICommand RemoveMaterialCommand { get; set; }
        public ICommand RenameMaterialCommand { get; set; }

        public ICommand AddLayerCommand { get; set; }
        public ICommand RemoveLayerCommand { get; set; }
        public ICommand MoveLayerUpCommand { get; set; }
        public ICommand MoveLayerDownCommand { get; set; }

        public ICommand AddElementCommand { get; set; }
        public ICommand RemoveElementCommand { get; set; }

        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private DatabaseDataContext Database;

        public ObservableCollection<Material> Materials { get; set; }

        private Material _SelectedMaterial;
        public Material SelectedMaterial
        {
            get { return _SelectedMaterial; }
            set
            {
                _SelectedMaterial = value;

                Layers.Clear();

                if (_SelectedMaterial != null)
                    if (_SelectedMaterial.Layers != null)
                        foreach (Layer layer in _SelectedMaterial.Layers)
                            Layers.Add(layer);

                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Layer> Layers { get; set; }

        private Layer _SelectedLayer;
        public Layer SelectedLayer
        {
            get { return _SelectedLayer; }
            set
            {
                _SelectedLayer = value;

                Elements.Clear();

                if (_SelectedLayer != null)
                    if (_SelectedLayer.Elements != null)
                        foreach (Element element in _SelectedLayer.Elements)
                            Elements.Add(element);

                RaisePropertyChanged();
            }
        }

        public CollectionViewSource LayersViewSource { get; set; }

        public ObservableCollection<Element> Elements { get; set; }

        private Element _SelectedElement;
        public Element SelectedElement
        { get { return _SelectedElement; } set { _SelectedElement = value; RaisePropertyChanged(); } }

        public MaterialEditorViewModel()
        {
            Database = MyGlobals.Database;

            Materials = new ObservableCollection<Material>(Database.Materials.Where(x => x.MaterialName != "(undefined)").ToList());
            Layers = new ObservableCollection<Layer>();
            Elements = new ObservableCollection<Element>();

            AddMaterialCommand = new RelayCommand(() => _AddMaterialCommand(), () => true);
            RemoveMaterialCommand = new RelayCommand(() => _RemoveMaterialCommand(), () => true);
            RenameMaterialCommand = new RelayCommand(() => _RenameMaterialCommand(), () => true);

            AddLayerCommand = new RelayCommand(() => _AddLayerCommand(), () => true);
            RemoveLayerCommand = new RelayCommand(() => _RemoveLayerCommand(), () => true);
            MoveLayerUpCommand = new RelayCommand(() => _MoveLayerUpCommand(), () => true);
            MoveLayerDownCommand = new RelayCommand(() => _MoveLayerDownCommand(), () => true);

            AddElementCommand = new RelayCommand(() => _AddElementCommand(), () => true);
            RemoveElementCommand = new RelayCommand(() => _RemoveElementCommand(), () => true);

            SaveCommand = new RelayCommand(() => _SaveCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            LayersViewSource = new CollectionViewSource();
            LayersViewSource.Source = Layers;
            LayersViewSource.SortDescriptions.Add(new SortDescription("LayerIndex", ListSortDirection.Ascending));
        }

        public void _AddMaterialCommand()
        {
            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new material name:", "new Material");
            if (inputDialog.ShowDialog() == true)
            {
                Material newMaterial = new Material { MaterialName = inputDialog.Answer };

                Database.Materials.InsertOnSubmit(newMaterial);

                Materials.Add(newMaterial);
            }
        }

        public void _RemoveMaterialCommand()
        {
            if (SelectedMaterial == null) return;

            Database.Elements.DeleteAllOnSubmit(Database.Elements.Where(x => x.MaterialID == SelectedMaterial.MaterialID));
            Database.Layers.DeleteAllOnSubmit(Database.Layers.Where(x => x.MaterialID == SelectedMaterial.MaterialID));
            Database.Materials.DeleteOnSubmit(SelectedMaterial);
            Materials.Remove(SelectedMaterial);

            SelectedMaterial = null;
        }

        public void _RenameMaterialCommand()
        {
            Views.Utils.InputDialog inputDialog = new Views.Utils.InputDialog("Enter new material name:", SelectedMaterial.MaterialName);
            if (inputDialog.ShowDialog() == true)
                SelectedMaterial.MaterialName = inputDialog.Answer;
        }

        public void _AddLayerCommand()
        {
            if (SelectedMaterial == null) return;

            Layer newLayer = new Layer { LayerIndex = Layers.Count(), MaterialID = SelectedMaterial.MaterialID, Density = 1 };

            Layers.Add(newLayer);

            SelectedMaterial.Layers.Add(newLayer);
        }

        public void _RemoveLayerCommand()
        {
            if (SelectedMaterial == null || SelectedLayer == null) return;

            var LayersToDecreaseIndex = Database.Layers.Where(x => x.MaterialID == SelectedMaterial.MaterialID && x.LayerIndex > SelectedLayer.LayerIndex);
            foreach (var layer in LayersToDecreaseIndex)
                layer.LayerIndex--;

            Database.Elements.DeleteAllOnSubmit(Database.Elements.Where(x => x.LayerID == SelectedLayer.LayerID));

            Database.Layers.DeleteOnSubmit(SelectedLayer);

            Layers.Remove(SelectedLayer);
            SelectedLayer = null;
        }

        public void _MoveLayerUpCommand()
        {
            if (SelectedLayer.LayerIndex == 0) return;

            int SelectedLayerIndex = SelectedLayer.LayerIndex;

            var topLayer = Layers.FirstOrDefault(x => x.LayerIndex == (SelectedLayerIndex - 1));
            var selectedLayer = Layers.FirstOrDefault(x => x.LayerIndex == SelectedLayerIndex);

            topLayer.LayerIndex += 1;
            selectedLayer.LayerIndex -= 1;

            LayersViewSource.View.Refresh();

            SelectedLayer = selectedLayer;
        }

        public void _MoveLayerDownCommand()
        {
            if (SelectedLayer.LayerIndex == SelectedMaterial.Layers.Count() - 1) return;

            int SelectedLayerIndex = SelectedLayer.LayerIndex;

            var selectedLayer = Layers.FirstOrDefault(x => x.LayerIndex == SelectedLayerIndex);
            var bottomLayer = Layers.FirstOrDefault(x => x.LayerIndex == (SelectedLayerIndex + 1));

            bottomLayer.LayerIndex -= 1;
            selectedLayer.LayerIndex += 1;

            LayersViewSource.View.Refresh();

            SelectedLayer = selectedLayer;
        }

        public void _AddElementCommand()
        {
            if (SelectedLayer == null) return;
            //if (SelectedLayer.LayerID == 0) { MessageBox.Show("Save the new layer before adding elements!"); return; }

            Element newElement = new Element { MaterialID = SelectedMaterial.MaterialID, LayerID = SelectedLayer.LayerID, ElementName = "", StoichiometricFactor = 1, AtomicNumber = 0, MassNumber = 0 };

            Elements.Add(newElement);

            SelectedLayer.Elements.Add(newElement);
            SelectedMaterial.Elements.Add(newElement);
        }

        public void _RemoveElementCommand()
        {
            if (SelectedLayer == null || SelectedElement == null) return;

            Database.Elements.DeleteOnSubmit(SelectedElement);
            Elements.Remove(SelectedElement);
        }

        public void _SaveCommand()
        {
            Database.SubmitChanges();

            trace.Value.TraceEvent(TraceEventType.Information, 0, "Saved material changes in the database");

            DialogResult = false;
        }

        public void _CancelCommand()
        {
            DialogResult = false;
        }
    }
}

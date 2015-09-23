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
    public class LayerElementListItem : ViewModelBase
    {
        public ObservableCollection<Element> Elements { get; set; }

        private Element _Element;
        public Element Element
        {
            get { return _Element; }
            set
            {
                _Element = value;

                Isotopes.Clear();
                foreach (Isotope i in value.Isotopes.OrderBy(x => x.MassNumber))
                    Isotopes.Add(i);

                Isotope = Isotopes.FirstOrDefault();

                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Isotope> Isotopes { get; set; }

        private Isotope _Isotope;
        public Isotope Isotope
        { get { return _Isotope; } set { _Isotope = value; LayerElement.Isotope = value; RaisePropertyChanged(); } }

        public LayerElement LayerElement { get; set; }

        public LayerElementListItem(DatabaseDataContext database, LayerElement layerElement)
        {
            Isotope initialIsotope = layerElement.Isotope;
            LayerElement = layerElement;

            Elements = new ObservableCollection<Element>(database.Elements.ToList());
            Isotopes = new ObservableCollection<Isotope>();

            if (layerElement.Isotope!=null)
                Element = layerElement.Isotope.Element;

            Isotope = initialIsotope;
        }
    }

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
        { get { return _SelectedLayer; } set { _SelectedLayer = value; FillLayerElements(); RaisePropertyChanged(); } }

        public CollectionViewSource LayersViewSource { get; set; }

        public ObservableCollection<LayerElementListItem> LayerElements { get; set; }

        private LayerElementListItem _SelectedLayerElement;
        public LayerElementListItem SelectedLayerElement
        { get { return _SelectedLayerElement; } set { _SelectedLayerElement = value; RaisePropertyChanged(); } }

        public MaterialEditorViewModel()
        {
            Database = MyGlobals.Database;

            Materials = new ObservableCollection<Material>(Database.Materials.Where(x => x.MaterialName != "(undefined)").ToList());
            Layers = new ObservableCollection<Layer>();
            LayerElements = new ObservableCollection<LayerElementListItem>();

            AddMaterialCommand = new RelayCommand(() => _AddMaterialCommand(), () => true);
            RemoveMaterialCommand = new RelayCommand(() => _RemoveMaterialCommand(), () => true);
            RenameMaterialCommand = new RelayCommand(() => _RenameMaterialCommand(), () => true);

            AddLayerCommand = new RelayCommand(() => _AddLayerCommand(), () => true);
            RemoveLayerCommand = new RelayCommand(() => _RemoveLayerCommand(), () => true);
            MoveLayerUpCommand = new RelayCommand(() => _MoveLayerUpCommand(), () => true);
            MoveLayerDownCommand = new RelayCommand(() => _MoveLayerDownCommand(), () => true);

            AddElementCommand = new RelayCommand(() => _AddLayerElementCommand(), () => true);
            RemoveElementCommand = new RelayCommand(() => _RemoveElementCommand(), () => true);

            SaveCommand = new RelayCommand(() => _SaveCommand(), () => true);
            CancelCommand = new RelayCommand(() => _CancelCommand(), () => true);

            LayersViewSource = new CollectionViewSource();
            LayersViewSource.Source = Layers;
            LayersViewSource.SortDescriptions.Add(new SortDescription("LayerIndex", ListSortDirection.Ascending));
        }

        private void FillLayerElements()
        {
            LayerElements.Clear();

            if (_SelectedLayer != null)
                if (_SelectedLayer.LayerElements != null)
                    foreach (LayerElement layerElement in _SelectedLayer.LayerElements)
                    {
                        var item = new LayerElementListItem(Database, layerElement);
                        LayerElements.Add(item);
                    }
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

            Database.LayerElements.DeleteAllOnSubmit(Database.LayerElements.Where(x => x.MaterialID == SelectedMaterial.MaterialID));
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

            Database.LayerElements.DeleteAllOnSubmit(Database.LayerElements.Where(x => x.LayerID == SelectedLayer.LayerID));

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

        public void _AddLayerElementCommand()
        {
            if (SelectedLayer == null) return;

            LayerElement newLayerElement = new LayerElement { MaterialID = SelectedMaterial.MaterialID, LayerID = SelectedLayer.LayerID, StoichiometricFactor = 1 };

            LayerElements.Add(new LayerElementListItem(Database, newLayerElement));

            SelectedLayer.LayerElements.Add(newLayerElement);
            SelectedMaterial.LayerElements.Add(newLayerElement);
        }

        public void _RemoveElementCommand()
        {
            if (SelectedLayer == null || SelectedLayerElement == null) return;

            Database.LayerElements.DeleteOnSubmit(SelectedLayerElement.LayerElement);
            LayerElements.Remove(SelectedLayerElement);
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

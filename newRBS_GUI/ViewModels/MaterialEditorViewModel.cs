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

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        private Models.RBS_Database Database;

        public ObservableCollection<Models.Material> Materials { get; set; }

        private Models.Material _SelectedMaterial;
        public Models.Material SelectedMaterial
        {
            get { return _SelectedMaterial; }
            set
            {
                _SelectedMaterial = value;

                Layers.Clear();

                if (_SelectedMaterial != null)
                    if (_SelectedMaterial.Layers != null)
                        foreach (Models.Layer layer in _SelectedMaterial.Layers)
                            Layers.Add(layer);

                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Models.Layer> Layers { get; set; }

        private Models.Layer _SelectedLayer;
        public Models.Layer SelectedLayer
        {
            get { return _SelectedLayer; }
            set
            {
                _SelectedLayer = value;

                Elements.Clear();

                if (_SelectedLayer != null)
                    if (_SelectedLayer.Elements != null)
                        foreach (Models.Element element in _SelectedLayer.Elements)
                            Elements.Add(element);

                RaisePropertyChanged();
            }
        }

        public CollectionViewSource LayersViewSource { get; set; }

        public ObservableCollection<Models.Element> Elements { get; set; }

        private Models.Element _SelectedElement;
        public Models.Element SelectedElement
        { get { return _SelectedElement; } set { _SelectedElement = value; RaisePropertyChanged(); } }

        public MaterialEditorViewModel()
        {
            Database = new Models.RBS_Database(MyGlobals.ConString);

            Materials = new ObservableCollection<Models.Material>(Database.Materials.ToList());
            Layers = new ObservableCollection<Models.Layer>();
            Elements = new ObservableCollection<Models.Element>();

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
            Console.WriteLine("_AddMaterialCommand");

            ViewUtils.InputDialog inputDialog = new ViewUtils.InputDialog("Enter new material name:", "new Material");
            if (inputDialog.ShowDialog() == true)
            {
  
                Models.Material newMaterial = new Models.Material { MaterialName = inputDialog.Answer };

                Database.Materials.InsertOnSubmit(newMaterial);
                Materials.Add(newMaterial);
            }
        }

        public void _RemoveMaterialCommand()
        {
            Console.WriteLine("_RemoveMaterialCommand");

            if (SelectedMaterial == null) return;

            Database.Elements.DeleteAllOnSubmit(Database.Elements.Where(x => x.MaterialID == SelectedMaterial.MaterialID));
            Database.Layers.DeleteAllOnSubmit(Database.Layers.Where(x => x.MaterialID == SelectedMaterial.MaterialID));
            Database.Materials.DeleteOnSubmit(SelectedMaterial);
            Materials.Remove(SelectedMaterial);

            SelectedMaterial = null;
        }

        public void _RenameMaterialCommand()
        {
            Console.WriteLine("_RenameMaterialCommand");

            ViewUtils.InputDialog inputDialog = new ViewUtils.InputDialog("Enter new material name:", SelectedMaterial.MaterialName);
            if (inputDialog.ShowDialog() == true)
                SelectedMaterial.MaterialName = inputDialog.Answer;
        }

        public void _AddLayerCommand()
        {
            Console.WriteLine("_AddLayerCommand");

            if (SelectedMaterial == null) return;

            Models.Layer newLayer = new Models.Layer { LayerIndex = Layers.Count(), MaterialID = SelectedMaterial.MaterialID, Density = 1 };

            Database.Layers.InsertOnSubmit(newLayer);
            Layers.Add(newLayer);
        }

        public void _RemoveLayerCommand()
        {
            Console.WriteLine("_RemoveLayerCommand");

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
            Console.WriteLine("_MoveLayerUpCommand");

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
            Console.WriteLine("_MoveLayerDownCommand");

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
            Console.WriteLine("_AddElementButton");

            if (SelectedLayer == null) return;
            //if (SelectedLayer.LayerID == 0) { MessageBox.Show("Save the new layer before adding elements!"); return; }

            Models.Element newElement = new Models.Element { MaterialID = SelectedMaterial.MaterialID, LayerID = SelectedLayer.LayerID };

            Database.Elements.InsertOnSubmit(newElement);
            Elements.Add(newElement);
        }

        public void _RemoveElementCommand()
        {
            Console.WriteLine("_RemoveElementButton");

            if (SelectedLayer == null || SelectedElement == null) return;

            Database.Elements.DeleteOnSubmit(SelectedElement);
            Elements.Remove(SelectedElement);
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

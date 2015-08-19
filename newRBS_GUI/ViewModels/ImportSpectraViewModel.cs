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
using Microsoft.Win32;
using System.IO;
using OxyPlot;



namespace newRBS.ViewModels
{
    public class AreaData
    {
        public double x1 { get; set; }
        public double y1 { get; set; }
        public double x2 { get; set; }
        public double y2 { get; set; }
    }

    public class ImportSpectraViewModel : ViewModelBase
    {
        public ICommand OpenFileCommand { get; set; }
        public ICommand AddCurrentSpectrumCommand { get; set; }

        private Models.DataSpectra dataSpectra;

        private bool? _DialogResult;
        public bool? DialogResult
        { get { return _DialogResult; } set { _DialogResult = value; RaisePropertyChanged(); } }

        public ObservableCollection<Models.Spectrum> newSpectra { get; set; }

        private Models.Spectrum _selectedSpectrum;
        public Models.Spectrum selectedSpectrum
        {
            get { return _selectedSpectrum; }
            set
            {
                _selectedSpectrum = value;
                areaData.Clear();
                int[] temp = value.SpectrumY;
                for (int i = 0; i < temp.Count(); i++)
                    areaData.Add(new AreaData { x1 = i, y1 = temp[i], x2 = i, y2 = 0 });
                RaisePropertyChanged("selectedSpectrum");
            }
        }

        private string _SelectedPath;
        public string SelectedPath
        { get { return _SelectedPath; } set { _SelectedPath = value; RaisePropertyChanged("SelectedPath"); } }

        private ObservableCollection<AreaData> _areaData;
        public ObservableCollection<AreaData> areaData
        { get { return _areaData; } set { _areaData = value; RaisePropertyChanged(); } }

        private string _FileContent;
        public string FileContent
        { get { return _FileContent; } set { _FileContent = value; RaisePropertyChanged("FileContent"); } }

        public ImportSpectraViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();
            OpenFileCommand = new RelayCommand(() => _OpenFileCommand(), () => true);
            AddCurrentSpectrumCommand = new RelayCommand(() => _AddCurrentSpectrumCommand(), () => true);

            areaData = new ObservableCollection<AreaData>() { new AreaData { x1 = 1, y1 = 10, x2 = 1, y2 = 0 }, new AreaData { x1 = 2, y1 = 20, x2 = 2, y2 = 0 } };
            newSpectra = new ObservableCollection<Models.Spectrum>();
            selectedSpectrum = new Models.Spectrum();
        }

        public void _OpenFileCommand()
        {
            var dialog = new OpenFileDialog { };
            dialog.ShowDialog();

            SelectedPath = dialog.FileName;

            List<Models.Spectrum> loadedSpectra = dataSpectra.ImportSpectra(SelectedPath);

            newSpectra.Clear();

            foreach (Models.Spectrum spectrum in loadedSpectra)
                newSpectra.Add(spectrum);

            selectedSpectrum = newSpectra.First();
        }

        public void _AddCurrentSpectrumCommand()
        {
            dataSpectra.AddSpectrum(selectedSpectrum);
        }
    }
}

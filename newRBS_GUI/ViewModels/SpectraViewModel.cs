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

namespace newRBS.ViewModels
{
    public delegate void SpectrumNewHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumRemoveHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumYHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumInfosHandler(object o, Models.SpectrumArgs e);
    public delegate void SpectrumFinishedHandler(object o, Models.SpectrumArgs e);


    public class SpectraViewModel : ViewModelBase
    {
        private Models.CAEN_x730 cAEN_x730;
        private Models.DataSpectra dataSpectra;
        private Models.MeasureSpectra measureSpectra;

        private SpectraListViewModel spectraListViewModel;
        private SpectraFilterViewModel spectraFilterViewModel;

        public string myString { get; set; }

        public SpectraViewModel()
        {
            spectraListViewModel = SimpleIoc.Default.GetInstance<SpectraListViewModel>();
            spectraFilterViewModel = SimpleIoc.Default.GetInstance<SpectraFilterViewModel>();

            // Hooking up to events from DataSpectra
            dataSpectra.EventSpectrumNew += new Models.DataSpectra.ChangedEventHandler(SpectrumNew);
            dataSpectra.EventSpectrumRemove += new Models.DataSpectra.ChangedEventHandler(SpectrumRemove);
            dataSpectra.EventSpectrumY += new Models.DataSpectra.ChangedEventHandler(SpectrumY);
            dataSpectra.EventSpectrumInfos += new Models.DataSpectra.ChangedEventHandler(SpectrumInfos);

            myString = "SomeString";
        }

        private void SpectrumNew(object sender, Models.SpectrumArgs e)
        {
            Console.WriteLine("New Spectra");
            spectraListViewModel.ChangeFilter(spectraFilterViewModel.selectedFilter);
            //Models.Spectrum newSpectrum = dataSpectra.spectra[e.ID];

            //_measurementList.Add(new Measurement(newSpectrum));
        }

        private void SpectrumRemove(object sender, Models.SpectrumArgs e)
        {
            Console.WriteLine("New Spectra");
            //Models.Spectrum newSpectrum = dataSpectra.spectra[e.ID];

            //_measurementList.Add(new Measurement(newSpectrum));
        }

        private void SpectrumY(object sender, Models.SpectrumArgs e)
        {
            Console.WriteLine("Updated Spectra");
            spectraListViewModel.UpdateLine(e.spec);
        }

        private void SpectrumInfos(object sender, Models.SpectrumArgs e)
        {
            Console.WriteLine("SpectrumInfos");
            //Models.Spectrum spectrum = dataSpectra.spectra[e.ID];
            //var found = _measurementList.FirstOrDefault(i => i.spectrumID == e.ID);
            //if (found != null)
            {
                //   int i = _measurementList.IndexOf(found);
                //_measurementList[i] = new Measurement(spectrum);
            }
        }
    }
}

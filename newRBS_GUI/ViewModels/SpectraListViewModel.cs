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

namespace newRBS.ViewModels
{
    public class SpectraListViewModel
    {
        private Models.DataSpectra dataSpectra { get; set; }

        public AsyncObservableCollection<Models.Spectrum> spectraList { get; set; }

        public SpectraListViewModel()
        {
            dataSpectra = SimpleIoc.Default.GetInstance<Models.DataSpectra>();

            spectraList = new AsyncObservableCollection<Models.Spectrum>();

            List<Models.Spectrum> temp = dataSpectra.GetSpectra_All();
            foreach (Models.Spectrum spectrum in temp)
                spectraList.Add(spectrum);
        }

        public void ChangeFilter(NodeViewModel selectedFilter)
        {
            spectraList.Clear();
            Console.WriteLine("FilterType: {0}", selectedFilter.Type);

            switch (selectedFilter.Type)
            {
                case "All":
                    {
                        List<Models.Spectrum> temp = dataSpectra.GetSpectra_All();
                        foreach (Models.Spectrum spectrum in temp)
                            spectraList.Add(spectrum);
                        break;
                    }
                case "Date":
                    {
                        List<Models.Spectrum> temp = dataSpectra.GetSpectra_Date(selectedFilter);
                        foreach (Models.Spectrum spectrum in temp)
                            spectraList.Add(spectrum);
                        break;
                    }
                case "Sample":
                    {

                        break;
                    }
                case "Channel":
                    {
                        List<Models.Spectrum> temp = dataSpectra.GetSpectra_Channel(selectedFilter);
                        foreach (Models.Spectrum spectrum in temp)
                            spectraList.Add(spectrum);
                        break;
                    }
            }
            Console.WriteLine("Length of spectraList: {0}", spectraList.Count());
        }

        public void UpdateLine(Models.Spectrum spec)
        {
            var found = spectraList.Where(x => x.SpectrumID == spec.SpectrumID).FirstOrDefault();

            if (found != null)
            {
                spectraList[spectraList.IndexOf(found)] = spec;
            }

        }
    }
}

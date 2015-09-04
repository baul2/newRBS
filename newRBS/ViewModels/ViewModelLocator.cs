/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:newRBS"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;

namespace newRBS.ViewModels
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            // The ViewModels
            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<MeasurementFilterViewModel>();
            SimpleIoc.Default.Register<MeasurementListViewModel>();
            SimpleIoc.Default.Register<MeasurementPlotViewModel>();
            SimpleIoc.Default.Register<EnergyCalibrationViewModel>(); 

            // The Models
            SimpleIoc.Default.Register<Models.CAEN_x730>();
            SimpleIoc.Default.Register<Models.MeasureSpectra>();
            SimpleIoc.Default.Register<Models.MeasureWaveform>();
        }

        // The ViewModels
        public MainViewModel mainViewModel
        { get { return ServiceLocator.Current.GetInstance<MainViewModel>(); } }

        public MeasurementFilterViewModel measurementFilterViewModel
        { get { return ServiceLocator.Current.GetInstance<MeasurementFilterViewModel>(); } }

        public MeasurementListViewModel measurementListViewModel
        { get { return ServiceLocator.Current.GetInstance<MeasurementListViewModel>(); } }

        public MeasurementPlotViewModel measurementPlotViewModel
        { get { return ServiceLocator.Current.GetInstance<MeasurementPlotViewModel>(); } }

        public EnergyCalibrationViewModel analysisViewModel
        { get { return ServiceLocator.Current.GetInstance<EnergyCalibrationViewModel>(); } }


        // The Models
        public Models.CAEN_x730 cAen_X730
        { get { return ServiceLocator.Current.GetInstance<Models.CAEN_x730>(); } }

        public Models.MeasureSpectra measureSpectra
        { get { return ServiceLocator.Current.GetInstance<Models.MeasureSpectra>(); } }

        public Models.MeasureWaveform measureWaveform
        { get { return ServiceLocator.Current.GetInstance<Models.MeasureWaveform>(); } }
        

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}
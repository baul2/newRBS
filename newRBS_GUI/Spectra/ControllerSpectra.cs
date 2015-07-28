using System;
using System.Collections.Generic;

namespace newRBS.Spectra
{
    public delegate void SpectrumNewHandler(object o, SpectrumArgs e);
    public delegate void SpectrumRemoveHandler(object o, SpectrumArgs e);
    public delegate void SpectrumYHandler(object o, SpectrumArgs e);
    public delegate void SpectrumInfosHandler(object o, SpectrumArgs e);
    public delegate void SpectrumFinishedHandler(object o, SpectrumArgs e);
    
    public class SpectrumNewListener
    {
        public void ShowOnScreen(object o, SpectrumArgs e) { Console.WriteLine("New spectrum for measurement {0}", e.ID); }
    }

    class ControllerSpectra
    {
        public static event SpectrumNewHandler EventSpectrumNew;
        public static event SpectrumRemoveHandler EventSpectrumRemove;
        public static event SpectrumYHandler EventSpectrumY;
        public static event SpectrumInfosHandler EventSpectrumParams;
        public static event SpectrumFinishedHandler EventSpectrumFinished;
        
        public ControllerSpectra()
        {
            SpectrumNewListener listener = new SpectrumNewListener();
            EventSpectrumNew = new SpectrumNewHandler(listener.ShowOnScreen);
        }
    }
}

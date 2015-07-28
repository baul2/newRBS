using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace newRBS
{
    class newRBS_Console
    {

        static void Main(string[] args)
        {
            Devices.CAEN_x730 cAEN_x730 = new Devices.CAEN_x730();
            Spectra.DataSpectra dataSpectra = new Spectra.DataSpectra();
            Spectra.MeasureSpectra measureSpectra = new Spectra.MeasureSpectra(cAEN_x730, dataSpectra);

            //Devices.ChannelParams channelParams = new Devices.ChannelParams();
            //channelParams.DCoffset = 1000;
            //cAEN_x730.SetChannelConfig(0, channelParams);

            measureSpectra.StartMeasurements(); // Default is channel 0

            System.Threading.Thread.Sleep(2000);

            measureSpectra.StopMeasurements();

            cAEN_x730.Close();
            Console.ReadKey();
        }
    }
}

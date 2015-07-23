using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace newRBS_Console
{
    class newRBS_Console
    {
        static void Main(string[] args)
        {
            CAEN_x730 cAENx730 = new CAEN_x730();
            cAENx730.StartAcquisition(0);
            System.Threading.Thread.Sleep(2000);
            UInt32[] hist = cAENx730.GetHistogram(0);
            int Sum = 0;
            for (int i = 0; i < hist.Length; i++)
            { Sum += (int)hist[i]; }
            Console.WriteLine(Sum);

            //Test
            //Logger.Info("Measurement running", "Main");
            //Logger.Error("sdf", "Main");


            cAENx730.StopAcquisition(0);
            cAENx730.Close();
            Console.ReadKey();
        }
    }
}

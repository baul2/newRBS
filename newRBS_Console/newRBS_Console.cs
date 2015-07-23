using System;
using System.Diagnostics;

namespace newRBS_Console
{
    class newRBS_Console
    {

        static void Main(string[] args)
        {
            TraceSource trace = new TraceSource("newRBS_Console");
            NamespaceCAEN_x730.CAEN_x730 cAENx730 = new NamespaceCAEN_x730.CAEN_x730();
            cAENx730.StartAcquisition(0);
            System.Threading.Thread.Sleep(2000);
            uint[] hist = cAENx730.GetHistogram(0);
            int Sum = 0;
            for (int i = 0; i < hist.Length; i++)
            { Sum += (int)hist[i]; }
            Console.WriteLine(Sum);

            trace.TraceEvent(TraceEventType.Information, 0, "{0} counts", Sum);

            cAENx730.StopAcquisition(0);
            cAENx730.Close();
            Console.ReadKey();
        }
    }
}

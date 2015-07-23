using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace newRBS_Console.Classes
{
    public delegate void NewSpectrumHandler(object o, NewSpectrumArgs e);

    public class NewSpectrumArgs : EventArgs
    {
        public readonly int ID;
        public NewSpectrumArgs(int id) { ID = id; }
    }

    public class NewSpectrumListener // Has to move to controller
    {
        public void ShowOnScreen(object o, NewSpectrumArgs e) { Console.WriteLine("New spectrum for measurement {0}", e.ID); }
    }

    enum Ion
    {
        H,
        He,
        Li,
    };

    class DataSpectrum
    {
        public int ID;
        public int channel;
        public string name;
        public Ion ion;
        public int ionEnergy;
        public DateTime startTime;
        public DateTime stopTime;
        public DateTime duration;
        public float progress;
        public bool runs;
        public float energyCalOffset = 0;
        public float energyCalSlope = 1;

        public readonly int[] SpectrumX = new int[16384];
        public float[] SpectrumCalX
        {
            get
            {
                float[] temp = new float[SpectrumX.Length];
                for (int i = 0; i < SpectrumX.Length; i++)
                    temp[i] = energyCalSlope * SpectrumX[i] + energyCalOffset;
                return temp;
            }
        }
        private int[] spectrumY;

        public int[] SpectrumY
        {
            get { return spectrumY; }
            set
            {
                spectrumY = value;
                NewSpectrumArgs e1 = new NewSpectrumArgs(ID);
                if (EventNewSpectrum != null) EventNewSpectrum(this, e1);
            }
        }

        public static event NewSpectrumHandler EventNewSpectrum;

        public DataSpectrum(int id, int ch)
        {
            ID = id;
            channel = ch;
            startTime = DateTime.Now;
            for (int i = 0; i < 16384; i++) { SpectrumX[i] = i; }
            NewSpectrumListener listener = new NewSpectrumListener(); // Has to move to controller
            EventNewSpectrum = new NewSpectrumHandler(listener.ShowOnScreen); // Has to move to controller
        }
    }
}

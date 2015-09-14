using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;


namespace newRBS.Models
{
    public class Coulombo
    {
       
        private SerialPort SPort;
        private char IMess;
        private char ZS = (char)13;

        private void Init(string COM)
        {
            string Port;
            int b, i, d;
            Parity pari;
            StopBits sb;

            Port = COM.Substring(0, 4);
            COM = COM.Remove(0, 5);
            i = COM.IndexOf(",", 0);
            b = int.Parse(COM.Substring(0, i));
            COM = COM.Remove(0, i + 1);
            i = COM.IndexOf(",", 0);
            pari = (Parity)Enum.Parse(typeof(Parity), COM.Substring(0, i), true);
            COM = COM.Remove(0, i + 1);
            i = COM.IndexOf(",", 0);
            d = int.Parse(COM.Substring(0, i));
            COM = COM.Remove(0, i + 1);
            i = COM.IndexOf(",", 0);
            sb = (StopBits)Enum.Parse(typeof(StopBits), COM.Substring(0, i), true);
            COM = COM.Remove(0, i + 1);
            IMess = (char)int.Parse(COM, System.Globalization.NumberStyles.HexNumber);
               
            
            SPort = new SerialPort(Port, b, pari, d, sb);
            SPort.ReadBufferSize = 100;
            SPort.WriteBufferSize = 100;
            SPort.Encoding = Encoding.GetEncoding(28591); 
           
            SPort.Open();
        }
        

        private string WRCom(string text)
        {
         string indata;

        indata = SPort.ReadExisting();
            indata = "";
            SPort.Write(text);
            Thread.Sleep(200);
            indata = SPort.ReadExisting();

            if (indata == "")
            { return "Error"; }
            else
            { return indata; }
        }

        [PreferredConstructor]
        public Coulombo()
        {
            string COM = "COM3,9600,None,8,Two,94";  //94 = Kenncode des Gerätes in Hex
            Init(COM);
        }

        public Coulombo(string COM)
        {
            Init(COM);
        }

        public void Close()
        {
            SPort.Close();
        }

        public string Version()
        {
            return WRCom("V" + IMess + ZS);
        }

        public string Stop()
        {
            return WRCom("P" + IMess + ZS);
        }

        public string Start()
        {
            return WRCom("S" + IMess + ZS);
        }

        public string Cont()
        {
            return WRCom("K" + IMess + ZS);
        }

        public string MBereich(string mbs)
        {
            string mb;

            mb = "7";
            switch (mbs)
            {
                case "1nA"  : mb = "9"; break;
                case "10nA" : mb = "8"; break;
                case "100nA": mb = "7"; break;
                case "1µA"  : mb = "6"; break;
                case "10µA" : mb = "5"; break;
                
            }
            
            return WRCom(mb + IMess + ZS);
        }

        public string SetLadung(double lm)
        {
            string lms,lmsm;
            int lmi;

            lmi = (int) System.Math.Round(lm * 1000000);
            lms = lmi.ToString("D12");
            lmsm = "";
            lmsm= lmsm + (char)((lms[0] & 15) * 16 + (lms[1] & 15))+
                        (char)((lms[2] & 15) * 16 + (lms[3] & 15))+
                        (char)((lms[4] & 15) * 16 + (lms[5] & 15))+
                        (char)((lms[6] & 15) * 16 + (lms[7] & 15))+
                        (char)((lms[8] & 15) * 16 + (lms[9] & 15))+
                        (char)((lms[10] & 15) * 16 + (lms[11] & 15));

            return WRCom(lmsm + "pC" + IMess + ZS);
        }

        public double GetCharge()
        {
            string lms;
           
            lms = WRCom("Q" + IMess + ZS);
            if (lms.Length == 9)
            {
                return (double)(lms[0] & 240) / 16 * 100000 + (double)(lms[0] & 15) * 10000 +
                    (double)(lms[1] & 240) / 16 * 1000 + (double)(lms[1] & 15) * 100 +
                    (double)(lms[2] & 240) / 16 * 10 + (double)(lms[2] & 15) +
                    (double)(lms[3] & 240) / 160 + (double)(lms[3] & 15) / 100 +
                    (double)(lms[4] & 240) / 16000 + (double)(lms[4] & 15) / 10000 +
                    (double)(lms[5] & 240) / 1600000 + (double)(lms[5] & 15) / 1000000;
            }
            else
            { return -1; }
        }

        public string Zustand()
        {
            return WRCom("Z" + IMess + ZS);
        }

        public double GetStrom()
        {
            string lms;

            lms = WRCom("I" + IMess + ZS);
            if (lms.Length == 8)
            {
                if ((byte)lms[1] == 255)
                { return -2; }
                else
                {
                    return (double)(lms[1] & 240) / 16 * 10000 + (double)(lms[1] & 15) * 1000 +
                           (double)(lms[2] & 240) / 16 * 100 + (double)(lms[2] & 15) * 10 +
                           (double)(lms[3] & 240) / 16 + (double)(lms[3] & 15) / 10 +
                           (double)(lms[4] & 240) / 1600 + (double)(lms[4] & 15) / 1000;
                }
            }
            else
            { return -1; }
        }

    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System.Diagnostics;
using System.Reflection;


namespace newRBS.Models
{
    public static class Coulombo
    {
        public static bool IsInit = false;

        private static string className = MethodBase.GetCurrentMethod().DeclaringType.Name;
        private static readonly Lazy<TraceSource> trace = new Lazy<TraceSource>(() => TraceSources.Create(className));

        private static SerialPort SPort;
        private static char IMess;
        private static char ZS = (char)13;

        public static void Init()
        {
            Init("COM4", 9600, Parity.None, 8, StopBits.Two, "94"); //94 = Kenncode des Gerätes in Hex
        }

        public static void Init(string PortName, int BaudRate, Parity parity, int DataBits, StopBits stopBits, string CoulomboID)
        {
            //"COM3,9600,None,8,Two,94"
            IMess = (char)int.Parse(CoulomboID, System.Globalization.NumberStyles.HexNumber);
            //IMess = (char)148;

            SPort = new SerialPort(PortName, BaudRate, parity, DataBits, stopBits);
            SPort.ReadBufferSize = 100;
            SPort.WriteBufferSize = 100;
            SPort.Encoding = Encoding.GetEncoding(28591);

            try
            {
                SPort.Open();

                if (Version() == "Error")
                    throw new System.IO.IOException();

                trace.Value.TraceEvent(TraceEventType.Information, 0, "Coulombo opend");
                IsInit = true;
            }
            catch (System.IO.IOException e)
            {
                trace.Value.TraceEvent(TraceEventType.Error, 0, "Coulombo couldn't be opend");
            }

        }

        private static string Command(string text)
        {
            if (SPort.IsOpen == false) return "Error";

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

        public static void Close()
        {
            SPort.Close();
        }

        public static string Version()
        {
            return Command("V" + IMess + ZS);
        }

        public static void Stop()
        {
            if (Command("P" + IMess + ZS) == "Error")
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Charge measurement couldn't be stoped");
            else
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Charge measurement stoped");
        }

        public static void Start()
        {
            if (Command("S" + IMess + ZS) == "Error")
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Charge measurement couldn't be started");
            else
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Charge measurement started");
        }

        public static void Continue()
        {
            if (Command("K" + IMess + ZS) == "Error")
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Charge measurement couldn't be continued");
            else
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Charge measurement continued");
        }

        public static void SetMeasurementRange(string mbs)
        {
            string mb = "7";

            switch (mbs)
            {
                case "1nA": mb = "9"; break;
                case "10nA": mb = "8"; break;
                case "100nA": mb = "7"; break;
                case "1µA": mb = "6"; break;
                case "10µA": mb = "5"; break;
            }

            if (Command(mb + IMess + ZS) == "Error")
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Measurement range couldn't be canged");
            else
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Measurement range set to " + mbs);
        }

        public static void SetCharge(double lm)
        {
            string lms, lmsm;
            int lmi;

            lmi = (int)Math.Round(lm * 1000000);
            lms = lmi.ToString("D12");
            lmsm = "";
            lmsm = lmsm + (char)((lms[0] & 15) * 16 + (lms[1] & 15)) +
                        (char)((lms[2] & 15) * 16 + (lms[3] & 15)) +
                        (char)((lms[4] & 15) * 16 + (lms[5] & 15)) +
                        (char)((lms[6] & 15) * 16 + (lms[7] & 15)) +
                        (char)((lms[8] & 15) * 16 + (lms[9] & 15)) +
                        (char)((lms[10] & 15) * 16 + (lms[11] & 15));

            if (Command(lmsm + "pC" + IMess + ZS) == "Error")
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Final charge couldn't be canged");
            else
                trace.Value.TraceEvent(TraceEventType.Information, 0, "Final charge set to " + lm.ToString("{0.00}"));
        }

        public static double GetCharge()
        {
            string lms;

            double charge;

            lms = Command("Q" + IMess + ZS);
            if (lms.Length == 9)
            {
                charge = (double)(lms[0] & 240) / 16 * 100000 + (double)(lms[0] & 15) * 10000 +
                    (double)(lms[1] & 240) / 16 * 1000 + (double)(lms[1] & 15) * 100 +
                    (double)(lms[2] & 240) / 16 * 10 + (double)(lms[2] & 15) +
                    (double)(lms[3] & 240) / 160 + (double)(lms[3] & 15) / 100 +
                    (double)(lms[4] & 240) / 16000 + (double)(lms[4] & 15) / 10000 +
                    (double)(lms[5] & 240) / 1600000 + (double)(lms[5] & 15) / 1000000;
            }
            else
            { charge = 0; }

            if (lms == "Error")
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Current charge couldn't be read");
            else
                trace.Value.TraceEvent(TraceEventType.Verbose, 0, "Current charge was read: " + charge.ToString("{0.00}" + "µC"));

            return charge;
        }

        public static string GetStatus()
        {
            return Command("Z" + IMess + ZS);
        }

        public static double GetCurrent()
        {
            string lms;

            double current;

            lms = Command("I" + IMess + ZS);
            if (lms.Length == 8)
            {
                if ((byte)lms[1] == 255)
                { return -2; }
                else
                {
                    current = (double)(lms[1] & 240) / 16 * 10000 + (double)(lms[1] & 15) * 1000 +
                           (double)(lms[2] & 240) / 16 * 100 + (double)(lms[2] & 15) * 10 +
                           (double)(lms[3] & 240) / 16 + (double)(lms[3] & 15) / 10 +
                           (double)(lms[4] & 240) / 1600 + (double)(lms[4] & 15) / 1000;
                }
            }
            else
            { current = -1; }

            if (lms == "Error")
                trace.Value.TraceEvent(TraceEventType.Warning, 0, "Current current couldn't be read");
            else
                trace.Value.TraceEvent(TraceEventType.Verbose, 0, "Current current was read: " + current.ToString("{0.00}" + "µC"));

            return current;
        }

    }
}

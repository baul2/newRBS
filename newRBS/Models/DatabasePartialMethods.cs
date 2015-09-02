using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data;
using System.ComponentModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Command;

namespace newRBS.Models
{
    public partial class DatabaseDataContext
    {
        partial void OnCreated()
        {
            //Console.WriteLine("OnCreated");
            var dlo = new DataLoadOptions();
            dlo.LoadWith<Measurement>(c => c.Sample);
            dlo.LoadWith<Material>(c => c.Layers);
            dlo.LoadWith<Layer>(c => c.Elements);
            //this.LoadOptions = dlo;
            //this.Log = Console.Out;
        }
    }

    public partial class Measurement
    {
        public int[] SpectrumY
        {
            get
            {
                if (SpectrumYByte == null)
                    Console.WriteLine("null");
                int[] intArray = new int[SpectrumYByte.Length / sizeof(int)];
                Buffer.BlockCopy(SpectrumYByte.ToArray(), 0, intArray, 0, intArray.Length * sizeof(int));
                return intArray;
            }
            set
            {
                byte[] byteArray = new byte[value.Length * sizeof(int)];
                Buffer.BlockCopy(value, 0, byteArray, 0, byteArray.Length);
                SpectrumYByte = byteArray;
            }
        }

        public float[] SpectrumXCal
        {
            get
            {
                float[] spectrumXCal = new float[NumOfChannels];
                for (int i = 0; i < NumOfChannels; i++)
                    spectrumXCal[i] = (float)EnergyCalOffset + i * (float)EnergyCalSlope;
                return spectrumXCal;
            }
        }
    }

        public partial class DatabaseDataContext
        {
            partial void InsertMeasurement(Measurement measurement)
            {
                //Console.WriteLine("DatabaseDataContext.InsertMeasurement");

                ExecuteDynamicInsert(measurement);

                int temp = measurement.Sample.SampleID;
                DatabaseUtils.SendMeasurementNewEvent(measurement);
            }

            partial void UpdateMeasurement(Measurement measurement)
            {
                //Console.WriteLine("DatabaseDataContext.UpdateMeasurement");

                ExecuteDynamicUpdate(measurement);

                int temp = measurement.Sample.SampleID;
                DatabaseUtils.SendMeasurementUpdateEvent(measurement);
            }

            partial void DeleteMeasurement(Measurement measurement)
            {
                //Console.WriteLine("DatabaseDataContext.DeleteMeasurement");

                ExecuteDynamicDelete(measurement);

                DatabaseUtils.SendMeasurementRemoveEvent(measurement);
            }
        }
    }

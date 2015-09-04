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
    /// <summary>
    /// Class that represents the MS SQL Server database. 
    /// </summary>
    /// <remarks>
    /// It contains tables of types <see cref="Measurement"/>, <see cref="Sample"/>, <see cref="Material"/>, <see cref="Layer"/>, <see cref="Element"/>, <see cref="Project"/> and <see cref="Measurement_Project"/>.
    /// </remarks>
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

    /// <summary>
    /// Class that stores a single measurement.
    /// </summary>
    /// <remarks>
    /// Can be saved to the MS SQL Server database via a table of <see cref="Measurement"/>s in <see cref="DatabaseDataContext"/>.
    /// </remarks>
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

    /// <summary>
    /// Class that stores a element in <see cref="Layer"/> of a <see cref="Material"/>.
    /// </summary>
    /// <remarks>
    /// Can be saved to the MS SQL Server database via a table of <see cref="Element"/>s in <see cref="DatabaseDataContext"/>.
    /// </remarks>
    public partial class Element{}

    /// <summary>
    /// Class that stores a layer of a <see cref="Material"/>.
    /// </summary>
    /// <remarks>
    /// Can be saved to the MS SQL Server database via a table of <see cref="Layer"/>s in <see cref="DatabaseDataContext"/>.
    /// </remarks>
    public partial class Layer { }

    /// <summary>
    /// Class that stores a material definition. <see cref="Sample"/>s can contain a reference to a <see cref="Material"/>.
    /// </summary>
    /// <remarks>
    /// Can be saved to the MS SQL Server database via a table of <see cref="Material"/>s in <see cref="DatabaseDataContext"/>.
    /// </remarks>
    public partial class Material { }

    /// <summary>
    /// Class that stores a sample. Can contain a reference to a <see cref="Material"/>.
    /// </summary>
    /// <remarks>
    /// Can be saved to the MS SQL Server database via a table of <see cref="Sample"/>s in <see cref="DatabaseDataContext"/>.
    /// </remarks>
    public partial class Sample { }

    /// <summary>
    /// Class that stores a project containing several <see cref="Measurement"/>s as defined in <see cref="Measurement_Project"/>.
    /// </summary>
    /// <remarks>
    /// Can be saved to the MS SQL Server database via a table of <see cref="Project"/>s in <see cref="DatabaseDataContext"/>.
    /// </remarks>
    public partial class Project { }

    /// <summary>
    /// Class that stores the relationship between <see cref="Measurement"/>s and <see cref="Project"/>s.
    /// </summary>
    /// <remarks>
    /// Can be saved to the MS SQL Server database via a table of <see cref="Measurement_Project"/>s in <see cref="DatabaseDataContext"/>.
    /// </remarks>
    public partial class Measurement_Project { }
}

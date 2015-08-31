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
        public event PropertyChangedEventHandler NewSampleToAdd;

        partial void OnSampleIDChanged()
        {
            //Console.WriteLine("OnSampleIDChanged");

            if (SampleID == 2)
                if ((this.NewSampleToAdd != null))
                    this.NewSampleToAdd(this, new PropertyChangedEventArgs("SampleID"));
        }
    }

    public partial class DatabaseDataContext
    {
        partial void InsertMeasurement(Measurement measurement)
        {
            //Console.WriteLine("DatabaseDataContext.InsertMeasurement");

            ExecuteDynamicInsert(measurement);

            int temp = measurement.Sample.SampleID;
            SimpleIoc.Default.GetInstance<DatabaseUtils>().SendMeasurementNewEvent(measurement);
        }

        partial void UpdateMeasurement(Measurement measurement)
        {
            //Console.WriteLine("DatabaseDataContext.UpdateMeasurement");

            ExecuteDynamicUpdate(measurement);

            int temp = measurement.Sample.SampleID;
            SimpleIoc.Default.GetInstance<DatabaseUtils>().SendMeasurementUpdateEvent(measurement);
        }

        partial void DeleteMeasurement(Measurement measurement)
        {
            //Console.WriteLine("DatabaseDataContext.DeleteMeasurement");

            ExecuteDynamicDelete(measurement);

            SimpleIoc.Default.GetInstance<DatabaseUtils>().SendMeasurementRemoveEvent(measurement);
        }
    }
}

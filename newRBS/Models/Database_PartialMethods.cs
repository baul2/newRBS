using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data;

namespace newRBS.Models
{
    public partial class DatabaseDataContext
    {
        partial void OnCreated()
        {
            Console.WriteLine("OnCreated");
            var dlo = new DataLoadOptions();
            dlo.LoadWith<Measurement>(c => c.Sample);
            dlo.LoadWith<Material>(c => c.Layers);
            dlo.LoadWith<Layer>(c => c.Elements);
            //this.LoadOptions = dlo;
            //this.Log = Console.Out;
        }
    }
}

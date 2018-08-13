using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModLoaderLibrary.API
{
    class ACEOProcurement
    {
        static void AddProcurement(ProcureableProduct template)
        {
            Singleton<ProcurementController>.Instance.allAvailableProcureableProducts.Add(template);
        }
    }
}
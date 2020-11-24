﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor.Reports2
{
    public class BandsawReportAssembly2
    {
        public int Qty { get; set; }
        public string AssemblyName { get; set; }

        public string AssemblyDesc { get; set; }

        public List<byte []> Pages { get; set; }

        public List<BandsawReportOrder2> ReportOrders { get; set; }


    }
}

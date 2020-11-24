using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VaultItemProcessor.Reports1
{
    public class BandsawReportPart
    {
        public int Qty { get; set; }
        public string PartName { get; set; }

        public string PartDesc { get; set; }

        public string Material { get; set; }

        public string Thickness { get; set; }

        public string Operation { get; set; }
        public List<byte[]> Pages { get; set; }
    }
}

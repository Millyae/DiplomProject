using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject.ViewModels
{
    public class RateViewModel
    {
        public int IdRate { get; set; }
        [Browsable(false)]
        public int IdObject { get; set; }
        [Browsable(false)]
        public int IdAddress { get; set; }
        public int IdService { get; set; }
        public string ObjectName { get; set; }
        public string Address { get; set; }
        public string ServiceName { get; set; }
        public decimal HourlyRate { get; set; }

    }
}

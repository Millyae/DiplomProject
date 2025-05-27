using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject.ViewModels
{
    public class ObjectViewModel
    {
        public int IdObject { get; set; }
        public string ObjectName { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string Building { get; set; }
        public string Corpus { get; set; }
        public string Office { get; set; }
        public string FullAddress { get; set; }
    }
}

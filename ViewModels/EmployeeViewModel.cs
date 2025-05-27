using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject.ViewModels
{
    public class EmployeeViewModel
    {
        public int IdEmployee { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Metro { get; set; }
        public string Experience { get; set; }
        public string Schedules { get; set; }
        public string Notes { get; set; }
        public string Comments { get; set; }
        public DateOnly? HireDate { get; set; }
        public int? IdFio { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomProject
{
    public class WorkScheduleDisplay
    {
        public string ObjectName { get; set; }
        public string Address { get; set; }
        public string ServiceName { get; set; }
        public DateOnly? WorkDate { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public decimal HoursWorked { get; set; }
        public string Notes { get; set; }
        public DateTime RecordCreated { get; set; }
        public int ScheduleId { get; set; }
        public int? ObjectId { get; set; }
        public int? AddressId { get; set; }
        public int? ServiceId { get; set; }
    }
}

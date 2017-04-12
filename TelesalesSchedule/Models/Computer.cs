using System.Collections.Generic;

namespace TelesalesSchedule.Models
{
    public class Computer
    {
        public Computer()
        {
            this.Schedules = new HashSet<Schedule>();
        }
        public int Id { get; set; }

        public string Name { get; set; }

        public string ComputerName { get; set; }

        public string IpAddress { get; set; }

        public string Location { get; set; }

        public bool IsWorking { get; set; }

        public virtual ICollection<Schedule> Schedules { get; set; }
    }
}
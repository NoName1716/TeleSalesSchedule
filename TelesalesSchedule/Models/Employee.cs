using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TelesalesSchedule.Models
{
    public class Employee
    {
        public Employee()
        {
            this.Schedules = new HashSet<Schedule>();
        }
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        public DateTime BirthDay { get; set; }

        [Required]
        public bool FullTimeAgent { get; set; }

        public bool SeniorSpecialist { get; set; }

        public int? ManagerId { get; set; }

        public Employee Manager { get; set; }
        
        public bool IsDeleted { get; set; }

        public string UserName { get; set; }
        
        public bool SaveDeskAgent { get; set; }

        public virtual ICollection<Schedule> Schedules { get; set; }
    }
}
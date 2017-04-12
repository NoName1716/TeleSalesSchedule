using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TelesalesSchedule.Models
{
    public class EditEmployeeViewModel
    {
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        public string UserName { get; set; }

        public int? ManagerId { get; set; }

        [DisplayName("Manager")]
        public string ManagerFullName { get; set; }

        public DateTime BirthDay { get; set; }

        [Required]
        public bool FullTimeAgent { get; set; }

        public bool SaveDeskAgent { get; set; }

        public bool SeniorSpecialist { get; set; }

        public bool IsDeleted { get; set; }
    }
}
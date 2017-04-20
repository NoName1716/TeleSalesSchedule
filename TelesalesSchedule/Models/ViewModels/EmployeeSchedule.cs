using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TelesalesSchedule.Models.ViewModels
{
    public class EmployeeSchedule
    {
        public int Id { get; set; }

        public string FullName { get; set; }
        public double? Hours { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TelesalesSchedule.Models.ViewModels
{
    public class ScheduleView
    {
        public int Id { get; set; }

        public string MondayStart { get; set; }

        public string MondayEnd { get; set; }

        public string ThuesdayStart { get; set; }

        public string ThuesdayEnd { get; set; }

        public string WednesdayStart { get; set; }

        public string WednesdayEnd { get; set; }

        public string ThursdayStart { get; set; }

        public string ThursdayEnd { get; set; }

        public string FridayStart { get; set; }

        public string FridayEnd { get; set; }

        public string SaturdayStart { get; set; }

        public string SaturdayEnd { get; set; }

        public string SundayStart { get; set; }

        public string SundayEnd { get; set; }

        public string Hours { get; set; }  // added

        public string StartDate { get; set; }

        public string EndDate { get; set; }
    }
}
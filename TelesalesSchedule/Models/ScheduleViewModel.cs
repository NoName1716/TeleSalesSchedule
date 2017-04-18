using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TelesalesSchedule.Models
{
    public class ScheduleViewModel
    {
        public int Id { get; set; }

        //it must be Monday
        [Required]
        public DateTime StartDate { get; set; }

        //it must be Sunday
        [Required]
        public DateTime EndDate { get; set; }

        [Range(9, 13)]
        [DisplayName("Monday Shift One")]
        public double? MondayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? MondayShiftOneEnd { get; set; }

        [Range(13, 17)]
        [DisplayName("Monday Shift Two")]
        public double? MondayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? MondayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        [DisplayName("Monday Shift Three")]
        public double? MondayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? MondayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        [DisplayName("Thuesday Shift One")]
        public double? ThuesdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? ThuesdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        [DisplayName("Thuesday Shift Two")]
        public double? ThuesdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? ThuesdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        [DisplayName("Thuesday Shift Three")]
        public double? ThuesdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? ThuesdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        [DisplayName("Wednesday Shift One")]
        public double? WednesdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? WednesdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        [DisplayName("Wednesday Shift Two")]
        public double? WednesdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? WednesdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        [DisplayName("Wednesday Shift Three")]
        public double? WednesdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? WednesdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        [DisplayName("Thursday Shift One")]
        public double? ThursdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? ThursdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        [DisplayName("Thursday Shift Two")]
        public double? ThursdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? ThursdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        [DisplayName("Thursday Shift Three")]
        public double? ThursdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? ThursdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        [DisplayName("Friday Shift One")]
        public double? FridayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? FridayShiftOneEnd { get; set; }

        [Range(13, 17)]
        [DisplayName("Friday Shift Two")]
        public double? FridayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? FridayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        [DisplayName("Friday Shift Three")]
        public double? FridayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? FridayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        [DisplayName("Saturday Shift One")]
        public double? SaturdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? SaturdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        [DisplayName("Saturday Shift Two")]
        public double? SaturdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? SaturdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        [DisplayName("Saturday Shift Three")]
        public double? SaturdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? SaturdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        [DisplayName("Sunday Shift One")]
        public double? SundayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? SundayShiftOneEnd { get; set; }

        [Range(13, 17)]
        [DisplayName("Sunday Shift Two")]
        public double? SundayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? SundayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        [DisplayName("Sunday Shift Three")]
        public double? SundayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? SundayShiftThreeEnd { get; set; }
    }
}
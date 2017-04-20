using System;
using System.ComponentModel.DataAnnotations;

namespace TelesalesSchedule.Models
{
    public class Schedule
    {
        //public Schedule()
        //{
        //    this.Employees = new HashSet<Employee>();
        //    this.Computers = new HashSet<Computer>();
        //}
        public int Id { get; set; }
        //it must be Monday
        [Required]
        public DateTime StartDate { get; set; }
        //it must be Sunday
        [Required]
        public DateTime EndDate { get; set; }
        [Range(9, 13)]
        public double? MondayShiftOneStart { get; set; }
        [Range(9, 13)]
        public double? MondayShiftOneEnd { get; set; }
        [Range(13, 17)]
        public double? MondayShiftTwoStart { get; set; }
        [Range(13, 17)]
        public double? MondayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        public double? MondayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? MondayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        public double? ThuesdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? ThuesdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        public double? ThuesdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? ThuesdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        public double? ThuesdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? ThuesdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        public double? WednesdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? WednesdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        public double? WednesdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? WednesdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        public double? WednesdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? WednesdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        public double? ThursdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? ThursdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        public double? ThursdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? ThursdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        public double? ThursdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? ThursdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        public double? FridayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? FridayShiftOneEnd { get; set; }

        [Range(13, 17)]
        public double? FridayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? FridayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        public double? FridayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? FridayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        public double? SaturdayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? SaturdayShiftOneEnd { get; set; }

        [Range(13, 17)]
        public double? SaturdayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? SaturdayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        public double? SaturdayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? SaturdayShiftThreeEnd { get; set; }

        [Range(9, 13)]
        public double? SundayShiftOneStart { get; set; }

        [Range(9, 13)]
        public double? SundayShiftOneEnd { get; set; }

        [Range(13, 17)]
        public double? SundayShiftTwoStart { get; set; }

        [Range(13, 17)]
        public double? SundayShiftTwoEnd { get; set; }

        [Range(17, 21)]
        public double? SundayShiftThreeStart { get; set; }

        [Range(17, 21)]
        public double? SundayShiftThreeEnd { get; set; }

        public double? Hours { get; set; }

        //public virtual ICollection<Employee> Employees { get; set; }

        //public virtual ICollection<Computer> Computers { get; set; }

        public Computer Computer { get; set; }

        public Employee Employee { get; set; }
    }
}
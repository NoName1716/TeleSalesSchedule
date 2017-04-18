using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using TelesalesSchedule.Extensions;
using TelesalesSchedule.Models;
using TelesalesSchedule.Models.ViewModels;

namespace TelesalesSchedule.Controllers
{
    public class ScheduleController : Controller
    {
        // GET: Schedule
        public ActionResult Index()
        {
            return View();
        }

        //Get: 
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(ScheduleView model)
        {
            using (var db = new TelesalesScheduleDbContext())
            {
                var employee = db.Employees.Where(e => e.UserName == this.User.Identity.Name).FirstOrDefault();

                if (employee == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                else
                {
                    DateTime nextMonday = DateTime.Now.AddDays(1);
                    while (nextMonday.DayOfWeek != DayOfWeek.Monday)
                    {
                        nextMonday = nextMonday.AddDays(1);
                    }
                    var nextSunday = nextMonday.AddDays(6);

                    //chek if Employee Schedule is exist
                    if (employee.Schedules.Select(s => s.StartDate.Date).Contains(nextMonday.Date) || employee.Schedules.Select(s => s.StartDate.Date).Contains(nextSunday.Date))
                    {
                        ViewBag.ErrorMessage = "Schedule already exist. If you want you can edit it!";
                        return View();
                    }
                    else
                    {
                        //Create schedule for each pc for the next week
                        CreatePcSchedule(db, nextMonday, nextSunday);
                        string error = string.Empty;
                        error = MondayCheck(db, nextMonday, nextSunday, error, model);
                        if (!string.IsNullOrEmpty(error))
                        {
                            ViewBag.MondayError = error;
                            return View();
                        }
                        error = ThuesdayCheck(db, nextMonday, nextSunday, error, model);
                        if (!string.IsNullOrEmpty(error))
                        {
                            ViewBag.ThuesdayError = error;
                            return View();
                        }
                        error = WednesdayCheck(db, nextMonday, nextSunday, error, model);
                        if (!string.IsNullOrEmpty(error))
                        {
                            ViewBag.WednesdayError = error;
                            return View();
                        }
                        error = ThursdayCheck(db, nextMonday, nextSunday, error, model);
                        if (!string.IsNullOrEmpty(error))
                        {
                            ViewBag.ThursdayError = error;
                            return View();
                        }
                        error = FridayCheck(db, nextMonday, nextSunday, error, model);
                        if (!string.IsNullOrEmpty(error))
                        {
                            ViewBag.FridayError = error;
                            return View();
                        }
                        error = SaturdayCheck(db, nextMonday, nextSunday, error, model);
                        if (!string.IsNullOrEmpty(error))
                        {
                            ViewBag.SaturdayError = error;
                            return View();
                        }
                        error = SundayCheck(db, nextMonday, nextSunday, error, model);
                        if (!string.IsNullOrEmpty(error))
                        {
                            ViewBag.SundayError = error;
                            return View();
                        }
                        //Add schedule to user
                        AddingScheduleToEmployee(db, nextMonday, nextSunday, model, employee);

                        db.SaveChanges();
                        this.AddNotification("Schedule created.", NotificationType.SUCCESS);
                    }
                }
                return RedirectToAction("List");
            }
        }

        public ActionResult ListMySchedules()
        {
            using (var db = new TelesalesScheduleDbContext())
            {
                var emp = db.Employees.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var schedules = emp.Schedules.ToList();
                return View(schedules);
            }
        }

        public ActionResult ListAll()
        {
            using (var db = new TelesalesScheduleDbContext())
            {
                var schedules = db.Schedules.ToList();
                return View(schedules);
            }
        }

        //
        // GET: Schedule/Edit
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                var schedule = context.Schedules
                    .FirstOrDefault(s => s.Id == id);

                if (schedule == null)
                {
                    return HttpNotFound();
                }

                var model = new ScheduleView();
                model.Id = schedule.Id;
                model.Hours = schedule.Hours.ToString();

                //Monday
                if (schedule.MondayShiftThreeEnd - schedule.MondayShiftOneStart == 9)
                {
                    model.MondayStart = schedule.MondayShiftOneStart.ToString();
                    model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
                }
                else if (schedule.MondayShiftTwoEnd - schedule.MondayShiftOneStart == 8)
                {
                    model.MondayStart = schedule.MondayShiftOneStart.ToString();
                    model.MondayEnd = schedule.MondayShiftTwoEnd.ToString();
                }
                else if (schedule.MondayShiftThreeEnd - schedule.MondayShiftTwoStart == 8)
                {
                    model.MondayStart = schedule.MondayShiftTwoStart.ToString();
                    model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
                }
                else if (schedule.MondayShiftOneEnd - schedule.MondayShiftOneStart == 4)
                {
                    model.MondayStart = schedule.MondayShiftOneStart.ToString();
                    model.MondayEnd = schedule.MondayShiftOneEnd.ToString();
                }
                else if (schedule.MondayShiftTwoEnd - schedule.MondayShiftTwoStart == 4)
                {
                    model.MondayStart = schedule.MondayShiftTwoStart.ToString();
                    model.MondayEnd = schedule.MondayShiftTwoEnd.ToString();
                }
                else if (schedule.MondayShiftThreeEnd - schedule.MondayShiftThreeStart == 4)
                {
                    model.MondayStart = schedule.MondayShiftThreeStart.ToString();
                    model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
                }

                //Thuesday
                if (schedule.ThuesdayShiftThreeEnd - schedule.ThuesdayShiftOneStart == 9)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThuesdayShiftTwoEnd - schedule.ThuesdayShiftOneStart == 8)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThuesdayShiftThreeEnd - schedule.ThuesdayShiftTwoStart == 8)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftTwoStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThuesdayShiftOneEnd - schedule.ThuesdayShiftOneStart == 4)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftOneEnd.ToString();
                }
                else if (schedule.ThuesdayShiftTwoEnd - schedule.ThuesdayShiftTwoStart == 4)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftTwoStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThuesdayShiftThreeEnd - schedule.ThuesdayShiftThreeStart == 4)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftThreeStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
                }

                //Wednesday
                if (schedule.WednesdayShiftThreeEnd - schedule.WednesdayShiftOneStart == 9)
                {
                    model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
                }
                else if (schedule.WednesdayShiftTwoEnd - schedule.WednesdayShiftOneStart == 8)
                {
                    model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftTwoEnd.ToString();
                }
                else if (schedule.WednesdayShiftThreeEnd - schedule.WednesdayShiftTwoStart == 8)
                {
                    model.WednesdayStart = schedule.WednesdayShiftTwoStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
                }
                else if (schedule.WednesdayShiftOneEnd - schedule.WednesdayShiftOneStart == 4)
                {
                    model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftOneEnd.ToString();
                }
                else if (schedule.WednesdayShiftTwoEnd - schedule.WednesdayShiftTwoStart == 4)
                {
                    model.WednesdayStart = schedule.WednesdayShiftTwoStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftTwoEnd.ToString();
                }
                else if (schedule.WednesdayShiftThreeEnd - schedule.WednesdayShiftThreeStart == 4)
                {
                    model.WednesdayStart = schedule.WednesdayShiftThreeStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
                }

                //Thursday
                if (schedule.ThursdayShiftThreeEnd - schedule.ThursdayShiftOneStart == 9)
                {
                    model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThursdayShiftTwoEnd - schedule.ThursdayShiftOneStart == 8)
                {
                    model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThursdayShiftThreeEnd - schedule.ThursdayShiftTwoStart == 8)
                {
                    model.ThursdayStart = schedule.ThursdayShiftTwoStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThursdayShiftOneEnd - schedule.ThursdayShiftOneStart == 4)
                {
                    model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftOneEnd.ToString();
                }
                else if (schedule.ThursdayShiftTwoEnd - schedule.ThursdayShiftTwoStart == 4)
                {
                    model.ThursdayStart = schedule.ThursdayShiftTwoStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThursdayShiftThreeEnd - schedule.ThursdayShiftThreeStart == 4)
                {
                    model.ThursdayStart = schedule.ThursdayShiftThreeStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
                }

                //Friday
                if (schedule.FridayShiftThreeEnd - schedule.FridayShiftOneStart == 9)
                {
                    model.FridayStart = schedule.FridayShiftOneStart.ToString();
                    model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
                }
                else if (schedule.FridayShiftTwoEnd - schedule.FridayShiftOneStart == 8)
                {
                    model.FridayStart = schedule.FridayShiftOneStart.ToString();
                    model.FridayEnd = schedule.FridayShiftTwoEnd.ToString();
                }
                else if (schedule.FridayShiftThreeEnd - schedule.FridayShiftTwoStart == 8)
                {
                    model.FridayStart = schedule.FridayShiftTwoStart.ToString();
                    model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
                }
                else if (schedule.FridayShiftOneEnd - schedule.FridayShiftOneStart == 4)
                {
                    model.FridayStart = schedule.FridayShiftOneStart.ToString();
                    model.FridayEnd = schedule.FridayShiftOneEnd.ToString();
                }
                else if (schedule.FridayShiftTwoEnd - schedule.FridayShiftTwoStart == 4)
                {
                    model.FridayStart = schedule.FridayShiftTwoStart.ToString();
                    model.FridayEnd = schedule.FridayShiftTwoEnd.ToString();
                }
                else if (schedule.FridayShiftThreeEnd - schedule.FridayShiftThreeStart == 4)
                {
                    model.FridayStart = schedule.FridayShiftThreeStart.ToString();
                    model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
                }

                //Saturday
                if (schedule.SaturdayShiftThreeEnd - schedule.SaturdayShiftOneStart == 9)
                {
                    model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
                }
                else if (schedule.SaturdayShiftTwoEnd - schedule.SaturdayShiftOneStart == 8)
                {
                    model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftTwoEnd.ToString();
                }
                else if (schedule.SaturdayShiftThreeEnd - schedule.SaturdayShiftTwoStart == 8)
                {
                    model.SaturdayStart = schedule.SaturdayShiftTwoStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
                }
                else if (schedule.SaturdayShiftOneEnd - schedule.SaturdayShiftOneStart == 4)
                {
                    model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftOneEnd.ToString();
                }
                else if (schedule.SaturdayShiftTwoEnd - schedule.SaturdayShiftTwoStart == 4)
                {
                    model.SaturdayStart = schedule.SaturdayShiftTwoStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftTwoEnd.ToString();
                }
                else if (schedule.SaturdayShiftThreeEnd - schedule.SaturdayShiftThreeStart == 4)
                {
                    model.SaturdayStart = schedule.SaturdayShiftThreeStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
                }

                //Sunday
                if (schedule.SundayShiftThreeEnd - schedule.SundayShiftOneStart == 9)
                {
                    model.SundayStart = schedule.SundayShiftOneStart.ToString();
                    model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
                }
                else if (schedule.SundayShiftTwoEnd - schedule.SundayShiftOneStart == 8)
                {
                    model.SundayStart = schedule.SundayShiftOneStart.ToString();
                    model.SundayEnd = schedule.SundayShiftTwoEnd.ToString();
                }
                else if (schedule.SundayShiftThreeEnd - schedule.SundayShiftTwoStart == 8)
                {
                    model.SundayStart = schedule.SundayShiftTwoStart.ToString();
                    model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
                }

                else if (schedule.SundayShiftOneEnd - schedule.SundayShiftOneStart == 4)
                {
                    model.SundayStart = schedule.SundayShiftOneStart.ToString();
                    model.SundayEnd = schedule.SundayShiftOneEnd.ToString();
                }
                else if (schedule.SundayShiftTwoEnd - schedule.SundayShiftTwoStart == 4)
                {
                    model.SundayStart = schedule.SundayShiftTwoStart.ToString();
                    model.SundayEnd = schedule.SundayShiftTwoEnd.ToString();
                }
                else if (schedule.SundayShiftThreeEnd - schedule.SundayShiftThreeStart == 4)
                {
                    model.SundayStart = schedule.SundayShiftThreeStart.ToString();
                    model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
                }

                return View(model);
            }
        }

        //
        // POST: Schedule/Edit
        [HttpPost]
        public ActionResult Edit(ScheduleView model)
        {
            // TODO: something is not right...
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    var schedule = context.Schedules
                        .FirstOrDefault(s => s.Id == model.Id && s.Employees.Any(e => e.UserName == this.User.Identity.Name));

                    CreatePcSchedule(context, schedule.StartDate, schedule.EndDate);

                    string error = string.Empty;

                    error = MondayCheck(context, schedule.StartDate, schedule.EndDate, error, model);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.MondayError = error;
                        return View();
                    }

                    error = ThuesdayCheck(context, schedule.StartDate, schedule.EndDate, error, model);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.ThuesdayError = error;
                        return View();
                    }

                    error = WednesdayCheck(context, schedule.StartDate, schedule.EndDate, error, model);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.WednesdayError = error;
                        return View();
                    }

                    error = ThursdayCheck(context, schedule.StartDate, schedule.EndDate, error, model);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.ThursdayError = error;
                        return View();
                    }

                    error = FridayCheck(context, schedule.StartDate, schedule.EndDate, error, model);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.FridayError = error;
                        return View();
                    }

                    error = SaturdayCheck(context, schedule.StartDate, schedule.EndDate, error, model);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.SaturdayError = error;
                        return View();
                    }

                    error = SundayCheck(context, schedule.StartDate, schedule.EndDate, error, model);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.SundayError = error;
                        return View();
                    }

                    SetScheduleProperties(context, schedule.StartDate, schedule.EndDate, model, schedule);

                    context.Entry(schedule).State = EntityState.Modified;
                    context.SaveChanges();

                    return RedirectToAction("ListMySchedules");
                }
            }

            return View(model);
        }

        private string MondayCheck(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, string error, ScheduleView model)
        {
            if (model.MondayStart != null || model.MondayEnd != null)
            {
                bool isValid = false;
                double mondayStart = double.Parse(model.MondayStart);
                double mondayEnd = double.Parse(model.MondayEnd);
                if (mondayEnd - mondayStart <= 0 || mondayEnd - mondayStart < 4)
                {
                    error = "InvalidShift";
                }
                int found = 0;
                var hours = mondayEnd - mondayStart;
                if (hours == 9)
                {
                    if (mondayStart != 9 && mondayEnd != 18)
                    {
                        error = "Invalid Shift!";
                    }
                    isValid = true;
                    var computers = db.Computers.ToList();
                    foreach (var c in computers)
                    {
                        var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                        if (schedule.MondayShiftOneStart == null && schedule.MondayShiftOneEnd == null && schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null && schedule.MondayShiftThreeStart == null && schedule.MondayShiftThreeEnd == null)
                        {
                            schedule.MondayShiftOneStart = mondayStart;
                            schedule.MondayShiftOneEnd = 13;
                            schedule.MondayShiftTwoStart = 13;
                            schedule.MondayShiftTwoEnd = 17;
                            schedule.MondayShiftThreeStart = 17;
                            schedule.MondayShiftThreeEnd = mondayEnd;
                            found = 1;
                            break;
                        }
                    }
                    if (found == 0)
                    {
                        error = "No free places!";
                    }

                }
                if (hours == 8)
                {

                    if (mondayStart == 9 && mondayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.MondayShiftOneStart == null && schedule.MondayShiftOneEnd == null && schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null)
                            {
                                schedule.MondayShiftOneStart = mondayStart;
                                schedule.MondayShiftOneEnd = 13;
                                schedule.MondayShiftTwoStart = 13;
                                schedule.MondayShiftTwoEnd = mondayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (mondayStart == 13 && mondayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null && schedule.MondayShiftThreeStart == null && schedule.MondayShiftThreeEnd == null)
                            {

                                schedule.MondayShiftTwoStart = mondayStart;
                                schedule.MondayShiftTwoEnd = 17;
                                schedule.MondayShiftThreeStart = 17;
                                schedule.MondayShiftThreeEnd = mondayEnd;
                                found = 1;
                                break;
                            }

                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                }
                if (hours == 4)
                {
                    if (mondayStart == 9 && mondayEnd == 13)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.MondayShiftOneStart == null && schedule.MondayShiftOneEnd == null)
                            {

                                schedule.MondayShiftOneStart = mondayStart;
                                schedule.MondayShiftOneEnd = mondayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (mondayStart == 13 && mondayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null)
                            {

                                schedule.MondayShiftTwoStart = mondayStart;
                                schedule.MondayShiftTwoEnd = mondayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (mondayStart == 17 && mondayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.MondayShiftThreeStart == null && schedule.MondayShiftThreeEnd == null)
                            {

                                schedule.MondayShiftThreeStart = mondayStart;
                                schedule.MondayShiftThreeEnd = mondayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }

                }
                else
                {
                    if (!isValid)
                    {
                        error = "Invalid shift!";
                    }
                }

            }
            return error;
        }

        private string ThuesdayCheck(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, string error, ScheduleView model)
        {
            if (model.ThuesdayStart != null && model.ThuesdayEnd != null)
            {
                bool isValid = false;
                double thuesdayStart = double.Parse(model.ThuesdayStart);
                double thuesdayEnd = double.Parse(model.ThuesdayEnd);
                if (thuesdayEnd - thuesdayStart <= 0 || thuesdayEnd - thuesdayStart < 4)
                {
                    error = "InvalidShift";
                }
                int found = 0;
                var hours = thuesdayEnd - thuesdayStart;
                if (hours == 9)
                {
                    if (thuesdayStart != 9 && thuesdayEnd != 18)
                    {
                        error = "Invalid Shift!";
                    }
                    isValid = true;
                    var computers = db.Computers.ToList();
                    foreach (var c in computers)
                    {
                        var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                        if (schedule.ThuesdayShiftOneStart == null && schedule.ThuesdayShiftOneEnd == null && schedule.ThuesdayShiftTwoStart == null && schedule.ThuesdayShiftTwoEnd == null && schedule.ThuesdayShiftThreeStart == null && schedule.ThuesdayShiftThreeEnd == null)
                        {
                            schedule.ThuesdayShiftOneStart = thuesdayStart;
                            schedule.ThuesdayShiftOneEnd = 13;
                            schedule.ThuesdayShiftTwoStart = 13;
                            schedule.ThuesdayShiftTwoEnd = 17;
                            schedule.ThuesdayShiftThreeStart = 17;
                            schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                            found = 1;
                            break;
                        }
                    }
                    if (found == 0)
                    {
                        error = "No free places!";
                    }

                }
                if (hours == 8)
                {

                    if (thuesdayStart == 9 && thuesdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThuesdayShiftOneStart == null && schedule.ThuesdayShiftOneEnd == null && schedule.ThuesdayShiftTwoStart == null && schedule.ThuesdayShiftTwoEnd == null)
                            {
                                schedule.ThuesdayShiftOneStart = thuesdayStart;
                                schedule.ThuesdayShiftOneEnd = 13;
                                schedule.ThuesdayShiftTwoStart = 13;
                                schedule.ThuesdayShiftTwoEnd = thuesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (thuesdayStart == 13 && thuesdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThuesdayShiftTwoStart == null && schedule.ThuesdayShiftTwoEnd == null && schedule.ThuesdayShiftThreeStart == null && schedule.ThuesdayShiftThreeEnd == null)
                            {

                                schedule.ThuesdayShiftTwoStart = thuesdayStart;
                                schedule.ThuesdayShiftTwoEnd = 17;
                                schedule.ThuesdayShiftThreeStart = 17;
                                schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                                found = 1;
                                break;
                            }

                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                }
                if (hours == 4)
                {

                    if (thuesdayStart == 9 && thuesdayEnd == 13)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThuesdayShiftOneStart == null && schedule.ThuesdayShiftOneEnd == null)
                            {

                                schedule.ThuesdayShiftOneStart = thuesdayStart;
                                schedule.ThuesdayShiftOneEnd = thuesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (thuesdayStart == 13 && thuesdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThuesdayShiftTwoStart == null && schedule.ThuesdayShiftTwoEnd == null)
                            {

                                schedule.ThuesdayShiftTwoStart = thuesdayStart;
                                schedule.ThuesdayShiftTwoEnd = thuesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (thuesdayStart == 17 && thuesdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThuesdayShiftThreeStart == null && schedule.ThuesdayShiftThreeEnd == null)
                            {

                                schedule.ThuesdayShiftThreeStart = thuesdayStart;
                                schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }

                }
                else
                {
                    if (!isValid)
                    {
                        error = "Invalid shift!";
                    }
                }

            }
            return error;
        }

        private string WednesdayCheck(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, string error, ScheduleView model)
        {
            if (model.WednesdayStart != null && model.WednesdayEnd != null)
            {
                bool isValid = false;
                double wednesdayStart = double.Parse(model.WednesdayStart);
                double wednesdayEnd = double.Parse(model.WednesdayEnd);
                if (wednesdayEnd - wednesdayStart <= 0 || wednesdayEnd - wednesdayStart < 4)
                {
                    error = "InvalidShift";
                }
                int found = 0;
                var hours = wednesdayEnd - wednesdayStart;
                if (hours == 9)
                {
                    if (wednesdayStart != 9 && wednesdayEnd != 18)
                    {
                        error = "Invalid Shift!";
                    }
                    isValid = true;
                    var computers = db.Computers.ToList();
                    foreach (var c in computers)
                    {
                        var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                        if (schedule.WednesdayShiftOneStart == null && schedule.WednesdayShiftOneEnd == null && schedule.WednesdayShiftTwoStart == null && schedule.WednesdayShiftTwoEnd == null && schedule.WednesdayShiftThreeStart == null && schedule.WednesdayShiftThreeEnd == null)
                        {
                            schedule.WednesdayShiftOneStart = wednesdayStart;
                            schedule.WednesdayShiftOneEnd = 13;
                            schedule.WednesdayShiftTwoStart = 13;
                            schedule.WednesdayShiftTwoEnd = 17;
                            schedule.WednesdayShiftThreeStart = 17;
                            schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                            found = 1;
                            break;
                        }
                    }
                    if (found == 0)
                    {
                        error = "No free places!";
                    }

                }
                if (hours == 8)
                {

                    if (wednesdayStart == 9 && wednesdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.WednesdayShiftOneStart == null && schedule.WednesdayShiftOneEnd == null && schedule.WednesdayShiftTwoStart == null && schedule.WednesdayShiftTwoEnd == null)
                            {
                                schedule.WednesdayShiftOneStart = wednesdayStart;
                                schedule.WednesdayShiftOneEnd = 13;
                                schedule.WednesdayShiftTwoStart = 13;
                                schedule.WednesdayShiftTwoEnd = wednesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (wednesdayStart == 13 && wednesdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.WednesdayShiftTwoStart == null && schedule.WednesdayShiftTwoEnd == null && schedule.WednesdayShiftThreeStart == null && schedule.WednesdayShiftThreeEnd == null)
                            {

                                schedule.WednesdayShiftTwoStart = wednesdayStart;
                                schedule.WednesdayShiftTwoEnd = 17;
                                schedule.WednesdayShiftThreeStart = 17;
                                schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                                found = 1;
                                break;
                            }

                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                }
                if (hours == 4)
                {

                    if (wednesdayStart == 9 && wednesdayEnd == 13)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.WednesdayShiftOneStart == null && schedule.WednesdayShiftOneEnd == null)
                            {

                                schedule.WednesdayShiftOneStart = wednesdayStart;
                                schedule.WednesdayShiftOneEnd = wednesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (wednesdayStart == 13 && wednesdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.WednesdayShiftTwoStart == null && schedule.WednesdayShiftTwoEnd == null)
                            {

                                schedule.WednesdayShiftTwoStart = wednesdayStart;
                                schedule.WednesdayShiftTwoEnd = wednesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (wednesdayStart == 17 && wednesdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.WednesdayShiftThreeStart == null && schedule.WednesdayShiftThreeEnd == null)
                            {

                                schedule.WednesdayShiftThreeStart = wednesdayStart;
                                schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }

                }
                else
                {
                    if (!isValid)
                    {
                        error = "Invalid shift!";
                    }
                }

            }
            return error;
        }

        private string ThursdayCheck(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, string error, ScheduleView model)
        {
            if (model.ThursdayStart != null && model.ThursdayStart != null)
            {
                bool isValid = false;
                double thursdayStart = double.Parse(model.ThursdayStart);
                double thursdayEnd = double.Parse(model.ThursdayEnd);
                if (thursdayEnd - thursdayStart <= 0 || thursdayEnd - thursdayStart < 4)
                {
                    error = "InvalidShift";
                }
                int found = 0;
                var hours = thursdayEnd - thursdayStart;
                if (hours == 9)
                {
                    if (thursdayStart != 9 && thursdayEnd != 18)
                    {
                        error = "Invalid Shift!";
                    }
                    isValid = true;
                    var computers = db.Computers.ToList();
                    foreach (var c in computers)
                    {
                        var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                        if (schedule.ThursdayShiftOneStart == null && schedule.ThursdayShiftOneEnd == null && schedule.ThursdayShiftTwoStart == null && schedule.ThursdayShiftTwoEnd == null && schedule.ThursdayShiftThreeStart == null && schedule.ThursdayShiftThreeEnd == null)
                        {
                            schedule.ThursdayShiftOneStart = thursdayStart;
                            schedule.ThursdayShiftOneEnd = 13;
                            schedule.ThursdayShiftTwoStart = 13;
                            schedule.ThursdayShiftTwoEnd = 17;
                            schedule.ThursdayShiftThreeStart = 17;
                            schedule.ThursdayShiftThreeEnd = thursdayEnd;
                            found = 1;
                            break;
                        }
                    }
                    if (found == 0)
                    {
                        error = "No free places!";
                    }

                }
                if (hours == 8)
                {

                    if (thursdayStart == 9 && thursdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThursdayShiftOneStart == null && schedule.ThursdayShiftOneEnd == null && schedule.ThursdayShiftTwoStart == null && schedule.ThursdayShiftTwoEnd == null)
                            {
                                schedule.ThursdayShiftOneStart = thursdayStart;
                                schedule.ThursdayShiftOneEnd = 13;
                                schedule.ThursdayShiftTwoStart = 13;
                                schedule.ThursdayShiftTwoEnd = thursdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (thursdayStart == 13 && thursdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThursdayShiftTwoStart == null && schedule.ThursdayShiftTwoEnd == null && schedule.ThursdayShiftThreeStart == null && schedule.ThursdayShiftThreeEnd == null)
                            {

                                schedule.ThursdayShiftTwoStart = thursdayStart;
                                schedule.ThursdayShiftTwoEnd = 17;
                                schedule.ThursdayShiftThreeStart = 17;
                                schedule.ThursdayShiftThreeEnd = thursdayEnd;
                                found = 1;
                                break;
                            }

                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                }
                if (hours == 4)
                {

                    if (thursdayStart == 9 && thursdayEnd == 13)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThursdayShiftOneStart == null && schedule.ThursdayShiftOneEnd == null)
                            {

                                schedule.ThursdayShiftOneStart = thursdayStart;
                                schedule.ThursdayShiftOneEnd = thursdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (thursdayStart == 13 && thursdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThursdayShiftTwoStart == null && schedule.ThursdayShiftTwoEnd == null)
                            {

                                schedule.ThursdayShiftTwoStart = thursdayStart;
                                schedule.ThursdayShiftTwoEnd = thursdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (thursdayStart == 17 && thursdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.ThursdayShiftThreeStart == null && schedule.ThursdayShiftThreeEnd == null)
                            {

                                schedule.ThursdayShiftThreeStart = thursdayStart;
                                schedule.ThursdayShiftThreeEnd = thursdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }

                }
                else
                {
                    if (!isValid)
                    {
                        error = "Invalid shift!";
                    }
                }

            }
            return error;
        }

        private string FridayCheck(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, string error, ScheduleView model)
        {
            if (model.FridayStart != null && model.FridayEnd != null)
            {
                bool isValid = false;
                double fridayStart = double.Parse(model.FridayStart);
                double fridayEnd = double.Parse(model.FridayEnd);
                if (fridayEnd - fridayStart <= 0 || fridayEnd - fridayStart < 4)
                {
                    error = "InvalidShift";
                }
                int found = 0;
                var hours = fridayEnd - fridayStart;
                if (hours == 9)
                {
                    if (fridayStart != 9 && fridayEnd != 18)
                    {
                        error = "Invalid Shift!";
                    }
                    isValid = true;
                    var computers = db.Computers.ToList();
                    foreach (var c in computers)
                    {
                        var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                        if (schedule.FridayShiftOneStart == null && schedule.FridayShiftOneEnd == null && schedule.FridayShiftTwoStart == null && schedule.FridayShiftTwoEnd == null && schedule.FridayShiftThreeStart == null && schedule.FridayShiftThreeEnd == null)
                        {
                            schedule.FridayShiftOneStart = fridayStart;
                            schedule.FridayShiftOneEnd = 13;
                            schedule.FridayShiftTwoStart = 13;
                            schedule.FridayShiftTwoEnd = 17;
                            schedule.FridayShiftThreeStart = 17;
                            schedule.FridayShiftThreeEnd = fridayEnd;
                            found = 1;
                            break;
                        }
                    }
                    if (found == 0)
                    {
                        error = "No free places!";
                    }

                }
                if (hours == 8)
                {

                    if (fridayStart == 9 && fridayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.FridayShiftOneStart == null && schedule.FridayShiftOneEnd == null && schedule.FridayShiftTwoStart == null && schedule.FridayShiftTwoEnd == null)
                            {
                                schedule.FridayShiftOneStart = fridayStart;
                                schedule.FridayShiftOneEnd = 13;
                                schedule.FridayShiftTwoStart = 13;
                                schedule.FridayShiftTwoEnd = fridayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (fridayStart == 13 && fridayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.FridayShiftTwoStart == null && schedule.FridayShiftTwoEnd == null && schedule.FridayShiftThreeStart == null && schedule.FridayShiftThreeEnd == null)
                            {

                                schedule.FridayShiftTwoStart = fridayStart;
                                schedule.FridayShiftTwoEnd = 17;
                                schedule.FridayShiftThreeStart = 17;
                                schedule.FridayShiftThreeEnd = fridayEnd;
                                found = 1;
                                break;
                            }

                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                }
                if (hours == 4)
                {

                    if (fridayStart == 9 && fridayEnd == 13)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.FridayShiftOneStart == null && schedule.FridayShiftOneEnd == null)
                            {

                                schedule.FridayShiftOneStart = fridayStart;
                                schedule.FridayShiftOneEnd = fridayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (fridayStart == 13 && fridayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.FridayShiftTwoStart == null && schedule.FridayShiftTwoEnd == null)
                            {

                                schedule.FridayShiftTwoStart = fridayStart;
                                schedule.FridayShiftTwoEnd = fridayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (fridayStart == 17 && fridayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.FridayShiftThreeStart == null && schedule.FridayShiftThreeEnd == null)
                            {

                                schedule.FridayShiftThreeStart = fridayStart;
                                schedule.FridayShiftThreeEnd = fridayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }

                }
                else
                {
                    if (!isValid)
                    {
                        error = "Invalid shift!";
                    }
                }

            }
            return error;
        }

        private string SaturdayCheck(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, string error, ScheduleView model)
        {
            if (model.SaturdayStart != null && model.SaturdayEnd != null)
            {
                bool isValid = false;
                double saturdayStart = double.Parse(model.SaturdayStart);
                double saturdayEnd = double.Parse(model.SaturdayEnd);
                if (saturdayEnd - saturdayStart <= 0 || saturdayEnd - saturdayStart < 4)
                {
                    error = "InvalidShift";
                }
                int found = 0;
                var hours = saturdayEnd - saturdayStart;
                if (hours == 9)
                {
                    if (saturdayStart != 9 && saturdayEnd != 18)
                    {
                        error = "Invalid Shift!";
                    }
                    isValid = true;
                    var computers = db.Computers.ToList();
                    foreach (var c in computers)
                    {
                        var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                        if (schedule.SaturdayShiftOneStart == null && schedule.SaturdayShiftOneEnd == null && schedule.SaturdayShiftTwoStart == null && schedule.SaturdayShiftTwoEnd == null && schedule.SaturdayShiftThreeStart == null && schedule.SaturdayShiftThreeEnd == null)
                        {
                            schedule.SaturdayShiftOneStart = saturdayStart;
                            schedule.SaturdayShiftOneEnd = 13;
                            schedule.SaturdayShiftTwoStart = 13;
                            schedule.SaturdayShiftTwoEnd = 17;
                            schedule.SaturdayShiftThreeStart = 17;
                            schedule.SaturdayShiftThreeEnd = saturdayEnd;
                            found = 1;
                            break;
                        }
                    }
                    if (found == 0)
                    {
                        error = "No free places!";
                    }

                }
                if (hours == 8)
                {

                    if (saturdayStart == 9 && saturdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SaturdayShiftOneStart == null && schedule.SaturdayShiftOneEnd == null && schedule.SaturdayShiftTwoStart == null && schedule.SaturdayShiftTwoEnd == null)
                            {
                                schedule.SaturdayShiftOneStart = saturdayStart;
                                schedule.SaturdayShiftOneEnd = 13;
                                schedule.SaturdayShiftTwoStart = 13;
                                schedule.SaturdayShiftTwoEnd = saturdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (saturdayStart == 13 && saturdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SaturdayShiftTwoStart == null && schedule.SaturdayShiftTwoEnd == null && schedule.SaturdayShiftThreeStart == null && schedule.SaturdayShiftThreeEnd == null)
                            {

                                schedule.SaturdayShiftTwoStart = saturdayStart;
                                schedule.SaturdayShiftTwoEnd = 17;
                                schedule.SaturdayShiftThreeStart = 17;
                                schedule.SaturdayShiftThreeEnd = saturdayEnd;
                                found = 1;
                                break;
                            }

                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                }
                if (hours == 4)
                {

                    if (saturdayStart == 9 && saturdayEnd == 13)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SaturdayShiftOneStart == null && schedule.SaturdayShiftOneEnd == null)
                            {

                                schedule.SaturdayShiftOneStart = saturdayStart;
                                schedule.SaturdayShiftOneEnd = saturdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (saturdayStart == 13 && saturdayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SaturdayShiftTwoStart == null && schedule.SaturdayShiftTwoEnd == null)
                            {

                                schedule.SaturdayShiftTwoStart = saturdayStart;
                                schedule.SaturdayShiftTwoEnd = saturdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (saturdayStart == 17 && saturdayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SaturdayShiftThreeStart == null && schedule.SaturdayShiftThreeEnd == null)
                            {

                                schedule.SaturdayShiftThreeStart = saturdayStart;
                                schedule.SaturdayShiftThreeEnd = saturdayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }

                }
                else
                {
                    if (!isValid)
                    {
                        error = "Invalid shift!";
                    }
                }

            }
            return error;
        }

        private string SundayCheck(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, string error, ScheduleView model)
        {
            if (model.SundayStart != null && model.SundayEnd != null)
            {
                bool isValid = false;
                double sundayStart = double.Parse(model.SundayStart);
                double sundayEnd = double.Parse(model.SundayEnd);
                if (sundayEnd - sundayStart <= 0 || sundayEnd - sundayStart < 4)
                {
                    error = "InvalidShift";
                }
                int found = 0;
                var hours = sundayEnd - sundayStart;
                if (hours == 9)
                {
                    if (sundayStart != 9 && sundayEnd != 18)
                    {
                        error = "Invalid Shift!";
                    }
                    isValid = true;
                    var computers = db.Computers.ToList();
                    foreach (var c in computers)
                    {
                        var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                        if (schedule.SundayShiftOneStart == null && schedule.SundayShiftOneEnd == null && schedule.SundayShiftTwoStart == null && schedule.SundayShiftTwoEnd == null && schedule.SundayShiftThreeStart == null && schedule.SundayShiftThreeEnd == null)
                        {
                            schedule.SundayShiftOneStart = sundayStart;
                            schedule.SundayShiftOneEnd = 13;
                            schedule.SundayShiftTwoStart = 13;
                            schedule.SundayShiftTwoEnd = 17;
                            schedule.SundayShiftThreeStart = 17;
                            schedule.SundayShiftThreeEnd = sundayEnd;
                            found = 1;
                            break;
                        }
                    }
                    if (found == 0)
                    {
                        error = "No free places!";
                    }

                }
                if (hours == 8)
                {

                    if (sundayStart == 9 && sundayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SundayShiftOneStart == null && schedule.SundayShiftOneEnd == null && schedule.SundayShiftTwoStart == null && schedule.SundayShiftTwoEnd == null)
                            {
                                schedule.SundayShiftOneStart = sundayStart;
                                schedule.SundayShiftOneEnd = 13;
                                schedule.SundayShiftTwoStart = 13;
                                schedule.SundayShiftTwoEnd = sundayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (sundayStart == 13 && sundayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SundayShiftTwoStart == null && schedule.SundayShiftTwoEnd == null && schedule.SundayShiftThreeStart == null && schedule.SundayShiftThreeEnd == null)
                            {

                                schedule.SundayShiftTwoStart = sundayStart;
                                schedule.SundayShiftTwoEnd = 17;
                                schedule.SundayShiftThreeStart = 17;
                                schedule.SundayShiftThreeEnd = sundayEnd;
                                found = 1;
                                break;
                            }

                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                }
                if (hours == 4)
                {

                    if (sundayStart == 9 && sundayEnd == 13)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SundayShiftOneStart == null && schedule.SundayShiftOneEnd == null)
                            {

                                schedule.SundayShiftOneStart = sundayStart;
                                schedule.SundayShiftOneEnd = sundayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (sundayStart == 13 && sundayEnd == 17)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SundayShiftTwoStart == null && schedule.SundayShiftTwoEnd == null)
                            {

                                schedule.SundayShiftTwoStart = sundayStart;
                                schedule.SundayShiftTwoEnd = sundayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }
                    if (sundayStart == 17 && sundayEnd == 21)
                    {
                        isValid = true;
                        var computers = db.Computers.ToList();
                        foreach (var c in computers)
                        {
                            var schedule = c.Schedules.Where(s => s.EndDate == nextSunday.Date && s.StartDate == nextMonday.Date).FirstOrDefault();
                            if (schedule.SundayShiftThreeStart == null && schedule.SundayShiftThreeEnd == null)
                            {

                                schedule.SundayShiftThreeStart = sundayStart;
                                schedule.SundayShiftThreeEnd = sundayEnd;
                                found = 1;
                                break;
                            }
                        }
                        if (found == 0)
                        {
                            error = "No free places!";
                        }
                    }

                }
                else
                {
                    if (!isValid)
                    {
                        error = "Invalid shift!";
                    }
                }

            }
            return error;
        }

        private static void CreatePcSchedule(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday)
        {
            var pcs = db.Computers.Where(c => c.IsWorking == true).ToList();
            foreach (var pc in pcs)
            {

                if (pc.Schedules.Count == 0)
                {
                    var schedule = new Schedule
                    {
                        StartDate = nextMonday.Date,
                        EndDate = nextSunday.Date
                    };
                    pc.Schedules.Add(schedule);
                    db.SaveChanges();
                }
                else
                {
                    foreach (var sch in pc.Schedules)
                    {
                        if (sch.StartDate != nextMonday.Date && sch.EndDate != nextSunday.Date)
                        {
                            var schedule = new Schedule
                            {
                                StartDate = nextMonday.Date,
                                EndDate = nextSunday.Date
                            };
                            pc.Schedules.Add(schedule);
                            db.SaveChanges();
                        }
                    }
                }
            }
        }

        private void AddingScheduleToEmployee(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, ScheduleView model, Employee employee)
        {
            double hours = 0;
            Schedule schedule = new Schedule
            {
                StartDate = nextMonday.Date,
                EndDate = nextSunday.Date
            };
            //Monday
            if (model.MondayStart != null && model.MondayEnd != null)
            {
                double mondayStart = double.Parse(model.MondayStart);
                double mondayEnd = double.Parse(model.MondayEnd);
                double mondayDiff = mondayEnd - mondayStart;
                if (mondayDiff == 9)
                {
                    schedule.MondayShiftOneStart = mondayStart;
                    schedule.MondayShiftOneEnd = 13;
                    schedule.MondayShiftTwoStart = 13;
                    schedule.MondayShiftTwoEnd = 17;
                    schedule.MondayShiftThreeStart = 17;
                    schedule.MondayShiftThreeEnd = mondayEnd;
                    hours += mondayDiff;
                }
                else if (mondayDiff == 8)
                {
                    if (mondayStart == 9 && mondayEnd == 17)
                    {
                        schedule.MondayShiftOneStart = mondayStart;
                        schedule.MondayShiftOneEnd = 13;
                        schedule.MondayShiftTwoStart = 13;
                        schedule.MondayShiftTwoEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                    else
                    {
                        schedule.MondayShiftTwoStart = mondayStart;
                        schedule.MondayShiftTwoEnd = 17;
                        schedule.MondayShiftThreeStart = 17;
                        schedule.MondayShiftThreeEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                }
                else if (mondayDiff == 4)
                {
                    if (mondayStart == 9 && mondayEnd == 13)
                    {
                        schedule.MondayShiftOneStart = mondayStart;
                        schedule.MondayShiftOneEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                    else if (mondayStart == 13 && mondayEnd == 17)
                    {
                        schedule.MondayShiftTwoStart = mondayStart;
                        schedule.MondayShiftTwoEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                    else if (mondayStart == 17 && mondayEnd == 21)
                    {
                        schedule.MondayShiftThreeStart = mondayStart;
                        schedule.MondayShiftThreeEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                }
            }
            //Thuesday
            if (model.ThuesdayStart != null && model.ThuesdayEnd != null)
            {
                double thuesdayStart = double.Parse(model.ThuesdayStart);
                double thuesdayEnd = double.Parse(model.ThuesdayEnd);
                double thuesdayDiff = thuesdayEnd - thuesdayStart;
                if (thuesdayDiff == 9)
                {
                    schedule.ThuesdayShiftOneStart = thuesdayStart;
                    schedule.ThuesdayShiftOneEnd = 13;
                    schedule.ThuesdayShiftTwoStart = 13;
                    schedule.ThuesdayShiftTwoEnd = 17;
                    schedule.ThuesdayShiftThreeStart = 17;
                    schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                    hours += thuesdayDiff;
                }
                else if (thuesdayDiff == 8)
                {
                    if (thuesdayStart == 9 && thuesdayEnd == 17)
                    {
                        schedule.ThuesdayShiftOneStart = thuesdayStart;
                        schedule.ThuesdayShiftOneEnd = 13;
                        schedule.ThuesdayShiftTwoStart = 13;
                        schedule.ThuesdayShiftTwoEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                    else
                    {
                        schedule.ThuesdayShiftTwoStart = thuesdayStart;
                        schedule.ThuesdayShiftTwoEnd = 17;
                        schedule.ThuesdayShiftThreeStart = 17;
                        schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                }
                else if (thuesdayDiff == 4)
                {
                    if (thuesdayStart == 9 && thuesdayEnd == 13)
                    {
                        schedule.ThuesdayShiftOneStart = thuesdayStart;
                        schedule.ThuesdayShiftOneEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                    else if (thuesdayStart == 13 && thuesdayEnd == 17)
                    {
                        schedule.ThuesdayShiftTwoStart = thuesdayStart;
                        schedule.ThuesdayShiftTwoEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                    else if (thuesdayStart == 17 && thuesdayEnd == 21)
                    {
                        schedule.ThuesdayShiftThreeStart = thuesdayStart;
                        schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                }
            }
            //Wednesday
            if (model.WednesdayStart != null && model.WednesdayEnd != null)
            {
                double wednesdayStart = double.Parse(model.WednesdayStart);
                double wednesdayEnd = double.Parse(model.WednesdayEnd);
                double wednesdayDiff = wednesdayEnd - wednesdayStart;
                if (wednesdayDiff == 9)
                {
                    schedule.WednesdayShiftOneStart = wednesdayStart;
                    schedule.WednesdayShiftOneEnd = 13;
                    schedule.WednesdayShiftTwoStart = 13;
                    schedule.WednesdayShiftTwoEnd = 17;
                    schedule.WednesdayShiftThreeStart = 17;
                    schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                    hours += wednesdayDiff;
                }
                else if (wednesdayDiff == 8)
                {
                    if (wednesdayStart == 9 && wednesdayEnd == 17)
                    {
                        schedule.WednesdayShiftOneStart = wednesdayStart;
                        schedule.WednesdayShiftOneEnd = 13;
                        schedule.WednesdayShiftTwoStart = 13;
                        schedule.WednesdayShiftTwoEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                    else
                    {
                        schedule.WednesdayShiftTwoStart = wednesdayStart;
                        schedule.WednesdayShiftTwoEnd = 17;
                        schedule.WednesdayShiftThreeStart = 17;
                        schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                }
                else if (wednesdayDiff == 4)
                {
                    if (wednesdayStart == 9 && wednesdayEnd == 13)
                    {
                        schedule.WednesdayShiftOneStart = wednesdayStart;
                        schedule.WednesdayShiftOneEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                    else if (wednesdayStart == 13 && wednesdayEnd == 17)
                    {
                        schedule.WednesdayShiftTwoStart = wednesdayStart;
                        schedule.WednesdayShiftTwoEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                    else if (wednesdayStart == 17 && wednesdayEnd == 21)
                    {
                        schedule.WednesdayShiftThreeStart = wednesdayStart;
                        schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                }
            }
            //Thursday
            if (model.ThursdayEnd != null && model.ThursdayStart != null)
            {
                double thursdayStart = double.Parse(model.ThursdayStart);
                double thursdayEnd = double.Parse(model.ThursdayEnd);
                double thursdayDiff = thursdayEnd - thursdayStart;
                if (thursdayDiff == 9)
                {
                    schedule.ThursdayShiftOneStart = thursdayStart;
                    schedule.ThursdayShiftOneEnd = 13;
                    schedule.ThursdayShiftTwoStart = 13;
                    schedule.ThursdayShiftTwoEnd = 17;
                    schedule.ThursdayShiftThreeStart = 17;
                    schedule.ThursdayShiftThreeEnd = thursdayEnd;
                    hours += thursdayDiff;
                }
                else if (thursdayDiff == 8)
                {
                    if (thursdayStart == 9 && thursdayEnd == 17)
                    {
                        schedule.ThursdayShiftOneStart = thursdayStart;
                        schedule.ThursdayShiftOneEnd = 13;
                        schedule.ThursdayShiftTwoStart = 13;
                        schedule.ThursdayShiftTwoEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                    else
                    {
                        schedule.ThursdayShiftTwoStart = thursdayStart;
                        schedule.ThursdayShiftTwoEnd = 17;
                        schedule.ThursdayShiftThreeStart = 17;
                        schedule.ThursdayShiftThreeEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                }
                else if (thursdayDiff == 4)
                {
                    if (thursdayStart == 9 && thursdayEnd == 13)
                    {
                        schedule.ThursdayShiftOneStart = thursdayStart;
                        schedule.ThursdayShiftOneEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                    else if (thursdayStart == 13 && thursdayEnd == 17)
                    {
                        schedule.ThursdayShiftTwoStart = thursdayStart;
                        schedule.ThursdayShiftTwoEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                    else if (thursdayStart == 17 && thursdayEnd == 21)
                    {
                        schedule.ThursdayShiftThreeStart = thursdayStart;
                        schedule.ThursdayShiftThreeEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                }
            }
            //Friday
            if (model.FridayStart != null && model.FridayEnd != null)
            {
                double fridayStart = double.Parse(model.FridayStart);
                double fridayEnd = double.Parse(model.FridayEnd);
                double fridayDiff = fridayEnd - fridayStart;
                if (fridayDiff == 9)
                {
                    schedule.FridayShiftOneStart = fridayStart;
                    schedule.FridayShiftOneEnd = 13;
                    schedule.FridayShiftTwoStart = 13;
                    schedule.FridayShiftTwoEnd = 17;
                    schedule.FridayShiftThreeStart = 17;
                    schedule.FridayShiftThreeEnd = fridayEnd;
                    hours += fridayDiff;
                }
                else if (fridayDiff == 8)
                {
                    if (fridayStart == 9 && fridayEnd == 17)
                    {
                        schedule.FridayShiftOneStart = fridayStart;
                        schedule.FridayShiftOneEnd = 13;
                        schedule.FridayShiftTwoStart = 13;
                        schedule.FridayShiftTwoEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                    else
                    {
                        schedule.FridayShiftTwoStart = fridayStart;
                        schedule.FridayShiftTwoEnd = 17;
                        schedule.FridayShiftThreeStart = 17;
                        schedule.FridayShiftThreeEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                }
                else if (fridayDiff == 4)
                {
                    if (fridayStart == 9 && fridayEnd == 13)
                    {
                        schedule.FridayShiftOneStart = fridayStart;
                        schedule.FridayShiftOneEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                    else if (fridayStart == 13 && fridayEnd == 17)
                    {
                        schedule.FridayShiftTwoStart = fridayStart;
                        schedule.FridayShiftTwoEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                    else if (fridayStart == 17 && fridayEnd == 21)
                    {
                        schedule.FridayShiftThreeStart = fridayStart;
                        schedule.FridayShiftThreeEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                }
            }
            //Saturday
            if (model.SaturdayStart != null && model.SaturdayEnd != null)
            {
                double saturdayStart = double.Parse(model.SaturdayStart);
                double saturdayEnd = double.Parse(model.SaturdayEnd);
                double satudrdayDiff = saturdayEnd - saturdayStart;
                if (satudrdayDiff == 9)
                {
                    schedule.SaturdayShiftOneStart = saturdayStart;
                    schedule.SaturdayShiftOneEnd = 13;
                    schedule.SaturdayShiftTwoStart = 13;
                    schedule.SaturdayShiftTwoEnd = 17;
                    schedule.SaturdayShiftThreeStart = 17;
                    schedule.SaturdayShiftThreeEnd = saturdayEnd;
                    hours += satudrdayDiff;
                }
                else if (satudrdayDiff == 8)
                {
                    if (saturdayStart == 9 && saturdayEnd == 17)
                    {
                        schedule.SaturdayShiftOneStart = saturdayStart;
                        schedule.SaturdayShiftOneEnd = 13;
                        schedule.SaturdayShiftTwoStart = 13;
                        schedule.SaturdayShiftTwoEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                    else
                    {
                        schedule.SaturdayShiftTwoStart = saturdayStart;
                        schedule.SaturdayShiftTwoEnd = 17;
                        schedule.SaturdayShiftThreeStart = 17;
                        schedule.SaturdayShiftThreeEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                }
                else if (satudrdayDiff == 4)
                {
                    if (saturdayStart == 9 && saturdayEnd == 13)
                    {
                        schedule.SaturdayShiftOneStart = saturdayStart;
                        schedule.SaturdayShiftOneEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                    else if (saturdayStart == 13 && saturdayEnd == 17)
                    {
                        schedule.SaturdayShiftTwoStart = saturdayStart;
                        schedule.SaturdayShiftTwoEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                    else if (saturdayStart == 17 && saturdayEnd == 21)
                    {
                        schedule.SaturdayShiftThreeStart = saturdayStart;
                        schedule.SaturdayShiftThreeEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                }
            }
            //Sunday
            if (model.SundayStart != null && model.SundayEnd != null)
            {
                double sundayStart = double.Parse(model.SundayStart);
                double sundayEnd = double.Parse(model.SundayEnd);
                double sundayDiff = sundayEnd - sundayStart;
                if (sundayDiff == 9)
                {
                    schedule.SundayShiftOneStart = sundayStart;
                    schedule.SundayShiftOneEnd = 13;
                    schedule.SundayShiftTwoStart = 13;
                    schedule.SundayShiftTwoEnd = 17;
                    schedule.SundayShiftThreeStart = 17;
                    schedule.SundayShiftThreeEnd = sundayEnd;
                    hours += sundayDiff;
                }
                else if (sundayDiff == 8)
                {
                    if (sundayStart == 9 && sundayEnd == 17)
                    {
                        schedule.SundayShiftOneStart = sundayStart;
                        schedule.SundayShiftOneEnd = 13;
                        schedule.SundayShiftTwoStart = 13;
                        schedule.SundayShiftTwoEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                    else
                    {
                        schedule.SundayShiftTwoStart = sundayStart;
                        schedule.SundayShiftTwoEnd = 17;
                        schedule.SundayShiftThreeStart = 17;
                        schedule.SundayShiftThreeEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                }
                else if (sundayDiff == 4)
                {
                    if (sundayStart == 9 && sundayEnd == 13)
                    {
                        schedule.SundayShiftOneStart = sundayStart;
                        schedule.SundayShiftOneEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                    else if (sundayStart == 13 && sundayEnd == 17)
                    {
                        schedule.SundayShiftTwoStart = sundayStart;
                        schedule.SundayShiftTwoEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                    else if (sundayStart == 17 && sundayEnd == 21)
                    {
                        schedule.SundayShiftThreeStart = sundayStart;
                        schedule.SundayShiftThreeEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                }
            }
            schedule.Hours = hours;
            employee.Schedules.Add(schedule);

        }

        private void SetScheduleProperties(TelesalesScheduleDbContext db, DateTime nextMonday, DateTime nextSunday, ScheduleView model, Schedule schedule)
        {
            double hours = 0;
            if (model.Hours != null)
            {
                hours = double.Parse(model.Hours);
            }

            //Monday
            if (model.MondayStart != null && model.MondayEnd != null)
            {
                double mondayStart = double.Parse(model.MondayStart);
                double mondayEnd = double.Parse(model.MondayEnd);
                double mondayDiff = mondayEnd - mondayStart;
                if (mondayDiff == 9)
                {
                    schedule.MondayShiftOneStart = mondayStart;
                    schedule.MondayShiftOneEnd = 13;
                    schedule.MondayShiftTwoStart = 13;
                    schedule.MondayShiftTwoEnd = 17;
                    schedule.MondayShiftThreeStart = 17;
                    schedule.MondayShiftThreeEnd = mondayEnd;
                    hours += mondayDiff;
                }
                else if (mondayDiff == 8)
                {
                    if (mondayStart == 9 && mondayEnd == 17)
                    {
                        schedule.MondayShiftOneStart = mondayStart;
                        schedule.MondayShiftOneEnd = 13;
                        schedule.MondayShiftTwoStart = 13;
                        schedule.MondayShiftTwoEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                    else
                    {
                        schedule.MondayShiftTwoStart = mondayStart;
                        schedule.MondayShiftTwoEnd = 17;
                        schedule.MondayShiftThreeStart = 17;
                        schedule.MondayShiftThreeEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                }
                else if (mondayDiff == 4)
                {
                    if (mondayStart == 9 && mondayEnd == 13)
                    {
                        schedule.MondayShiftOneStart = mondayStart;
                        schedule.MondayShiftOneEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                    else if (mondayStart == 13 && mondayEnd == 17)
                    {
                        schedule.MondayShiftTwoStart = mondayStart;
                        schedule.MondayShiftTwoEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                    else if (mondayStart == 17 && mondayEnd == 21)
                    {
                        schedule.MondayShiftThreeStart = mondayStart;
                        schedule.MondayShiftThreeEnd = mondayEnd;
                        hours += mondayDiff;
                    }
                }
            }
            //Thuesday
            if (model.ThuesdayStart != null && model.ThuesdayEnd != null)
            {
                double thuesdayStart = double.Parse(model.ThuesdayStart);
                double thuesdayEnd = double.Parse(model.ThuesdayEnd);
                double thuesdayDiff = thuesdayEnd - thuesdayStart;
                if (thuesdayDiff == 9)
                {
                    schedule.ThuesdayShiftOneStart = thuesdayStart;
                    schedule.ThuesdayShiftOneEnd = 13;
                    schedule.ThuesdayShiftTwoStart = 13;
                    schedule.ThuesdayShiftTwoEnd = 17;
                    schedule.ThuesdayShiftThreeStart = 17;
                    schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                    hours += thuesdayDiff;
                }
                else if (thuesdayDiff == 8)
                {
                    if (thuesdayStart == 9 && thuesdayEnd == 17)
                    {
                        schedule.ThuesdayShiftOneStart = thuesdayStart;
                        schedule.ThuesdayShiftOneEnd = 13;
                        schedule.ThuesdayShiftTwoStart = 13;
                        schedule.ThuesdayShiftTwoEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                    else
                    {
                        schedule.ThuesdayShiftTwoStart = thuesdayStart;
                        schedule.ThuesdayShiftTwoEnd = 17;
                        schedule.ThuesdayShiftThreeStart = 17;
                        schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                }
                else if (thuesdayDiff == 4)
                {
                    if (thuesdayStart == 9 && thuesdayEnd == 13)
                    {
                        schedule.ThuesdayShiftOneStart = thuesdayStart;
                        schedule.ThuesdayShiftOneEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                    else if (thuesdayStart == 13 && thuesdayEnd == 17)
                    {
                        schedule.ThuesdayShiftTwoStart = thuesdayStart;
                        schedule.ThuesdayShiftTwoEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                    else if (thuesdayStart == 17 && thuesdayEnd == 21)
                    {
                        schedule.ThuesdayShiftThreeStart = thuesdayStart;
                        schedule.ThuesdayShiftThreeEnd = thuesdayEnd;
                        hours += thuesdayDiff;
                    }
                }
            }
            //Wednesday
            if (model.WednesdayStart != null && model.WednesdayEnd != null)
            {
                double wednesdayStart = double.Parse(model.WednesdayStart);
                double wednesdayEnd = double.Parse(model.WednesdayEnd);
                double wednesdayDiff = wednesdayEnd - wednesdayStart;
                if (wednesdayDiff == 9)
                {
                    schedule.WednesdayShiftOneStart = wednesdayStart;
                    schedule.WednesdayShiftOneEnd = 13;
                    schedule.WednesdayShiftTwoStart = 13;
                    schedule.WednesdayShiftTwoEnd = 17;
                    schedule.WednesdayShiftThreeStart = 17;
                    schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                    hours += wednesdayDiff;
                }
                else if (wednesdayDiff == 8)
                {
                    if (wednesdayStart == 9 && wednesdayEnd == 17)
                    {
                        schedule.WednesdayShiftOneStart = wednesdayStart;
                        schedule.WednesdayShiftOneEnd = 13;
                        schedule.WednesdayShiftTwoStart = 13;
                        schedule.WednesdayShiftTwoEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                    else
                    {
                        schedule.WednesdayShiftTwoStart = wednesdayStart;
                        schedule.WednesdayShiftTwoEnd = 17;
                        schedule.WednesdayShiftThreeStart = 17;
                        schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                }
                else if (wednesdayDiff == 4)
                {
                    if (wednesdayStart == 9 && wednesdayEnd == 13)
                    {
                        schedule.WednesdayShiftOneStart = wednesdayStart;
                        schedule.WednesdayShiftOneEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                    else if (wednesdayStart == 13 && wednesdayEnd == 17)
                    {
                        schedule.WednesdayShiftTwoStart = wednesdayStart;
                        schedule.WednesdayShiftTwoEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                    else if (wednesdayStart == 17 && wednesdayEnd == 21)
                    {
                        schedule.WednesdayShiftThreeStart = wednesdayStart;
                        schedule.WednesdayShiftThreeEnd = wednesdayEnd;
                        hours += wednesdayDiff;
                    }
                }
            }
            //Thursday
            if (model.ThursdayEnd != null && model.ThursdayStart != null)
            {
                double thursdayStart = double.Parse(model.ThursdayStart);
                double thursdayEnd = double.Parse(model.ThursdayEnd);
                double thursdayDiff = thursdayEnd - thursdayStart;
                if (thursdayDiff == 9)
                {
                    schedule.ThursdayShiftOneStart = thursdayStart;
                    schedule.ThursdayShiftOneEnd = 13;
                    schedule.ThursdayShiftTwoStart = 13;
                    schedule.ThursdayShiftTwoEnd = 17;
                    schedule.ThursdayShiftThreeStart = 17;
                    schedule.ThursdayShiftThreeEnd = thursdayEnd;
                    hours += thursdayDiff;
                }
                else if (thursdayDiff == 8)
                {
                    if (thursdayStart == 9 && thursdayEnd == 17)
                    {
                        schedule.ThursdayShiftOneStart = thursdayStart;
                        schedule.ThursdayShiftOneEnd = 13;
                        schedule.ThursdayShiftTwoStart = 13;
                        schedule.ThursdayShiftTwoEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                    else
                    {
                        schedule.ThursdayShiftTwoStart = thursdayStart;
                        schedule.ThursdayShiftTwoEnd = 17;
                        schedule.ThursdayShiftThreeStart = 17;
                        schedule.ThursdayShiftThreeEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                }
                else if (thursdayDiff == 4)
                {
                    if (thursdayStart == 9 && thursdayEnd == 13)
                    {
                        schedule.ThursdayShiftOneStart = thursdayStart;
                        schedule.ThursdayShiftOneEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                    else if (thursdayStart == 13 && thursdayEnd == 17)
                    {
                        schedule.ThursdayShiftTwoStart = thursdayStart;
                        schedule.ThursdayShiftTwoEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                    else if (thursdayStart == 17 && thursdayEnd == 21)
                    {
                        schedule.ThursdayShiftThreeStart = thursdayStart;
                        schedule.ThursdayShiftThreeEnd = thursdayEnd;
                        hours += thursdayDiff;
                    }
                }
            }
            //Friday
            if (model.FridayStart != null && model.FridayEnd != null)
            {
                double fridayStart = double.Parse(model.FridayStart);
                double fridayEnd = double.Parse(model.FridayEnd);
                double fridayDiff = fridayEnd - fridayStart;
                if (fridayDiff == 9)
                {
                    schedule.FridayShiftOneStart = fridayStart;
                    schedule.FridayShiftOneEnd = 13;
                    schedule.FridayShiftTwoStart = 13;
                    schedule.FridayShiftTwoEnd = 17;
                    schedule.FridayShiftThreeStart = 17;
                    schedule.FridayShiftThreeEnd = fridayEnd;
                    hours += fridayDiff;
                }
                else if (fridayDiff == 8)
                {
                    if (fridayStart == 9 && fridayEnd == 17)
                    {
                        schedule.FridayShiftOneStart = fridayStart;
                        schedule.FridayShiftOneEnd = 13;
                        schedule.FridayShiftTwoStart = 13;
                        schedule.FridayShiftTwoEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                    else
                    {
                        schedule.FridayShiftTwoStart = fridayStart;
                        schedule.FridayShiftTwoEnd = 17;
                        schedule.FridayShiftThreeStart = 17;
                        schedule.FridayShiftThreeEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                }
                else if (fridayDiff == 4)
                {
                    if (fridayStart == 9 && fridayEnd == 13)
                    {
                        schedule.FridayShiftOneStart = fridayStart;
                        schedule.FridayShiftOneEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                    else if (fridayStart == 13 && fridayEnd == 17)
                    {
                        schedule.FridayShiftTwoStart = fridayStart;
                        schedule.FridayShiftTwoEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                    else if (fridayStart == 17 && fridayEnd == 21)
                    {
                        schedule.FridayShiftThreeStart = fridayStart;
                        schedule.FridayShiftThreeEnd = fridayEnd;
                        hours += fridayDiff;
                    }
                }
            }
            //Saturday
            if (model.SaturdayStart != null && model.SaturdayEnd != null)
            {
                double saturdayStart = double.Parse(model.SaturdayStart);
                double saturdayEnd = double.Parse(model.SaturdayEnd);
                double satudrdayDiff = saturdayEnd - saturdayStart;
                if (satudrdayDiff == 9)
                {
                    schedule.SaturdayShiftOneStart = saturdayStart;
                    schedule.SaturdayShiftOneEnd = 13;
                    schedule.SaturdayShiftTwoStart = 13;
                    schedule.SaturdayShiftTwoEnd = 17;
                    schedule.SaturdayShiftThreeStart = 17;
                    schedule.SaturdayShiftThreeEnd = saturdayEnd;
                    hours += satudrdayDiff;
                }
                else if (satudrdayDiff == 8)
                {
                    if (saturdayStart == 9 && saturdayEnd == 17)
                    {
                        schedule.SaturdayShiftOneStart = saturdayStart;
                        schedule.SaturdayShiftOneEnd = 13;
                        schedule.SaturdayShiftTwoStart = 13;
                        schedule.SaturdayShiftTwoEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                    else
                    {
                        schedule.SaturdayShiftTwoStart = saturdayStart;
                        schedule.SaturdayShiftTwoEnd = 17;
                        schedule.SaturdayShiftThreeStart = 17;
                        schedule.SaturdayShiftThreeEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                }
                else if (satudrdayDiff == 4)
                {
                    if (saturdayStart == 9 && saturdayEnd == 13)
                    {
                        schedule.SaturdayShiftOneStart = saturdayStart;
                        schedule.SaturdayShiftOneEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                    else if (saturdayStart == 13 && saturdayEnd == 17)
                    {
                        schedule.SaturdayShiftTwoStart = saturdayStart;
                        schedule.SaturdayShiftTwoEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                    else if (saturdayStart == 17 && saturdayEnd == 21)
                    {
                        schedule.SaturdayShiftThreeStart = saturdayStart;
                        schedule.SaturdayShiftThreeEnd = saturdayEnd;
                        hours += satudrdayDiff;
                    }
                }
            }
            //Sunday
            if (model.SundayStart != null && model.SundayEnd != null)
            {
                double sundayStart = double.Parse(model.SundayStart);
                double sundayEnd = double.Parse(model.SundayEnd);
                double sundayDiff = sundayEnd - sundayStart;
                if (sundayDiff == 9)
                {
                    schedule.SundayShiftOneStart = sundayStart;
                    schedule.SundayShiftOneEnd = 13;
                    schedule.SundayShiftTwoStart = 13;
                    schedule.SundayShiftTwoEnd = 17;
                    schedule.SundayShiftThreeStart = 17;
                    schedule.SundayShiftThreeEnd = sundayEnd;
                    hours += sundayDiff;
                }
                else if (sundayDiff == 8)
                {
                    if (sundayStart == 9 && sundayEnd == 17)
                    {
                        schedule.SundayShiftOneStart = sundayStart;
                        schedule.SundayShiftOneEnd = 13;
                        schedule.SundayShiftTwoStart = 13;
                        schedule.SundayShiftTwoEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                    else
                    {
                        schedule.SundayShiftTwoStart = sundayStart;
                        schedule.SundayShiftTwoEnd = 17;
                        schedule.SundayShiftThreeStart = 17;
                        schedule.SundayShiftThreeEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                }
                else if (sundayDiff == 4)
                {
                    if (sundayStart == 9 && sundayEnd == 13)
                    {
                        schedule.SundayShiftOneStart = sundayStart;
                        schedule.SundayShiftOneEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                    else if (sundayStart == 13 && sundayEnd == 17)
                    {
                        schedule.SundayShiftTwoStart = sundayStart;
                        schedule.SundayShiftTwoEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                    else if (sundayStart == 17 && sundayEnd == 21)
                    {
                        schedule.SundayShiftThreeStart = sundayStart;
                        schedule.SundayShiftThreeEnd = sundayEnd;
                        hours += sundayDiff;
                    }
                }
            }
            schedule.Hours = hours;
        }
    }
}
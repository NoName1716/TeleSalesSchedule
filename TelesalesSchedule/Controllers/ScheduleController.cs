using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
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

                    //chek if Schedule is exist
                    if (employee.Schedules.Select(s => s.StartDate).Contains(nextMonday) || employee.Schedules.Select(s => s.StartDate).Contains(nextSunday))
                    {
                        ViewBag.ErrorMessage = "Schedule already exist. If you want you can edit it!";
                        return View();
                    }
                    else
                    {
                        FindPlace(db, nextMonday, nextSunday, model);
                    }
                }

            }
            return RedirectToAction("List");
        }

        private void FindPlace(TelesalesScheduleDbContext db, DateTime monday, DateTime sunday, ScheduleView model)
        {
            var computers = db.Computers.ToList();

            for (int i = 1; i <= computers.Count; i++)
            {
                var c = db.Computers.Find(i);
                var schedule = c.Schedules.Where(s => s.StartDate == monday && s.EndDate == sunday);
                if (schedule.Count() == 0)
                {
                    var scheduleToAdd = new Schedule
                    {
                        StartDate = monday,
                        EndDate = sunday
                    };
                    c.Schedules.Add(scheduleToAdd);
                    db.SaveChanges();
                }
                else
                {
                    //

                }
            }
            //monday check
            if (model.MondayStart != null || model.MondayEnd != null)
            {
                double mondayStart = double.Parse(model.MondayStart);
                double mondayEnd = double.Parse(model.MondayEnd);
                for (int i = 1; i <= computers.Count; i++)
                {
                    var comp = db.Computers.Find(i);
                    var schedule = comp.Schedules.Where(s => s.StartDate == monday && s.EndDate == sunday).FirstOrDefault();
                    if (mondayStart < 13 && mondayEnd <=13)
                    {
                        if(schedule.MondayShiftOneStart == null && schedule.MondayShiftOneEnd == null)
                        {
                            schedule.MondayShiftOneStart = mondayStart;
                            schedule.MondayShiftOneEnd = mondayEnd;
                            db.SaveChanges();
                        }
                        else
                        {
                            break;
                        }
                       
                    }
                    if (mondayStart < 13 && mondayEnd <= 17)
                    {
                        if (schedule.MondayShiftOneStart == null && schedule.MondayShiftOneEnd == null && schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null)
                        {
                            schedule.MondayShiftOneStart = mondayStart;
                            schedule.MondayShiftOneEnd = 13;
                            schedule.MondayShiftTwoStart = 13;
                            schedule.MondayShiftTwoEnd = mondayEnd;
                            db.SaveChanges();
                        }
                        else
                        {
                            break;
                        }

                    }
                    if (mondayStart < 13 && mondayEnd <= 21 && mondayEnd - mondayStart > 8)
                    {
                        if (schedule.MondayShiftOneStart == null && schedule.MondayShiftOneEnd == null && schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null && schedule.MondayShiftThreeStart == null && schedule.MondayShiftThreeEnd == null)
                        {
                            schedule.MondayShiftOneStart = mondayStart;
                            schedule.MondayShiftOneEnd = 13;
                            schedule.MondayShiftTwoStart = 13;
                            schedule.MondayShiftTwoEnd = 17;
                            schedule.MondayShiftThreeStart = 17;
                            schedule.MondayShiftThreeEnd = mondayEnd;
                            db.SaveChanges();
                        }
                        else
                        {
                            if(schedule.MondayShiftOneStart == null && schedule.MondayShiftOneEnd == null && schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null && schedule.MondayShiftThreeStart >= mondayEnd)
                            {
                                schedule.MondayShiftOneStart = mondayStart;
                                schedule.MondayShiftOneEnd = 13;
                                schedule.MondayShiftTwoStart = 13;
                                schedule.MondayShiftTwoEnd = 17;
                                schedule.MondayShiftThreeStart = 17;
                                db.SaveChanges();
                            }
                        }

                    }
                    if (mondayStart >= 13 && mondayEnd <= 17)
                    {
                        if (schedule.MondayShiftTwoStart == null && schedule.MondayShiftTwoEnd == null)
                        {
                            schedule.MondayShiftTwoStart = mondayStart;
                            schedule.MondayShiftTwoEnd = mondayEnd;
                            db.SaveChanges();
                        }
                        else
                        {
                            if(schedule.MondayShiftTwoEnd <= mondayStart)
                            {
                                schedule.MondayShiftTwoEnd = mondayStart;
                                db.SaveChanges();
                            }
                        }

                    }
                    else
                    {
                        if (schedule.MondayShiftThreeStart == null && schedule.MondayShiftThreeEnd == null)
                        {
                            schedule.MondayShiftThreeStart = mondayStart;
                            schedule.MondayShiftThreeEnd = mondayEnd;
                            db.SaveChanges();
                        }
                        else
                        {
                            if(schedule.MondayShiftThreeEnd <= mondayStart)
                            {
                                schedule.MondayShiftThreeEnd = mondayEnd;
                                db.SaveChanges();
                            }
                        }
                    }

                }
            }

        }

        public ActionResult List()
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

                var model = new ScheduleViewModel();
                this.GetValues(schedule, model);

                return View(model);
            }
        }

        //
        // POST: Schedule/Edit
        [HttpPost]
        public ActionResult Edit(ScheduleViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    var schedule = context.Schedules
                        .FirstOrDefault(
                        s => s.Id == model.Id &&
                        s.StartDate == model.StartDate &&
                        s.EndDate == model.EndDate);

                    string shiftValue = Request.Form["shift"];
                    this.SetValues(shiftValue, schedule, model);

                    context.Entry(schedule).State = EntityState.Modified;
                    context.SaveChanges();

                    return RedirectToAction("List");
                }
            }

            return View(model);
        }

        private void GetValues(Schedule schedule, ScheduleViewModel model)
        {
            model.Id = schedule.Id;
            model.StartDate = schedule.StartDate;
            model.EndDate = schedule.EndDate;

            model.MondayShiftOneStart = schedule.MondayShiftOneStart;
            model.MondayShiftOneEnd = schedule.MondayShiftOneEnd;
            model.MondayShiftTwoStart = schedule.MondayShiftTwoStart;
            model.MondayShiftTwoEnd = schedule.MondayShiftTwoEnd;
            model.MondayShiftThreeStart = schedule.MondayShiftThreeStart;
            model.MondayShiftThreeEnd = schedule.MondayShiftThreeEnd;

            model.ThuesdayShiftOneStart = schedule.ThuesdayShiftOneStart;
            model.ThuesdayShiftOneEnd = schedule.ThuesdayShiftOneEnd;
            model.ThuesdayShiftTwoStart = schedule.ThuesdayShiftTwoStart;
            model.ThuesdayShiftTwoEnd = schedule.ThuesdayShiftTwoEnd;
            model.ThuesdayShiftThreeStart = schedule.ThuesdayShiftThreeStart;
            model.ThuesdayShiftThreeEnd = schedule.ThuesdayShiftThreeEnd;

            model.WednesdayShiftOneStart = schedule.WednesdayShiftOneStart;
            model.WednesdayShiftOneEnd = schedule.WednesdayShiftOneEnd;
            model.WednesdayShiftTwoStart = schedule.WednesdayShiftTwoStart;
            model.WednesdayShiftTwoEnd = schedule.WednesdayShiftTwoEnd;
            model.WednesdayShiftThreeStart = schedule.WednesdayShiftThreeStart;
            model.WednesdayShiftThreeEnd = schedule.WednesdayShiftThreeEnd;

            model.ThursdayShiftOneStart = schedule.ThursdayShiftOneStart;
            model.ThursdayShiftOneEnd = schedule.ThursdayShiftOneEnd;
            model.ThursdayShiftTwoStart = schedule.ThursdayShiftTwoStart;
            model.ThursdayShiftTwoEnd = schedule.ThursdayShiftTwoEnd;
            model.ThursdayShiftThreeStart = schedule.ThursdayShiftThreeStart;
            model.ThursdayShiftThreeEnd = schedule.ThursdayShiftThreeEnd;

            model.FridayShiftOneStart = schedule.FridayShiftOneStart;
            model.FridayShiftOneEnd = schedule.FridayShiftOneEnd;
            model.FridayShiftTwoStart = schedule.FridayShiftTwoStart;
            model.FridayShiftTwoEnd = schedule.FridayShiftTwoEnd;
            model.FridayShiftThreeStart = schedule.FridayShiftThreeStart;
            model.FridayShiftThreeEnd = schedule.FridayShiftThreeEnd;

            model.SaturdayShiftOneStart = schedule.SaturdayShiftOneStart;
            model.SaturdayShiftOneEnd = schedule.SaturdayShiftOneEnd;
            model.SaturdayShiftTwoStart = schedule.SaturdayShiftTwoStart;
            model.SaturdayShiftTwoEnd = schedule.SaturdayShiftTwoEnd;
            model.SaturdayShiftThreeStart = schedule.SaturdayShiftThreeStart;
            model.SaturdayShiftThreeEnd = schedule.SaturdayShiftThreeEnd;

            model.SundayShiftOneStart = schedule.SundayShiftOneStart;
            model.SundayShiftOneEnd = schedule.SundayShiftOneEnd;
            model.SundayShiftTwoStart = schedule.SundayShiftTwoStart;
            model.SundayShiftTwoEnd = schedule.SundayShiftTwoEnd;
            model.SundayShiftThreeStart = schedule.SundayShiftThreeStart;
            model.SundayShiftThreeEnd = schedule.SundayShiftThreeEnd;
        }

        private void SetValues(string shiftValue, Schedule schedule, ScheduleViewModel model)
        {
            if (shiftValue == "First")
            {
                if (model.MondayShiftOneStart != null)
                {
                    schedule.MondayShiftOneStart = model.MondayShiftOneStart;
                }
                if (model.MondayShiftOneEnd != null)
                {
                    schedule.MondayShiftOneEnd = model.MondayShiftOneEnd;
                }

                if (model.ThuesdayShiftOneStart != null)
                {
                    schedule.ThuesdayShiftOneStart = model.ThuesdayShiftOneStart;
                }
                if (model.ThuesdayShiftOneEnd != null)
                {
                    schedule.ThuesdayShiftOneEnd = model.ThuesdayShiftOneEnd;
                }

                if (model.WednesdayShiftOneStart != null)
                {
                    schedule.WednesdayShiftOneStart = model.WednesdayShiftOneStart;
                }
                if (model.WednesdayShiftOneEnd != null)
                {
                    schedule.WednesdayShiftOneEnd = model.WednesdayShiftOneEnd;
                }

                if (model.ThursdayShiftOneStart != null)
                {
                    schedule.ThursdayShiftOneStart = model.ThursdayShiftOneStart;
                }
                if (model.ThursdayShiftOneEnd != null)
                {
                    schedule.ThursdayShiftOneEnd = model.ThursdayShiftOneEnd;
                }

                if (model.FridayShiftOneStart != null)
                {
                    schedule.FridayShiftOneStart = model.FridayShiftOneStart;
                }
                if (model.FridayShiftOneEnd != null)
                {
                    schedule.FridayShiftOneEnd = model.FridayShiftOneEnd;
                }

                if (model.SaturdayShiftOneStart != null)
                {
                    schedule.SaturdayShiftOneStart = model.SaturdayShiftOneStart;
                }
                if (model.SaturdayShiftOneEnd != null)
                {
                    schedule.SaturdayShiftOneEnd = model.SaturdayShiftOneEnd;
                }

                if (model.SundayShiftOneStart != null)
                {
                    schedule.SundayShiftOneStart = model.SundayShiftOneStart;
                }
                if (model.SundayShiftOneEnd != null)
                {
                    schedule.SundayShiftOneEnd = model.SundayShiftOneEnd;
                }
            }
            else if (shiftValue == "Second")
            {
                if (model.MondayShiftTwoStart != null)
                {
                    schedule.MondayShiftTwoStart = model.MondayShiftTwoStart;
                }
                if (model.MondayShiftTwoEnd != null)
                {
                    schedule.MondayShiftTwoEnd = model.MondayShiftTwoEnd;
                }

                if (model.ThuesdayShiftTwoStart != null)
                {
                    schedule.ThuesdayShiftTwoStart = model.ThuesdayShiftTwoStart;
                }
                if (model.ThuesdayShiftTwoEnd != null)
                {
                    schedule.ThuesdayShiftTwoEnd = model.ThuesdayShiftTwoEnd;
                }

                if (model.WednesdayShiftTwoStart != null)
                {
                    schedule.WednesdayShiftTwoStart = model.WednesdayShiftTwoStart;
                }
                if (model.WednesdayShiftTwoEnd != null)
                {
                    schedule.WednesdayShiftTwoEnd = model.WednesdayShiftTwoEnd;
                }

                if (model.ThursdayShiftTwoStart != null)
                {
                    schedule.ThursdayShiftTwoStart = model.ThursdayShiftTwoStart;
                }
                if (model.ThursdayShiftTwoEnd != null)
                {
                    schedule.ThursdayShiftTwoEnd = model.ThursdayShiftTwoEnd;
                }

                if (model.FridayShiftTwoStart != null)
                {
                    schedule.FridayShiftTwoStart = model.FridayShiftTwoStart;
                }
                if (model.FridayShiftTwoEnd != null)
                {
                    schedule.FridayShiftTwoEnd = model.FridayShiftTwoEnd;
                }

                if (model.SaturdayShiftTwoStart != null)
                {
                    schedule.SaturdayShiftTwoStart = model.SaturdayShiftTwoStart;
                }
                if (model.SaturdayShiftTwoEnd != null)
                {
                    schedule.SaturdayShiftTwoEnd = model.SaturdayShiftTwoEnd;
                }

                if (model.SundayShiftTwoStart != null)
                {
                    schedule.SundayShiftTwoStart = model.SundayShiftTwoStart;
                }
                if (model.SundayShiftTwoEnd != null)
                {
                    schedule.SundayShiftTwoEnd = model.SundayShiftTwoEnd;
                }
            }
            else if (shiftValue == "Third")
            {
                if (model.MondayShiftThreeStart != null)
                {
                    schedule.MondayShiftThreeStart = model.MondayShiftThreeStart;
                }
                if (model.MondayShiftThreeEnd != null)
                {
                    schedule.MondayShiftThreeEnd = model.MondayShiftThreeEnd;
                }

                if (model.ThuesdayShiftThreeStart != null)
                {
                    schedule.ThuesdayShiftThreeStart = model.ThuesdayShiftThreeStart;
                }
                if (model.ThuesdayShiftThreeEnd != null)
                {
                    schedule.ThuesdayShiftThreeEnd = model.ThuesdayShiftThreeEnd;
                }

                if (model.WednesdayShiftThreeStart != null)
                {
                    schedule.WednesdayShiftThreeStart = model.WednesdayShiftThreeStart;
                }
                if (model.WednesdayShiftThreeEnd != null)
                {
                    schedule.WednesdayShiftThreeEnd = model.WednesdayShiftThreeEnd;
                }

                if (model.ThursdayShiftThreeStart != null)
                {
                    schedule.ThursdayShiftThreeStart = model.ThursdayShiftThreeStart;
                }
                if (model.ThursdayShiftThreeEnd != null)
                {
                    schedule.ThursdayShiftThreeEnd = model.ThursdayShiftThreeEnd;
                }

                if (model.FridayShiftThreeStart != null)
                {
                    schedule.FridayShiftThreeStart = model.FridayShiftThreeStart;
                }
                if (model.FridayShiftThreeEnd != null)
                {
                    schedule.FridayShiftThreeEnd = model.FridayShiftThreeEnd;
                }

                if (model.SaturdayShiftThreeStart != null)
                {
                    schedule.SaturdayShiftThreeStart = model.SaturdayShiftThreeStart;
                }
                if (model.SaturdayShiftThreeEnd != null)
                {
                    schedule.SaturdayShiftThreeEnd = model.SaturdayShiftThreeEnd;
                }

                if (model.SundayShiftThreeStart != null)
                {
                    schedule.SundayShiftThreeStart = model.SundayShiftThreeStart;
                }
                if (model.SundayShiftThreeEnd != null)
                {
                    schedule.SundayShiftThreeEnd = model.SundayShiftThreeEnd;
                }
            }
        }
    }
}
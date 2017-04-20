using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
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
        [Authorize]
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
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
                return RedirectToAction("ListMySchedules");
            }
        }

        [Authorize]
        public ActionResult ListMySchedules()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("ListAll");
            }
            using (var db = new TelesalesScheduleDbContext())
            {
                var emp = db.Employees.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                var schedules = emp.Schedules.ToList().OrderBy(s => s.StartDate).ToList();
                return View(schedules);
            }
        }
        [Authorize(Roles="Admin")]
        public ActionResult ListAll()
        {
            using (var db = new TelesalesScheduleDbContext())
            {
                var schedules = db.Schedules.Select(s => new DateModel
                {
                    StartDate = s.StartDate,
                    EndDate = s.EndDate
                }).Distinct().ToList();
                
                
                return View(schedules);
            }
        }
        [Authorize(Roles = "Admin")]
        public ActionResult EmployeesWithSchedules(DateModel model)
        {
            using (var db = new TelesalesScheduleDbContext())
            {
                //var schedules = db.Schedules.Where(s => s.StartDate == model.StartDate && s.EndDate == model.EndDate).Where(e => e.Employee.Schedules.Count > 0).ToList();

                var employees = db.Employees.ToList();
                var empSchedules = new List<EmployeeSchedule>();
                foreach (var emp in employees)
                {
                    //emp.Schedules.Where(s => s.StartDate == model.StartDate && s.EndDate == model.EndDate)
                    foreach (var schedule in emp.Schedules)
                    {
                        if(schedule.StartDate == model.StartDate && schedule.EndDate == model.EndDate)
                        {
                            var sch = new EmployeeSchedule
                            {
                                Id = schedule.Id,
                                FullName = emp.FullName,
                                Hours = schedule.Hours
                            };
                            empSchedules.Add(sch);
                        }
                    }
                }
               
                return View(empSchedules);
            }
            
        }

        [Authorize(Roles = "Admin")]
        public ActionResult EmployeesWithoutSchedule(DateModel model)
        {
            using (var db = new TelesalesScheduleDbContext())
            {
                var employees = db.Employees.ToList();
                var empSchedules = new List<EmployeeSchedule>();
                foreach (var e in employees)
                {
                    if(e.Schedules.Count == 0)
                    {
                        var sch = new EmployeeSchedule
                        {
                            FullName = e.FullName
                        };
                        empSchedules.Add(sch);
                    }
                    else
                    {
                        foreach (var schedule in e.Schedules)
                        {
                            if (schedule == null || schedule.StartDate != model.StartDate)
                            {
                                var sch = new EmployeeSchedule
                                {
                                    FullName = e.FullName
                                };
                                empSchedules.Add(sch);
                            }
                        }
                    }
                    
                }
                return View(empSchedules);
            }
        }
        [Authorize(Roles = "Admin")]
        public void ExportToExcel(DateModel model)
        {
            using (var db = new TelesalesScheduleDbContext())
            {

                var employees = db.Employees.ToList();
                var empSchedules = new List<EmployeeSchedule>();
                foreach (var emp in employees)
                {
                    
                    foreach (var schedule in emp.Schedules)
                    {
                        if (schedule.StartDate == model.StartDate && schedule.EndDate == model.EndDate)
                        {
                            var sch = new EmployeeSchedule
                            {
                                Id = schedule.Id,
                                FullName = emp.FullName,
                                Hours = schedule.Hours
                            };
                            empSchedules.Add(sch);
                        }
                    }
                }

                var grid = new GridView();
                grid.DataSource = from data in empSchedules.OrderBy(e => e.FullName)
                                  select new
                                  {
                                      FullName = data.FullName,
                                      Hours = data.Hours
                                  };
                grid.DataBind();
                Response.ClearContent();
                Response.AddHeader("content-disposition", "attachment; filename=Employees.xls");
                Response.ContentType = "application/ms-excel";
                Response.ContentEncoding = System.Text.Encoding.Unicode;
                Response.BinaryWrite(System.Text.Encoding.Unicode.GetPreamble());
                StringWriter writer = new StringWriter();
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(writer);
                grid.RenderControl(htmlTextWriter);
                Response.Write(writer.ToString());
                Response.End();
            }
        }

        //
        // GET: Schedule/Details
        [Authorize]
        public ActionResult Details(int? id)
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
                model.StartDate = schedule.StartDate.Day.ToString() + "." + schedule.StartDate.Month.ToString() + "." + schedule.StartDate.Year.ToString();
                model.EndDate = schedule.EndDate.Day.ToString() + "." + schedule.EndDate.Month.ToString() + "." + schedule.EndDate.Year.ToString();

                // Monday
                if (schedule.MondayShiftOneStart != null && schedule.MondayShiftThreeEnd != null)
                {
                    model.MondayStart = schedule.MondayShiftOneStart.ToString();
                    model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
                }
                else if (schedule.MondayShiftOneStart != null && schedule.MondayShiftTwoEnd != null)
                {
                    model.MondayStart = schedule.MondayShiftOneStart.ToString();
                    model.MondayEnd = schedule.MondayShiftTwoEnd.ToString();
                }
                else if (schedule.MondayShiftTwoStart != null && schedule.MondayShiftThreeEnd != null)
                {
                    model.MondayStart = schedule.MondayShiftTwoStart.ToString();
                    model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
                }
                else if (schedule.MondayShiftOneStart != null && schedule.MondayShiftOneEnd != null)
                {
                    model.MondayStart = schedule.MondayShiftOneStart.ToString();
                    model.MondayEnd = schedule.MondayShiftOneEnd.ToString();
                }
                else if (schedule.MondayShiftTwoStart != null && schedule.MondayShiftTwoEnd != null)
                {
                    model.MondayStart = schedule.MondayShiftTwoStart.ToString();
                    model.MondayEnd = schedule.MondayShiftTwoEnd.ToString();
                }
                else if (schedule.MondayShiftThreeStart != null && schedule.MondayShiftThreeEnd != null)
                {
                    model.MondayStart = schedule.MondayShiftThreeStart.ToString();
                    model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
                }

                // Thuesday
                if (schedule.ThuesdayShiftOneStart != null && schedule.ThuesdayShiftThreeEnd != null)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThuesdayShiftOneStart != null && schedule.ThuesdayShiftTwoEnd != null)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThuesdayShiftTwoStart != null && schedule.ThuesdayShiftThreeEnd != null)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftTwoStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThuesdayShiftOneStart != null && schedule.ThuesdayShiftOneEnd != null)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftOneEnd.ToString();
                }
                else if (schedule.ThuesdayShiftTwoStart != null && schedule.ThuesdayShiftTwoEnd != null)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftTwoStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThuesdayShiftThreeStart != null && schedule.ThuesdayShiftThreeEnd != null)
                {
                    model.ThuesdayStart = schedule.ThuesdayShiftThreeStart.ToString();
                    model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
                }

                // Wednesday
                if (schedule.WednesdayShiftOneStart != null && schedule.WednesdayShiftThreeEnd != null)
                {
                    model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
                }
                else if (schedule.WednesdayShiftOneStart != null && schedule.WednesdayShiftTwoEnd != null)
                {
                    model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftTwoEnd.ToString();
                }
                else if (schedule.WednesdayShiftTwoStart != null && schedule.WednesdayShiftThreeEnd != null)
                {
                    model.WednesdayStart = schedule.WednesdayShiftTwoStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
                }
                else if (schedule.WednesdayShiftOneStart != null && schedule.WednesdayShiftOneEnd != null)
                {
                    model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftOneEnd.ToString();
                }
                else if (schedule.WednesdayShiftTwoStart != null && schedule.WednesdayShiftTwoEnd != null)
                {
                    model.WednesdayStart = schedule.WednesdayShiftTwoStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftTwoEnd.ToString();
                }
                else if (schedule.WednesdayShiftThreeStart != null && schedule.WednesdayShiftThreeEnd != null)
                {
                    model.WednesdayStart = schedule.WednesdayShiftThreeStart.ToString();
                    model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
                }

                // Thursday
                if (schedule.ThursdayShiftOneStart != null && schedule.ThursdayShiftThreeEnd != null)
                {
                    model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThursdayShiftOneStart != null && schedule.ThursdayShiftTwoEnd != null)
                {
                    model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThursdayShiftTwoStart != null && schedule.ThursdayShiftThreeEnd != null)
                {
                    model.ThursdayStart = schedule.ThursdayShiftTwoStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
                }
                else if (schedule.ThursdayShiftOneStart != null && schedule.ThursdayShiftOneEnd != null)
                {
                    model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftOneEnd.ToString();
                }
                else if (schedule.ThursdayShiftTwoStart != null && schedule.ThursdayShiftTwoEnd != null)
                {
                    model.ThursdayStart = schedule.ThursdayShiftTwoStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftTwoEnd.ToString();
                }
                else if (schedule.ThursdayShiftThreeStart != null && schedule.ThursdayShiftThreeEnd != null)
                {
                    model.ThursdayStart = schedule.ThursdayShiftThreeStart.ToString();
                    model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
                }

                // Friday
                if (schedule.FridayShiftOneStart != null && schedule.FridayShiftThreeEnd != null)
                {
                    model.FridayStart = schedule.FridayShiftOneStart.ToString();
                    model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
                }
                else if (schedule.FridayShiftOneStart != null && schedule.FridayShiftTwoEnd != null)
                {
                    model.FridayStart = schedule.FridayShiftOneStart.ToString();
                    model.FridayEnd = schedule.FridayShiftTwoEnd.ToString();
                }
                else if (schedule.FridayShiftTwoStart != null && schedule.FridayShiftThreeEnd != null)
                {
                    model.FridayStart = schedule.FridayShiftTwoStart.ToString();
                    model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
                }
                else if (schedule.FridayShiftOneStart != null && schedule.FridayShiftOneEnd != null)
                {
                    model.FridayStart = schedule.FridayShiftOneStart.ToString();
                    model.FridayEnd = schedule.FridayShiftOneEnd.ToString();
                }
                else if (schedule.FridayShiftTwoStart != null && schedule.FridayShiftTwoEnd != null)
                {
                    model.FridayStart = schedule.FridayShiftTwoStart.ToString();
                    model.FridayEnd = schedule.FridayShiftTwoEnd.ToString();
                }
                else if (schedule.FridayShiftThreeStart != null && schedule.FridayShiftThreeEnd != null)
                {
                    model.FridayStart = schedule.FridayShiftThreeStart.ToString();
                    model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
                }

                // Saturday
                if (schedule.SaturdayShiftOneStart != null && schedule.SaturdayShiftThreeEnd != null)
                {
                    model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
                }
                else if (schedule.SaturdayShiftOneStart != null && schedule.SaturdayShiftTwoEnd != null)
                {
                    model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftTwoEnd.ToString();
                }
                else if (schedule.SaturdayShiftTwoStart != null && schedule.SaturdayShiftThreeEnd != null)
                {
                    model.SaturdayStart = schedule.SaturdayShiftTwoStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
                }
                else if (schedule.SaturdayShiftOneStart != null && schedule.SaturdayShiftOneEnd != null)
                {
                    model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftOneEnd.ToString();
                }
                else if (schedule.SaturdayShiftTwoStart != null && schedule.SaturdayShiftTwoEnd != null)
                {
                    model.SaturdayStart = schedule.SaturdayShiftTwoStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftTwoEnd.ToString();
                }
                else if (schedule.SaturdayShiftThreeStart != null && schedule.SaturdayShiftThreeEnd != null)
                {
                    model.SaturdayStart = schedule.SaturdayShiftThreeStart.ToString();
                    model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
                }

                // Sunday
                if (schedule.SundayShiftOneStart != null && schedule.SundayShiftThreeEnd != null)
                {
                    model.SundayStart = schedule.SundayShiftOneStart.ToString();
                    model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
                }
                else if (schedule.SundayShiftOneStart != null && schedule.SundayShiftTwoEnd != null)
                {
                    model.SundayStart = schedule.SundayShiftOneStart.ToString();
                    model.SundayEnd = schedule.SundayShiftTwoEnd.ToString();
                }
                else if (schedule.SundayShiftTwoStart != null && schedule.SundayShiftThreeEnd != null)
                {
                    model.SundayStart = schedule.SundayShiftTwoStart.ToString();
                    model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
                }
                else if (schedule.SundayShiftOneStart != null && schedule.SundayShiftOneEnd != null)
                {
                    model.SundayStart = schedule.SundayShiftOneStart.ToString();
                    model.SundayEnd = schedule.SundayShiftOneEnd.ToString();
                }
                else if (schedule.SundayShiftTwoStart != null && schedule.SundayShiftTwoEnd != null)
                {
                    model.SundayStart = schedule.SundayShiftTwoStart.ToString();
                    model.SundayEnd = schedule.SundayShiftTwoEnd.ToString();
                }
                else if (schedule.SundayShiftThreeStart != null && schedule.SundayShiftThreeEnd != null)
                {
                    model.SundayStart = schedule.SundayShiftThreeStart.ToString();
                    model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
                }

                return View(model);
            }
        }

        //
        // GET: Schedule/Edit
        [Authorize]
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
                ScheduleViewBuilder(schedule, model);

                return View(model);
            }
        }

        private static void ScheduleViewBuilder(Schedule schedule, ScheduleView model)
        {
            // Monday
            if (schedule.MondayShiftOneStart != null && schedule.MondayShiftThreeEnd != null)
            {
                model.MondayStart = schedule.MondayShiftOneStart.ToString();
                model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
            }
            else if (schedule.MondayShiftOneStart != null && schedule.MondayShiftTwoEnd != null)
            {
                model.MondayStart = schedule.MondayShiftOneStart.ToString();
                model.MondayEnd = schedule.MondayShiftTwoEnd.ToString();
            }
            else if (schedule.MondayShiftTwoStart != null && schedule.MondayShiftThreeEnd != null)
            {
                model.MondayStart = schedule.MondayShiftTwoStart.ToString();
                model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
            }
            else if (schedule.MondayShiftOneStart != null && schedule.MondayShiftOneEnd != null)
            {
                model.MondayStart = schedule.MondayShiftOneStart.ToString();
                model.MondayEnd = schedule.MondayShiftOneEnd.ToString();
            }
            else if (schedule.MondayShiftTwoStart != null && schedule.MondayShiftTwoEnd != null)
            {
                model.MondayStart = schedule.MondayShiftTwoStart.ToString();
                model.MondayEnd = schedule.MondayShiftTwoEnd.ToString();
            }
            else if (schedule.MondayShiftThreeStart != null && schedule.MondayShiftThreeEnd != null)
            {
                model.MondayStart = schedule.MondayShiftThreeStart.ToString();
                model.MondayEnd = schedule.MondayShiftThreeEnd.ToString();
            }
            else
            {
                model.MondayStart = string.Empty;
                model.MondayEnd = string.Empty;
            }


            // Thuesday
            if (schedule.ThuesdayShiftOneStart != null && schedule.ThuesdayShiftThreeEnd != null)
            {
                model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
            }
            else if (schedule.ThuesdayShiftOneStart != null && schedule.ThuesdayShiftTwoEnd != null)
            {
                model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                model.ThuesdayEnd = schedule.ThuesdayShiftTwoEnd.ToString();
            }
            else if (schedule.ThuesdayShiftTwoStart != null && schedule.ThuesdayShiftThreeEnd != null)
            {
                model.ThuesdayStart = schedule.ThuesdayShiftTwoStart.ToString();
                model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
            }
            else if (schedule.ThuesdayShiftOneStart != null && schedule.ThuesdayShiftOneEnd != null)
            {
                model.ThuesdayStart = schedule.ThuesdayShiftOneStart.ToString();
                model.ThuesdayEnd = schedule.ThuesdayShiftOneEnd.ToString();
            }
            else if (schedule.ThuesdayShiftTwoStart != null && schedule.ThuesdayShiftTwoEnd != null)
            {
                model.ThuesdayStart = schedule.ThuesdayShiftTwoStart.ToString();
                model.ThuesdayEnd = schedule.ThuesdayShiftTwoEnd.ToString();
            }
            else if (schedule.ThuesdayShiftThreeStart != null && schedule.ThuesdayShiftThreeEnd != null)
            {
                model.ThuesdayStart = schedule.ThuesdayShiftThreeStart.ToString();
                model.ThuesdayEnd = schedule.ThuesdayShiftThreeEnd.ToString();
            }
            else
            {
                model.ThuesdayStart = string.Empty;
                model.ThuesdayEnd = string.Empty;
            }

            // Wednesday
            if (schedule.WednesdayShiftOneStart != null && schedule.WednesdayShiftThreeEnd != null)
            {
                model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
            }
            else if (schedule.WednesdayShiftOneStart != null && schedule.WednesdayShiftTwoEnd != null)
            {
                model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                model.WednesdayEnd = schedule.WednesdayShiftTwoEnd.ToString();
            }
            else if (schedule.WednesdayShiftTwoStart != null && schedule.WednesdayShiftThreeEnd != null)
            {
                model.WednesdayStart = schedule.WednesdayShiftTwoStart.ToString();
                model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
            }
            else if (schedule.WednesdayShiftOneStart != null && schedule.WednesdayShiftOneEnd != null)
            {
                model.WednesdayStart = schedule.WednesdayShiftOneStart.ToString();
                model.WednesdayEnd = schedule.WednesdayShiftOneEnd.ToString();
            }
            else if (schedule.WednesdayShiftTwoStart != null && schedule.WednesdayShiftTwoEnd != null)
            {
                model.WednesdayStart = schedule.WednesdayShiftTwoStart.ToString();
                model.WednesdayEnd = schedule.WednesdayShiftTwoEnd.ToString();
            }
            else if (schedule.WednesdayShiftThreeStart != null && schedule.WednesdayShiftThreeEnd != null)
            {
                model.WednesdayStart = schedule.WednesdayShiftThreeStart.ToString();
                model.WednesdayEnd = schedule.WednesdayShiftThreeEnd.ToString();
            }
            else
            {
                model.WednesdayStart = string.Empty;
                model.WednesdayEnd = string.Empty;
            }

            // Thursday
            if (schedule.ThursdayShiftOneStart != null && schedule.ThursdayShiftThreeEnd != null)
            {
                model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
            }
            else if (schedule.ThursdayShiftOneStart != null && schedule.ThursdayShiftTwoEnd != null)
            {
                model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                model.ThursdayEnd = schedule.ThursdayShiftTwoEnd.ToString();
            }
            else if (schedule.ThursdayShiftTwoStart != null && schedule.ThursdayShiftThreeEnd != null)
            {
                model.ThursdayStart = schedule.ThursdayShiftTwoStart.ToString();
                model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
            }
            else if (schedule.ThursdayShiftOneStart != null && schedule.ThursdayShiftOneEnd != null)
            {
                model.ThursdayStart = schedule.ThursdayShiftOneStart.ToString();
                model.ThursdayEnd = schedule.ThursdayShiftOneEnd.ToString();
            }
            else if (schedule.ThursdayShiftTwoStart != null && schedule.ThursdayShiftTwoEnd != null)
            {
                model.ThursdayStart = schedule.ThursdayShiftTwoStart.ToString();
                model.ThursdayEnd = schedule.ThursdayShiftTwoEnd.ToString();
            }
            else if (schedule.ThursdayShiftThreeStart != null && schedule.ThursdayShiftThreeEnd != null)
            {
                model.ThursdayStart = schedule.ThursdayShiftThreeStart.ToString();
                model.ThursdayEnd = schedule.ThursdayShiftThreeEnd.ToString();
            }
            else
            {
                model.ThursdayStart = string.Empty;
                model.ThursdayEnd = string.Empty;
            }

            // Friday
            if (schedule.FridayShiftOneStart != null && schedule.FridayShiftThreeEnd != null)
            {
                model.FridayStart = schedule.FridayShiftOneStart.ToString();
                model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
            }
            else if (schedule.FridayShiftOneStart != null && schedule.FridayShiftTwoEnd != null)
            {
                model.FridayStart = schedule.FridayShiftOneStart.ToString();
                model.FridayEnd = schedule.FridayShiftTwoEnd.ToString();
            }
            else if (schedule.FridayShiftTwoStart != null && schedule.FridayShiftThreeEnd != null)
            {
                model.FridayStart = schedule.FridayShiftTwoStart.ToString();
                model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
            }
            else if (schedule.FridayShiftOneStart != null && schedule.FridayShiftOneEnd != null)
            {
                model.FridayStart = schedule.FridayShiftOneStart.ToString();
                model.FridayEnd = schedule.FridayShiftOneEnd.ToString();
            }
            else if (schedule.FridayShiftTwoStart != null && schedule.FridayShiftTwoEnd != null)
            {
                model.FridayStart = schedule.FridayShiftTwoStart.ToString();
                model.FridayEnd = schedule.FridayShiftTwoEnd.ToString();
            }
            else if (schedule.FridayShiftThreeStart != null && schedule.FridayShiftThreeEnd != null)
            {
                model.FridayStart = schedule.FridayShiftThreeStart.ToString();
                model.FridayEnd = schedule.FridayShiftThreeEnd.ToString();
            }
            else
            {
                model.FridayStart = string.Empty;
                model.FridayEnd = string.Empty;
            }

            // Saturday
            if (schedule.SaturdayShiftOneStart != null && schedule.SaturdayShiftThreeEnd != null)
            {
                model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
            }
            else if (schedule.SaturdayShiftOneStart != null && schedule.SaturdayShiftTwoEnd != null)
            {
                model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                model.SaturdayEnd = schedule.SaturdayShiftTwoEnd.ToString();
            }
            else if (schedule.SaturdayShiftTwoStart != null && schedule.SaturdayShiftThreeEnd != null)
            {
                model.SaturdayStart = schedule.SaturdayShiftTwoStart.ToString();
                model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
            }
            else if (schedule.SaturdayShiftOneStart != null && schedule.SaturdayShiftOneEnd != null)
            {
                model.SaturdayStart = schedule.SaturdayShiftOneStart.ToString();
                model.SaturdayEnd = schedule.SaturdayShiftOneEnd.ToString();
            }
            else if (schedule.SaturdayShiftTwoStart != null && schedule.SaturdayShiftTwoEnd != null)
            {
                model.SaturdayStart = schedule.SaturdayShiftTwoStart.ToString();
                model.SaturdayEnd = schedule.SaturdayShiftTwoEnd.ToString();
            }
            else if (schedule.SaturdayShiftThreeStart != null && schedule.SaturdayShiftThreeEnd != null)
            {
                model.SaturdayStart = schedule.SaturdayShiftThreeStart.ToString();
                model.SaturdayEnd = schedule.SaturdayShiftThreeEnd.ToString();
            }
            else
            {
                model.SaturdayStart = string.Empty;
                model.SaturdayEnd = string.Empty;
            }

            // Sunday
            if (schedule.SundayShiftOneStart != null && schedule.SundayShiftThreeEnd != null)
            {
                model.SundayStart = schedule.SundayShiftOneStart.ToString();
                model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
            }
            else if (schedule.SundayShiftOneStart != null && schedule.SundayShiftTwoEnd != null)
            {
                model.SundayStart = schedule.SundayShiftOneStart.ToString();
                model.SundayEnd = schedule.SundayShiftTwoEnd.ToString();
            }
            else if (schedule.SundayShiftTwoStart != null && schedule.SundayShiftThreeEnd != null)
            {
                model.SundayStart = schedule.SundayShiftTwoStart.ToString();
                model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
            }
            else if (schedule.SundayShiftOneStart != null && schedule.SundayShiftOneEnd != null)
            {
                model.SundayStart = schedule.SundayShiftOneStart.ToString();
                model.SundayEnd = schedule.SundayShiftOneEnd.ToString();
            }
            else if (schedule.SundayShiftTwoStart != null && schedule.SundayShiftTwoEnd != null)
            {
                model.SundayStart = schedule.SundayShiftTwoStart.ToString();
                model.SundayEnd = schedule.SundayShiftTwoEnd.ToString();
            }
            else if (schedule.SundayShiftThreeStart != null && schedule.SundayShiftThreeEnd != null)
            {
                model.SundayStart = schedule.SundayShiftThreeStart.ToString();
                model.SundayEnd = schedule.SundayShiftThreeEnd.ToString();
            }
            else
            {
                model.SundayStart = string.Empty;
                model.SundayEnd = string.Empty;
            }

        }

        //
        // POST: Schedule/Edit
        [HttpPost]
        [Authorize]
        public ActionResult Edit(ScheduleView modelView)
        {
            // TODO: something is not right... almost fixed :)
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    var schedule = context.Schedules.FirstOrDefault(s => s.Id == modelView.Id);
                    var monday = schedule.StartDate;
                    var sunday = schedule.EndDate;
                    var computers = context.Computers.Include("Schedules").Where(c => c.IsWorking == true).ToList();
                    var model = new ScheduleView();
                    model.Id = schedule.Id;
                    model.Hours = schedule.Hours.ToString();
                    ScheduleViewBuilder(schedule, model);
                    CheckForEmptyString(model);
                    string error = string.Empty;

                    //If Monday Shift is Changed
                    if (modelView.MondayStart != model.MondayStart || modelView.MondayEnd != model.MondayEnd)
                    {
                        if (modelView.MondayStart == null && modelView.MondayEnd == null)
                        {
                            error = MondayScheduleCleaner(schedule, computers, model, error);
                            ChangeEmployeeSchedule(schedule, modelView);
                        }
                        else
                        {
                            var mondayDiff = double.Parse(modelView.MondayEnd) - double.Parse(modelView.MondayStart);

                            if(mondayDiff < 4)
                            {
                                error = "Invalid Shift!";
                            }
                            else
                            {
                                if(model.MondayStart == null && model.MondayEnd == null)
                                {
                                    error = MondayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                                else
                                {
                                    error = MondayScheduleCleaner(schedule, computers, model, error);
                                    error = MondayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.MondayError = error;
                        return View();
                    }
                    

                    // If Thuesday Shift is Changed
                    if (modelView.ThuesdayStart != model.ThuesdayStart || modelView.ThuesdayEnd != model.ThuesdayEnd)
                    {
                        if (modelView.ThuesdayStart == null && modelView.ThuesdayEnd == null)
                        {
                            error = ThuesdayScheduleCleaner(schedule, computers, model, error);
                            ChangeEmployeeSchedule(schedule, modelView);
                        }
                        else
                        {
                            var thuesdayDiff = double.Parse(modelView.ThuesdayEnd) - double.Parse(modelView.ThuesdayStart);

                            if (thuesdayDiff < 4)
                            {
                                error = "Invalid Shift!";
                            }
                            else
                            {
                                if (model.ThuesdayStart == null && model.ThuesdayEnd == null)
                                {
                                    error = ThuesdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                                else
                                {
                                    error = ThuesdayScheduleCleaner(schedule, computers, model, error);
                                    error = ThuesdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.ThuesdayError = error;
                        return View();
                    }

                    // If Wednesday Shift is Changed
                    if (modelView.WednesdayStart != model.WednesdayStart || modelView.WednesdayEnd != model.WednesdayEnd)
                    {
                        if (modelView.WednesdayStart == null && modelView.WednesdayEnd == null)
                        {
                            error = WednesdayScheduleCleaner(schedule, computers, model, error);
                            ChangeEmployeeSchedule(schedule, modelView);
                        }
                        else
                        {
                            var wednesdayDiff = double.Parse(modelView.WednesdayEnd) - double.Parse(modelView.WednesdayStart);

                            if (wednesdayDiff < 4)
                            {
                                error = "Invalid Shift!";
                            }
                            else
                            {
                                if (model.WednesdayStart == null && model.WednesdayEnd == null)
                                {
                                    error = WednesdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                                else
                                {
                                    error = WednesdayScheduleCleaner(schedule, computers, model, error);
                                    error = WednesdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.WednesdayError = error;
                        return View();
                    }

                    // If Thursday Shift is Changed
                    if (modelView.ThursdayStart != model.ThursdayStart || modelView.ThursdayEnd != model.ThursdayEnd)
                    {
                        if (modelView.ThursdayStart == null && modelView.ThursdayEnd == null)
                        {
                            error = ThursdayScheduleCleaner(schedule, computers, model, error);
                            ChangeEmployeeSchedule(schedule, modelView);
                        }
                        else
                        {
                            var thursdayDiff = double.Parse(modelView.ThursdayEnd) - double.Parse(modelView.ThursdayStart);

                            if (thursdayDiff < 4)
                            {
                                error = "Invalid Shift!";
                            }
                            else
                            {
                                if (model.ThursdayStart == null && model.ThursdayEnd == null)
                                {
                                    error = ThursdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                                else
                                {
                                    error = ThursdayScheduleCleaner(schedule, computers, model, error);
                                    error = ThursdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.ThursdayError = error;
                        return View();
                    }

                    // If Friday Shift is Changed
                    if (modelView.FridayStart != model.FridayStart || modelView.FridayEnd != model.FridayEnd)
                    {
                        if (modelView.FridayStart == null && modelView.FridayEnd == null)
                        {
                            error = FridayScheduleCleaner(schedule, computers, model, error);
                            ChangeEmployeeSchedule(schedule, modelView);
                        }
                        else
                        {
                            var fridayDiff = double.Parse(modelView.FridayEnd) - double.Parse(modelView.FridayStart);

                            if (fridayDiff < 4)
                            {
                                error = "Invalid Shift!";
                            }
                            else
                            {
                                if (model.FridayStart == null && model.FridayEnd == null)
                                {
                                    error = FridayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                                else
                                {
                                    error = FridayScheduleCleaner(schedule, computers, model, error);
                                    error = FridayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.FridayError = error;
                        return View();
                    }

                    // If Saturday Shift is Changed
                    if (modelView.SaturdayStart != model.SaturdayStart || modelView.SaturdayEnd != model.SaturdayEnd)
                    {
                        if (modelView.SaturdayStart == null && modelView.SaturdayEnd == null)
                        {
                            error = SaturdayScheduleCleaner(schedule, computers, model, error);
                            ChangeEmployeeSchedule(schedule, modelView);
                        }
                        else
                        {
                            var saturdayDiff = double.Parse(modelView.SaturdayEnd) - double.Parse(modelView.SaturdayStart);

                            if (saturdayDiff < 4)
                            {
                                error = "Invalid Shift!";
                            }
                            else
                            {
                                if (model.SaturdayStart == null && model.SaturdayEnd == null)
                                {
                                    error = SaturdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                                else
                                {
                                    error = SaturdayScheduleCleaner(schedule, computers, model, error);
                                    error = SaturdayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.SaturdayError = error;
                        return View();
                    }

                    // If Sunday Shift is Changed
                    if (modelView.SundayStart != model.SundayStart || modelView.SundayEnd != model.SundayEnd)
                    {
                        if (modelView.SundayStart == null && modelView.SundayEnd == null)
                        {
                            error = SundayScheduleCleaner(schedule, computers, model, error);
                            ChangeEmployeeSchedule(schedule, modelView);
                        }
                        else
                        {
                            var sundayDiff = double.Parse(modelView.SundayEnd) - double.Parse(modelView.SundayStart);

                            if (sundayDiff < 4)
                            {
                                error = "Invalid Shift!";
                            }
                            else
                            {
                                if (model.SundayStart == null && model.SundayEnd == null)
                                {
                                    error = SundayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                                else
                                {
                                    error = SundayScheduleCleaner(schedule, computers, model, error);
                                    error = SundayCheck(context, monday, sunday, error, modelView);
                                    ChangeEmployeeSchedule(schedule, modelView);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        ViewBag.SundayError = error;
                        return View();
                    }

                    context.SaveChanges();
                    this.AddNotification("Schedule changed.", NotificationType.SUCCESS);
                    return RedirectToAction("ListMySchedules");
                }
            }

            return View();
        }

        private void CheckForEmptyString(ScheduleView model)
        {
            if(model.MondayStart == "" || model.MondayEnd == "")
            {
                model.MondayStart = null;
                model.MondayEnd = null;
            }
            if (model.ThuesdayStart == "" || model.ThuesdayEnd == "")
            {
                model.ThuesdayStart = null;
                model.ThuesdayEnd = null;
            }
            if (model.WednesdayStart == "" || model.WednesdayEnd == "")
            {
                model.WednesdayStart = null;
                model.WednesdayEnd = null;
            }
            if (model.ThursdayStart == "" || model.ThursdayEnd == "")
            {
                model.ThursdayStart = null;
                model.ThursdayEnd = null;
            }
            if (model.FridayStart == "" || model.FridayEnd == "")
            {
                model.FridayStart = null;
                model.FridayEnd = null;
            }
            if (model.SaturdayStart == "" || model.SaturdayEnd == "")
            {
                model.SaturdayStart = null;
                model.SaturdayEnd = null;
            }
            if (model.SundayStart == "" || model.SundayEnd == "")
            {
                model.SundayStart = null;
                model.SundayEnd = null;
            }


        }

        private static string MondayScheduleCleaner(Schedule schedule, List<Computer> computers, ScheduleView model, string error)
        {
            bool found = false;
            var mondayStart = double.Parse(model.MondayStart);
            var mondayEnd = double.Parse(model.MondayEnd);
            var mondayhours = mondayEnd - mondayStart;

            if (mondayhours == 4)
            {
                if (mondayStart == 9 && mondayEnd == 13)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.MondayShiftOneStart == 9 && pcSchedule.MondayShiftOneEnd == 13 && found == false)
                        {
                            pcSchedule.MondayShiftOneStart = null;
                            pcSchedule.MondayShiftOneEnd = null;
                            found = true;
                        }
                    }
                }
                else if (mondayStart == 13 && mondayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.MondayShiftTwoStart == 13 && pcSchedule.MondayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.MondayShiftTwoStart = null;
                            pcSchedule.MondayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (mondayStart == 17 && mondayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.MondayShiftThreeStart == 17 && pcSchedule.MondayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.MondayShiftThreeStart = null;
                            pcSchedule.MondayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
                else
                {
                    error = "Invalid shift!";
                }
            }
            else if (mondayhours == 8)
            {
                if (mondayStart == 9 && mondayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.MondayShiftOneStart == 9 && pcSchedule.MondayShiftOneEnd == 13 && pcSchedule.MondayShiftTwoStart == 13 && pcSchedule.MondayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.MondayShiftOneStart = null;
                            pcSchedule.MondayShiftOneEnd = null;
                            pcSchedule.MondayShiftTwoStart = null;
                            pcSchedule.MondayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (mondayStart == 13 && mondayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.MondayShiftTwoStart == 13 && pcSchedule.MondayShiftTwoEnd == 17 && pcSchedule.MondayShiftThreeStart == 17 && pcSchedule.MondayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.MondayShiftTwoStart = null;
                            pcSchedule.MondayShiftTwoEnd = null;
                            pcSchedule.MondayShiftThreeStart = null;
                            pcSchedule.MondayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
            }
            else if (mondayStart == 9 && mondayEnd == 18)
            {
                foreach (var c in computers)
                {
                    var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                    if (pcSchedule.MondayShiftOneStart == 9 && pcSchedule.MondayShiftOneEnd == 13 && pcSchedule.MondayShiftTwoStart == 13 && pcSchedule.MondayShiftTwoEnd == 17 && pcSchedule.MondayShiftThreeStart == 17 && pcSchedule.MondayShiftThreeEnd == 18 && found == false)
                    {
                        pcSchedule.MondayShiftOneStart = null;
                        pcSchedule.MondayShiftOneEnd = null;
                        pcSchedule.MondayShiftTwoStart = null;
                        pcSchedule.MondayShiftTwoEnd = null;
                        pcSchedule.MondayShiftThreeStart = null;
                        pcSchedule.MondayShiftThreeEnd = null;
                        found = true;
                    }
                }
            }

            return error;
        }

        private static string ThuesdayScheduleCleaner(Schedule schedule, List<Computer> computers, ScheduleView model, string error)
        {
            bool found = false;
            var thuesdayStart = double.Parse(model.ThuesdayStart);
            var thuesdayEnd = double.Parse(model.ThuesdayEnd);
            var thuesdayhours = thuesdayEnd - thuesdayStart;

            if (thuesdayhours == 4)
            {
                if (thuesdayStart == 9 && thuesdayEnd == 13)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThuesdayShiftOneStart == 9 && pcSchedule.ThuesdayShiftOneEnd == 13 && found == false)
                        {
                            pcSchedule.ThuesdayShiftOneStart = null;
                            pcSchedule.ThuesdayShiftOneEnd = null;
                            found = true;
                        }
                    }
                }
                else if (thuesdayStart == 13 && thuesdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThuesdayShiftTwoStart == 13 && pcSchedule.ThuesdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.ThuesdayShiftTwoStart = null;
                            pcSchedule.ThuesdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (thuesdayStart == 17 && thuesdayEnd == 21)
                {
                    foreach (var c in computers)
                    {

                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThuesdayShiftThreeStart == 17 && pcSchedule.ThuesdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.ThuesdayShiftThreeStart = null;
                            pcSchedule.ThuesdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
                else
                {
                    error = "Invalid shift!";

                }
            }
            else if (thuesdayhours == 8)
            {
                if (thuesdayStart == 9 && thuesdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThuesdayShiftOneStart == 9 && pcSchedule.ThuesdayShiftOneEnd == 13 && pcSchedule.ThuesdayShiftTwoStart == 13 && pcSchedule.ThuesdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.ThuesdayShiftOneStart = null;
                            pcSchedule.ThuesdayShiftOneEnd = null;
                            pcSchedule.ThuesdayShiftTwoStart = null;
                            pcSchedule.ThuesdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (thuesdayStart == 13 && thuesdayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThuesdayShiftTwoStart == 13 && pcSchedule.ThuesdayShiftTwoEnd == 17 && pcSchedule.ThuesdayShiftThreeStart == 17 && pcSchedule.ThuesdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.ThuesdayShiftTwoStart = null;
                            pcSchedule.ThuesdayShiftTwoEnd = null;
                            pcSchedule.ThuesdayShiftThreeStart = null;
                            pcSchedule.ThuesdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
            }
            else if (thuesdayStart == 9 && thuesdayEnd == 18)
            {
                foreach (var c in computers)
                {
                    var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                    if (pcSchedule.ThuesdayShiftOneStart == 9 && pcSchedule.ThuesdayShiftOneEnd == 13 && pcSchedule.ThuesdayShiftTwoStart == 13 && pcSchedule.ThuesdayShiftTwoEnd == 17 && pcSchedule.ThuesdayShiftThreeStart == 17 && pcSchedule.ThuesdayShiftThreeEnd == 18 && found == false)
                    {
                        pcSchedule.ThuesdayShiftOneStart = null;
                        pcSchedule.ThuesdayShiftOneEnd = null;
                        pcSchedule.ThuesdayShiftTwoStart = null;
                        pcSchedule.ThuesdayShiftTwoEnd = null;
                        pcSchedule.ThuesdayShiftThreeStart = null;
                        pcSchedule.ThuesdayShiftThreeEnd = null;
                        found = true;

                    }
                }
            }

            return error;
        }

        private static string WednesdayScheduleCleaner(Schedule schedule, List<Computer> computers, ScheduleView model, string error)
        {
            bool found = false;
            var wednesdayStart = double.Parse(model.WednesdayStart);
            var wednesdayEnd = double.Parse(model.WednesdayEnd);
            var wednesdayhours = wednesdayEnd - wednesdayStart;

            if (wednesdayhours == 4)
            {
                if (wednesdayStart == 9 && wednesdayEnd == 13)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.WednesdayShiftOneStart == 9 && pcSchedule.WednesdayShiftOneEnd == 13 && found == false)
                        {
                            pcSchedule.WednesdayShiftOneStart = null;
                            pcSchedule.WednesdayShiftOneEnd = null;
                            found = true;
                        }
                    }
                }
                else if (wednesdayStart == 13 && wednesdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.WednesdayShiftTwoStart == 13 && pcSchedule.WednesdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.WednesdayShiftTwoStart = null;
                            pcSchedule.WednesdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (wednesdayStart == 17 && wednesdayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.WednesdayShiftThreeStart == 17 && pcSchedule.WednesdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.WednesdayShiftThreeStart = null;
                            pcSchedule.WednesdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
                else
                {
                    error = "Invalid shift!";
                }
            }
            else if (wednesdayhours == 8)
            {
                if (wednesdayStart == 9 && wednesdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.WednesdayShiftOneStart == 9 && pcSchedule.WednesdayShiftOneEnd == 13 && pcSchedule.WednesdayShiftTwoStart == 13 && pcSchedule.WednesdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.WednesdayShiftOneStart = null;
                            pcSchedule.WednesdayShiftOneEnd = null;
                            pcSchedule.WednesdayShiftTwoStart = null;
                            pcSchedule.WednesdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (wednesdayStart == 13 && wednesdayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.WednesdayShiftTwoStart == 13 && pcSchedule.WednesdayShiftTwoEnd == 17 && pcSchedule.WednesdayShiftThreeStart == 17 && pcSchedule.WednesdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.WednesdayShiftTwoStart = null;
                            pcSchedule.WednesdayShiftTwoEnd = null;
                            pcSchedule.WednesdayShiftThreeStart = null;
                            pcSchedule.WednesdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
            }
            else if (wednesdayStart == 9 && wednesdayEnd == 18)
            {
                foreach (var c in computers)
                {
                    var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                    if (pcSchedule.WednesdayShiftOneStart == 9 && pcSchedule.WednesdayShiftOneEnd == 13 && pcSchedule.WednesdayShiftTwoStart == 13 && pcSchedule.WednesdayShiftTwoEnd == 17 && pcSchedule.WednesdayShiftThreeStart == 17 && pcSchedule.WednesdayShiftThreeEnd == 18 && found == false)
                    {
                        pcSchedule.WednesdayShiftOneStart = null;
                        pcSchedule.WednesdayShiftOneEnd = null;
                        pcSchedule.WednesdayShiftTwoStart = null;
                        pcSchedule.WednesdayShiftTwoEnd = null;
                        pcSchedule.WednesdayShiftThreeStart = null;
                        pcSchedule.WednesdayShiftThreeEnd = null;
                        found = true;
                    }
                }
            }

            return error;
        }

        private static string ThursdayScheduleCleaner(Schedule schedule, List<Computer> computers, ScheduleView model, string error)
        {
            bool found = false;
            var thursdayStart = double.Parse(model.ThursdayStart);
            var thursdayEnd = double.Parse(model.ThursdayEnd);
            var thursdayhours = thursdayEnd - thursdayStart;

            if (thursdayhours == 4)
            {
                if (thursdayStart == 9 && thursdayEnd == 13)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThursdayShiftOneStart == 9 && pcSchedule.ThursdayShiftOneEnd == 13 && found == false)
                        {
                            pcSchedule.ThursdayShiftOneStart = null;
                            pcSchedule.ThursdayShiftOneEnd = null;
                            found = true;
                        }
                    }
                }
                else if (thursdayStart == 13 && thursdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThursdayShiftTwoStart == 13 && pcSchedule.ThursdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.ThursdayShiftTwoStart = null;
                            pcSchedule.ThursdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (thursdayStart == 17 && thursdayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThursdayShiftThreeStart == 17 && pcSchedule.ThursdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.ThursdayShiftThreeStart = null;
                            pcSchedule.ThursdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
                else
                {
                    error = "Invalid shift!";
                }
            }
            else if (thursdayhours == 8)
            {
                if (thursdayStart == 9 && thursdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThursdayShiftOneStart == 9 && pcSchedule.ThursdayShiftOneEnd == 13 && pcSchedule.ThursdayShiftTwoStart == 13 && pcSchedule.ThursdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.ThursdayShiftOneStart = null;
                            pcSchedule.ThursdayShiftOneEnd = null;
                            pcSchedule.ThursdayShiftTwoStart = null;
                            pcSchedule.ThursdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (thursdayStart == 13 && thursdayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.ThursdayShiftTwoStart == 13 && pcSchedule.ThursdayShiftTwoEnd == 17 && pcSchedule.ThursdayShiftThreeStart == 17 && pcSchedule.ThursdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.ThursdayShiftTwoStart = null;
                            pcSchedule.ThursdayShiftTwoEnd = null;
                            pcSchedule.ThursdayShiftThreeStart = null;
                            pcSchedule.ThursdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
            }
            else if (thursdayStart == 9 && thursdayEnd == 18)
            {
                foreach (var c in computers)
                {
                    var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                    if (pcSchedule.ThursdayShiftOneStart == 9 && pcSchedule.ThursdayShiftOneEnd == 13 && pcSchedule.ThursdayShiftTwoStart == 13 && pcSchedule.ThursdayShiftTwoEnd == 17 && pcSchedule.ThursdayShiftThreeStart == 17 && pcSchedule.ThursdayShiftThreeEnd == 18 && found == false)
                    {
                        pcSchedule.ThursdayShiftOneStart = null;
                        pcSchedule.ThursdayShiftOneEnd = null;
                        pcSchedule.ThursdayShiftTwoStart = null;
                        pcSchedule.ThursdayShiftTwoEnd = null;
                        pcSchedule.ThursdayShiftThreeStart = null;
                        pcSchedule.ThursdayShiftThreeEnd = null;
                        found = true;
                    }
                }
            }

            return error;
        }

        private static string FridayScheduleCleaner(Schedule schedule, List<Computer> computers, ScheduleView model, string error)
        {
            bool found = false;
            var fridayStart = double.Parse(model.FridayStart);
            var fridayEnd = double.Parse(model.FridayEnd);
            var fridayhours = fridayEnd - fridayStart;

            if (fridayhours == 4)
            {
                if (fridayStart == 9 && fridayEnd == 13)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.FridayShiftOneStart == 9 && pcSchedule.FridayShiftOneEnd == 13 && found == false)
                        {
                            pcSchedule.FridayShiftOneStart = null;
                            pcSchedule.FridayShiftOneEnd = null;
                            found = true;
                        }
                    }
                }
                else if (fridayStart == 13 && fridayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.FridayShiftTwoStart == 13 && pcSchedule.FridayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.FridayShiftTwoStart = null;
                            pcSchedule.FridayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (fridayStart == 17 && fridayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.FridayShiftThreeStart == 17 && pcSchedule.FridayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.FridayShiftThreeStart = null;
                            pcSchedule.FridayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
                else
                {
                    error = "Invalid shift!";
                }
            }
            else if (fridayhours == 8)
            {
                if (fridayStart == 9 && fridayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.FridayShiftOneStart == 9 && pcSchedule.FridayShiftOneEnd == 13 && pcSchedule.FridayShiftTwoStart == 13 && pcSchedule.FridayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.FridayShiftOneStart = null;
                            pcSchedule.FridayShiftOneEnd = null;
                            pcSchedule.FridayShiftTwoStart = null;
                            pcSchedule.FridayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (fridayStart == 13 && fridayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.FridayShiftTwoStart == 13 && pcSchedule.FridayShiftTwoEnd == 17 && pcSchedule.FridayShiftThreeStart == 17 && pcSchedule.FridayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.FridayShiftTwoStart = null;
                            pcSchedule.FridayShiftTwoEnd = null;
                            pcSchedule.FridayShiftThreeStart = null;
                            pcSchedule.FridayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
            }
            else if (fridayStart == 9 && fridayEnd == 18)
            {
                foreach (var c in computers)
                {
                    var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                    if (pcSchedule.FridayShiftOneStart == 9 && pcSchedule.FridayShiftOneEnd == 13 && pcSchedule.FridayShiftTwoStart == 13 && pcSchedule.FridayShiftTwoEnd == 17 && pcSchedule.FridayShiftThreeStart == 17 && pcSchedule.FridayShiftThreeEnd == 18 && found == false)
                    {
                        pcSchedule.FridayShiftOneStart = null;
                        pcSchedule.FridayShiftOneEnd = null;
                        pcSchedule.FridayShiftTwoStart = null;
                        pcSchedule.FridayShiftTwoEnd = null;
                        pcSchedule.FridayShiftThreeStart = null;
                        pcSchedule.FridayShiftThreeEnd = null;
                        found = true;
                    }
                }
            }

            return error;
        }

        private static string SaturdayScheduleCleaner(Schedule schedule, List<Computer> computers, ScheduleView model, string error)
        {
            bool found = false;
            var saturdayStart = double.Parse(model.SaturdayStart);
            var saturdayEnd = double.Parse(model.SaturdayEnd);
            var saturdayhours = saturdayEnd - saturdayStart;

            if (saturdayhours == 4)
            {
                if (saturdayStart == 9 && saturdayEnd == 13)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SaturdayShiftOneStart == 9 && pcSchedule.SaturdayShiftOneEnd == 13 && found == false)
                        {
                            pcSchedule.SaturdayShiftOneStart = null;
                            pcSchedule.SaturdayShiftOneEnd = null;
                            found = true;
                        }
                    }
                }
                else if (saturdayStart == 13 && saturdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SaturdayShiftTwoStart == 13 && pcSchedule.SaturdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.SaturdayShiftTwoStart = null;
                            pcSchedule.SaturdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (saturdayStart == 17 && saturdayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SaturdayShiftThreeStart == 17 && pcSchedule.SaturdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.SaturdayShiftThreeStart = null;
                            pcSchedule.SaturdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
                else
                {
                    error = "Invalid shift!";
                }
            }
            else if (saturdayhours == 8)
            {
                if (saturdayStart == 9 && saturdayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SaturdayShiftOneStart == 9 && pcSchedule.SaturdayShiftOneEnd == 13 && pcSchedule.SaturdayShiftTwoStart == 13 && pcSchedule.SaturdayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.SaturdayShiftOneStart = null;
                            pcSchedule.SaturdayShiftOneEnd = null;
                            pcSchedule.SaturdayShiftTwoStart = null;
                            pcSchedule.SaturdayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (saturdayStart == 13 && saturdayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SaturdayShiftTwoStart == 13 && pcSchedule.SaturdayShiftTwoEnd == 17 && pcSchedule.SaturdayShiftThreeStart == 17 && pcSchedule.SaturdayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.SaturdayShiftTwoStart = null;
                            pcSchedule.SaturdayShiftTwoEnd = null;
                            pcSchedule.SaturdayShiftThreeStart = null;
                            pcSchedule.SaturdayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
            }
            else if (saturdayStart == 9 && saturdayEnd == 18)
            {
                foreach (var c in computers)
                {
                    var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                    if (pcSchedule.SaturdayShiftOneStart == 9 && pcSchedule.SaturdayShiftOneEnd == 13 && pcSchedule.SaturdayShiftTwoStart == 13 && pcSchedule.SaturdayShiftTwoEnd == 17 && pcSchedule.SaturdayShiftThreeStart == 17 && pcSchedule.SaturdayShiftThreeEnd == 18 && found == false)
                    {
                        pcSchedule.SaturdayShiftOneStart = null;
                        pcSchedule.SaturdayShiftOneEnd = null;
                        pcSchedule.SaturdayShiftTwoStart = null;
                        pcSchedule.SaturdayShiftTwoEnd = null;
                        pcSchedule.SaturdayShiftThreeStart = null;
                        pcSchedule.SaturdayShiftThreeEnd = null;
                        found = true;
                    }
                }
            }

            return error;
        }

        private static string SundayScheduleCleaner(Schedule schedule, List<Computer> computers, ScheduleView model, string error)
        {
            bool found = false;
            var sundayStart = double.Parse(model.SundayStart);
            var sundayEnd = double.Parse(model.SundayEnd);
            var sundayhours = sundayEnd - sundayStart;

            if (sundayhours == 4)
            {
                if (sundayStart == 9 && sundayEnd == 13)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SundayShiftOneStart == 9 && pcSchedule.SundayShiftOneEnd == 13 && found == false)
                        {
                            pcSchedule.SundayShiftOneStart = null;
                            pcSchedule.SundayShiftOneEnd = null;
                            found = true;
                        }
                    }
                }
                else if (sundayStart == 13 && sundayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SundayShiftTwoStart == 13 && pcSchedule.SundayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.SundayShiftTwoStart = null;
                            pcSchedule.SundayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (sundayStart == 17 && sundayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SundayShiftThreeStart == 17 && pcSchedule.SundayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.SundayShiftThreeStart = null;
                            pcSchedule.SundayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
                else
                {
                    error = "Invalid shift!";
                }
            }
            else if (sundayhours == 8)
            {
                if (sundayStart == 9 && sundayEnd == 17)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SundayShiftOneStart == 9 && pcSchedule.SundayShiftOneEnd == 13 && pcSchedule.SundayShiftTwoStart == 13 && pcSchedule.SundayShiftTwoEnd == 17 && found == false)
                        {
                            pcSchedule.SundayShiftOneStart = null;
                            pcSchedule.SundayShiftOneEnd = null;
                            pcSchedule.SundayShiftTwoStart = null;
                            pcSchedule.SundayShiftTwoEnd = null;
                            found = true;
                        }
                    }
                }
                else if (sundayStart == 13 && sundayEnd == 21)
                {
                    foreach (var c in computers)
                    {
                        var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                        if (pcSchedule.SundayShiftTwoStart == 13 && pcSchedule.SundayShiftTwoEnd == 17 && pcSchedule.SundayShiftThreeStart == 17 && pcSchedule.SundayShiftThreeEnd == 21 && found == false)
                        {
                            pcSchedule.SundayShiftTwoStart = null;
                            pcSchedule.SundayShiftTwoEnd = null;
                            pcSchedule.SundayShiftThreeStart = null;
                            pcSchedule.SundayShiftThreeEnd = null;
                            found = true;
                        }
                    }
                }
            }
            else if (sundayStart == 9 && sundayEnd == 18)
            {
                foreach (var c in computers)
                {
                    var pcSchedule = c.Schedules.FirstOrDefault(s => s.StartDate == schedule.StartDate && s.EndDate == schedule.EndDate);

                    if (pcSchedule.SundayShiftOneStart == 9 && pcSchedule.SundayShiftOneEnd == 13 && pcSchedule.SundayShiftTwoStart == 13 && pcSchedule.SundayShiftTwoEnd == 17 && pcSchedule.SundayShiftThreeStart == 17 && pcSchedule.SundayShiftThreeEnd == 18 && found == false)
                    {
                        pcSchedule.SundayShiftOneStart = null;
                        pcSchedule.SundayShiftOneEnd = null;
                        pcSchedule.SundayShiftTwoStart = null;
                        pcSchedule.SundayShiftTwoEnd = null;
                        pcSchedule.SundayShiftThreeStart = null;
                        pcSchedule.SundayShiftThreeEnd = null;
                        found = true;
                    }
                }
            }

            return error;
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
                        if (schedule == null)
                        {
                            break;
                        }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if(schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                        if (schedule == null)
                        {
                            break;
                        }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                        if (schedule == null)
                        {
                            break;
                        }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                        if (schedule == null)
                        {
                            break;
                        }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                        if (schedule == null)
                        {
                            break;
                        }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                        if (schedule == null)
                        {
                            break;
                        }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                        if (schedule == null)
                        {
                            break;
                        }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
                            if (schedule == null)
                            {
                                break;
                            }
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
            db.Schedules.Add(schedule);
            db.SaveChanges();
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

        private void ChangeEmployeeSchedule(Schedule schedule, ScheduleView model)
        {
            double hours = 0;
            schedule.Hours = 0;
            //Monday
            if (model.MondayStart != null && model.MondayEnd != null)
            {
                double mondayStart = double.Parse(model.MondayStart);
                double mondayEnd = double.Parse(model.MondayEnd);
                double mondayDiff = mondayEnd - mondayStart;
                //first set all to null
                schedule.MondayShiftOneStart = null;
                schedule.MondayShiftOneEnd = null;
                schedule.MondayShiftTwoStart = null;
                schedule.MondayShiftTwoEnd = null;
                schedule.MondayShiftThreeStart = null;
                schedule.MondayShiftThreeEnd = null;

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
            else if(model.MondayStart == null && model.MondayEnd == null)
            {
                schedule.MondayShiftOneStart = null;
                schedule.MondayShiftOneEnd = null;
                schedule.MondayShiftTwoStart = null;
                schedule.MondayShiftTwoEnd = null;
                schedule.MondayShiftThreeStart = null;
                schedule.MondayShiftThreeEnd = null;
            }
            //Thuesday
            if (model.ThuesdayStart != null && model.ThuesdayEnd != null)
            {
                double thuesdayStart = double.Parse(model.ThuesdayStart);
                double thuesdayEnd = double.Parse(model.ThuesdayEnd);
                double thuesdayDiff = thuesdayEnd - thuesdayStart;

                schedule.ThuesdayShiftOneStart = null;
                schedule.ThuesdayShiftOneEnd = null;
                schedule.ThuesdayShiftTwoStart = null;
                schedule.ThuesdayShiftTwoEnd = null;
                schedule.ThuesdayShiftThreeStart = null;
                schedule.ThuesdayShiftThreeEnd = null;

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
            else if (model.ThuesdayStart == null && model.ThuesdayEnd == null)
            {
                schedule.ThuesdayShiftOneStart = null;
                schedule.ThuesdayShiftOneEnd = null;
                schedule.ThuesdayShiftTwoStart = null;
                schedule.ThuesdayShiftTwoEnd = null;
                schedule.ThuesdayShiftThreeStart = null;
                schedule.ThuesdayShiftThreeEnd = null;
            }
            //Wednesday
            if (model.WednesdayStart != null && model.WednesdayEnd != null)
            {
                double wednesdayStart = double.Parse(model.WednesdayStart);
                double wednesdayEnd = double.Parse(model.WednesdayEnd);
                double wednesdayDiff = wednesdayEnd - wednesdayStart;

                schedule.WednesdayShiftOneStart = null;
                schedule.WednesdayShiftOneEnd = null;
                schedule.WednesdayShiftTwoStart = null;
                schedule.WednesdayShiftTwoEnd = null;
                schedule.WednesdayShiftThreeStart = null;
                schedule.WednesdayShiftThreeEnd = null;

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
            else if (model.WednesdayStart == null && model.WednesdayEnd == null)
            {
                schedule.WednesdayShiftOneStart = null;
                schedule.WednesdayShiftOneEnd = null;
                schedule.WednesdayShiftTwoStart = null;
                schedule.WednesdayShiftTwoEnd = null;
                schedule.WednesdayShiftThreeStart = null;
                schedule.WednesdayShiftThreeEnd = null;
            }
            //Thursday
            if (model.ThursdayEnd != null && model.ThursdayStart != null)
            {
                double thursdayStart = double.Parse(model.ThursdayStart);
                double thursdayEnd = double.Parse(model.ThursdayEnd);
                double thursdayDiff = thursdayEnd - thursdayStart;

                schedule.ThursdayShiftOneStart = null;
                schedule.ThursdayShiftOneEnd = null;
                schedule.ThursdayShiftTwoStart = null;
                schedule.ThursdayShiftTwoEnd = null;
                schedule.ThursdayShiftThreeStart = null;
                schedule.ThursdayShiftThreeEnd = null;

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
            else if (model.ThursdayStart == null && model.ThursdayEnd == null)
            {
                schedule.ThursdayShiftOneStart = null;
                schedule.ThursdayShiftOneEnd = null;
                schedule.ThursdayShiftTwoStart = null;
                schedule.ThursdayShiftTwoEnd = null;
                schedule.ThursdayShiftThreeStart = null;
                schedule.ThursdayShiftThreeEnd = null;
            }
            //Friday
            if (model.FridayStart != null && model.FridayEnd != null)
            {
                double fridayStart = double.Parse(model.FridayStart);
                double fridayEnd = double.Parse(model.FridayEnd);
                double fridayDiff = fridayEnd - fridayStart;

                schedule.FridayShiftOneStart = null;
                schedule.FridayShiftOneEnd = null;
                schedule.FridayShiftTwoStart = null;
                schedule.FridayShiftTwoEnd = null;
                schedule.FridayShiftThreeStart = null;
                schedule.FridayShiftThreeEnd = null;

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
            else if (model.FridayStart == null && model.FridayEnd == null)
            {
                schedule.FridayShiftOneStart = null;
                schedule.FridayShiftOneEnd = null;
                schedule.FridayShiftTwoStart = null;
                schedule.FridayShiftTwoEnd = null;
                schedule.FridayShiftThreeStart = null;
                schedule.FridayShiftThreeEnd = null;
            }
            //Saturday
            if (model.SaturdayStart != null && model.SaturdayEnd != null)
            {
                double saturdayStart = double.Parse(model.SaturdayStart);
                double saturdayEnd = double.Parse(model.SaturdayEnd);
                double satudrdayDiff = saturdayEnd - saturdayStart;

                schedule.SaturdayShiftOneStart = null;
                schedule.SaturdayShiftOneEnd = null;
                schedule.SaturdayShiftTwoStart = null;
                schedule.SaturdayShiftTwoEnd = null;
                schedule.SaturdayShiftThreeStart = null;
                schedule.SaturdayShiftThreeEnd = null;

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
            else if (model.SaturdayStart == null && model.SaturdayEnd == null)
            {
                schedule.SaturdayShiftOneStart = null;
                schedule.SaturdayShiftOneEnd = null;
                schedule.SaturdayShiftTwoStart = null;
                schedule.SaturdayShiftTwoEnd = null;
                schedule.SaturdayShiftThreeStart = null;
                schedule.SaturdayShiftThreeEnd = null;
            }
            //Sunday
            if (model.SundayStart != null && model.SundayEnd != null)
            {
                double sundayStart = double.Parse(model.SundayStart);
                double sundayEnd = double.Parse(model.SundayEnd);
                double sundayDiff = sundayEnd - sundayStart;

                schedule.SundayShiftOneStart = null;
                schedule.SundayShiftOneEnd = null;
                schedule.SundayShiftTwoStart = null;
                schedule.SundayShiftTwoEnd = null;
                schedule.SundayShiftThreeStart = null;
                schedule.SundayShiftThreeEnd = null;

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
            else if (model.SundayStart == null && model.SundayEnd == null)
            {
                schedule.SundayShiftOneStart = null;
                schedule.SundayShiftOneEnd = null;
                schedule.SundayShiftTwoStart = null;
                schedule.SundayShiftTwoEnd = null;
                schedule.SundayShiftThreeStart = null;
                schedule.SundayShiftThreeEnd = null;
            }
            schedule.Hours = hours;
        }
    }
}
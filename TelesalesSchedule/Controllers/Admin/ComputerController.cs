using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TelesalesSchedule.Models;

namespace TelesalesSchedule.Controllers.Admin
{
    public class ComputerController : Controller
    {
        // GET: Computer
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        //
        // GET: Computer/List
        public ActionResult List()
        {
            using (var context = new TelesalesScheduleDbContext())
            {
                var computers = context.Computers.ToList();

                return View(computers);
            }
        }

        //
        // GET: Computer/Create
        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: Computer/Create
        [HttpPost]
        public ActionResult Create(Computer computer)
        {
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    context.Computers.Add(computer);
                    context.SaveChanges();

                    return RedirectToAction("List");
                }
            }

            return View(computer);
        }

        //
        // GET: Computer/Edit
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                var computer = context.Computers
                    .FirstOrDefault(c => c.Id == id);

                if (computer == null)
                {
                    return HttpNotFound();
                }

                return View(computer);
            }
        }

        //
        // POST: Computer/Edit
        [HttpPost]
        public ActionResult Edit(Computer computer)
        {
            if (ModelState.IsValid)
            {
                using (var context = new TelesalesScheduleDbContext())
                {
                    context.Entry(computer).State = EntityState.Modified;
                    context.SaveChanges();

                    return RedirectToAction("List");
                }
            }

            return View(computer);
        }

        //
        // GET: Computer/Delete
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            using (var context = new TelesalesScheduleDbContext())
            {
                var computer = context.Computers
                    .FirstOrDefault(c => c.Id == id);

                if (computer == null)
                {
                    return HttpNotFound();
                }

                return View(computer);
            }
        }

        // 
        // POST: Computer/Delete
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult DeleteConfirmed(int? id)
        {
            using (var context = new TelesalesScheduleDbContext())
            {
                var computer = context.Computers
                    .FirstOrDefault(c => c.Id == id);

                // TODO: To remove all schedules for this computer first

                context.Computers.Remove(computer);
                context.SaveChanges();

                return RedirectToAction("List");
            }
        }
    }
}
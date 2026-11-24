using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BrewHaven.Web.Models;
using BrewHaven.Web.Security;

namespace BrewHaven.Web.Controllers
{
    /// <summary>
    /// Owner passcode gate — deliberately not a user-account login (a coffee
    /// shop site has no customer accounts). One shared passcode unlocks the
    /// staff area for the current session; five wrong attempts lock the form
    /// for fifteen minutes. Closing the browser re-locks automatically.
    /// </summary>
    public class AdminController : Controller
    {
        // GET: /Admin/Unlock
        [HttpGet]
        public ActionResult Unlock(string returnUrl)
        {
            if (true.Equals(Session["BrewHaven.IsAdmin"]))
            {
                return AfterUnlock(returnUrl);
            }

            ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            return View();
        }

        // POST: /Admin/Unlock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(string passcode, string returnUrl)
        {
            ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

            var lockUntil = Session["BrewHaven.AdminLockUntil"] as DateTime?;
            if (lockUntil.HasValue && lockUntil.Value > DateTime.Now)
            {
                var minutes = (int)Math.Ceiling((lockUntil.Value - DateTime.Now).TotalMinutes);
                ModelState.AddModelError("", "Too many wrong attempts — locked for another " + minutes + " min.");
                return View();
            }

            if (AdminPasscode.Verify(passcode))
            {
                Session["BrewHaven.IsAdmin"] = true;
                Session.Remove("BrewHaven.AdminFails");
                Session.Remove("BrewHaven.AdminLockUntil");
                TempData["Flash"] = "Staff area unlocked.";
                return AfterUnlock(returnUrl);
            }

            var fails = (Session["BrewHaven.AdminFails"] as int? ?? 0) + 1;
            Session["BrewHaven.AdminFails"] = fails;
            if (fails >= 5)
            {
                Session["BrewHaven.AdminLockUntil"] = DateTime.Now.AddMinutes(15);
                ModelState.AddModelError("", "Wrong passcode five times — locked for 15 minutes.");
            }
            else
            {
                ModelState.AddModelError("", "Wrong passcode — attempt " + fails + " of 5.");
            }
            return View();
        }

        // POST: /Admin/Lock — re-lock the staff area for this session.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Lock()
        {
            Session.Remove("BrewHaven.IsAdmin");
            Session.Remove("BrewHaven.AdminFails");
            Session.Remove("BrewHaven.AdminLockUntil");
            TempData["Flash"] = "Staff area locked again.";
            return RedirectToAction("Menu", "Home");
        }

        // GET: /Admin/Dashboard — sales analytics for the owner (passcode gate).
        [HttpGet]
        [AdminOnly]
        public ActionResult Dashboard()
        {
            using (var db = new BrewHavenContext())
            {
                // Demo-scale dataset: load once, slice in memory.
                var orders = db.Orders.Include("Items").ToList();
                var today = DateTime.Today;
                var start = today.AddDays(-13);

                var vm = new DashboardViewModel
                {
                    TotalOrders = orders.Count,
                    TotalRevenue = orders.Sum(o => o.Total),
                    OrdersToday = orders.Count(o => o.PlacedAt >= today),
                    RevenueToday = orders.Where(o => o.PlacedAt >= today).Sum(o => o.Total),
                    PlacedCount = orders.Count(o => o.Status != "Completed"),
                    CompletedCount = orders.Count(o => o.Status == "Completed"),
                    ItemsOnMenu = db.MenuItems.Count(),
                    ItemsAvailable = db.MenuItems.Count(m => m.IsAvailable),
                    Days = Enumerable.Range(0, 14)
                        .Select(i => new DailyPoint { Date = start.AddDays(i) })
                        .ToList(),
                    TopItems = orders.SelectMany(o => o.Items)
                        .GroupBy(i => i.ItemName)
                        .Select(g => new TopItemLine
                        {
                            Name = g.Key,
                            Quantity = g.Sum(x => x.Quantity),
                            Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                        })
                        .OrderByDescending(t => t.Quantity)
                        .Take(5)
                        .ToList(),
                    RecentOrders = orders.OrderByDescending(o => o.PlacedAt).Take(5).ToList()
                };

                vm.AvgOrderValue = vm.TotalOrders > 0
                    ? Math.Round(vm.TotalRevenue / vm.TotalOrders, 2)
                    : 0m;

                foreach (var point in vm.Days)
                {
                    var dayOrders = orders.Where(o => o.PlacedAt.Date == point.Date.Date).ToList();
                    point.Orders = dayOrders.Count;
                    point.Revenue = dayOrders.Sum(o => o.Total);
                }

                return View(vm);
            }
        }

        private ActionResult AfterUnlock(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "MenuItems");
        }
    }
}

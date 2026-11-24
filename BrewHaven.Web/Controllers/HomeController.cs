using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BrewHaven.Web.Models;

namespace BrewHaven.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // The landing page uses the dark premium theme (dark header etc.).
            ViewBag.Theme = "dark";

            // Popular drinks straight from the menu table — whatever the admin
            // badges "Popular" shows up here. Tops up from the coffee category
            // so the grid always has six cards when the menu is stocked.
            List<MenuItem> popular;
            try
            {
                using (var db = new BrewHavenContext())
                {
                    popular = db.MenuItems
                        .Where(m => m.IsAvailable && m.Badge == "Popular")
                        .OrderBy(m => m.Id)
                        .ToList();

                    if (popular.Count < 6)
                    {
                        var pickedIds = popular.Select(p => p.Id).ToList();
                        var topUp = db.MenuItems
                            .Where(m => m.IsAvailable
                                && m.Category == "Coffee & Espresso"
                                && !pickedIds.Contains(m.Id))
                            .OrderBy(m => m.Id)
                            .Take(6 - popular.Count)
                            .ToList();
                        popular.AddRange(topUp);
                    }
                }
            }
            catch
            {
                // Database not ready (e.g. LocalDB starting up) — show the page
                // without the drinks grid instead of crashing the home page.
                popular = new List<MenuItem>();
            }

            return View(popular);
        }

        // GET: /Home/Menu?q=&cat=&sort= — server-side search, category filter
        // and sorting. Featured order (the default) keeps the grouped layout;
        // any active filter switches the page to a single results grid.
        public ActionResult Menu(string q, string cat, string sort)
        {
            List<MenuItem> items;
            using (var db = new BrewHavenContext())
            {
                items = db.MenuItems
                    .OrderBy(m => m.Id)
                    .ToList();
            }

            var query = (q ?? "").Trim();
            var selectedCat = string.IsNullOrWhiteSpace(cat) ? null : cat;
            var sortKey = string.IsNullOrWhiteSpace(sort) ? "featured" : sort;

            if (query.Length > 0)
            {
                items = items.Where(m =>
                    (m.Name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (m.Description ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (m.Category ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            if (selectedCat != null && Categories.Order.Contains(selectedCat))
            {
                items = items.Where(m => m.Category == selectedCat).ToList();
            }
            else
            {
                selectedCat = null; // unknown category in the query string → ignore it
            }

            switch (sortKey)
            {
                case "price-asc":
                    items = items.OrderBy(m => m.Price).ToList();
                    break;
                case "price-desc":
                    items = items.OrderByDescending(m => m.Price).ToList();
                    break;
                case "name":
                    items = items.OrderBy(m => m.Name).ToList();
                    break;
                default:
                    sortKey = "featured";
                    break;
            }

            return View(new MenuViewModel
            {
                Items = items,
                Q = query,
                Cat = selectedCat,
                Sort = sortKey,
                IsFiltered = query.Length > 0 || selectedCat != null || sortKey != "featured",
                Categories = Categories.Order.ToList()
            });
        }

        public ActionResult About()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Contact()
        {
            return View(new ContactForm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Contact(ContactForm form)
        {
            if (ModelState.IsValid)
            {
                // Front-end only: no persistence layer yet. The success panel
                // confirms the form model bound and validated correctly.
                ViewBag.Sent = true;
                return View(new ContactForm());
            }

            ViewBag.Sent = false;
            return View(form);
        }
    }
}

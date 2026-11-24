using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using BrewHaven.Web.Models;
using BrewHaven.Web.Security;

namespace BrewHaven.Web.Controllers
{
    /// <summary>
    /// Staff pages: create / edit / delete menu items (CRUD). Gated behind the
    /// owner passcode — [AdminOnly] redirects to /Admin/Unlock until unlocked
    /// (see Security/AdminOnlyAttribute.cs).
    /// </summary>
    [AdminOnly]
    public class MenuItemsController : Controller
    {
        private readonly BrewHavenContext _db = new BrewHavenContext();

        // GET: /MenuItems
        public ActionResult Index()
        {
            var items = _db.MenuItems
                .OrderBy(m => m.Id)
                .ToList();
            return View(items);
        }

        // GET: /MenuItems/Create
        [HttpGet]
        public ActionResult Create()
        {
            FillPhotoList();
            return View(new MenuItem { IsAvailable = true, Price = 0m });
        }

        // POST: /MenuItems/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MenuItem item)
        {
            if (!ModelState.IsValid)
            {
                FillPhotoList();
                return View(item);
            }

            _db.MenuItems.Add(item);
            _db.SaveChanges();
            TempData["Flash"] = item.Name + " was added to the menu.";
            return RedirectToAction("Index");
        }

        // GET: /MenuItems/Edit/5
        [HttpGet]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var item = _db.MenuItems.Find(id.Value);
            if (item == null)
            {
                return HttpNotFound();
            }

            FillPhotoList();
            return View(item);
        }

        // POST: /MenuItems/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MenuItem item)
        {
            if (!ModelState.IsValid)
            {
                FillPhotoList();
                return View(item);
            }

            _db.Entry(item).State = EntityState.Modified;
            _db.SaveChanges();
            TempData["Flash"] = item.Name + " was updated.";
            return RedirectToAction("Index");
        }

        // GET: /MenuItems/Delete/5
        [HttpGet]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var item = _db.MenuItems.Find(id.Value);
            if (item == null)
            {
                return HttpNotFound();
            }

            return View(item);
        }

        // POST: /MenuItems/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var item = _db.MenuItems.Find(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            _db.MenuItems.Remove(item);
            _db.SaveChanges();
            TempData["Flash"] = item.Name + " was removed from the menu.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Filenames of the bundled menu photos — feeds the suggestion list
        /// under the Photo field so paths can't be mistyped into thin air.
        /// </summary>
        private void FillPhotoList()
        {
            var dir = Server.MapPath("~/Content/images/menu");
            ViewBag.Photos = Directory.Exists(dir)
                ? Directory.GetFiles(dir, "*.jpg").Select(Path.GetFileName).OrderBy(n => n).ToList()
                : new List<string>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

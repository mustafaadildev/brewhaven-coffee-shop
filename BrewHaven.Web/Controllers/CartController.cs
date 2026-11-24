using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BrewHaven.Web.Models;

namespace BrewHaven.Web.Controllers
{
    /// <summary>
    /// The shopping cart lives in the session until the order is placed.
    /// Quantity and per-item "changes" can be edited on the cart page.
    /// </summary>
    public class CartController : Controller
    {
        private const string CartKey = "BrewHaven.Cart";

        private List<CartLine> Cart
        {
            get
            {
                var cart = Session[CartKey] as List<CartLine>;
                if (cart == null)
                {
                    cart = new List<CartLine>();
                    Session[CartKey] = cart;
                }
                return cart;
            }
        }

        // GET: /Cart
        public ActionResult Index()
        {
            var lines = BuildCartView();
            ViewBag.CartCount = Cart.Sum(c => c.Quantity);
            return View(lines);
        }

        // POST: /Cart/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(int menuItemId, int quantity)
        {
            if (quantity < 1) quantity = 1;
            if (quantity > 20) quantity = 20;

            using (var db = new BrewHavenContext())
            {
                var item = db.MenuItems.Find(menuItemId);
                if (item == null || !item.IsAvailable)
                {
                    TempData["Flash"] = "Sorry, that item can't be ordered right now.";
                    return RedirectToAction("Menu", "Home");
                }

                var cart = Cart;
                var existing = cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
                if (existing == null)
                {
                    cart.Add(new CartLine { MenuItemId = menuItemId, Quantity = quantity, Notes = "" });
                }
                else
                {
                    existing.Quantity = Math.Min(existing.Quantity + quantity, 99);
                }

                TempData["Flash"] = "Added " + item.Name + " to your order.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/Update — saves quantity and per-item changes for one line.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int menuItemId, int quantity, string notes)
        {
            var line = Cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
            if (line != null)
            {
                if (quantity <= 0)
                {
                    Cart.Remove(line);
                    TempData["Flash"] = "Item removed from your order.";
                }
                else
                {
                    line.Quantity = Math.Min(quantity, 99);
                    line.Notes = notes == null ? "" : notes.Trim();
                    TempData["Flash"] = "Your order was updated.";
                }
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/Remove
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Remove(int menuItemId)
        {
            var line = Cart.FirstOrDefault(c => c.MenuItemId == menuItemId);
            if (line != null)
            {
                Cart.Remove(line);
                TempData["Flash"] = "Item removed from your order.";
            }

            return RedirectToAction("Index");
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Clear()
        {
            Session.Remove(CartKey);
            TempData["Flash"] = "Your order was cleared.";
            return RedirectToAction("Index");
        }

        /// <summary>Joins the session lines with current menu data (name, price).</summary>
        private List<CartLineView> BuildCartView()
        {
            var cart = Cart;
            if (cart.Count == 0)
            {
                return new List<CartLineView>();
            }

            var ids = cart.Select(c => c.MenuItemId).ToList();
            using (var db = new BrewHavenContext())
            {
                var items = db.MenuItems
                    .Where(m => ids.Contains(m.Id))
                    .ToDictionary(m => m.Id);

                return cart
                    .Where(c => items.ContainsKey(c.MenuItemId))
                    .Select(c => new CartLineView
                    {
                        MenuItemId = c.MenuItemId,
                        Name = items[c.MenuItemId].Name,
                        UnitPrice = items[c.MenuItemId].Price,
                        Quantity = c.Quantity,
                        Notes = c.Notes
                    })
                    .ToList();
            }
        }
    }
}

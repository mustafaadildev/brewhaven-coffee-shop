using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BrewHaven.Web.Models;
using BrewHaven.Web.Security;

namespace BrewHaven.Web.Controllers
{
    /// <summary>
    /// Order placement (checkout → save to database → confirmation) and the
    /// staff pages that list and complete orders. The staff list (/Orders) and
    /// the Complete action are gated by [AdminOnly]; Confirmation and Details
    /// stay open because the customer's confirmation page links to Details.
    /// </summary>
    public class OrdersController : Controller
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

        // GET: /Orders/Checkout
        [HttpGet]
        public ActionResult Checkout()
        {
            var lines = BuildCartView();
            if (lines.Count == 0)
            {
                TempData["Flash"] = "Your order is empty — add something tasty from the menu first.";
                return RedirectToAction("Menu", "Home");
            }

            ViewBag.Lines = lines;
            return View(new CheckoutForm());
        }

        // POST: /Orders/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout(CheckoutForm form)
        {
            var lines = BuildCartView();
            if (lines.Count == 0)
            {
                TempData["Flash"] = "Your order is empty — add something tasty from the menu first.";
                return RedirectToAction("Menu", "Home");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Lines = lines;
                return View(form);
            }

            // Prices come from the database, never from the client.
            var ids = lines.Select(l => l.MenuItemId).ToList();
            var order = new Order
            {
                CustomerName = form.CustomerName,
                Email = form.Email,
                Phone = form.Phone,
                Notes = form.Notes,
                PlacedAt = DateTime.Now,
                Status = "Placed",
                Items = new List<OrderItem>()
            };

            using (var db = new BrewHavenContext())
            {
                var menu = db.MenuItems
                    .Where(m => ids.Contains(m.Id))
                    .ToDictionary(m => m.Id);

                foreach (var line in lines)
                {
                    var item = menu[line.MenuItemId];
                    order.Items.Add(new OrderItem
                    {
                        MenuItemId = item.Id,
                        ItemName = item.Name,
                        UnitPrice = item.Price,
                        Quantity = line.Quantity,
                        Notes = line.Notes
                    });
                }

                db.Orders.Add(order);
                db.SaveChanges();
                ViewBag.PlacedId = order.Id;
            }

            Session.Remove(CartKey);
            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        // GET: /Orders/Confirmation/5
        [HttpGet]
        public ActionResult Confirmation(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            using (var db = new BrewHavenContext())
            {
                var order = db.Orders.Include("Items").FirstOrDefault(o => o.Id == id.Value);
                if (order == null)
                {
                    return HttpNotFound();
                }

                return View(order);
            }
        }

        // GET: /Orders — staff list of every order placed (owner passcode gate).
        [HttpGet]
        [AdminOnly]
        public ActionResult Index()
        {
            using (var db = new BrewHavenContext())
            {
                var orders = db.Orders
                    .Include("Items")
                    .OrderByDescending(o => o.PlacedAt)
                    .ToList();
                return View(orders);
            }
        }

        // GET: /Orders/Details/5
        [HttpGet]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(400);
            }

            using (var db = new BrewHavenContext())
            {
                var order = db.Orders.Include("Items").FirstOrDefault(o => o.Id == id.Value);
                if (order == null)
                {
                    return HttpNotFound();
                }

                return View(order);
            }
        }

        // POST: /Orders/Complete/5 — staff marks an order done (passcode gate).
        [HttpPost]
        [AdminOnly]
        [ValidateAntiForgeryToken]
        public ActionResult Complete(int id)
        {
            using (var db = new BrewHavenContext())
            {
                var order = db.Orders.Find(id);
                if (order == null)
                {
                    return HttpNotFound();
                }

                order.Status = "Completed";
                db.SaveChanges();
                TempData["Flash"] = "Order #" + order.Id + " marked as completed.";
            }

            return RedirectToAction("Index");
        }

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

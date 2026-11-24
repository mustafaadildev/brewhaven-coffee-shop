using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Globalization;
using System.Linq;

namespace BrewHaven.Web.Models
{
    /// <summary>
    /// A menu item stored in the database (the old in-memory MenuItem was
    /// replaced by this). The admin CRUD pages write to this table.
    /// </summary>
    public class MenuItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please give the item a name.")]
        [StringLength(80)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Description { get; set; }

        [Range(0.01, 999.00, ErrorMessage = "Price must be between 0.01 and 999.00.")]
        public decimal Price { get; set; }

        /// <summary>One of the fixed category names in Categories.Order.</summary>
        [Required(ErrorMessage = "Please pick a category.")]
        public string Category { get; set; }

        /// <summary>Optional short badge: "V", "GF", "New", "Popular". Null when none.</summary>
        [StringLength(20)]
        public string Badge { get; set; }

        /// <summary>
        /// App-relative photo path, e.g. "~/Content/images/menu/latte.jpg".
        /// Null or empty → the card shows placeholder art instead of a photo.
        /// </summary>
        [StringLength(200)]
        public string ImageUrl { get; set; }

        /// <summary>False hides the Add-to-order button (shown as "Sold out today").</summary>
        public bool IsAvailable { get; set; }
    }

    /// <summary>A customer order: who ordered, contact details and the ordered lines.</summary>
    public class Order
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please tell us your name.")]
        [StringLength(80)]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "We need your email to confirm the order.")]
        [EmailAddress(ErrorMessage = "That email address does not look right.")]
        [StringLength(120)]
        public string Email { get; set; }

        [Phone(ErrorMessage = "That phone number does not look right.")]
        [StringLength(30)]
        public string Phone { get; set; }

        /// <summary>Order-level requests, e.g. "no sugar anywhere, extra napkins".</summary>
        [StringLength(500)]
        public string Notes { get; set; }

        public DateTime PlacedAt { get; set; }

        /// <summary>"Placed" or "Completed".</summary>
        [StringLength(20)]
        public string Status { get; set; }

        public virtual List<OrderItem> Items { get; set; }

        /// <summary>Convenience total — not stored; computed from Items.</summary>
        public decimal Total
        {
            get { return Items == null ? 0m : Items.Sum(i => i.UnitPrice * i.Quantity); }
        }
    }

    /// <summary>One ordered product line with quantity and per-item "changes".</summary>
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public virtual Order Order { get; set; }

        public int MenuItemId { get; set; }

        /// <summary>Name snapshot taken at order time (survives later menu edits).</summary>
        [StringLength(80)]
        public string ItemName { get; set; }

        /// <summary>Price snapshot taken at order time.</summary>
        public decimal UnitPrice { get; set; }

        [Range(1, 99)]
        public int Quantity { get; set; }

        /// <summary>Per-item customisation, e.g. "oat milk, extra hot".</summary>
        [StringLength(200)]
        public string Notes { get; set; }
    }

    /// <summary>A cart line kept in the session between "Add" and checkout.</summary>
    public class CartLine
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>Read-only view of a cart line with data joined from the menu.</summary>
    public class CartLineView
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; }
        public decimal LineTotal
        {
            get { return UnitPrice * Quantity; }
        }
    }

    /// <summary>Bound from the checkout form; order lines come from the session cart.</summary>
    public class CheckoutForm
    {
        [Required(ErrorMessage = "Please tell us your name.")]
        [StringLength(80)]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "We need your email to confirm the order.")]
        [EmailAddress(ErrorMessage = "That email address does not look right.")]
        [StringLength(120)]
        public string Email { get; set; }

        [Phone(ErrorMessage = "That phone number does not look right.")]
        [StringLength(30)]
        public string Phone { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }
    }

    /// <summary>Fixed menu categories, in display order, with their anchor keys.</summary>
    public static class Categories
    {
        public static readonly string[] Order =
        {
            "Coffee & Espresso",
            "Slow Brews & Not-Coffee",
            "Breakfast",
            "Pastries & Bakery"
        };

        /// <summary>Category name → anchor id used by the category pill navigation.</summary>
        public static string KeyFor(string category)
        {
            switch (category)
            {
                case "Coffee & Espresso": return "coffee";
                case "Slow Brews & Not-Coffee": return "brewed";
                case "Breakfast": return "breakfast";
                case "Pastries & Bakery": return "bakery";
                default: return "other";
            }
        }
    }

    /// <summary>Consistent USD formatting for prices.</summary>
    public static class Money
    {
        public static string Usd(decimal amount)
        {
            return amount.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        }
    }

    /// <summary>
    /// EF6 context. LocalDB database file lives in App_Data and is created
    /// and seeded automatically on first run (see BrewHavenDbInitializer).
    /// </summary>
    public class BrewHavenContext : DbContext
    {
        public BrewHavenContext() : base("BrewHavenContext") { }

        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
    }

    /// <summary>
    /// Creates the database on first run and fills it with the starter menu.
    /// When the model changes (e.g. a new column like MenuItem.ImageUrl) an
    /// existing database is dropped and re-created with fresh seed data —
    /// any locally saved test orders are reset with it.
    /// </summary>
    public class BrewHavenDbInitializer : DropCreateDatabaseIfModelChanges<BrewHavenContext>
    {
        /// <summary>
        /// Self-heal: if a database named "BrewHaven" is still registered in the
        /// LocalDB instance but its file is gone (project moved/re-extracted to
        /// a different folder), the login fails before EF can create the real
        /// one. Drop only such dead registrations, then let the normal
        /// create/seed logic run.
        /// </summary>
        public override void InitializeDatabase(BrewHavenContext context)
        {
            try
            {
                var masterCs = new System.Data.SqlClient.SqlConnectionStringBuilder(
                    context.Database.Connection.ConnectionString);
                masterCs.InitialCatalog = "master";
                using (var master = new System.Data.SqlClient.SqlConnection(masterCs.ConnectionString))
                {
                    master.Open();
                    using (var cmd = master.CreateCommand())
                    {
                        cmd.CommandText =
                            "IF DB_ID('BrewHaven') IS NOT NULL AND " +
                            "DATABASEPROPERTYEX('BrewHaven','Status') IN " +
                            "('RECOVERY_PENDING','SUSPECT','OFFLINE','RESTORING','EMERGENCY') " +
                            "DROP DATABASE [BrewHaven]";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Couldn't reach master — let base.InitializeDatabase surface
                // the real error instead of hiding it.
            }

            base.InitializeDatabase(context);
        }

        protected override void Seed(BrewHavenContext context)
        {
            var img = "~/Content/images/menu/";
            var items = new List<MenuItem>
            {
                // Coffee & Espresso
                new MenuItem { Name = "Espresso", Description = "Double shot of our Haven blend — cocoa, toasted almond, brown sugar finish.", Price = 3.00m, Category = "Coffee & Espresso", ImageUrl = img + "espresso-shot.jpg", IsAvailable = true },
                new MenuItem { Name = "Macchiato", Description = "Espresso marked with a spoonful of steamed milk foam.", Price = 3.50m, Category = "Coffee & Espresso", ImageUrl = img + "macchiato.jpg", IsAvailable = true },
                new MenuItem { Name = "Cortado", Description = "Equal parts espresso and silky steamed milk, served in glass.", Price = 3.75m, Category = "Coffee & Espresso", ImageUrl = img + "cortado.jpg", IsAvailable = true },
                new MenuItem { Name = "Flat White", Description = "Ristretto double under velvet microfoam. Strong, smooth, small.", Price = 4.25m, Category = "Coffee & Espresso", ImageUrl = img + "flat-white.jpg", IsAvailable = true },
                new MenuItem { Name = "Cappuccino", Description = "Classic third-third-third build with a dusting of cinnamon on request.", Price = 4.25m, Category = "Coffee & Espresso", ImageUrl = img + "cappuccino.jpg", IsAvailable = true },
                new MenuItem { Name = "Caffe Latte", Description = "Any size, your choice of whole, oat, almond or soy milk.", Price = 4.50m, Category = "Coffee & Espresso", ImageUrl = img + "latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Caramel Macchiato", Description = "Vanilla-kissed milk, espresso poured over, caramel drizzle.", Price = 5.00m, Category = "Coffee & Espresso", Badge = "Popular", ImageUrl = img + "caramel-macchiato.jpg", IsAvailable = true },
                new MenuItem { Name = "Mocha Haven", Description = "Espresso, dark Belgian chocolate, steamed milk, whipped cream.", Price = 5.00m, Category = "Coffee & Espresso", ImageUrl = img + "mocha.jpg", IsAvailable = true },
                new MenuItem { Name = "Cold Brew", Description = "Steeped 18 hours, poured over a single clear cube. Naturally sweet.", Price = 4.50m, Category = "Coffee & Espresso", Badge = "Popular", ImageUrl = img + "cold-brew.jpg", IsAvailable = true },
                new MenuItem { Name = "Iced Latte", Description = "Espresso over cold milk and ice; oat milk makes it extra creamy.", Price = 4.75m, Category = "Coffee & Espresso", ImageUrl = img + "iced-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Affogato", Description = "A scoop of vanilla bean gelato drowned in a fresh double shot.", Price = 5.50m, Category = "Coffee & Espresso", ImageUrl = img + "affogato.jpg", IsAvailable = true },
                new MenuItem { Name = "Batch Brew", Description = "Rotating single origin brewed all morning. Free refills, always.", Price = 3.00m, Category = "Coffee & Espresso", ImageUrl = img + "batch-brew.jpg", IsAvailable = true },

                // Slow Brews & Not-Coffee
                new MenuItem { Name = "Pour Over (V60)", Description = "Ask for this week's single origin. Ethiopia Guji and Colombia Huila rotate.", Price = 5.00m, Category = "Slow Brews & Not-Coffee", Badge = "New", ImageUrl = img + "pour-over.jpg", IsAvailable = true },
                new MenuItem { Name = "Chemex for Two", Description = "A 600 ml share pot served with two cups and a tasting card.", Price = 8.00m, Category = "Slow Brews & Not-Coffee", ImageUrl = img + "chemex.jpg", IsAvailable = true },
                new MenuItem { Name = "Honey Oat Latte", Description = "House-made honey vanilla syrup, steamed oat milk, cinnamon.", Price = 5.25m, Category = "Slow Brews & Not-Coffee", Badge = "Popular", ImageUrl = img + "honey-oat-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Lavender Latte", Description = "Dried-lavender infusion with a light honey touch. Subtly floral.", Price = 5.50m, Category = "Slow Brews & Not-Coffee", ImageUrl = img + "lavender-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Spiced Maple Latte", Description = "Real maple, warm pie spice, espresso. Autumn in a cup.", Price = 5.50m, Category = "Slow Brews & Not-Coffee", ImageUrl = img + "maple-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Matcha Latte", Description = "Ceremonial-grade matcha whisked to order, hot or iced.", Price = 5.00m, Category = "Slow Brews & Not-Coffee", ImageUrl = img + "matcha-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Chai Latte", Description = "Spiced black tea concentrate steeped with fresh ginger, steamed milk.", Price = 4.50m, Category = "Slow Brews & Not-Coffee", ImageUrl = img + "chai-latte.jpg", IsAvailable = true },
                new MenuItem { Name = "Hot Chocolate", Description = "Dark Belgian chocolate melted into milk; marshmallows optional.", Price = 4.00m, Category = "Slow Brews & Not-Coffee", ImageUrl = img + "hot-chocolate.jpg", IsAvailable = true },
                new MenuItem { Name = "Pot of Leaf Tea", Description = "English breakfast, earl grey, green jasmine or chamomile.", Price = 3.50m, Category = "Slow Brews & Not-Coffee", ImageUrl = img + "leaf-tea.jpg", IsAvailable = true },

                // Breakfast
                new MenuItem { Name = "The Haven Breakfast", Description = "Soft scrambled eggs, sourdough, smashed avocado, roasted tomatoes, herb oil.", Price = 11.50m, Category = "Breakfast", Badge = "Popular", ImageUrl = "~/Content/images/breakfast.jpg", IsAvailable = true },
                new MenuItem { Name = "Avocado Toast", Description = "Grainy sourdough, lemony avocado, chili flakes, pickled onion, dukkah.", Price = 9.50m, Category = "Breakfast", Badge = "V", ImageUrl = img + "avocado-toast.jpg", IsAvailable = true },
                new MenuItem { Name = "Breakfast Burrito", Description = "Scrambled egg, cheddar, black bean, potato hash, salsa verde, wrapped to go.", Price = 10.00m, Category = "Breakfast", ImageUrl = img + "breakfast-burrito.jpg", IsAvailable = true },
                new MenuItem { Name = "Buttermilk Pancakes", Description = "A short stack with whipped butter, warm maple and caramelised banana.", Price = 10.50m, Category = "Breakfast", Badge = "V", ImageUrl = img + "pancakes.jpg", IsAvailable = true },
                new MenuItem { Name = "Brioche French Toast", Description = "Thick-cut brioche, vanilla custard, berries, whipped mascarpone.", Price = 10.50m, Category = "Breakfast", Badge = "V", ImageUrl = img + "french-toast.jpg", IsAvailable = true },
                new MenuItem { Name = "Bacon, Egg & Cheese Croissant", Description = "Streaky bacon, folded egg, aged cheddar on a butter croissant.", Price = 9.00m, Category = "Breakfast", ImageUrl = img + "bec-croissant.jpg", IsAvailable = true },
                new MenuItem { Name = "Granola Bowl", Description = "House granola, Greek yogurt, seasonal fruit, toasted seeds and honey.", Price = 8.50m, Category = "Breakfast", Badge = "V", ImageUrl = img + "granola-bowl.jpg", IsAvailable = true },
                new MenuItem { Name = "Steel-Cut Oats", Description = "Slow-cooked with cream, brown sugar, poached pear and walnuts.", Price = 7.00m, Category = "Breakfast", Badge = "GF", ImageUrl = img + "steel-cut-oats.jpg", IsAvailable = true },
                new MenuItem { Name = "Quiche of the Day", Description = "Warm slice with a side salad of greens and pickles. Ask your barista.", Price = 8.50m, Category = "Breakfast", ImageUrl = img + "quiche.jpg", IsAvailable = true },

                // Pastries & Bakery
                new MenuItem { Name = "Butter Croissant", Description = "Seventy-two hour laminated dough, shattering crust, honeyed crumb.", Price = 3.75m, Category = "Pastries & Bakery", Badge = "V", ImageUrl = "~/Content/images/croissant.jpg", IsAvailable = true },
                new MenuItem { Name = "Almond Croissant", Description = "Twice-baked with frangipane, toasted almonds, snowfall of icing sugar.", Price = 4.50m, Category = "Pastries & Bakery", Badge = "Popular", ImageUrl = img + "almond-croissant.jpg", IsAvailable = true },
                new MenuItem { Name = "Cinnamon Roll", Description = "Overnight brioche roll, cream-cheese glaze, extra cinnamon butter.", Price = 4.25m, Category = "Pastries & Bakery", Badge = "V", ImageUrl = img + "cinnamon-roll.jpg", IsAvailable = true },
                new MenuItem { Name = "Blueberry Scone", Description = "Cream scone studded with wild blueberries, lemon glaze.", Price = 3.50m, Category = "Pastries & Bakery", Badge = "V", ImageUrl = img + "blueberry-scone.jpg", IsAvailable = true },
                new MenuItem { Name = "Banana Bread Slice", Description = "Toasted with salted butter on request, as it should be.", Price = 3.75m, Category = "Pastries & Bakery", Badge = "V", ImageUrl = img + "banana-bread.jpg", IsAvailable = true },
                new MenuItem { Name = "Coffee Cake Slice", Description = "Brown sugar crumb, cinnamon swirl, made to pair with batch brew.", Price = 3.75m, Category = "Pastries & Bakery", Badge = "V", ImageUrl = img + "coffee-cake.jpg", IsAvailable = true },
                new MenuItem { Name = "Chocolate Chip Cookie", Description = "Brown-butter dough, flaky salt, still warm at 8am.", Price = 2.75m, Category = "Pastries & Bakery", Badge = "V", ImageUrl = img + "choc-chip-cookie.jpg", IsAvailable = true },
                new MenuItem { Name = "Bagel & Cream Cheese", Description = "Sesame or everything, whipped cream cheese, dill pickle on the side.", Price = 4.00m, Category = "Pastries & Bakery", Badge = "V", ImageUrl = img + "bagel.jpg", IsAvailable = true }
            };

            foreach (var item in items)
            {
                context.MenuItems.Add(item);
            }

            base.Seed(context);
        }
    }
}

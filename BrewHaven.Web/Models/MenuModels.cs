using System.Collections.Generic;

namespace BrewHaven.Web.Models
{
    /// <summary>
    /// Menu page model: the (already filtered and sorted) items plus the
    /// filter state echoed back for the toolbar. IsFiltered switches the
    /// page from the grouped category sections to a single results grid.
    /// </summary>
    public class MenuViewModel
    {
        public List<MenuItem> Items { get; set; }
        public string Q { get; set; }
        public string Cat { get; set; }
        public string Sort { get; set; }
        public bool IsFiltered { get; set; }
        public List<string> Categories { get; set; }
    }

    /// <summary>
    /// Card shown in the "Featured this week" section of the home page.
    /// (The menu itself now lives in the database — see StoreModels.cs.)
    /// </summary>
    public class FeaturedItem
    {
        public string Image { get; set; }   // app-relative path, e.g. ~/Content/images/latte.jpg
        public string Name { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public string Tag { get; set; }
    }

    /// <summary>
    /// Strongly typed model for the contact form. Server-side validation runs
    /// through DataAnnotations when the form posts.
    /// </summary>
    public class ContactForm
    {
        public ContactForm()
        {
            Topic = "General question";
        }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please tell us your name.")]
        [System.ComponentModel.DataAnnotations.StringLength(80)]
        public string Name { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please add your email so we can reply.")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "That email address does not look right.")]
        [System.ComponentModel.DataAnnotations.StringLength(120)]
        public string Email { get; set; }

        [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "That phone number does not look right.")]
        public string Phone { get; set; }

        public string Topic { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please add a short message.")]
        [System.ComponentModel.DataAnnotations.StringLength(1500, ErrorMessage = "Please keep the message under 1500 characters.")]
        public string Message { get; set; }
    }

    /// <summary>In-memory featured cards for the home page only.</summary>
    public static class MenuData
    {
        private static readonly List<FeaturedItem> _featured = new List<FeaturedItem>
        {
            new FeaturedItem
            {
                Image = "~/Content/images/latte.jpg",
                Name = "Honey Oat Latte",
                Description = "House honey-vanilla syrup and steamed oat milk over a double shot.",
                Price = "$5.25",
                Tag = "Barista pick"
            },
            new FeaturedItem
            {
                Image = "~/Content/images/croissant.jpg",
                Name = "Almond Croissant",
                Description = "Twice-baked, frangipane-filled, finished with toasted almonds.",
                Price = "$4.50",
                Tag = "Fresh baked"
            },
            new FeaturedItem
            {
                Image = "~/Content/images/breakfast.jpg",
                Name = "The Haven Breakfast",
                Description = "Soft eggs, smashed avocado, sourdough and roasted tomatoes.",
                Price = "$11.50",
                Tag = "Most loved"
            }
        };

        public static IList<FeaturedItem> GetFeaturedItems()
        {
            return _featured;
        }
    }
}

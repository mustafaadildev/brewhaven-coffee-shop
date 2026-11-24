# Brew Haven — Coffee Shop Website with Ordering (ASP.NET MVC 5 · .NET Framework 4.8)

A coffee shop website built as a classic **ASP.NET MVC 5** application targeting **.NET Framework 4.8**. It started as a front end and now includes **menu CRUD and order placement backed by a real database** (Entity Framework 6 + LocalDB). Open the solution in Visual Studio on Windows, press **F5**, and everything runs under IIS Express — the database file is created and seeded automatically on first run.

> **Note:** `.NET Framework` / Visual Studio only runs on Windows, so this repository ships the full solution source. A pixel-accurate static preview (`../BrewHaven-Preview.html`, single self-contained file) and screenshots (`../screenshots/`) are included so you can see the design immediately.

---

## 1. What's inside

### Public pages

| Page | Route | Highlights |
| --- | --- | --- |
| **Home** | `/` | Dark premium hero, **Popular drinks straight from the database** (live Add buttons), why-us tiles, stats band, roastery story, testimonials, hours & map, CTA band |
| **Menu** | `/Home/Menu` | Items come **from the database**, shown as **photo cards** (large photo, badge chip, description, price, **Add** button); **server-side search, category filter chips and price/name sorting** — any filter switches to a results grid with counts and a friendly empty state; grouped sections in featured view; unavailable items show "Sold out"; wrong photo paths fall back to placeholder art |
| **Your order (cart)** | `/Cart` | Session cart: change quantities, add per-item **changes/notes** ("oat milk, extra hot"), remove lines, clear order, live total |
| **Checkout** | `/Orders/Checkout` | Customer details form (name, **email**, phone, order-level notes) with server-side validation; total recalculated server-side |
| **Confirmation** | `/Orders/Confirmation/{id}` | Order number, every line with its notes, total, placed-at stamp |
| **About / Contact** | `/Home/About`, `/Home/Contact` | Story, team, contact form with validation |

### Staff (admin) pages

| Page | Route | What it does |
| --- | --- | --- |
| **Dashboard** | `/Admin/Dashboard` | Sales analytics: today's + all-time revenue/orders, **hand-drawn SVG revenue chart** (last 14 days), best sellers, order-status split, latest 5 orders |
| **Manage menu** | `/MenuItems` | List every item → **Create**, **Edit**, **Delete** (full CRUD). Untick "Available" to show a sold-out badge instead of deleting |
| **All orders** | `/Orders` | Every placed order, newest first, with status badges and a **Complete** button |
| **Order details** | `/Orders/Details/{id}` | Full item list incl. per-item changes + customer contact block |

Admin pages are **hidden from customers** — the footer *Staff* section was removed. Open them via direct URL when you need them: `/Admin/Dashboard`, `/MenuItems` and `/Orders`. They sit behind the owner passcode gate — see **Security** below.

### Data layer

- **Entity Framework 6.4.4** (NuGet) over **LocalDB** `(localdb)\MSSQLLocalDB` (ships with Visual Studio).
- Database: named database **BrewHaven** inside the `(localdb)\MSSQLLocalDB` instance, connection string `BrewHavenContext` in `Web.config`. No `.mdf` attach — moving or re-extracting the project folder can't break the connection.
- Tables: `MenuItems` (Name, Description, Price, Category, Badge, **ImageUrl**, IsAvailable), `Orders` (customer info, notes, status, placed-at), `OrderItems` (line snapshots: name, unit price, quantity, per-item notes).
- On first run `BrewHavenDbInitializer` creates and seeds the DB with all 38 menu items (each with a photo).
- **Model changes reset the DB**: the initializer is `DropCreateDatabaseIfModelChanges`, so after adding a column (like `ImageUrl`) an existing database is dropped, re-created and reseeded — locally saved test orders are cleared with it.
- **Prices are never taken from the client** — checkout re-reads them from the database, and each order line stores a name/price snapshot so later menu edits don't rewrite history.

```
BrewHaven.CoffeeShop/
├── BrewHaven.CoffeeShop.sln
└── BrewHaven.Web/
    ├── BrewHaven.Web.csproj          (.NET Framework 4.8 web project)
    ├── Global.asax(.cs)              app start → DB initializer + filters + routes
    ├── Web.config / Views/Web.config (connection string + EF6 section)
    ├── packages.config               (Mvc 5.2.9 stack + EntityFramework 6.4.4)
    ├── Controllers/
    │   ├── HomeController.cs         Index · Menu (from DB) · About · Contact
    │   ├── CartController.cs         Add / Update / Remove / Clear (session cart)
    │   ├── OrdersController.cs       Checkout GET/POST · Confirmation · Index · Details · Complete
    │   └── MenuItemsController.cs    CRUD for menu items
    ├── Models/
    │   ├── StoreModels.cs            MenuItem, Order, OrderItem, CartLine,
    │   │                             CheckoutForm, BrewHavenContext, seed initializer
    │   └── MenuModels.cs             ContactForm (legacy FeaturedItem kept for reference)
    ├── Views/
    │   ├── Shared/_Layout.cshtml     nav now has "Your order"; footer has NO admin links (customers can't see staff pages) + flash notes
    │   ├── Home/, About, Contact     Index = dark premium landing with DB-driven popular drinks; Menu = photo cards
    │   ├── Cart/Index.cshtml
    │   ├── Orders/{Checkout,Confirmation,Index,Details}.cshtml
    │   └── MenuItems/{Index,Create,Edit,Delete,_MenuItemFields}.cshtml
    ├── Content/site.css              design system + photo cards + dark landing styles
    ├── Content/images/*.jpg          6 scene photos + images/menu/ with 36 item photos
    └── Scripts/site.js               nav toggle, header shadow, reveal-on-scroll
```

## 2. Run it (Windows + Visual Studio)

1. Install **Visual Studio 2019 or 2022** with the *ASP.NET and web development* workload.
2. Open `BrewHaven.CoffeeShop.sln`.
3. Let NuGet restore the packages (**five** now, including EntityFramework 6.4.4 — VS restores on build, or right-click the solution → *Restore NuGet Packages*).
4. Press **F5**. The site opens at `http://localhost:52341/`.
5. First visit to the Menu page takes a few extra seconds — that's EF creating and seeding the **BrewHaven** database in the LocalDB instance.

**Try the full flow:** Menu → *Add* on a few items → *Your order* (set quantities, type changes like "oat milk, extra hot", Save) → *Place the order* → fill name + email → *Place the order* → confirmation page with your order number. Staff pages open by direct URL: `/Orders` to see it saved, `/MenuItems` to edit items.

**Reset the database:** stop the site and run again — the initializer drops and reseeds whenever the model changes. For a full wipe, run `sqllocaldb delete MSSQLLocalDB` in a command prompt (the throwaway LocalDB instance recreates itself on the next run), then press F5.

**Troubleshooting — "Cannot attach the file … as database 'BrewHaven'":** this happened when the same database name was attached from an older copy of the project in a different folder. This version stores the database **inside** the LocalDB instance (`Initial Catalog=BrewHaven`, no file attach), so it can't recur. If a stale database is still attached from an old copy, run `sqllocaldb stop MSSQLLocalDB` then `sqllocaldb start MSSQLLocalDB` once and press F5 again.

## 3. Customizing

- **Seed data:** `Models/StoreModels.cs` → `BrewHavenDbInitializer.Seed`.
- **Categories:** fixed list in `Models/StoreModels.cs` → `Categories.Order` (also drives the menu's anchor nav).
- **Email:** the customer's email is stored on the order. No SMTP is configured — when you want real confirmation mails, send from `OrdersController.Checkout` right after `SaveChanges()`.
- **Images / colors / fonts:** menu card photos live in `Content/images/menu/` — set one on an item via the **Photo** field in Create/Edit (app path, e.g. `~/Content/images/menu/latte.jpg`); you can drop your own image into that folder. Colors/fonts are CSS variables at the top of `Content/site.css`.
- **Staff pages:** gated behind the owner passcode (`Security/AdminOnlyAttribute.cs` → redirects to `/Admin/Unlock`). See **Security** below.

## 4. Design decisions

- **No Bootstrap / jQuery**: hand-written CSS + vanilla JS, same as before; new cart/admin styles live at the bottom of `site.css`.
- **Server-rendered Razor everywhere**: cart works without JavaScript (plain forms + per-row `form` attributes for the editable inputs).
- **C# 5 syntax only** in controllers/models: no Roslyn package required.
- **Session cart, DB-backed checkout**: cart survives page turns in the session; only at checkout does it become a persisted `Order` with validated data and DB prices.
- Menu/brand details are realistic **sample data** — swap in the real shop's facts.

## 5. Security

Practical hardening that fits a coffee shop (no customer accounts — the only gate is the owner's passcode):

- **Owner passcode gate** — `/MenuItems` (manage menu), `/Orders` (order list) and `/Orders/Complete` redirect to `/Admin/Unlock` until unlocked. Default demo passcode: **`brewhaven2026`**.
  - Stored as a **PBKDF2-SHA256 hash** (100,000 iterations) in `Web.config` → `AdminPasscodeHash`, never plain text, compared in constant time (`Security/AdminPasscode.cs`).
  - **Lockout:** five wrong attempts locks the form for 15 minutes (per session).
  - Closing the browser re-locks automatically (session flag, nothing persisted).
  - **Change the passcode:** run this in PowerShell, paste the output into `Web.config` → `AdminPasscodeHash`:
    ```powershell
    $s = [guid]::NewGuid().ToString("N")
    $k = [Convert]::ToBase64String((New-Object System.Security.Cryptography.Rfc2898DeriveBytes("YOUR-NEW-PASSCODE", [Text.Encoding]::UTF8.GetBytes($s), 100000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)).GetBytes(32))
    "PBKDF2|100000|$s|$k"
    ```
- **CSRF protection** — every POST form carries an anti-forgery token and every POST action validates it (`[ValidateAntiForgeryToken]`).
- **Server-side validation** — all form models validate through DataAnnotations (required fields, string lengths, price range 0.01–999, email/phone format, quantity 1–99), plus client-side unobtrusive validation.
- **Friendly error pages** — `customErrors mode="On"` renders branded pages (`Views/Shared/Error.cshtml`, `NotFound.cshtml`) instead of yellow stack traces. Flip to `mode="Off"` only while debugging.
- **Security headers** — `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy` on every response.
- **Cookies & session** — HttpOnly cookies (JavaScript can't read them); idle sessions expire after 20 minutes.
- **Prices are server-side** — the cart never trusts client prices; totals are computed from the database at checkout.
- **Known demo trade-offs:** `/Orders/Details/{id}` and `/Orders/Confirmation/{id}` stay open because the customer confirmation flow links to them (guessable IDs — in production, gate them and e-mail signed links); deploy over HTTPS and add `requireSSL="true"` to `httpCookies` for real traffic.

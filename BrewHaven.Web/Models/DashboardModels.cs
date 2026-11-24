using System;
using System.Collections.Generic;

namespace BrewHaven.Web.Models
{
    /// <summary>
    /// Everything the /Admin/Dashboard page shows: headline stats, a 14-day
    /// revenue series, best sellers, the order-status split and the most
    /// recent orders. Computed from the database in AdminController.Dashboard
    /// — never from client input.
    /// </summary>
    public class DashboardViewModel
    {
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrdersToday { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal AvgOrderValue { get; set; }
        public int ItemsOnMenu { get; set; }
        public int ItemsAvailable { get; set; }
        public int PlacedCount { get; set; }
        public int CompletedCount { get; set; }
        public List<DailyPoint> Days { get; set; }
        public List<TopItemLine> TopItems { get; set; }
        public List<Order> RecentOrders { get; set; }
    }

    /// <summary>One bar in the 14-day revenue chart (quiet days included as zero).</summary>
    public class DailyPoint
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
    }

    /// <summary>One row in the best-sellers list.</summary>
    public class TopItemLine
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }
}

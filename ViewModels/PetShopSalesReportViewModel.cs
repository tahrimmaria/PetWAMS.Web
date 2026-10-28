using System;
using System.Collections.Generic;

namespace PetWAMS.Web.ViewModels
{
    public class PetShopSalesReportViewModel
    {
        public DateTime GeneratedAt { get; set; }

        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public int WeeklyOrderCount { get; set; }

        public int WeeklyUnitsSold { get; set; }

        public decimal WeeklyRevenue { get; set; }

        public List<ProductSalesReportItemViewModel>
            WeeklyProductSales
        { get; set; }
            = new List<ProductSalesReportItemViewModel>();

        public DateTime MonthStart { get; set; }

        public DateTime MonthEnd { get; set; }

        public int MonthlyOrderCount { get; set; }

        public int MonthlyUnitsSold { get; set; }

        public decimal MonthlyRevenue { get; set; }

        public List<ProductSalesReportItemViewModel>
            MonthlyProductSales
        { get; set; }
            = new List<ProductSalesReportItemViewModel>();
    }

    public class ProductSalesReportItemViewModel
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; }
            = string.Empty;

        public string ProductCategory { get; set; }
            = string.Empty;

        public string PetType { get; set; }
            = string.Empty;

        public int QuantitySold { get; set; }

        public decimal Revenue { get; set; }

        public int CurrentStock { get; set; }

        public bool HasSales
        {
            get
            {
                return QuantitySold > 0;
            }
        }
    }
}
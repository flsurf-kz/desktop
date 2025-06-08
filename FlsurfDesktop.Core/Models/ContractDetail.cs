using System;

namespace FlsurfDesktop.Core.Models
{
    public class ContractDetail
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal RemainingBudget { get; set; }
        public decimal CostPerHour { get; set; }

        // Для клиента:
        public decimal TotalSpent { get; set; }
        public string JobTitle { get; set; } = "";

        public string RemainingBudgetStr => $"Remaining: {RemainingBudget:C}";
        public string TotalSpentStr => $"Spent: {TotalSpent:C}";
    }
}

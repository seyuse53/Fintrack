namespace FinTrack.Core.Models
{
    public class BudgetLimit
    {
        public int Id { get; set; }

        // Foreign Key
        public int CategoryId { get; set; }

        // Navigation Property
        public Category? Category { get; set; }

        /// <summary>Monthly spending limit in TRY.</summary>
        public decimal MonthlyLimit { get; set; }

        /// <summary>Day of month the budget period starts (1–28). Defaults to 1.</summary>
        public int BudgetStartDay { get; set; } = 1;
    }
}

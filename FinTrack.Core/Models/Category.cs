using System.Collections.Generic;

namespace FinTrack.Core.Models
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public TransactionType Type { get; set; } // Limits category to income or expense
        
        public string FullDisplayName => ParentCategory != null ? $"{ParentCategory.Name} > {Name}" : Name;
        
        // Hierarchy support
        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();

        // Navigation property for related transactions
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

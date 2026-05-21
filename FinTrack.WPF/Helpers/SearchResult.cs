using System.Collections.Generic;

namespace FinTrack.WPF.Helpers
{
    /// <summary>
    /// Represents a single search result item displayed in the Global Search dropdown.
    /// </summary>
    public class SearchResult
    {
        /// <summary>Emoji icon representing the result type ("💸", "🏦", "💳", "📈")</summary>
        public string Icon { get; set; } = "";

        /// <summary>Primary display text (e.g. "Market alışverişi", "Garanti - Vadesiz TL")</summary>
        public string Title { get; set; } = "";

        /// <summary>Secondary info line (e.g. "Market & Mutfak · 12 Mar 2026")</summary>
        public string Subtitle { get; set; } = "";

        /// <summary>Formatted amount string if applicable (e.g. "-₺450,00")</summary>
        public string? Amount { get; set; }

        /// <summary>WPF color string for the amount ("#C62828" red, "#27AE60" green)</summary>
        public string AmountColor { get; set; } = "#333333";

        /// <summary>Type of entity: "Transaction", "BankAccount", "CreditCard", "Investment"</summary>
        public string ResultType { get; set; } = "";

        /// <summary>Database ID used to open the detail/edit window</summary>
        public int EntityId { get; set; }
    }

    /// <summary>
    /// Groups search results by category for display in the dropdown.
    /// </summary>
    public class SearchResultGroup
    {
        /// <summary>Group header (e.g. "💸 İŞLEMLER (5)")</summary>
        public string GroupTitle { get; set; } = "";

        /// <summary>Results belonging to this group</summary>
        public List<SearchResult> Results { get; set; } = new();
    }
}

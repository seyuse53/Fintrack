namespace FinTrack.Core.Models
{
    public class SymbolItem
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public string DisplayText => string.IsNullOrWhiteSpace(Name) ? Symbol : $"{Symbol} - {Name}";

        public override string ToString()
        {
            return DisplayText;
        }
    }
}

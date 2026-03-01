namespace FinTrack.Core.Models
{
    public class CreditCardAccount
    {
        public int Id { get; set; }

        /// <summary>Bank name, e.g. "İş Bankası", "Garanti"</summary>
        public required string BankName { get; set; }

        /// <summary>Card label, e.g. "Asıl Kart", "Sanal Kart", "Ek Kart"</summary>
        public required string CardLabel { get; set; }

        /// <summary>When false the card is hidden from the payment method picker.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>The day of the month the statement is generated (e.g., 15).</summary>
        public int StatementDay { get; set; } = 1;

        /// <summary>The day of the month the payment is due (e.g., 25).</summary>
        public int PaymentDueDay { get; set; } = 15;

        // Parent/Child relationship for consolidated billing
        public int? ParentCardId { get; set; }
        public CreditCardAccount? ParentCard { get; set; }
        public System.Collections.Generic.ICollection<CreditCardAccount> LinkedCards { get; set; } = new System.Collections.Generic.List<CreditCardAccount>();

        /// <summary>
        /// Calculates the start and end dates of the statement period that includes the given date.
        /// </summary>
        public (System.DateTime Start, System.DateTime End) GetStatementPeriod(System.DateTime forDate)
        {
            int validStatementDay = System.Math.Max(1, StatementDay);
            
            int safeStatementDay = System.Math.Min(validStatementDay, System.DateTime.DaysInMonth(forDate.Year, forDate.Month));
            System.DateTime currentMonthEnd = new System.DateTime(forDate.Year, forDate.Month, safeStatementDay, 23, 59, 59);

            System.DateTime periodEnd;
            if (forDate.Date <= currentMonthEnd.Date)
            {
                periodEnd = currentMonthEnd;
            }
            else
            {
                var nextMonth = forDate.AddMonths(1);
                int nextSafeDay = System.Math.Min(validStatementDay, System.DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                periodEnd = new System.DateTime(nextMonth.Year, nextMonth.Month, nextSafeDay, 23, 59, 59);
            }

            var prevMonth = periodEnd.AddMonths(-1);
            int prevSafeDay = System.Math.Min(validStatementDay, System.DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month));
            
            System.DateTime previousEnd = new System.DateTime(prevMonth.Year, prevMonth.Month, prevSafeDay, 23, 59, 59);
            System.DateTime periodStart = previousEnd.AddSeconds(1); // 00:00:00 of the next day
            
            return (periodStart, periodEnd);
        }
    }
}

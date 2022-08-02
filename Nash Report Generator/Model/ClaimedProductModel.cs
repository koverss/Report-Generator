using SQLite;

namespace Nash_Report_Generator.Model
{
    public class ClaimedProductModel
    {
        [Indexed(Name = "CompositeKey", Order = 1, Unique = true)]
        public string Code { get; set; }

        [Indexed(Name = "CompositeKey", Order = 2, Unique = true)]
        public int Quantity { get; set; }

        [Indexed(Name = "CompositeKey", Order = 3, Unique = true)]
        public string CustCode { get; set; }

        [Indexed(Name = "CompositeKey", Order = 4, Unique = true)]
        public string ClaimDate { get; set; }

        //[Indexed(Name = "CompositeKey", Order = 5, Unique = true)]
        public int Reason { get; set; }

        //[Indexed(Name = "CompositeKey", Order = 6, Unique = true)]
        public string Description { get; set; }

        [Indexed(Name = "CompositeKey", Order = 5, Unique = true)]
        public string RefNumber { get; set; }
    }
}
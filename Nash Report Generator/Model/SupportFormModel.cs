using System.Collections.Generic;

namespace Nash_Report_Generator.Model
{
    public class SupportFormModel
    {
        public string RefNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }
        public string Date { get; set; }
        public string Address { get; set; }

        public List<string> ProductCodes { get; set; }
        public List<string> ProductNames { get; set; }
        public List<string> Quantities { get; set; }
        public List<string> IssueDesc { get; set; }
        public List<string> ReturnReason { get; set; }
        public List<string> POR { get; set; }
    }
}
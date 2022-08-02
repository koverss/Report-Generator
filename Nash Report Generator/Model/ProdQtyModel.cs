using SQLite;

namespace Nash_Report_Generator.Model
{
    public class ProdQtyModel
    {
        [PrimaryKey]
        public string ProdCode { get; set; }

        public int ProdQty { get; set; }
    }
}
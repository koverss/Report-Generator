using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Nash_Report_Generator.Model
{
    public static class DataGridContent
    {
        public static List<ClaimedProductModel> PrepareDataTableContent(List<SupportFormModel> listOfForms)
        {
            List<ClaimedProductModel> dataGridSource = new List<ClaimedProductModel>();

            foreach (var form in listOfForms)
            {
                for (var i = 0; i < form.ProductCodes.Count; i++)
                {
                    dataGridSource.Add(new ClaimedProductModel
                    {
                        Code = form.ProductCodes[i],
                        Quantity = int.Parse(form.Quantities[i]),
                        CustCode = form.CustomerCode,
                        Reason = int.TryParse(form.ReturnReason[i], out int outParseReturnResult) ? outParseReturnResult : -1,
                        Description = form.IssueDesc[i],
                        ClaimDate = form.Date,
                        RefNumber = form.RefNumber
                    });
                }
            }

            return dataGridSource;
        }

        public static List<ProdQtyModel> PrepareProdQtyList(List<ClaimedProductModel> listOfClaims)
        {
            List<ProdQtyModel> prodQtyList = new List<ProdQtyModel>();
            List<string> productCodes = new List<string>();
            List<string> uniqueProductCodes = new List<string>();
            int qtySum = 0;

            foreach (var claim in listOfClaims)
            {
                productCodes.Add(claim.Code.ToUpper());
            }

            uniqueProductCodes = productCodes.Distinct().ToList();

            foreach (var code in uniqueProductCodes)
            {
                qtySum = 0;
                var matchingClaims = listOfClaims.Where(x => x.Code == code).ToList();
                foreach (var cl in matchingClaims)
                {
                    qtySum += cl.Quantity;
                }
                prodQtyList.Add(new ProdQtyModel() { ProdCode = code, ProdQty = qtySum });
            }

            return prodQtyList;
        }
    }
}
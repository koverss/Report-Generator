using Microsoft.Office.Interop.Excel;
using Nash_Report_Generator.ProcessingData;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nash_Report_Generator.Model
{
    public class ReportGatherer
    {
        //17 for pl 21 for uk 16 w nowej
        public static List<string> fileList;

        private static int startingIndexForCompanyForms = 17;

        public static async Task<List<CustAndClaimNumberModel>> ReturnMostActiveCustomers(List<ClaimedProductModel> listOfClaims)
        {
            List<string> listOfCustomers = new List<string>();
            List<string> distinctListOfCustomers = new List<string>();
            List<CustAndClaimNumberModel> resultDistinct = new List<CustAndClaimNumberModel>();

            await new TaskFactory().StartNew(() =>
            {
                foreach (var claim in listOfClaims)
                {
                    listOfCustomers.Add(claim.CustCode);
                }

                distinctListOfCustomers = listOfCustomers.Distinct().ToList();

                foreach (var cust in distinctListOfCustomers)
                {
                    resultDistinct.Add(new CustAndClaimNumberModel() { Cust = cust, ClaimNumber = listOfClaims.Where(x => x.CustCode == cust).ToList().Sum(x => x.Quantity) });
                }

                _ = resultDistinct.OrderByDescending(x => x.ClaimNumber);
            });
            return resultDistinct;
        }

        internal static async Task<List<SupportFormModel>> GatherDataAsync(string filePath)
        {
            var listOfForms = new List<SupportFormModel>();

            return await new TaskFactory().StartNew(() =>
            {
                var files = ExcelWorker.CollectFileNames(filePath);
                int numberOfSupportForms = files.Count;

                var excel = ExcelWorker.TryOpenExcel(false);

                foreach (var form in files)
                {
                    if (!form.Contains("~$"))
                    {
                        var wb = ExcelWorker.TryOpenForm(excel, form);
                        if (wb != null)
                        {
                            _Worksheet sheet = wb.ActiveSheet;

                            var cell3 = sheet.Cells[3, "B"].value2;
                            var cell4 = sheet.Cells[4, "B"].value2;

                            if (cell3 == null && cell4 == null)
                                wb.Close(0);
                            else
                            {
                                var customer = new SupportFormModel
                                {
                                    CustomerCode = ContentDataValidation.ValidateCellContent(sheet),
                                    Date = File.GetLastWriteTime(form).ToShortDateString().Replace('/','.'),

                                    ProductCodes = PopulateList(sheet, "A"),
                                    ProductNames = PopulateList(sheet, "B"),
                                    Quantities = PopulatQuantitiesList(sheet, "C"),
                                    IssueDesc = PopulateList(sheet, "D"),
                                    ReturnReason = PopulateList(sheet, "E"),
                                    RefNumber = Path.GetFileNameWithoutExtension(form)
                                };

                                listOfForms.Add(customer);
                                wb.Close(0);
                            }
                        }
                    }
                }
                excel.Quit();

                return listOfForms;
            });
        }

        private static List<string> PopulateList(_Worksheet sh, string startingCellCol)
        {
            var i = 0;

            var cell3 = sh.Cells[16, "A"].value2;

            if (cell3 == null)
                startingIndexForCompanyForms = 17;
            else
            {
                if (cell3.ToString().Trim().Length > 5)
                {
                    startingIndexForCompanyForms = 17;
                }
                else
                {
                    startingIndexForCompanyForms = 16;
                }
            }

            List<string> resultList = new List<string>();

            if (startingCellCol == "A")
            {
                while (sh.Cells[startingIndexForCompanyForms + i, startingCellCol].Value2 != null)
                {
                    var rslt = sh.Cells[startingIndexForCompanyForms + i, startingCellCol];
                    string rsltval = rslt.Value2.ToString();

                    if (rsltval.Trim().Length < 6)
                    {
                        if (rsltval == null)
                            rsltval = "x";
                        else if (rsltval.Trim().Length > 6)
                            rsltval = "incorrect data";

                        resultList.Add(rsltval.Trim().ToUpper());
                    }

                    i++;
                }
            }
            else if (startingCellCol == "E")
            {
                while (sh.Cells[startingIndexForCompanyForms + i, "A"].Value2 != null)
                {
                    var rslt = sh.Cells[startingIndexForCompanyForms + i, startingCellCol];

                    if (rslt.Value2 is string && string.IsNullOrEmpty(rslt.Value2) || rslt.Value2 == null)
                        resultList.Add("-1");
                    else
                    {
                        string rsltval = rslt.Value2.ToString();
                        resultList.Add(new string(rsltval.Skip(rsltval.IndexOf(" ") + 1).Take(1).ToArray()));
                    }
                    i++;
                }
            }
            else
            {
                while (sh.Cells[startingIndexForCompanyForms + i, "A"].Value2 != null)
                {
                    var rslt = sh.Cells[startingIndexForCompanyForms + i, startingCellCol];
                    if (rslt.Value2 is string && string.IsNullOrEmpty(rslt.Value2) || rslt.Value2 == null)
                    {
                        resultList.Add("x");
                    }
                    else
                    {
                        resultList.Add(rslt.Value2.ToString());
                    }

                    i++;
                }
            }

            return resultList;
        }

        private static List<string> PopulatQuantitiesList(_Worksheet sh, string startingCellCol)
        {
            List<string> resultList = new List<string>();

            var cell3 = sh.Cells[16, "A"].value2;

            if (cell3 == null)
                startingIndexForCompanyForms = 17;
            else
            {
                if (cell3.ToString().Trim().Length > 5)
                    startingIndexForCompanyForms = 17;
                else
                    startingIndexForCompanyForms = 16;
            }

            var i = 0;

            while (sh.Cells[startingIndexForCompanyForms + i, "A"].Value2 != null)
            {
                if (sh.Cells[startingIndexForCompanyForms + i, startingCellCol].Value2 == null)
                    resultList.Add("0");
                else
                {
                    var rslt = sh.Cells[startingIndexForCompanyForms + i, startingCellCol];
                    var rsltval = rslt.Value2.ToString().ToUpper();
                    string valToAdd = Regex.Replace(rsltval.ToString(), "[^0-9]", "");

                    if (valToAdd.Trim().Length != 0)
                        resultList.Add(Regex.Replace(rsltval.ToString(), "[^0-9]", ""));
                    else
                        resultList.Add("0");
                }
                i++;
            }

            return resultList;
        }
    }
}
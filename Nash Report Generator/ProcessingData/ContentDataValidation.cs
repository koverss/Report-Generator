using GemBox.Spreadsheet;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace Nash_Report_Generator.ProcessingData
{
    public static class ContentDataValidation
    {
        public static string ValidateCellContent(_Worksheet sheet)
        {
            for (int i = 3; i < 8; i++)
            {
                if (sheet.Cells[i, "A"].Value2 != null)
                {
                    if (sheet.Cells[i, "A"].Value2.ToString().Trim().Contains("Account")
                        || sheet.Cells[i, "A"].Value2.ToString().Trim().Contains("Kod"))
                    {
                        return sheet.Cells[i, "B"].Value2.ToString().Trim();
                    }
                }
            }

            if (sheet.Cells[3, "B"].Value2 != null)
            {
                if (sheet.Cells[3, "B"].Value2.ToString() != string.Empty)
                    return sheet.Cells[3, "B"].Value2.ToString();
                else if ((sheet.Cells[4, "B"].Value2.ToString() != string.Empty))
                    return sheet.Cells[4, "B"].Value2.ToString();
            }
            return "incorrect code";
        }

        public static string ValidateCellContentODS(ExcelCell cell)
        {
            string result = "";
            if (cell.Value != null && cell.Value.ToString() != "")
            {
                result = cell.Value.ToString();
                _ = Marshal.ReleaseComObject(cell);
                return result;
            }

            _ = Marshal.ReleaseComObject(cell);
            return result;
        }
    }
}
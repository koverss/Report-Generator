using GemBox.Spreadsheet;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace Nash_Report_Generator.ProcessingData
{
    public static class ContentDataValidation
    {
        public static string ValidateCellContent(_Worksheet sheet)
        {
            if (sheet.Cells[3, "B"].Value2 != null && sheet.Cells[3, "B"].Value2.ToString() != "")
                return sheet.Cells[3, "B"].Value2.ToString().ToUpper();
            else
                return sheet.Cells[2, "A"] == null ? sheet.Cells[3, "B"].Value2.ToString() : sheet.Cells[4, "B"].Value2.ToString().ToUpper();
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
using GemBox.Spreadsheet;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nash_Report_Generator.Model
{
    public static class ExcelWorker
    {
        private static List<string> fileList;

        public static Application TryOpenExcel(bool isVisible)
        {
            try
            {
                return new Application { Visible = isVisible };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static _Workbook TryOpenForm(Application excel, string filePath)
        {
            try
            {
                return excel.Workbooks.Open(filePath, 0, false, 5);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<string> CollectFileNames(string path)
        {
            fileList = new List<string>();
            fileList = Directory.GetFiles(path, "*.xlsx").ToList();

            return fileList;
        }

        private static void ConvertToXLSX(List<string> listToChange)
        {
            SpreadsheetInfo.SetLicense("FREE-LIMITED-KEY");
            var excel = TryOpenExcel(false);
            _Workbook wb;

            foreach (var fl in listToChange)
            {
                if (File.Exists(fl))
                {
                    wb = TryOpenForm(excel, fl);
                    if (wb != null)
                    {
                        wb.SaveAs(fl, FileFormat: XlFileFormat.xlOpenXMLWorkbook);
                    }
                }
            }

            excel.Quit();
        }

        public static void WriteToExcel(List<ClaimedProductModel> itemList, string saveLocation, bool openExcel)
        {
            var excelApp = TryOpenExcel(false);
            var workbook = excelApp.Workbooks.Add("");
            var sheet = workbook.ActiveSheet;

            List<ProdQtyModel> prodQty = new List<ProdQtyModel>();

            int rowindex = 2;

            sheet.Cells[1, 1] = "Product Code";
            sheet.Cells[1, 2] = "Quantity";
            sheet.Cells[1, 3] = "Customer Code";
            sheet.Cells[1, 4] = "Date";
            sheet.Cells[1, 5] = "Reason";
            sheet.Cells[1, 6] = "Issue Description";
            sheet.Cells[1, 7] = "ref number";

            foreach(var item in itemList)
            {
                sheet.Cells[rowindex, 1] = item.Code;
                sheet.Cells[rowindex, 2] = item.Quantity;
                sheet.Cells[rowindex, 3] = "'"  + item.CustCode;
                sheet.Cells[rowindex, 4] = item.ClaimDate;
                sheet.Cells[rowindex, 5] = item.Reason;
                sheet.Cells[rowindex, 6] = item.Description;
                sheet.Cells[rowindex, 7] = item.RefNumber;

                if (prodQty.Where(x => x.ProdCode == item.Code).Count() <= 0)
                {
                    prodQty.Add(new ProdQtyModel() { ProdCode = item.Code, ProdQty = item.Quantity });
                }
                else
                {
                    prodQty.Where(x => x.ProdCode == item.Code).ToList().First().ProdQty += item.Quantity;
                }

                rowindex++;              
            }

            _ = workbook.Worksheets.Add();
            sheet = workbook.ActiveSheet;
            sheet.Cells[1, 1] = "Product Code";
            sheet.Cells[1, 2] = "Quantity";
            rowindex = 2;

            foreach (var item in prodQty)
            {
                sheet.Cells[rowindex, 1] = item.ProdCode;
                sheet.Cells[rowindex, 2] = item.ProdQty;

                rowindex++;
            }

            workbook.SaveAs(saveLocation, XlFileFormat.xlWorkbookDefault, Type.Missing, Type.Missing, false, false, XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            workbook.Close(0);
            excelApp.Quit();
        }
    }
}
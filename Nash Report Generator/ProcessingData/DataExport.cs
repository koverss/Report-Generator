using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nash_Report_Generator.Model
{
    public static class DataExport
    {
        private static CommonOpenFileDialog fileDialog;
        private static string saveLocation;

        public static async Task SelectLocationAsync(List<ClaimedProductModel> itemList, bool openExcel)
        {
            fileDialog = new CommonOpenFileDialog();

            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                saveLocation = fileDialog.InitialDirectory + fileDialog.FileName;

                await new TaskFactory().StartNew(() =>
                {
                    ExcelWorker.WriteToExcel(itemList, saveLocation, openExcel);
                    if (openExcel)
                    {
                        var excel = ExcelWorker.TryOpenExcel(true);
                        ExcelWorker.TryOpenForm(excel, saveLocation);
                    }
                });
            }
        }
    }
}
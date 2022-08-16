using Microsoft.WindowsAPICodePack.Dialogs;
using Nash_Report_Generator.Model;
using Nash_Report_Generator.ProcessingData;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Nash_Report_Generator
{
    public partial class MainWindow : Window
    {
        public string reportFormsLocation = "";
        private string selectedDBstring;
        private List<SupportFormModel> listOfForms;
        private List<ClaimedProductModel> dbListAll;
        private List<ProdQtyModel> dbListProdQty;
        public int listSelector;
        public int progressStatus = 0;
        public bool ignoreTextBox;
        private List<ClaimedProductModel> itemsToUpdate = new List<ClaimedProductModel>();
        private bool blockEditHandler = false;
        private bool blockCheckBtnHandler = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Btn_SelectReportsContainer_Btn_Click(object sender, RoutedEventArgs e)
        {
            tb_NoResults.Visibility = Visibility.Hidden;
            var fileDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                reportFormsLocation = fileDialog.FileName;
                dataGrid.Visibility = Visibility.Hidden;
                EnableButtonsWhileProcessing(false);

                if (Directory.Exists(reportFormsLocation))
                {
                    listOfForms = await ReportGatherer.GatherDataAsync(reportFormsLocation);
                }
                else
                {
                    _ = MessageBox.Show("Path is incorrect"); //should never reach here
                }

                EnableButtonsWhileProcessing(true);
                dataGrid.Visibility = Visibility.Visible;
            }
            _ = Focus();

            if (listOfForms != null)
            {
                if (listOfForms.Count != 0)
                {
                    ImgCheck.Visibility = Visibility.Visible;
                }
            }
        }

        private async void Btn_ProcessData_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (listOfForms != null)
            {
                try
                {
                    RestoreDefaultState();
                    tb_NoResults.Visibility = Visibility.Hidden;
                    EnableButtonsWhileProcessing(false);
                    //spinner2.Visibility = Visibility.Visible;
                    await new TaskFactory().StartNew(async () =>
                    {
                        dbListAll = new List<ClaimedProductModel>();

                        dbListAll = DataGridContent.PrepareDataTableContent(listOfForms);

                        using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
                        {
                            _ = connection.CreateTable<ClaimedProductModel>();

                            foreach (var claim in dbListAll)
                            {
                                try
                                {
                                    _ = connection.InsertOrReplace(claim);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Insert db\n" + ex.Message);
                                }
                            }
                        }

                        dbListProdQty = new List<ProdQtyModel>();
                        dbListProdQty = DataGridContent.PrepareProdQtyList(dbListAll);

                        using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
                        {
                            _ = connection.CreateTable<ProdQtyModel>();

                            foreach (var prod in dbListProdQty)
                            {
                                try
                                {
                                    _ = connection.InsertOrReplace(prod);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("Insert prodqty db\n" + ex.Message);
                                }
                            }
                        }
                        await ReadDbAsync();
                    });
                    dataGrid.ItemsSource = dbListAll.OrderBy(x => x.RefNumber).ToList();

                    await FillInfoLabelsAsync();
                    EnableButtonsWhileProcessing(true);
                    ImgCheck.Visibility = Visibility.Hidden;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Btn_ProcessData_ClickAsync\n" + ex.Message);
                }
            }
            else
            {
                _ = MessageBox.Show("No claims to process, please specify path");
            }
        }

        public async Task FillInfoLabelsAsync()
        {
            List<CustAndClaimNumberModel> topResults = new List<CustAndClaimNumberModel>();
            List<ClaimedProductModel> claimedProds = new List<ClaimedProductModel>();
            List<ProdQtyModel> prodQty = new List<ProdQtyModel>();
            List<string> toLabels = new List<string>();
            List<string> distinctCodes = new List<string>();
            List<string> distinctRefNumbers = new List<string>();
            List<CustAndRefModel> distinctRefsForCust = new List<CustAndRefModel>();
            List<CustAndClaimNumberModel> customersAndNumberOfClaims = new List<CustAndClaimNumberModel>();
            List<DataAndPercentageModel> dataAndPercentageFirst = new List<DataAndPercentageModel>();
            List<DataAndPercentageModel> dataAndPercentageSecond = new List<DataAndPercentageModel>();
            List<DataAndPercentageModel> dataAndPercentageThird = new List<DataAndPercentageModel>();

            //List<ClaimedProductModel> lcpm = dataGrid.ItemsSource as List<ClaimedProductModel>;

            claimedProds = ReturnFilteredItemsSource(dbListAll, false);
            topResults = await ReportGatherer.ReturnMostActiveCustomers(claimedProds);
            topResults = topResults.OrderByDescending(x => x.ClaimNumber).ToList();

            //first table
            double sum = topResults.Sum(x => x.ClaimNumber);

            foreach (var el in topResults)
            {
                dataAndPercentageFirst.Add(new DataAndPercentageModel()
                {
                    Code = el.Cust,
                    Number = el.ClaimNumber,
                    Percentage = Math.Round((el.ClaimNumber * 100 / sum), 2).ToString() + "%"
                });
            }

            dg_ClientsWithMostProducts.ItemsSource = dataAndPercentageFirst;

            //second table
            foreach (var el in claimedProds)
            {
                distinctCodes.Add(el.Code);
                distinctRefNumbers.Add(el.RefNumber);
            }

            distinctCodes = distinctCodes.Distinct().ToList();
            distinctRefNumbers = distinctRefNumbers.Distinct().ToList();

            foreach (var code in distinctCodes)
            {
                prodQty.Add(new ProdQtyModel() { ProdCode = code, ProdQty = claimedProds.Where(x => x.Code == code).Sum(x => x.Quantity) });
            }

            prodQty = prodQty.OrderByDescending(x => x.ProdQty).ToList();
            sum = prodQty.Sum(x => x.ProdQty);
            lbl_secondTotal.Content = sum;

            foreach (var el in prodQty)
            {
                dataAndPercentageSecond.Add(new DataAndPercentageModel()
                {
                    Code = el.ProdCode,
                    Number = el.ProdQty,
                    Percentage = Math.Round((double)el.ProdQty * 100 / sum, 2).ToString() + "%"
                }); ;
            }

            dg_ProductsWithMostClaims.ItemsSource = dataAndPercentageSecond;

            //third table
            foreach (var refNo in distinctRefNumbers)
            {
                distinctRefsForCust.Add(new CustAndRefModel() { Cust = claimedProds.Where(x => x.RefNumber == refNo).ToList().First().CustCode, RefNumber = refNo });
            }

            foreach (var cust in topResults)
            {
                customersAndNumberOfClaims.Add(new CustAndClaimNumberModel() { Cust = cust.Cust, ClaimNumber = distinctRefsForCust.Count(x => x.Cust == cust.Cust) });
            }

            customersAndNumberOfClaims = customersAndNumberOfClaims.OrderByDescending(x => x.ClaimNumber).ToList();

            sum = distinctRefNumbers.Count;
            lbl_thirdTotal.Content = sum;

            foreach (var el in customersAndNumberOfClaims)
            {
                dataAndPercentageThird.Add(new DataAndPercentageModel()
                {
                    Code = el.Cust,
                    Number = el.ClaimNumber,
                    Percentage = Math.Round((double)el.ClaimNumber * 100 / sum).ToString() + "%"
                });
            }

            dg_MostFormsSent.ItemsSource = dataAndPercentageThird;

            //if (dataGrid.Items.Count != 0 && listSelector == 1)
            //{
            //    var c = new CustomHandlers();
            //    c.SortHandler(dataGrid, new DataGridSortingEventArgs(dataGrid.Columns[3]));
            //}

            UpdateLastUpdateDate();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private async void Btn_saveDataClickAsync(object sender, RoutedEventArgs e)
        {
            List<ClaimedProductModel> cpmList = dataGrid.ItemsSource as List<ClaimedProductModel>;
            var resultList = new List<ClaimedProductModel>();
            EnableButtonsWhileProcessing(false);

            await new TaskFactory().StartNew(() =>
            {
                using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
                {
                    _ = connection.CreateTable<ClaimedProductModel>();

                    dbListProdQty = DataGridContent.PrepareProdQtyList(cpmList);
                    foreach (var item in dbListProdQty)
                    {
                        try
                        {
                            var insertResult = connection.InsertOrReplace(item);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("insert save\n\n0" + ex.Message);
                        }
                    }

                    _ = connection.CreateTable<ClaimedProductModel>();

                    if (cpmList.Count != 0)
                    {
                        int i = 0;
                        while (i < cpmList.Count - 1)
                        {
                            var claim = cpmList[i];
                            try
                            {
                                _ = connection.Insert(claim);
                                _ = cpmList.Remove(claim);
                            }
                            catch (Exception ex) //not adding if exists, based on composite key
                            {
                                _ = cpmList.Remove(claim);
                            }
                        }
                    }
                    resultList = connection.Table<ClaimedProductModel>().ToList();
                }
            });
            dataGrid.ItemsSource = ReturnFilteredItemsSource(dbListAll, false).OrderBy(x => x.RefNumber).ToList();

            EnableButtonsWhileProcessing(true);
        }

        private async Task ReadDbAsync()
        {
            await new TaskFactory().StartNew(() =>
            {
                try
                {
                    using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring, SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex)) //, SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache| SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.FullMutex
                    {
                        _ = connection.CreateTable<ClaimedProductModel>();
                        dbListAll = connection.Table<ClaimedProductModel>().ToList();

                        _ = connection.CreateTable<ProdQtyModel>();
                        dbListProdQty = connection.Table<ProdQtyModel>().ToList();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ReadDb insight connection?     " + ex.Message);
                }
            });
        }

        private async void Btn_prodSummary_ClickAsync(object sender, RoutedEventArgs e)
        {
            listSelector = 2;
            EnableButtonsWhileProcessing(false);
            await CheckAndApplyExistingFilters(true);
            List<ProdQtyModel> clm = dataGrid.ItemsSource as List<ProdQtyModel>;
            dataGrid.IsReadOnly = true;
            dataGrid.ItemsSource = clm.OrderByDescending(x => x.ProdQty).ToList();

            EnableButtonsWhileProcessing(true);
        }

        private async void Btn_allData_ClickAsync(object sender, RoutedEventArgs e)
        {
            listSelector = 1;
            dataGrid.IsReadOnly = false;
            EnableButtonsWhileProcessing(false);
            await CheckAndApplyExistingFilters(true);
            EnableButtonsWhileProcessing(true);
        }

        private void RenameColumns()
        {
            if (dataGrid.Columns.Count == 2)
            {
                dataGrid.Columns[0].Header = "Product Code";
                dataGrid.Columns[1].Header = "Quantity";
            }
            else if (dataGrid.Columns.Count != 0)
            {
                dataGrid.Columns[0].Header = "Product Code";
                dataGrid.Columns[1].Header = "Quantity";
                dataGrid.Columns[2].Header = "Customer Code";
                dataGrid.Columns[3].Header = "Claim Date";
                dataGrid.Columns[4].Header = "Reason";
                dataGrid.Columns[5].Header = "Issue description";
                dataGrid.Columns[6].Header = "ref no";
            }
        }

        private async void OnTextChangedTBAsync(object sender, TextChangedEventArgs e)
        {
            EnableButtonsWhileProcessing(false);

            await CheckAndApplyExistingFilters(true, ignoreTextBox);

            EnableButtonsWhileProcessing(true);
            tb_searchBox.Focus();
        }

        private async void Btn_applyFilters_ClickAsync(object sender, RoutedEventArgs e)
        {
            await CheckAndApplyExistingFilters(true);
        }

        private async void Btn_removeFiltersClick(object sender, RoutedEventArgs e)
        {
            cB_reasonSelection.SelectedIndex = 0;
            dtp_fromDate.SelectedDate = new DateTime(2020, 10, 01);
            dtp_toDate.SelectedDate = DateTime.Today;
            tb_searchBox.Text = "";

            EnableButtonsWhileProcessing(false);
            await CheckAndApplyExistingFilters(true);
            EnableButtonsWhileProcessing(true);
        }

        private async Task RemoveFromDb()
        {
            using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
            {
                List<ClaimedProductModel> listOfItemsInDb = dataGrid.SelectedItems.Cast<ClaimedProductModel>().ToList();

                if (listOfItemsInDb.Count != 0)
                {
                    await new TaskFactory().StartNew(() =>
                    {
                        if (listSelector == 1)
                        {
                            foreach (var item in listOfItemsInDb)
                            {
                                _ = connection.Table<ClaimedProductModel>().Where(x =>
                                x.Code == item.Code
                                && x.Quantity == item.Quantity
                                && x.CustCode == item.CustCode
                                && x.Description == item.Description).Delete();
                            }
                        }
                    });

                    dataGrid.ItemsSource = connection.Table<ClaimedProductModel>().OrderBy(x => x.RefNumber).ToList();
                    //dataGrid.ItemsSource = (dataGrid.ItemsSource as List<ClaimedProductModel>).OrderBy(x => x.RefNumber).ToList();
                    await FillInfoLabelsAsync();
                }
            }
        }

        private async void Rbtn_PL_Checked(object sender, RoutedEventArgs e)
        {
            if (blockCheckBtnHandler == false)
            {
                EnableButtonsWhileProcessing(false);
                listOfForms = new List<SupportFormModel>();

                if (dbListAll != null)
                {
                    RestoreDefaultState();
                }

                selectedDBstring = App.dbAllDataPathPL;

                await ReadDbAsync();

                dataGrid.ItemsSource = dbListAll.OrderBy(x => x.RefNumber).ToList();
                await FillInfoLabelsAsync();
                EnableButtonsWhileProcessing(true);
                HideTableIfEmpty(dbListAll);
            }
        }

        private void RestoreDefaultState()
        {
            tb_searchBox.Text = "";
            cB_reasonSelection.SelectedIndex = 0;
            dataGrid.ItemsSource = null;
            dbListAll = new List<ClaimedProductModel>();
            dtp_fromDate.SelectedDate = new DateTime(2020, 10, 01);
            dtp_toDate.SelectedDate = DateTime.Today;
        }

        private async void Rbtn_UK_Checked(object sender, RoutedEventArgs e)
        {
            EnableButtonsWhileProcessing(false);
            listOfForms = new List<SupportFormModel>();
            RestoreDefaultState();

            selectedDBstring = App.dbAllDataPathUK;

            await ReadDbAsync();
            dataGrid.ItemsSource = dbListAll.OrderBy(x => x.RefNumber).ToList();

            await FillInfoLabelsAsync();

            EnableButtonsWhileProcessing(true);

            HideTableIfEmpty(dbListAll);
        }

        private async void Cb_reasonSelection_DropDownClosedAsync(object sender, EventArgs e)
        {
            EnableButtonsWhileProcessing(false);
            await CheckAndApplyExistingFilters(true);
            EnableButtonsWhileProcessing(true);
        }

        private async Task CheckAndApplyExistingFilters(bool changeItemSource, bool ignoreTb)
        {
            List<ClaimedProductModel> listOfClaimedProducts;

            //using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
            //{
            //    listOfClaimedProducts = connection.Table<ClaimedProductModel>().ToList();
            //}
            listOfClaimedProducts = dbListAll;
            //dates range
            List<ClaimedProductModel> listToReturn = new List<ClaimedProductModel>();

            listToReturn = listOfClaimedProducts.Where(x => DateTime.ParseExact(x.ClaimDate, "dd.MM.yyyy", null) >= dtp_fromDate.SelectedDate
                && DateTime.ParseExact(x.ClaimDate, "dd.MM.yyyy", null) <= dtp_toDate.SelectedDate).ToList();

            //reason
            if (cB_reasonSelection.SelectedIndex == 1)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 1).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 2)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 2).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 3)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 3).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 4)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 4).ToList();
            }
            //else default not filtered list

            //search box
            if (ignoreTb == false)
            {
                listToReturn = listToReturn.Where(x => x.Code.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        | x.ClaimDate.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        | x.CustCode.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        | x.RefNumber.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        //| x.Description.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        | x.Reason.ToString().ToUpper().Contains(tb_searchBox.Text.ToUpper())).ToList();
            }

            if (changeItemSource)
            {
                if (listSelector == 1)
                {
                    dataGrid.ItemsSource = listToReturn;
                    var c = new CustomHandlers();
                    c.SortHandler(dataGrid, new DataGridSortingEventArgs(dataGrid.Columns[3]));
                }
                else
                {
                    dataGrid.ItemsSource = (System.Collections.IEnumerable)DataGridContent.PrepareProdQtyList(listToReturn).OrderByDescending(x => x.ProdQty).ToList();
                }
            }

            HideTableIfEmpty(listToReturn);

            await FillInfoLabelsAsync();
        }

        private async Task CheckAndApplyExistingFilters(bool changeItemSource)
        {
            List<ClaimedProductModel> listOfClaimedProducts;

            using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
            {
                listOfClaimedProducts = connection.Table<ClaimedProductModel>().ToList();
            }

            //dates range
            List<ClaimedProductModel> listToReturn = new List<ClaimedProductModel>();

            listToReturn = listOfClaimedProducts.Where(x => DateTime.ParseExact(x.ClaimDate, "dd.MM.yyyy", null) >= dtp_fromDate.SelectedDate
                && DateTime.ParseExact(x.ClaimDate, "dd.MM.yyyy", null) <= dtp_toDate.SelectedDate).ToList();

            //reason
            if (cB_reasonSelection.SelectedIndex == 1)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 1).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 2)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 2).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 3)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 3).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 4)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 4).ToList();
            }
            //else default not filtered list

            //search box
            listToReturn = listToReturn.Where(x => x.Code.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                    | x.ClaimDate.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                    | x.CustCode.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                    //| x.Description.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                    | x.Reason.ToString().ToUpper().Contains(tb_searchBox.Text.ToUpper())).ToList();

            if (changeItemSource)
            {
                if (listSelector == 1)
                {
                    dataGrid.ItemsSource = listToReturn;
                    var c = new CustomHandlers();
                    c.SortHandler(dataGrid, new DataGridSortingEventArgs(dataGrid.Columns[3]));
                }
                else
                {
                    dataGrid.ItemsSource = (System.Collections.IEnumerable)DataGridContent.PrepareProdQtyList(listToReturn).OrderByDescending(x => x.ProdQty).ToList();
                }
            }

            HideTableIfEmpty(listToReturn);

            await FillInfoLabelsAsync();
        }

        private List<ClaimedProductModel> ReturnFilteredItemsSource(List<ClaimedProductModel> items, bool ignoreSearchBox)
        {
            List<ClaimedProductModel> passedList;
            List<ClaimedProductModel> listToReturn = new List<ClaimedProductModel>();

            //using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
            //{
            //    passedList = connection.Table<ClaimedProductModel>().ToList();
            //}

            passedList = items;

            listToReturn = passedList.Where(x => DateTime.ParseExact(x.ClaimDate, "dd.MM.yyyy", null) >= dtp_fromDate.SelectedDate
                && DateTime.ParseExact(x.ClaimDate, "dd.MM.yyyy", null) <= dtp_toDate.SelectedDate).ToList();

            //reason
            if (cB_reasonSelection.SelectedIndex == 1)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 1).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 2)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 2).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 3)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 3).ToList();
            }
            else if (cB_reasonSelection.SelectedIndex == 4)
            {
                listToReturn = listToReturn.Where(x => x.Reason == 4).ToList();
            }
            //else default not filtered list

            //search box
            if (ignoreSearchBox == false)
            {
                listToReturn = listToReturn.Where(x => x.Code.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        | x.ClaimDate.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        | x.CustCode.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        //| x.RefNumber.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        //| x.Description.ToUpper().Contains(tb_searchBox.Text.ToUpper())
                        | x.Reason.ToString().ToUpper().Contains(tb_searchBox.Text.ToUpper())).ToList();
            }

            return listToReturn;
        }

        private async void Btn_exportToExcel_ClickAsync(object sender, RoutedEventArgs e)
        {
            EnableButtonsWhileProcessing(false);
            await DataExport.SelectLocationAsync(ReturnFilteredItemsSource(dbListAll, false), chkB_OpenExportedExcel.IsChecked == true ? true : false);
            EnableButtonsWhileProcessing(true);
        }

        private async void Btn_removeFromDbAndView_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (listSelector == 1)
            {
                //EnableButtonsWhileProcessing(false);
                await RemoveFromDb();
                await FillInfoLabelsAsync();
                //EnableButtonsWhileProcessing(true);
            }
        }

        private void EnableButtonsWhileProcessing(bool f)
        {
            if (f)
            {
                tb_NoResults.Visibility = Visibility.Hidden;
                spinner.Visibility = Visibility.Hidden;
                dataGrid.Visibility = Visibility.Visible;
                tb_WaitInfo.Visibility = Visibility.Hidden;
            }
            else
            {
                spinner.Visibility = Visibility.Visible;
                dataGrid.Visibility = Visibility.Hidden;
                //tb_NoResults.Visibility = Visibility.Visible;
                tb_WaitInfo.Visibility = Visibility.Visible;
            }

            btn_SaveData.IsEnabled = f;
            btn_applyDateFilter.IsEnabled = f;
            btn_removeFilters.IsEnabled = f;
            btn_removeFromDbAndView.IsEnabled = f;
            cB_reasonSelection.IsEnabled = f;
            dtp_fromDate.IsEnabled = f;
            dtp_toDate.IsEnabled = f;
            PL_rbtn.IsEnabled = f;
            Uk_rbtn.IsEnabled = f;
            btn_prodSummary.IsEnabled = f;
            btn_allData.IsEnabled = f;
            tb_reasonSelect.IsEnabled = f;
            tb_searchBox.IsEnabled = f;
            btn_exportToExcel.IsEnabled = f;
            btn_SelectFormsLocation.IsEnabled = f;
            btn_processData.IsEnabled = f;
            btn_saveChanges.IsEnabled = f;
            chkB_OpenExportedExcel.IsEnabled = f;
        }

        private void CheckIfSQLDbPathExists()
        {
            string folderPath = App.folderPath;
            string fullPath = folderPath + "\\SQLite";
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                listSelector = 1;

                selectedDBstring = App.dbAllDataPathPL;
                blockCheckBtnHandler = true;
                PL_rbtn.IsChecked = true;
                blockCheckBtnHandler = false;
                CheckIfSQLDbPathExists();

                dtp_fromDate.SelectedDate = new DateTime(2020, 10, 01);
                dtp_toDate.SelectedDate = DateTime.Today;

                try
                {
                    await ReadDbAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("ReadDB:" + ex.Message);
                }

                await FillInfoLabelsAsync();

                dataGrid.ItemsSource = dbListAll.OrderBy(x => x.RefNumber).ToList();

                HideTableIfEmpty(dbListAll);

                tb_NoResults.Visibility = Visibility.Hidden;

                dg_ClientsWithMostProducts.Height = Double.NaN;
                dg_MostFormsSent.Height = Double.NaN;
                dg_ProductsWithMostClaims.Height = Double.NaN;

                UpdateLastUpdateDate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Win loaded: \n" + ex.Message);
            }
        }

        private void UpdateLastUpdateDate()
        {
            if (File.Exists(App.dbAllDataPathPL))
                tb_LastUpdate.Text = "Last update Poland: \n" + File.GetLastWriteTime(App.dbAllDataPathPL).ToShortDateString();
            if (File.Exists(App.dbAllDataPathUK))
                tb_LastUpdate.Text += "\nLast update UK: \n" + File.GetLastWriteTime(App.dbAllDataPathUK).ToShortDateString();
        }

        private void HideTableIfEmpty<T>(List<T> list)
        {
            if (list != null)
            {
                if (list.Count == 0)
                {
                    dataGrid.Visibility = Visibility.Hidden;
                    tb_NoResults.Visibility = Visibility.Visible;
                }
                else
                {
                    dataGrid.Visibility = Visibility.Visible;
                    tb_NoResults.Visibility = Visibility.Hidden;
                }
            }
        }

        private void DataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            RenameColumns();
        }

        private void DataGrid_doubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listSelector == 2 && dataGrid.SelectedItem != null)
            {
                ignoreTextBox = true;
                tb_searchBox.Text = ((ProdQtyModel)dataGrid.SelectedItem).ProdCode;
            }
            ignoreTextBox = false;
        }

        private void Btn_saveChanges_Click(object sender, RoutedEventArgs e)
        {
            using (SQLiteConnection connection = new SQLiteConnection(selectedDBstring))
            {
                if (listSelector == 1 && itemsToUpdate.Count > 0)
                {
                    List<ClaimedProductModel> currentDbList = connection.Table<ClaimedProductModel>().ToList();

                    foreach (ClaimedProductModel item in itemsToUpdate)
                    {
                        List<ClaimedProductModel> listMatchingElements = currentDbList.Where(x => x.Code == item.Code
                        && x.Quantity == item.Quantity
                        && x.CustCode == item.CustCode
                        && x.ClaimDate == item.ClaimDate
                        && x.RefNumber == item.RefNumber).ToList();

                        ClaimedProductModel itemToBeReplaced;

                        if (listMatchingElements.Count != 0)
                        {
                            itemToBeReplaced = listMatchingElements.First();

                            blockEditHandler = true;

                            connection.Table<ClaimedProductModel>().Where(x => x.Code == item.Code
                            && x.Quantity == item.Quantity
                            && x.CustCode == item.CustCode
                            && x.ClaimDate == item.ClaimDate
                            && x.RefNumber == item.RefNumber).Delete();

                            connection.Insert(item);
                            connection.Table<ClaimedProductModel>().ToList();
                            //dataGrid.ItemsSource = connection.Table<ClaimedProductModel>().ToList().OrderBy(x => x.RefNumber).ToList();

                            blockEditHandler = false;
                        }
                    }
                    itemsToUpdate.Clear();
                }
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (blockEditHandler == false)
            {
                if (listSelector == 1)
                {
                    ClaimedProductModel item = dataGrid.SelectedItem as ClaimedProductModel;

                    var temp = itemsToUpdate.Where(x => x.Code == item.Code && x.Quantity == item.Quantity && x.CustCode == item.CustCode && x.ClaimDate == item.ClaimDate && x.RefNumber == item.RefNumber).ToList();

                    if (temp.Count == 0)
                    {
                        itemsToUpdate.Add(dataGrid.SelectedItem as ClaimedProductModel);
                    }
                    else
                    {
                        temp.Remove(temp.First());
                        temp.Add(item);

                    }
                }
            }
            //else
            //{
            //    return;
            //}
        }

        private void PL_rbtn_Unchecked(object sender, RoutedEventArgs e)
        {
            tb_NoResults.Visibility = Visibility.Hidden;
        }

        private void Uk_rbtn_Unchecked(object sender, RoutedEventArgs e)
        {
            tb_NoResults.Visibility = Visibility.Hidden;
        }
    }
}
using Nash_Report_Generator.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Nash_Report_Generator.ProcessingData
{
    public class CustomHandlers : DataGrid
    {
        public void SortHandler(object sender, DataGridSortingEventArgs e)
        {
            DataGridColumn column = e.Column;

            if ((string)e.Column.Header == "ClaimDate")
            {
                // prevent the built-in sort from sorting
                e.Handled = true;

                ListSortDirection? direction = (e.Column.SortDirection != ListSortDirection.Ascending)
                            ? ListSortDirection.Ascending
                            : ListSortDirection.Descending;

                ListCollectionView lcv = (ListCollectionView)CollectionViewSource.GetDefaultView(((DataGrid)sender).ItemsSource as List<ClaimedProductModel>);
                IComparer comparer = new CustomSort(direction, lcv);

                column.SortDirection = direction;
                //apply the sort
                lcv.CustomSort = comparer;
            }
        }
    }
}
using Nash_Report_Generator.Model;
using System.Collections;
using System.ComponentModel;
using System.Windows.Data;

namespace Nash_Report_Generator.ProcessingData
{
    internal class CustomSort : IComparer
    {
        private ListCollectionView lcv;
        private ListSortDirection SortDirection { get; set; }

        public CustomSort(ListSortDirection? sortDirection, ListCollectionView lcv)
        {
            SortDirection = (ListSortDirection)sortDirection;
            this.lcv = lcv;
        }

        public int Compare(object x, object y)
        {
            string firstDate = ((ClaimedProductModel)x).ClaimDate;
            string secondDate = ((ClaimedProductModel)y).ClaimDate;

            if (SortDirection == ListSortDirection.Ascending)
            {
                if (int.Parse(firstDate.Substring(6)) > int.Parse(secondDate.Substring(6)))
                {
                    return 1;
                }
                else if (int.Parse(firstDate.Substring(6)) < int.Parse(secondDate.Substring(6)))
                {
                    return -1;
                }
                else if (int.Parse(firstDate.Substring(3, 2)) > int.Parse(secondDate.Substring(3, 2)))
                {
                    return 1;
                }
                else if (int.Parse(firstDate.Substring(3, 2)) < int.Parse(secondDate.Substring(3, 2)))
                {
                    return -1;
                }
                else if (int.Parse(firstDate.Substring(0, 2)) > int.Parse(secondDate.Substring(0, 2)))
                {
                    return 1;
                }
                else if (int.Parse(firstDate.Substring(0, 2)) < int.Parse(secondDate.Substring(0, 2)))
                {
                    return -1;
                }
                else return 0;
            }
            else
            {
                if (int.Parse(firstDate.Substring(6)) < int.Parse(secondDate.Substring(6)))
                {
                    return 1;
                }
                else if (int.Parse(firstDate.Substring(6)) > int.Parse(secondDate.Substring(6)))
                {
                    return -1;
                }
                else if (int.Parse(firstDate.Substring(3, 2)) < int.Parse(secondDate.Substring(3, 2)))
                {
                    return 1;
                }
                else if (int.Parse(firstDate.Substring(3, 2)) > int.Parse(secondDate.Substring(3, 2)))
                {
                    return -1;
                }
                else if (int.Parse(firstDate.Substring(0, 2)) < int.Parse(secondDate.Substring(0, 2)))
                {
                    return 1;
                }
                else if (int.Parse(firstDate.Substring(0, 2)) > int.Parse(secondDate.Substring(0, 2)))
                {
                    return -1;
                }
                else return 0;
            }
        }
    }
}
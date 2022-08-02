using Microsoft.Office.Interop.Excel;
using Nash_Report_Generator.Model;
using Nash_Report_Generator.ProcessingData;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ReportGeneratorTests
{
    public class Tests
    {
        private readonly List<ClaimedProductModel> claims = new List<ClaimedProductModel>();
        private readonly _Worksheet sh;

        [Fact]
        public void NullSheetForContentValidation()
        {
            bool newDb = false;
            Assert.Throws<NullReferenceException>(() => ContentDataValidation.ValidateCellContent(sh));
        }

        [Fact]
        public async void ReturnReturnMostActiveCustomersForEmptyList()
        {
            var list = await ReportGatherer.ReturnMostActiveCustomers(claims);
            bool isEmpty = list.Count == 0;

            Assert.True(isEmpty, "List has no elements");
        }

        [Fact]
        public void CheckIfPathIsCorrect()
        {
            string path = "C:\\Users\\EDC\\Desktop\\tests\\1 — kopia\\";
            var list = ExcelWorker.CollectFileNames(path);
            var listOfFactualFilesInFolder = new List<string>() { "C:\\Users\\EDC\\Desktop\\tests\\1 — kopia\\CL-2022-2-22-543.xlsx", "C:\\Users\\EDC\\Desktop\\tests\\1 — kopia\\CL-2022-37-4563.xlsx" };
            Assert.Equal(list, listOfFactualFilesInFolder);
        }

        [Fact]
        public void CheckIfPathIsIncorrect()
        {
            string path = "J:\\Users\\EDC\\Desktop\\tests\\1 — kopia";
            _ = Assert.Throws<DirectoryNotFoundException>(() => ExcelWorker.CollectFileNames(path));
        }

        [Fact]
        public void CheckDatabaseConstraint()
        {
            ClaimedProductModel testItem1 = new ClaimedProductModel()
            {
                Code = "T1200",
                Quantity = 5,
                ClaimDate = "29.03.2022",
                CustCode = "3ATW01",
                Description = "testDescrABC",
                Reason = 4,
                RefNumber = "clc123--1"
            };

            ClaimedProductModel testItem2 = new ClaimedProductModel()
            {
                Code = "T1200",
                Quantity = 5,
                ClaimDate = "29.03.2022",
                CustCode = "3ATW01",
                Description = "testDescrABC",
                Reason = 4,
                RefNumber = "clc123--1"
            };

            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string testDbPath = System.IO.Path.Combine(folderPath, "testDb-8ha--31].db");
            FileInfo fi = new FileInfo(testDbPath);

            if (fi.Exists)
                fi.Delete();

            using (SQLiteConnection connection = new SQLiteConnection(testDbPath))
            {
                _ = connection.CreateTable<ClaimedProductModel>();
                _ = connection.Insert(testItem1);
                _ = Assert.Throws<SQLiteException>(() => connection.Insert(testItem2));
            }

            fi.Delete();
        }
    }
}
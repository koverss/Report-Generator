using System;
using System.IO;
using System.Windows;

namespace Nash_Report_Generator
{
    public partial class App : Application
    {
        //private static readonly string appPath = AppDomain.CurrentDomain.BaseDirectory;
        //public static readonly string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        //nashserver specific location
        public static readonly string folderPath = "G:\\EDC\\SUPPORT";
        public static string dbAllDataPathPL = folderPath + "\\SQLite\\newFormProductClaimsPL.db";
        public static string dbAllDataPathUK = folderPath + "\\SQLite\\newFormProductClaimsUK.db";
    }
}
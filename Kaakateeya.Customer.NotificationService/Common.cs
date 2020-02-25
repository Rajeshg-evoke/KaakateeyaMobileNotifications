using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Kaakateeya.Customer.NotificationService
{
    public class Common
    {
        public static string LogMessage(string Parameter, string Method, object Message, object StackTrace)
        {
            return "An error occured with message " + Message + " and stack trace " + StackTrace + " for " + Method + " " + Parameter;
        }

        public static DataTable DataTableAdd(string CommaSeperate, DataTable dt, string ColumnName, string tName)
        {
            dt = new DataTable(tName); dt.Rows.Clear(); dt.Columns.Add(ColumnName);
            if (!string.IsNullOrEmpty(CommaSeperate) && CommaSeperate != "null")
            {
                string[] strarray = CommaSeperate.Split(',');
                foreach (var i in strarray) { dt.Rows.Add(i); }
            }

            return dt;
        }
    }
}

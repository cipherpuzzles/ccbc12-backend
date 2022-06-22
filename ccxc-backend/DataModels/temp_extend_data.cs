using Ccxc.Core.DbOrm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.DataModels
{
    public class temp_extend_data
    {
        [DbColumn(IsPrimaryKey = true, ColumnDescription = "年份")]
        public int year { get; set; }

        [DbColumn(ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "内容")]
        public string content { get; set; }
    }

    public class TempExtendData : MysqlClient<temp_extend_data>
    {
        public TempExtendData(string connStr) : base(connStr)
        {

        }
    }
}

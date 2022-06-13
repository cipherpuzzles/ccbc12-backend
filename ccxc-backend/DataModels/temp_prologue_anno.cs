using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.DataModels
{
    public class temp_prologue_anno
    {
        [DbColumn(IsPrimaryKey = true, ColumnDescription = "题目ID")]
        public int pid { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "创建时间", ColumnDataType = "DATETIME", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime create_time { get; set; }

        [DbColumn(ColumnDescription = "公告内容", ColumnDataType = "TEXT", IsNullable = true)]
        public string content { get; set; }
    }

    public class TempPrologueAnno : MysqlClient<temp_prologue_anno>
    {
        public TempPrologueAnno(string connStr) : base(connStr)
        {

        }
    }
}

using Ccxc.Core.DbOrm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.DataModels
{
    public class user_temp_progress
    {
        [DbColumn(IsPrimaryKey = true, ColumnDescription = "用户ID")]
        public int uid { get; set; }

        [DbColumn(ColumnDescription = "序章存档数据", IsJson = true, ColumnDataType = "JSON")]
        public PrologueSaveData prologue_data { get; set; }
    }

    public class UserTempProgress : MysqlClient<user_temp_progress>
    {
        public UserTempProgress(string connStr) : base(connStr)
        {

        }
    }
}

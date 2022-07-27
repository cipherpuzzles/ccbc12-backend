using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.System
{
    public class DefaultSettingResponse : BasicResponse
    {
        public long start_time { get; set; }
        public int start_type { get; set; }
    }

    public class ScoreBoardItem
    {
        public int gid { get; set; }
        public string group_name { get; set; }
        public string group_profile { get; set; }
        
        /// <summary>
        /// 总用时（小时）（完赛时间-开赛时间）+ 罚时
        /// </summary>
        public double total_time { get; set; }
        public int finished_group_count { get; set; }
        public int finished_puzzle_count { get; set; }
        public int u { get; set; }
        public int a { get; set; }
        public int is_finish { get; set; }
    }

    public class ScoreBoardResponse : BasicResponse
    {
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime cache_time { get; set; }
        public List<ScoreBoardItem> finished_groups { get; set; }
        public List<ScoreBoardItem> groups { get; set; }
    }
}

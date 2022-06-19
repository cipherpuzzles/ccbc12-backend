using ccxc_backend.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Game
{
    public class PrologueGetPuzzleDetailResponse : BasicResponse
    {
        public int puzzle_id { get; set; }
        public string problem_content { get; set; }
        public int used_replaced_assets { get; set; }
        public string content { get; set; }
    }

    public class PrologueScoreboardItem
    {
        /// <summary>
        /// 0-组队 1-个人
        /// </summary>
        public int type { get; set; }
        public string name { get; set; } //group name for type 0, and user name for type 1

        public string desc { get; set; }
        public int number { get; set; }

        [JsonConverter(typeof(Ccxc.Core.Utils.ExtensionFunctions.UnixTimestampConverter))]
        public DateTime last_correct_time { get; set; }
    }

    public class PrologueScoreboardResponse : BasicResponse
    {
        public List<PrologueScoreboardItem> data { get; set; }
    }

    public class PrologueAnnoRequest
    {
        public int page_num { get; set; }
        public int page_size { get; set; }
    }

    public class PrologueAnnoResponse : BasicResponse
    {
        public List<temp_prologue_anno> data { get; set; }
        public int sum_rows { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using ccxc_backend.DataModels;
using Newtonsoft.Json;

namespace ccxc_backend.Controllers.Game
{
    public class GetLastAnswerLogRequest
    {
        public int pid { get; set; }
    }

    public class GetLastAnswerLogResponse : BasicResponse
    {
        public List<AnswerLogView> answer_log { get; set; }
    }

    public class AnswerLogView : answer_log
    {
        public AnswerLogView(answer_log a)
        {
            id = a.id;
            create_time = a.create_time;
            uid = a.uid;
            gid = a.gid;
            pid = a.pid;
            answer = a.answer;
            status = a.status;
        }

        public string user_name { get; set; }
    }

    public class GetYearListResponse : BasicResponse
    {
        public List<SimplePuzzleGroup> data { get; set; }

        /// <summary>
        /// FinalMeta状态 0-未解锁 1-已解锁 2-已完成
        /// </summary>
        public int final_meta_type { get; set; }
        public int power_point { get; set; }

        [JsonConverter(typeof(Ccxc.Core.Utils.ExtensionFunctions.UnixTimestampConverter))]
        public DateTime power_point_calc_time { get; set; }
        public int power_point_increase_rate { get; set; }
        public int time_probe_cost { get; set; }
    }

    public class SimplePuzzleGroup
    {
        public int pgid { get; set; }
        public string group_name { get; set; }
        public List<SimplePuzzle> puzzles { get; set; }

        /// <summary>
        /// Meta状态 0-未解锁 1-已解锁 2-已完成
        /// </summary>
        public int meta_type { get; set; }
        public string meta_name { get; set; }
        public int unlock_cost { get; set; }
    }

    public class SimplePuzzle
    {
        public int year { get; set; }

        /// <summary>
        /// 题目状态 0-未解锁 1-已解锁 2-已完成
        /// </summary>
        public int type { get; set; }
    }

    public class YearProbeRequest
    {
        public int year { get; set; }
    }

    public class YearProbeResponse : BasicResponse
    {
        public string extra_message { get; set; }
    }

    public class ProbedYear
    {
        public int year { get; set; }

        /// <summary>
        /// 0-不是题目 1-是题目
        /// </summary>
        public int is_puzzle { get; set; }
    }

    public class GetProbedYearsListResponse : BasicResponse
    {
        public List<ProbedYear> data { get; set; }
    }
}

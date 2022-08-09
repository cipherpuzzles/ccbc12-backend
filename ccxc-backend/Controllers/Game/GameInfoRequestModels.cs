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
        public int try_answer_cost { get; set; }
        public int try_meta_answer_cost { get; set; }
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

    public class GetPuzzleBoardRequest
    {
        /// <summary>
        /// 0-按首杀时间排序 1-按解答队伍多少排序 2-按点赞数排序 3-按点踩数排序
        /// </summary>
        public int type { get; set; }
    }

    public class GetPuzzleBoardResponse : BasicResponse
    {
        [JsonConverter(typeof(Ccxc.Core.Utils.ExtensionFunctions.UnixTimestampConverter))]
        public DateTime cache_time { get; set; }
        public List<PuzzleBoardItem> data { get; set; }
    }

    public class PuzzleBoardItem
    {
        public string title { get; set; }
        public string first_solve_group_name { get; set; }

        [JsonConverter(typeof(Ccxc.Core.Utils.ExtensionFunctions.UnixTimestampConverter))]
        public DateTime first_solve_time { get; set; }
        public int solved_group_count { get; set; }
        public int like_count { get; set; }
        public int dislike_count { get; set; }
    }

    public class GetFinalEndResponse : BasicResponse
    {
        public int rank_temp { get; set; }
    }
}

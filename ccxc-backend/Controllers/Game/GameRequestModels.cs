using Ccxc.Core.Utils.ExtensionFunctions;
using ccxc_backend.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Game
{
    public class PuzzleStartResponse : BasicResponse
    {
        public string start_prefix { get; set; }
        public string ticket { get; set; }
        public int is_first { get; set; }
    }

    public class GetPuzzleGroupResponse : BasicResponse
    {
        //是否可以选择开放下一个组（0-无法开放 1-可以开放）
        public int is_open_next_group { get; set; }

        //是否显示FinalMeta准入题目（0-不显示 1-显示）
        public int is_open_pre_final { get; set; }

        //是否显示FinalMeta（0-不显示 1-显示）
        public int is_open_final_meta { get; set; }

        public List<PuzzleGroupView> puzzle_groups { get; set; }
    }

    public class GetGameInfoResponse : BasicResponse
    {
        public int open_group_count { get; set; }
        public int finished_puzzle_count { get; set; }
        public int is_open_next_group { get; set; }
        public double score { get; set; }
        public double penalty { get; set; }
    }

    public class PuzzleGroupView : puzzle_group
    {
        public PuzzleGroupView(puzzle_group pg)
        {
            pgid = pg.pgid;
            pg_name = pg.pg_name;
            is_hide = pg.is_hide;
            difficulty = pg.difficulty;
        }

        //是否完成本题目组（0-未完成 1-完成）
        public int is_finish { get; set; }

        //本题目组是否已开放（0-未开放 1-开放）
        public int is_open { get; set; }
    }

    public class GetPuzzleListRequest
    {
        public int pgid { get; set; }
    }

    public class GetPuzzleListResponse : BasicResponse
    {
        public puzzle_group puzzle_group_info { get; set; }
        public List<PuzzleOverview> puzzle_list { get; set; }
    }

    public class PuzzleOverview
    {
        public PuzzleOverview(puzzle p)
        {
            pid = p.pid;
            title = p.title;
            answer_type = p.answer_type;
        }

        public int pid { get; set; }
        public string title { get; set; }
        public int answer_type { get; set; }

        //是否完成本题（0-未完成 1-完成）
        public int is_finish { get; set; }
    }

    public class GetFinalMetaPuzzleListResponse : BasicResponse
    {
        public List<PuzzleOverview> puzzle_list { get; set; }
    }

    public class GetPuzzleDetailRequest
    {
        public int year { get; set; }
    }

    public class GetPuzzleDetailResponse : BasicResponse
    {
        public PuzzleView puzzle { get; set; }
        public int power_point { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime power_point_calc_time { get; set; }
        public int power_point_increase_rate { get; set; }
    }

    public class GetPuzzleTipsResponse : BasicResponse
    {
        public int is_tip_available { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime tip_available_time { get; set; }
        public double tip_available_progress { get; set; }
        public int unlock_cost { get; set; }
        public int unlock_delay { get; set; }
        public List<PuzzleTip> puzzle_tips { get; set; }
        public List<OracleSimpleItem> oracles { get; set; }
    }

    public class UnlockPuzzleTipRequest
    {
        public int year { get; set; }
        public int tip_num { get; set; }
    }

    public class PuzzleTip
    {
        public string tips_id { get; set; }

        /// <summary>
        /// 1/2/3
        /// </summary>
        public int tip_num { get; set; }

        public string title { get; set; }

        /// <summary>
        /// 0-未解锁 1-已解锁
        /// </summary>
        public int is_open { get; set; }
        public string content { get; set; }

    }

    public class OracleSimpleItem
    {
        public int oracle_id { get; set; }
        public int is_reply { get; set; }
        
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime unlock_time { get; set; }
    }

    public class PuzzleView
    {
        public PuzzleView(puzzle p)
        {
            pid = p.pid;
            second_key = p.second_key;
            type = p.type;
            title = p.title;
            content = p.content;
            image = p.image;
            html = p.html;
            answer_type = p.answer_type;
        }
        public int pid { get; set; }
        public int second_key { get; set; }
        public int type { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string image { get; set; }
        public string html { get; set; }
        public int answer_type { get; set; }
        public string extend_content { get; set; }
        public int is_finish { get; set; }
    }

    public class GetFinalInfoResponse : BasicResponse
    {
        public string desc { get; set; }
        public int rank_temp { get; set; }
    }

    public class OpenOracleRequest
    {
        public int oracle_id { get; set; }
    }

    public class OpenOracleResponse : BasicResponse
    {
        public oracle data { get; set; }
    }
}

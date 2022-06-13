using Ccxc.Core.DbOrm;
using Ccxc.Core.Utils.ExtensionFunctions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ccxc_backend.DataModels
{
    public class progress
    {
        [DbColumn(IsPrimaryKey = true, ColumnDescription = "组ID")]
        public int gid { get; set; }

        [DbColumn(ColumnDescription = "存档数据", IsJson = true, ColumnDataType = "JSON")]
        public SaveData data { get; set; } = new SaveData();

        [DbColumn(ColumnDescription = "得分（排序依据，不展示）")]
        public double score { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "更新时间", ColumnDataType = "DATETIME", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime update_time { get; set; }

        [DbColumn(ColumnDescription = "是否完赛（0-未完赛 1-完赛）")]
        public byte is_finish { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "完成时间", ColumnDataType = "DATETIME", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime finish_time { get; set; }

        [DbColumn(ColumnDescription = "罚时（单位小时）")]
        public double penalty { get; set; }

        [DbColumn(ColumnDescription = "能量点")]
        public int power_point { get; set; }

        [JsonConverter(typeof(UnixTimestampConverter))]
        [DbColumn(ColumnDescription = "能量点更新时间", ColumnDataType = "DATETIME", Length = 6, DefaultValue = "0000-00-00 00:00:00.000000")]
        public DateTime power_point_update_time { get; set; }

        [DbColumn(ColumnDescription = "序章存档数据", IsJson = true, ColumnDataType = "JSON")]
        public PrologueSaveData prologue_data { get; set; }
    }

    public class SaveData
    {
        /// <summary>
        /// 是否已开启本篇（条件：完成序章）
        /// </summary>
        public bool IsOpenMainProject { get; set; } = false;

        /// <summary>
        /// 已完成的题目（pid）
        /// </summary>
        public HashSet<int> FinishedProblems { get; set; } = new HashSet<int>();

        /// <summary>
        /// 已解锁的题目（pid）
        /// </summary>
        public HashSet<int> UnlockedProblems { get; set; } = new HashSet<int>();

        /// <summary>
        /// 可见的题目（pid）
        /// </summary>
        public HashSet<int> VisibleProblems { get; set; } = new HashSet<int>();

        /// <summary>
        /// 是否开放FinalMeta的第一部分（条件：解开全部6个Meta后显示）
        /// </summary>
        public bool IsOpenFinalPart1 { get; set; } = false;

        /// <summary>
        /// 是否开放FinalMeta的第二部分（条件：解开FinalMeta第一部分后显示）
        /// </summary>
        public bool IsOpenFinalPart2 { get; set; } = false;

        /// <summary>
        /// 已完成的分组ID（Meta已解出的分组）（pgid）
        /// </summary>
        public HashSet<int> FinishedGroups { get; set; } = new HashSet<int>();

        /// <summary>
        /// 题目解锁时间（pid -> 解锁时间）
        /// </summary>
        public Dictionary<int, DateTime> ProblemUnlockTime { get; set; } = new Dictionary<int, DateTime>();

        /// <summary>
        /// 题目解锁时耗费的能量（用于完成后返还）（pid -> 能量点数）
        /// </summary>
        public Dictionary<int, int> ProblemUnlockPowerPoint { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// 各题目的答案提交次数（pid -> 次数）
        /// </summary>
        public Dictionary<int, int> ProblemAnswerSubmissionsCount { get; set; } = new Dictionary<int, int>();

        /// <summary>
        /// 已兑换过的提示（pid -> (提示id)）
        /// </summary>
        public Dictionary<int, HashSet<int>> OpenedHints { get; set; } = new Dictionary<int, HashSet<int>>();

        /// <summary>
        /// 已解锁的年份（年份（非pid））
        /// </summary>
        public HashSet<int> UnlockedYears { get; set; } = new HashSet<int>();

        /// <summary>
        /// 用户提示信息进度（uid -> (tag)）
        /// </summary>
        public Dictionary<int, HashSet<string>> UserProgressTags { get; set; } = new Dictionary<int, HashSet<string>>();
    }

    public class PrologueSaveData
    {
        /// <summary>
        /// 当前正在解答的题目序号（从1开始递增，取mod12的值作为题目来源）
        /// </summary>
        public int CurrentProblem { get; set; }

        /// <summary>
        /// 当前正在解答的题目答案（用于重新访问时输出同样的题目）
        /// </summary>
        public string CurrentAnswer { get; set; }

        /// <summary>
        /// 所有模板的一组排列
        /// </summary>
        public int[] TemplateBag { get; set; }

        /// <summary>
        /// 当前的题目模板索引（TemplateBag[CurrentTemplateIndex]为当前使用的模板，从0开始递增，当值等于模板数量时，值归0，同时重新随机生成模板排列）
        /// </summary>
        public int CurrentTemplateIndex { get; set; }

        /// <summary>
        /// 是否已完成序章题目部分（任何时候只要输入meta答案即为完成）
        /// </summary>
        public bool IsFinished { get; set; }

        /// <summary>
        /// 上次回答正确的时间（更新时间）
        /// </summary>
        public DateTime LastAcceptTime { get; set; }
    }

    public class Progress : MysqlClient<progress>
    {
        public Progress(string connStr) : base(connStr)
        {

        }
    }
}

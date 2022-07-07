using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.Controllers.Game
{
    public class UnlockGroupRequest
    {
        public int unlock_puzzle_group_id { get; set; }
    }

    public class PrologueCheckAnswerRequest
    {
        public int pid { get; set; }
        public string answer { get; set; }
    }

    public class CheckAnswerRequest
    {
        public int year { get; set; }
        public string answer { get; set; }
    }

    public class AnswerResponse : BasicResponse
    {
        /// <summary>
        /// 答案状态（0-保留 1-正确 2-答案错误 3-在冷却中而未判断 4-该题目不可见而无法回答 5-发生存档错误而未判定 6-符合隐藏关键字而跳转 7-解锁提示 8-探测时间 9-解锁年份）
        /// </summary>
        public int answer_status { get; set; }
        /// <summary>
        /// 0-什么都不做 1-跳转到final 16-重新载入页面
        /// </summary>
        public int extend_flag { get; set; }
    }
}

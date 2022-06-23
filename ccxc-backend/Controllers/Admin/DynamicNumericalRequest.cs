using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Admin
{
    public class DynamicNumerical
    {
        /// <summary>
        /// 初始能量点
        /// </summary>
        public int initial_power_point { get; set; }

        /// <summary>
        /// 能量增加速度（每分钟）（更新需要通过单独API）
        /// </summary>
        public int power_increase_rate { get; set; }

        /// <summary>
        /// A区小题解锁消耗
        /// </summary>
        public int unlock_puzzle_cost_a { get; set; }

        /// <summary>
        /// B区小题解锁消耗
        /// </summary>
        public int unlock_puzzle_cost_b { get; set; }

        /// <summary>
        /// C区小题解锁消耗
        /// </summary>
        public int unlock_puzzle_cost_c { get; set; }

        /// <summary>
        /// D区小题解锁消耗
        /// </summary>
        public int unlock_puzzle_cost_d { get; set; }

        /// <summary>
        /// E区小题解锁消耗
        /// </summary>
        public int unlock_puzzle_cost_e { get; set; }

        /// <summary>
        /// F区小题解锁消耗
        /// </summary>
        public int unlock_puzzle_cost_f { get; set; }

        /// <summary>
        /// 探测年份信息消耗
        /// </summary>
        public int time_probe_cost { get; set; }

        /// <summary>
        /// 尝试答案消耗
        /// </summary>
        public int try_answer_cost { get; set; }

        /// <summary>
        /// 尝试Meta答案消耗
        /// </summary>
        public int try_meta_answer_cost { get; set; }

        /// <summary>
        /// 提示功能解锁时间（单位：分钟）
        /// </summary>
        public int unlock_tip_function_after { get; set; }

        /// <summary>
        /// 打开A区小题提示消耗
        /// </summary>
        public int unlock_tip_cost_a { get; set; }

        /// <summary>
        /// 打开B区小题提示消耗
        /// </summary>
        public int unlock_tip_cost_b { get; set; }

        /// <summary>
        /// 打开C区小题提示消耗
        /// </summary>
        public int unlock_tip_cost_c { get; set; }

        /// <summary>
        /// 打开D区小题提示消耗
        /// </summary>
        public int unlock_tip_cost_d { get; set; }

        /// <summary>
        /// 打开E区小题提示消耗
        /// </summary>
        public int unlock_tip_cost_e { get; set; }

        /// <summary>
        /// 打开F区小题提示消耗
        /// </summary>
        public int unlock_tip_cost_f { get; set; }

        /// <summary>
        /// 打开A区Meta提示消耗
        /// </summary>
        public int unlock_meta_tip_cost_a { get; set; }

        /// <summary>
        /// 打开B区Meta提示消耗
        /// </summary>
        public int unlock_meta_tip_cost_b { get; set; }

        /// <summary>
        /// 打开C区Meta提示消耗
        /// </summary>
        public int unlock_meta_tip_cost_c { get; set; }

        /// <summary>
        /// 打开D区Meta提示消耗
        /// </summary>
        public int unlock_meta_tip_cost_d { get; set; }

        /// <summary>
        /// 打开E区Meta提示消耗
        /// </summary>
        public int unlock_meta_tip_cost_e { get; set; }

        /// <summary>
        /// 打开F区Meta提示消耗
        /// </summary>
        public int unlock_meta_tip_cost_f { get; set; }

        /// <summary>
        /// 打开Final-a提示消耗
        /// </summary>
        public int unlock_final_a_tip_cost { get; set; }

        /// <summary>
        /// 打开Final-b提示消耗
        /// </summary>
        public int unlock_final_b_tip_cost { get; set; }
    }

    public class GetDynamicNumericalResponse : BasicResponse
    {
        public DynamicNumerical data { get; set; }
    }
}

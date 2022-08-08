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

    public class Backend2040Request
    {
        public string current_input { get; set; }
        public CalcContext context { get; set; }
    }

    public class Backend2040Response : BasicResponse
    {
        public CalcContext context { get; set; }
    }

    public class CalcContext
    {
        public string screen { get; set; }
        public string input_buffer { get; set; }
        public List<PartNumber> buffer { get; set; }
        
        /// <summary>
        /// 错误类型（0-无错误 1-算数溢出 2-除0 3-操作数不足 4-栈溢出 5-无效字符输入）
        /// </summary>
        public int error { get; set; }

        public static CalcContext Empty = new CalcContext
        {
            screen = "",
            input_buffer = "",
            buffer = new List<PartNumber>(),
            error = 0
        };
    }

    public class PartNumber
    {
        /// <summary>
        /// 0-原始数字 1-新数字 2-上溢 3-下溢 4-除0
        /// </summary>
        public int type { get; set; }
        public string content { get; set; }

        public static Dictionary<char, long> CharDict = new Dictionary<char, long>
        {
            {'ɀ', 1 }, //0
            {'Ɂ', 2 }, //1
            {'ɂ', 4 }, //2
            {'Ƀ', 8 }, //3
            {'Ʉ', 16 }, //4
            {'Ʌ', 32 }, //5
            {'Ɇ', 64 }, //6
            {'ɇ', 128 }, //7
            {'Ɉ', 256 }, //8
            {'ɉ', 512 }, //9
            {'Ɋ', 1024 }, //A
            {'ɋ', 2048 }, //B
            {'Ɍ', 4096 }, //C
            {'ɍ', 8192 }, //D
            {'Ɏ', 16384 }, //E
            {'ɏ', 32768 }, //F
            {'ɐ', 65536 } //G
        };

        //原始数字 字符表 （额外字符：空格-表示数字之间的分隔符  运算符：+ - * %  \r-AC ~-负号
        public const char C_SPACE = ' ';
        public const char C_NEGATIVE = '~';
        public const char C_PLUS = 'х'; //+
        public const char C_MINUS = 'ц'; //-
        public const char C_MULTIPLY = 'ч'; //*
        public const char C_MOD = 'щ'; //%    /(除号)- ш
        public const char C_AC = '\r';

        //新数字 字符表
        public const string Alphabet = "abcdefghijklmnopqrstuvwxyz";
        public const char N_NEGATIVE = '-';

        public long GetValue()
        {
            if (type == 0)
            {
                //首先判断第一个字符是否为负号(C_NEGATIVE)
                bool isNegative = content[0] == C_NEGATIVE;
                if (isNegative)
                {
                    content = content[1..];
                }

                var sum = 0L;
                for (int i = 0; i < content.Length; i++)
                {
                    sum += CharDict[content[i]];
                }

                if (isNegative)
                {
                    return -1 * sum;
                }
                else
                {
                    return sum;
                }
            }
            else
            {
                //首先判断第一个字符是否为负号(N_NEGATIVE)
                bool isNegative = content[0] == N_NEGATIVE;
                if (isNegative)
                {
                    content = content[1..];
                }

                //26进制转10进制
                var sum = 0L;
                for (int i = 0; i < content.Length; i++)
                {
                    var index = Alphabet.IndexOf(content[i]);
                    sum += index * (long)Math.Pow(26, content.Length - i - 1);
                }

                if (isNegative)
                {
                    return -1 * sum;
                }
                else
                {
                    return sum;
                }
            }
        }

        public static PartNumber CreateNew(long value)
        {
            //首先判断待转换数字是否为负数
            bool isNegative = value < 0;
            if (isNegative)
            {
                value = -value;
            }
            //将value转换为26进制
            var sb = new StringBuilder();
            if (value == 0)
            {
                sb.Append(Alphabet[0]);
            }
            else
            {
                while (value > 0)
                {
                    var remainder = value % 26;
                    sb.Insert(0, Alphabet[(int)remainder]);
                    value /= 26;
                }
            }
            //如果是负数，则在字符串前面加上负号(N_NEGATIVE)
            if (isNegative)
            {
                sb.Insert(0, N_NEGATIVE);
            }
            
            return new PartNumber
            {
                type = 1,
                content = sb.ToString()
            };
        }

        //重载+操作符
        public static PartNumber operator +(PartNumber a, PartNumber b)
        {
            var res = a.GetValue() + b.GetValue();
            if (res > int.MaxValue)
            {
                return new PartNumber
                {
                    type = 2,
                    content = "overflow"
                };
            }
            else if (res < int.MinValue)
            {
                return new PartNumber
                {
                    type = 3,
                    content = "underflow"
                };
            }
            else
            {
                return CreateNew(res);
            }
        }

        //重载-操作符
        public static PartNumber operator -(PartNumber a, PartNumber b)
        {
            var res = a.GetValue() - b.GetValue();
            if (res > int.MaxValue)
            {
                return new PartNumber
                {
                    type = 2,
                    content = "overflow"
                };
            }
            else if (res < int.MinValue)
            {
                return new PartNumber
                {
                    type = 3,
                    content = "underflow"
                };
            }
            else
            {
                return CreateNew(res);
            }
        }

        //重载*操作符
        public static PartNumber operator *(PartNumber a, PartNumber b)
        {
            var res = a.GetValue() * b.GetValue();
            if (res > int.MaxValue)
            {
                return new PartNumber
                {
                    type = 2,
                    content = "overflow"
                };
            }
            else if (res < int.MinValue)
            {
                return new PartNumber
                {
                    type = 3,
                    content = "underflow"
                };
            }
            else
            {
                return CreateNew(res);
            }
        }

        //重载%操作符
        public static PartNumber operator %(PartNumber a, PartNumber b)
        {
            //检查除数是否为0
            if (b.GetValue() == 0)
            {
                return new PartNumber
                {
                    type = 4,
                    content = "divide by 0"
                };
            }
            
            var res = a.GetValue() % b.GetValue();
            if (res > int.MaxValue)
            {
                return new PartNumber
                {
                    type = 2,
                    content = "overflow"
                };
            }
            else if (res < int.MinValue)
            {
                return new PartNumber
                {
                    type = 3,
                    content = "underflow"
                };
            }
            else
            {
                return CreateNew(res);
            }
        }
    }
}

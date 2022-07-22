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
            {'A', 1 },
            {'B', 2 },
            {'C', 4 },
            {'D', 8 },
            {'E', 16 },
            {'F', 32 },
            {'G', 64 },
            {'H', 128 },
            {'I', 256 },
            {'J', 512 },
            {'K', 1024 },
            {'L', 2048 },
            {'M', 4096 },
            {'N', 8192 },
            {'O', 16384 },
            {'P', 32768 },
            {'Q', 65536 }
        };

        public const string Alphabet = "abcdefghijklmnopqrstuvwxyz";

        //字符表 （额外字符：空格-表示数字之间的分隔符  运算符：+ - * %  .-AC
        public const char C_SPACE = ' ';
        public const char C_PLUS = '+';
        public const char C_MINUS = '-';
        public const char C_MULTIPLY = '*';
        public const char C_MOD = '%';
        public const char C_AC = '.';

        public long GetValue()
        {
            if (type == 0)
            {
                var sum = 0L;
                for (int i = 0; i < content.Length; i++)
                {
                    sum += CharDict[content[i]];
                }
                return sum;
            }
            else
            {
                //26进制转10进制
                var sum = 0L;
                for (int i = 0; i < content.Length; i++)
                {
                    var index = Alphabet.IndexOf(content[i]);
                    sum += index * (long)Math.Pow(26, content.Length - i - 1);
                }
                return sum;
            }
        }

        public static PartNumber CreateNew(long value)
        {
            //将value转换为26进制
            var sb = new StringBuilder();
            while (value > 0)
            {
                var remainder = value % 26;
                sb.Insert(0, Alphabet[(int)remainder]);
                value /= 26;
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

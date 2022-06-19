using Ccxc.Core.DbOrm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.DataModels
{
    public class puzzle_group
    {
        [DbColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "题目组ID")]
        public int pgid { get; set; }

        [DbColumn(ColumnDescription = "题目组名")]
        public string pg_name { get; set; }

        [DbColumn(ColumnDescription = "题目组描述", ColumnDataType = "TEXT", IsNullable = true)]
        public string pg_desc { get; set; }

        /// <summary>
        /// 是否为隐藏区域（0-不是 1-是）
        /// </summary>
        [DbColumn(DefaultValue = "0")]
        public byte is_hide { get; set; } = 0;

        /// <summary>
        /// 难度星级
        /// </summary>
        [DbColumn(DefaultValue = "1")]
        public int difficulty { get; set; } = 1;
    }

    public class PuzzleGroup : MysqlClient<puzzle_group>
    {
        public PuzzleGroup(string connStr) : base(connStr)
        {

        }

        public static List<SpPrologueContent> SpPrologueContentCache { get; set; } = null;

        public async Task<List<SpPrologueContent>> GetSpPrologueContent()
        {
            if (SpPrologueContentCache != null)
            {
                return SpPrologueContentCache;
            }

            var contentPgItem = await SimpleDb.AsQueryable().Where(x => x.pg_name == "prologue-puzzles").FirstAsync();
            if (contentPgItem == null) return null;

            var contentText = contentPgItem.pg_desc;
            var contentLines = contentText.Split('\n');
            var contentList = new List<SpPrologueContent>();
            foreach (var line in contentLines)
            {
                if (line.StartsWith("#")) continue;
                if (string.IsNullOrEmpty(line)) continue;
                if (!line.Contains('|')) continue;

                var f = line.Split('|', 2);
                var lowBound = -1;
                _ = int.TryParse(f[0], out lowBound);
                contentList.Add(new SpPrologueContent
                {
                    lowBound = lowBound,
                    text = f[1],
                    highBound = -1
                });
            }

            SpPrologueContent prevContentItem = null;
            foreach (var contentItem in contentList)
            {
                if (prevContentItem == null)
                {
                    prevContentItem = contentItem;
                    continue;
                }

                prevContentItem.highBound = contentItem.lowBound;
                prevContentItem = contentItem;
            }

            SpPrologueContentCache = contentList;
            return contentList;
        }

        public override Task InvalidateCache()
        {
            SpPrologueContentCache = null;
            return base.InvalidateCache();
        }
    }

    public class SpPrologueContent
    {
        public int lowBound { get; set; }
        public int highBound { get; set; }
        public string text { get; set; }
    }
}

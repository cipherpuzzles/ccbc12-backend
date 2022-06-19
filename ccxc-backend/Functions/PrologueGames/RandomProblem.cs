using ccxc_backend.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Functions.PrologueGames
{
    public static class RandomProblem
    {
        public const int TEMPLATE_MAX_LENGTH = 12;

        public static PrologueSaveData Init()
        {
            var data = new PrologueSaveData
            {
                CurrentProblem = 1,
                CurrentTemplateIndex = 0,
                IsFinished = false,
                LastAcceptTime = DateTime.Now
            };

            //获取当前题目
            var wordDict = WordDicts.W[data.CurrentProblem % 12];
            var random = new Random(GetSeed(1));
            var wordIndex = random.Next(0, 500);
            data.CurrentAnswer = wordDict[wordIndex];

            //获取模板排列
            data.TemplateBag = GetRandomTemplateList(random);

            return data;
        }

        public static PrologueSaveData GetNext(PrologueSaveData saveData)
        {
            //更新当前题目
            saveData.CurrentProblem++;
            var wordDict = WordDicts.W[saveData.CurrentProblem % 12];
            var random = new Random(GetSeed(saveData.CurrentProblem));
            var wordIndex = random.Next(0, 500);
            saveData.CurrentAnswer = wordDict[wordIndex];

            //更新模板排列
            saveData.CurrentTemplateIndex++;
            if (saveData.CurrentTemplateIndex == TEMPLATE_MAX_LENGTH)
            {
                saveData.CurrentTemplateIndex = 0;
                saveData.TemplateBag = GetRandomTemplateList(random);
            }

            //更新时间
            saveData.LastAcceptTime = DateTime.Now;

            return saveData;
        }

        public static int[] GetRandomTemplateList(Random random)
        {
            var arr = new int[TEMPLATE_MAX_LENGTH];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = i;
            }

            for (int i = arr.Length - 1; i >= 0; i--)
            {
                var idx = random.Next(0, i + 1);
                (arr[idx], arr[i]) = (arr[i], arr[idx]); //swap idx, i
            }

            return arr;
        }

        public static int GetSeed(int problemIndex)
        {
            return 51047 * problemIndex + 2133611;
        }
    }
}

﻿using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Functions.PrologueGames
{
    public static class Templates
    {
        public static Dictionary<int, Func<string, (string, int, string)>> TemplateDicts = new()
        {
            {0, AlphabetToNumber },
            {1, Add3 },
            {2, Morse },
            {3, Binary },
            {4, Braille },
            {5, Pigpen },
            {6, FlagSemaphore },
            {7, Revserse },
            {8, Phone9Key },
            {9, Polybius },
            {10, Atbash },
            {11, Railfence },
        };

        public static (string, int, string) GenerateProblem(PrologueSaveData data)
        {
            var answer = data.CurrentAnswer;
            var template = data.TemplateBag[data.CurrentTemplateIndex];

            var method = TemplateDicts[template];
            return method.Invoke(answer);
        }

        public static (string, int, string) AlphabetToNumber(string answer)
        {
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => x - 'A' + 1).ToList();
            var result = string.Join(" ", numbers);
            return (result, 0, "字母顺序替换");
        }

        public static (string, int, string) Add3(string answer)
        {
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => (char)(((x - 'A' + 3) % 26) + 'A')).ToList();
            var result = string.Join("", numbers);
            return (result, 0, "凯撒移位");
        }

        public static (string, int, string) Morse(string answer)
        {
            Dictionary<char, string> morseDict = new()
            {
                {'A', ".-" },
                {'B', "-..." },
                {'C', "-.-." },
                {'D', "-.." },
                {'E', "." },
                {'F', "..-." },
                {'G', "--." },
                {'H', "...." },
                {'I', ".." },
                {'J', ".---" },
                {'K', "-.-" },
                {'L', ".-.." },
                {'M', "--" },
                {'N', "-." },
                {'O', "---" },
                {'P', ".--." },
                {'Q', "--.-" },
                {'R', ".-." },
                {'S', "..." },
                {'T', "-" },
                {'U', "..-" },
                {'V', "...-" },
                {'W', ".--" },
                {'X', "-..-" },
                {'Y', "-.--" },
                {'Z', "--.." },
            };
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => morseDict[x]).ToList();
            var result = string.Join("/", numbers);
            return (result, 0, "摩尔斯电码");
        }

        public static (string, int, string) Binary(string answer)
        {
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => Convert.ToString((x - 'A' + 1), 2).PadLeft(5, '0')).ToList();
            var result = string.Join(" ", numbers);
            return (result, 0, "二进制");
        }

        public static (string, int, string) Braille(string answer)
        {
            Dictionary<char, string> brailleDict = new()
            {
                {'A', "⠁" },
                {'B', "⠃" },
                {'C', "⠉" },
                {'D', "⠙" },
                {'E', "⠑" },
                {'F', "⠋" },
                {'G', "⠛" },
                {'H', "⠓" },
                {'I', "⠊" },
                {'J', "⠚" },
                {'K', "⠅" },
                {'L', "⠇" },
                {'M', "⠍" },
                {'N', "⠝" },
                {'O', "⠕" },
                {'P', "⠏" },
                {'Q', "⠟" },
                {'R', "⠗" },
                {'S', "⠎" },
                {'T', "⠞" },
                {'U', "⠥" },
                {'V', "⠧" },
                {'W', "⠺" },
                {'X', "⠭" },
                {'Y', "⠽" },
                {'Z', "⠵" },
            };
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => brailleDict[x]).ToList();
            var result = string.Join(" ", numbers);
            return (result, 1, "盲文");
        }

        public static (string, int, string) Pigpen(string answer)
        {
            // 文件名命名规则
            //     1      5     6
            //    ——       \  /
            // 2 |  | 3
            //    ——       /  \
            //     4      7     8
            Dictionary<char, string> pigpenDict = new()
            {
                {'A', "い" },
                {'B', "え" },
                {'C', "ぃ" },
                {'D', "ぇ" },
                {'E', "ぉ" },
                {'F', "う" },
                {'G', "あ" },
                {'H', "ぅ" },
                {'I', "ぁ" },
                {'J', "き" },
                {'K', "け" },
                {'L', "が" },
                {'M', "ぐ" },
                {'N', "げ" },
                {'O', "く" },
                {'P', "か" },
                {'Q', "ぎ" },
                {'R', "お" },
                {'S', "こ" },
                {'T', "ご" },
                {'U', "さ" },
                {'V', "ざ" },
                {'W', "し" },
                {'X', "じ" },
                {'Y', "す" },
                {'Z', "ず" },
            };
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => pigpenDict[x]).ToList();
            var result = string.Join(" ", numbers);
            return (result, 2, "猪圈密码");
        }

        public static (string, int, string) Revserse(string answer)
        {
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Reverse().ToList();
            var result = string.Join("", numbers);
            return (result, 0, "倒序");
        }

        public static (string, int, string) FlagSemaphore(string answer)
        {
            // 文件名命名规则
            //  2   1   8
            //   \  |  /
            // 3 ——    —— 7
            //   /  |  \
            //  4   5   6
            Dictionary<char, string> flagDict = new()
            {
                {'A', "ぱ" },
                {'B', "ね" },
                {'C', "ど" },
                {'D', "つ" },
                {'E', "ぷ" },
                {'F', "ぶ" },
                {'G', "ふ" },
                {'H', "ぬ" },
                {'I', "と" },
                {'J', "て" },
                {'K', "っ" },
                {'L', "ぴ" },
                {'M', "び" },
                {'N', "ひ" },
                {'O', "で" },
                {'P', "ぢ" },
                {'Q', "ば" },
                {'R', "は" },
                {'S', "の" },
                {'T', "ち" },
                {'U', "に" },
                {'V', "づ" },
                {'W', "ぺ" },
                {'X', "べ" },
                {'Y', "な" },
                {'Z', "へ" },
            };
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => flagDict[x]).ToList();
            var result = string.Join(" ", numbers);
            return (result, 2, "旗语");
        }

        public static (string, int, string) Phone9Key(string answer)
        {
            // 1    2    3
            //     ABC  DEF
            // 4    5    6
            //GHI  JKL  MNO
            // 7    8    9
            //PQRS TUV  WXYZ
            Dictionary<char, string> phone9KeyDict = new()
            {
                {'A', "21" },
                {'B', "22" },
                {'C', "23" },
                {'D', "31" },
                {'E', "32" },
                {'F', "33" },
                {'G', "41" },
                {'H', "42" },
                {'I', "43" },
                {'J', "51" },
                {'K', "52" },
                {'L', "53" },
                {'M', "61" },
                {'N', "62" },
                {'O', "63" },
                {'P', "71" },
                {'Q', "72" },
                {'R', "73" },
                {'S', "74" },
                {'T', "81" },
                {'U', "82" },
                {'V', "83" },
                {'W', "91" },
                {'X', "92" },
                {'Y', "93" },
                {'Z', "94" },
            };
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => phone9KeyDict[x]).ToList();
            var result = string.Join(" ", numbers);
            return (result, 0, "九键键盘替换");
        }

        public static (string, int, string) Polybius(string answer)
        {
            Dictionary<char, string> polybiusDict = new()
            {
                {'A', "11" },
                {'B', "12" },
                {'C', "13" },
                {'D', "14" },
                {'E', "15" },
                {'F', "21" },
                {'G', "22" },
                {'H', "23" },
                {'I', "24" },
                {'J', "24" },
                {'K', "25" },
                {'L', "31" },
                {'M', "32" },
                {'N', "33" },
                {'O', "34" },
                {'P', "35" },
                {'Q', "41" },
                {'R', "42" },
                {'S', "43" },
                {'T', "44" },
                {'U', "45" },
                {'V', "51" },
                {'W', "52" },
                {'X', "53" },
                {'Y', "54" },
                {'Z', "55" },
            };
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => polybiusDict[x]).ToList();
            var result = string.Join(" ", numbers);
            return (result, 0, "5*5棋盘密码");
        }

        public static (string, int, string) Atbash(string answer)
        {
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();
            var numbers = answerLetters.Select(x => (char)(25 - x + 'A' + 'A')).ToList();
            var result = string.Join("", numbers);
            return (result, 0, "Atbash");
        }

        public static (string, int, string) Railfence(string answer)
        {
            var answerLetters = answer.ToUpper().Replace(" ", "").ToCharArray();

            var resultA = "";
            var resultB = "";
            for (int i = 0; i < answerLetters.Length; i++)
            {
                if (i % 2 == 0)
                {
                    resultA += answerLetters[i];
                }
                else
                {
                    resultB += answerLetters[i];
                }
            }
            var result = resultA + resultB;
            return (result, 0, "栅栏密码（2栏）");
        }

        public static async Task<string> GetPuzzleContent(int puzzleIndex, string method)
        {
            var pgdb = DbFactory.Get<PuzzleGroup>();
            var puzzleContentList = await pgdb.GetSpPrologueContent();

            var contentTemplate = "{{method}}";
            foreach (var item in puzzleContentList)
            {
                if (item.lowBound == -1) continue;
                if (puzzleIndex >= item.lowBound && puzzleIndex < item.highBound)
                {
                    contentTemplate = item.text;
                    break;
                }

                if (item.highBound == -1 && puzzleIndex >= item.lowBound)
                {
                    contentTemplate = item.text;
                }
            }

            return contentTemplate.Replace("{{method}}", method);
        }
    }
}

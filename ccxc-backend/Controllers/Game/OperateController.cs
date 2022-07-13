using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using ccxc_backend.Functions;
using ccxc_backend.Functions.PowerPoint;
using Newtonsoft.Json;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Game
{
    [Export(typeof(HttpController))]
    public class OperateController : HttpController
    {
        [HttpHandler("POST", "/check-answer")]
        public async Task CheckAnswer(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<CheckAnswerRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var now = DateTime.Now;
            var answerLogDb = DbFactory.Get<AnswerLog>();
            var answerLog = new answer_log
            {
                create_time = now,
                uid = userSession.uid,
                pid = requestJson.year,
                answer = requestJson.answer
            };

            var answer = requestJson.answer.ToLower().Replace(" ", "").Replace("-", "");

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("未确定组队？");
                return;
            }

            var gid = groupBindItem.gid;
            answerLog.gid = gid;

            //取得进度
            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            if (progress == null)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("没有进度，请返回首页重新开始。");
                return;
            }

            var progressData = progress.data;
            if (progressData == null)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            if (progressData.IsOpenMainProject == false)
            {
                await response.BadRequest("请求的部分还未解锁");
                return;
            }

            //取出待判定题目
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = await puzzleDb.SelectAllFromCache();

            /**
             * 判题流程
             * 1. 判断题目是否可见
             *   -- 不可见 -> 返回错误提示（注销登录并跳转回首页）
             *   -- 可见 -> 下一步
             * 2. 判断剩余能量是否足够（先判断是否是Meta，由于Meta消耗能量和小题不同）
             *   -- 不够 -> 返回能量不足提示
             *   -- 足够 -> 下一步
             * 3. 判断答案是否正确
             *   -- 不正确 -> {
             *      3.1. 判断是否为FinalMeta（FinalMeta有两重答案）
             *        -- 是FinalMeta -> {
             *           3.1.1. 判断是否和附加提示答案相同
             *             -- 是 -> 第二段FinalMeta标记为开，返回值标记刷新当前页，然后返回。
             *             -- 否 -> 扣减能量，返回答案错误。
             *           }
             *        -- 不是FinalMeta -> {
             *           3.1.2. 扣减能量
             *           3.1.3. 判断是否存在附加提示
             *             -- 存在 -> 返回答案错误+附加提示
             *             -- 不存在 -> 返回答案错误
             *           }
             *      }
             *   -- 正确 -> 下一步
             * 4. 判断该题是否为初次回答正确
             *   -- 不是初次回答正确 -> 返回回答正确
             *   -- 初次回答正确 -> 标记此题回答正确，然后下一步
             * 5. 判断是否为首杀
             *   -- 是首杀 -> 写入首杀数据库，然后下一步
             *   -- 不是首杀 -> 下一步
             * 6. 判断是否为FinalMeta
             *   -- 是FinalMeta -> {
             *      6.1. 判断是否已经完赛
             *        -- 已经完赛 -> 标记跳转到finalend，然后返回
             *        -- 未完赛 -> 标记完赛，然后标记跳转到finalend，然后返回
             *      }
             *   -- 不是 -> 下一步
             * 7. 判断是否为区域Meta
             *   -- 是区域Meta -> {
             *      7.1. 找出同区域所有题目，如果未解锁，将其标注为解锁
             *      7.2. 标记该区域完成
             *      7.3. 判断是否6个区域Meta都完成
             *        -- 是 -> 第一段FinalMeta标记为开，然后跳转到12
             *        -- 否 -> 跳转到12
             *      }
             *   -- 不是 -> 下一步
             * 8. 判断已答正确题数是否足够解锁该区Meta
             *   -- 足够 -> 标记本区域Meta解锁，然后下一步
             *   -- 不够 -> 下一步
             * 9. 检查本区域题目中是否全部可见
             *   -- 没有全部可见 -> 按顺序标记下一题可见，然后下一步
             *   -- 已全部可见 -> 下一步
             * 10. 检查下一区域是否全部可见
             *   -- 没有下一区域 -> 下一步
             *   -- 没有全部可见 -> 按顺序标记下一区域的下一题可见，然后下一步
             *   -- 已全部可见 -> 下一步
             * 11. 检查是否有需要返还的能量
             *   -- 有能量 -> 将能量返还，然后将存档中的记录设置为0，然后下一步
             *   -- 没有能量 -> 下一步
             * 12. 检查是否当前题目有扩展内容
             *   -- 有扩展内容 -> 返回标记刷新当前页
             *   -- 没有扩展内容 -> 返回
             */


            //1. 判定题目可见性
            var puzzleItem = puzzleList.Where(it => it.second_key == requestJson.year).FirstOrDefault();

            if (puzzleItem == null)
            {
                answerLog.status = 4;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("题目不存在或未解锁。");
                return;
            }

            //题目组需要是1~6或是7
            if (puzzleItem.pgid < 1 || puzzleItem.pgid > 7)
            {
                answerLog.status = 4;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("题目不存在或未解锁。");
                return;
            }

            //检查是否为FinalMeta、小Meta
            int wrongAnswerCost;
            if (puzzleItem.answer_type == 3)
            {
                //FinalMeta在任何时候都可进入并作答，暂时注释以下代码

                ////FinalMeta需要已解锁
                //if (!progressData.IsOpenFinalPart1)
                //{
                //    answerLog.status = 4;
                //    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                //    await response.BadRequest("题目不存在或未解锁。");
                //    return;
                //}

                wrongAnswerCost = await RedisNumberCenter.GetInt("try_meta_answer_cost");
            }
            else if (puzzleItem.answer_type == 1)
            {
                //小Meta需要对应分组开放
                if (!progressData.UnlockedMetaGroups.Contains(puzzleItem.pgid))
                {
                    answerLog.status = 4;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("题目不存在或未解锁。");
                    return;
                }

                wrongAnswerCost = await RedisNumberCenter.GetInt("try_meta_answer_cost");
            }
            else
            {
                //小题需要已解锁
                if (!progressData.UnlockedProblems.Contains(puzzleItem.second_key))
                {
                    answerLog.status = 4;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("题目不存在或未解锁。");
                    return;
                }

                wrongAnswerCost = await RedisNumberCenter.GetInt("try_answer_cost");
            }

            //2. 判断能量是否足够
            var currentPp = await PowerPoint.GetPowerPoint(progressDb, gid);
            if (currentPp < wrongAnswerCost)
            {
                answerLog.status = 3;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("能量点不足。");
                return;
            }

            //3. 判断答案是否正确
            var extendFlag = 0;
            var trueAnswer = puzzleItem.answer.ToLower().Replace(" ", "").Replace("-", "");
            if (!string.Equals(trueAnswer, answer, StringComparison.CurrentCultureIgnoreCase))
            {
                //答案错误，判断是否存在附加提示
                var addAnswerDb = DbFactory.Get<AdditionalAnswer>();
                var addAnswerListAll = await addAnswerDb.SelectAllFromCache();
                var addAnswerDict = addAnswerListAll.Where(x => x.pid == puzzleItem.pid).ToDictionary(x => x.answer.ToLower().Replace(" ", ""), x => x.message);

                var message = "答案错误";

                if (puzzleItem.answer_type == 3)
                {
                    //FinalMeta，但是符合附加提示时打开第二部分提示
                    if (addAnswerDict.ContainsKey(answer))
                    {
                        message = $"答案错误，但是获得了一些信息：{addAnswerDict[answer]}";

                        progress.data.IsOpenFinalPart2 = true;
                        await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time, it.power_point, it.power_point_update_time }).ExecuteCommandAsync();
                        
                        extendFlag = 16;
                    }
                    else
                    {
                        //扣减能量
                        await PowerPoint.UpdatePowerPoint(progressDb, gid, -wrongAnswerCost);
                    }
                }
                else
                {
                    //扣减能量
                    await PowerPoint.UpdatePowerPoint(progressDb, gid, -wrongAnswerCost);
                    if (addAnswerDict.ContainsKey(answer))
                    {
                        message = $"答案错误，但是获得了一些信息：{addAnswerDict[answer]}";
                    }
                }

                answerLog.status = 2;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.JsonResponse(406, new AnswerResponse //使用 406 Not Acceptable 作为答案错误的专用返回码。若返回 200 OK 则为答案正确
                {
                    status = 1,
                    answer_status = 2,
                    message = message,
                    extend_flag = extendFlag
                });
                return;
            }

            //答案正确
            answerLog.status = 1;
            await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

            //4. 判断是否为初次回答正确
            var successMessage = "OK";
            if (!progress.data.FinishedProblems.Contains(puzzleItem.second_key))
            {
                //标记此题回答正确
                progress.data.FinishedProblems.Add(puzzleItem.second_key);

                //5. 判断是否为首杀
                //计算是否为首杀
                var tempAnnoDb = DbFactory.Get<TempAnno>();
                var c = await tempAnnoDb.SimpleDb.AsQueryable().Where(x => x.pid == puzzleItem.second_key).CountAsync(); //TempAnno数据库中存储的pid是年份
                if (c == 0)
                {
                    //记录首杀
                    successMessage += "，顺便恭喜你在所有的平行宇宙中第一个扭转了此时间奇点";
                    var newTempAnno = new temp_anno
                    {
                        pid = puzzleItem.second_key,
                        first_solver_gid = gid,
                        first_solve_time = now
                    };

                    try
                    {
                        await tempAnnoDb.SimpleDb.AsInsertable(newTempAnno).ExecuteCommandAsync();
                    }
                    catch (Exception e)
                    {
                        Ccxc.Core.Utils.Logger.Error($"首杀数据写入失败，原因可能是：{e.Message}，附完整数据：{JsonConvert.SerializeObject(newTempAnno)}，详细信息：" + e.ToString());
                        //写入不成功可能是产生了竞争或者主键已存在。总之这里忽略掉这个异常。
                    }
                }

                //6. 判断是否为FinalMeta
                if (puzzleItem.answer_type == 3)
                {
                    //如果未标记为初次完赛则设定完赛状态
                    if (progress.is_finish == 0)
                    {
                        progress.is_finish = 1;
                        progress.finish_time = now;
                    }

                    extendFlag = 1;
                }

                //7. 判断是否为区域Meta
                else if (puzzleItem.answer_type == 1)
                {
                    //标记本区完成
                    progress.data.FinishedGroups.Add(puzzleItem.pgid);

                    //找出本区域所有题目，如果未解锁，将其全部标注为解锁。
                    var currentGroupPuzzles = puzzleList.Where(it => it.pgid == puzzleItem.pgid && it.answer_type == 0).ToList();
                    var unlockCurrentPuzzles = false;
                    foreach (var puzzle in currentGroupPuzzles)
                    {
                        if (!progress.data.UnlockedYears.Contains(puzzle.second_key))
                        {
                            progress.data.UnlockedYears.Add(puzzle.second_key);
                        }
                        if (!progress.data.VisibleProblems.Contains(puzzle.second_key))
                        {
                            progress.data.VisibleProblems.Add(puzzle.second_key);
                        }
                        if (!progress.data.UnlockedProblems.Contains(puzzle.second_key))
                        {
                            progress.data.UnlockedProblems.Add(puzzle.second_key);
                            progress.data.ProblemUnlockPowerPoint[puzzle.second_key] = 0;
                            progress.data.ProblemUnlockTime[puzzle.second_key] = now;
                            unlockCurrentPuzzles = true;
                        }
                    }

                    if (unlockCurrentPuzzles)
                    {
                        successMessage += "，沿时间线回溯探测已完成，所有穿越点均已建立";
                    }

                    //判断6个区域是否都已完成，若完成，解锁FinalMeta
                    if (progress.data.FinishedGroups.Contains(1) && progress.data.FinishedGroups.Contains(2) && progress.data.FinishedGroups.Contains(3) && progress.data.FinishedGroups.Contains(4) && progress.data.FinishedGroups.Contains(5) && progress.data.FinishedGroups.Contains(6))
                    {
                        //解锁FinalMeta
                        if (!progress.data.IsOpenFinalPart1)
                        {
                            progress.data.IsOpenFinalPart1 = true;
                            progress.data.ProblemUnlockTime[9999999] = now;
                            successMessage += "，机房大门后面传来了什么声音，时光机面板好像松了";
                        }
                    }
                }

                //小题
                else
                {
                    var currentGroupPuzzles = puzzleList.Where(it => it.pgid == puzzleItem.pgid && it.answer_type == 0).OrderBy(it => it.second_key).Select(it => it.second_key);
                    //8. 判断已答正确题数是否足够解出本区Meta
                    if (!progress.data.UnlockedMetaGroups.Contains(puzzleItem.pgid))
                    {
                        var currentGroupMetaUnlockCount = puzzleItem.pgid switch
                        {
                            1 => await RedisNumberCenter.GetInt("unlock_meta_puzzle_a"),
                            2 => await RedisNumberCenter.GetInt("unlock_meta_puzzle_b"),
                            3 => await RedisNumberCenter.GetInt("unlock_meta_puzzle_c"),
                            4 => await RedisNumberCenter.GetInt("unlock_meta_puzzle_d"),
                            5 => await RedisNumberCenter.GetInt("unlock_meta_puzzle_e"),
                            6 => await RedisNumberCenter.GetInt("unlock_meta_puzzle_f"),
                            _ => 50000
                        };

                        var count = progress.data.FinishedProblems.Where(it => currentGroupPuzzles.Contains(it)).Count();
                        if (count >= currentGroupMetaUnlockCount)
                        {
                            progress.data.UnlockedMetaGroups.Add(puzzleItem.pgid);
                            progress.data.ProblemUnlockTime[9999990 + puzzleItem.pgid] = now;
                            successMessage += "，已找到本时间线的终结点";
                        }
                    }
                    
                    //9. 检查本区域题目是否全部可见
                    foreach (var puzzleYear in currentGroupPuzzles)
                    {
                        if (!progress.data.VisibleProblems.Contains(puzzleYear))
                        {
                            progress.data.VisibleProblems.Add(puzzleYear);
                            progress.data.UnlockedYears.Add(puzzleYear);
                            break; //一次只加一个
                        }
                    }

                    //10. 检查下一区域是否全部可见
                    if (puzzleItem.pgid < 6)
                    {
                        var nextPgid = puzzleItem.pgid + 1;
                        var nextGroupPuzzles = puzzleList.Where(it => it.pgid == nextPgid && it.answer_type == 0).OrderBy(it => it.second_key).Select(it => it.second_key);
                        foreach (var puzzleYear in nextGroupPuzzles)
                        {
                            if (!progress.data.VisibleProblems.Contains(puzzleYear))
                            {
                                progress.data.VisibleProblems.Add(puzzleYear);
                                progress.data.UnlockedYears.Add(puzzleYear);
                                break; //一次只加一个
                            }
                        }
                    }

                    //11. 检查是否有需要返还的能量
                    if (progress.data.ProblemUnlockPowerPoint.ContainsKey(puzzleItem.second_key))
                    {
                        var backPp = progress.data.ProblemUnlockPowerPoint[puzzleItem.second_key];
                        if (backPp > 0)
                        {
                            progress.data.ProblemUnlockPowerPoint[puzzleItem.second_key] = 0;
                            await PowerPoint.UpdatePowerPoint(progressDb, gid, backPp);
                            successMessage += $"，回收了 {backPp} 点能量";
                        }
                    }
                }

                //回写存档
                if (puzzleItem.answer_type == 3)
                {
                    await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.power_point, it.power_point_update_time }).ExecuteCommandAsync();
                }
                else
                {
                    await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time, it.power_point, it.power_point_update_time }).ExecuteCommandAsync();
                }
                    
            }


            //12. 检查当前题目是否有扩展内容，如果有，必须刷新题目区域
            if (puzzleItem.answer_type != 3)
            {
                extendFlag = string.IsNullOrEmpty(puzzleItem.extend_content) ? 0 : 16; //如果存在扩展，extend_flag应为16，此时前端需要刷新，如果需要跳转final，extend_flag应为1。否则应为0。
            }

            //返回回答正确
            await response.JsonResponse(200, new AnswerResponse
            {
                status = 1,
                answer_status = 1,
                extend_flag = extendFlag,
                message = successMessage
            });
        }
    }
}

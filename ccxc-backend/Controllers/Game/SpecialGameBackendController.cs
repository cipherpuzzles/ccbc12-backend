using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using ccxc_backend.Functions;
using ccxc_backend.Functions.PrologueGames;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Game
{
    [Export(typeof(HttpController))]
    public class SpecialGameBackendController : HttpController
    {
        /// <summary>
        /// 序章题目详情获取：使用序章进度存档，调用模板生成题目并返回前端。
        /// </summary>
        [HttpHandler("POST", "/play/get-puzzle-detail")]
        public async Task GetPuzzleDetail(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal, true);
            if (userSession == null) return;

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);

            PrologueSaveData prologueSaveData = null;
            if (groupBindItem == null)
            {
                //单人
                var userTempDb = DbFactory.Get<UserTempProgress>();
                var userProgress = await userTempDb.SimpleDb.AsQueryable().Where(it => it.uid == userSession.uid).FirstAsync();

                if (userProgress == null)
                {
                    await response.BadRequest("没有进度，请返回首页重新开始。");
                    return;
                }

                prologueSaveData = userProgress.prologue_data;
                if (prologueSaveData == null)
                {
                    await response.BadRequest("未找到可用存档，请联系管理员。");
                    return;
                }
            }
            else
            {
                //组队
                var gid = groupBindItem.gid;

                //取得进度
                var progressDb = DbFactory.Get<Progress>();
                var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
                if (progress == null)
                {
                    await response.BadRequest("没有进度，请返回首页重新开始。");
                    return;
                }

                prologueSaveData = progress.prologue_data;
                if (prologueSaveData == null)
                {
                    await response.BadRequest("未找到可用存档，请联系管理员。");
                    return;
                }
            }

            if (prologueSaveData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            var (problemContent, usedReplacedAssets, method) = Templates.GenerateProblem(prologueSaveData);
            var content = await Templates.GetPuzzleContent(prologueSaveData.CurrentProblem, method);

            await response.JsonResponse(200, new PrologueGetPuzzleDetailResponse
            {
                status = 1,
                puzzle_id = prologueSaveData.CurrentProblem,
                problem_content = problemContent,
                used_replaced_assets = usedReplacedAssets,
                content = content
            });
        }

        /// <summary>
        /// 序章题目回答：判题并触发前进条件。
        /// </summary>
        [HttpHandler("POST", "/play/check-puzzle-answer")]
        public async Task CheckPuzzleAnswer(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal, true);
            if (userSession == null) return;

            var requestJson = request.Json<PrologueCheckAnswerRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var answerLogDb = DbFactory.Get<TempPrologueAnswerLog>();
            var answerLog = new temp_prologue_answer_log
            {
                create_time = DateTime.Now,
                uid = userSession.uid,
                pid = requestJson.pid,
                answer = requestJson.answer
            };

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);

            PrologueSaveData prologueSaveData = null;
            var gid = 0;
            user_temp_progress userProgress = null;
            progress progress = null;
            if (groupBindItem == null)
            {
                //单人
                var userTempDb = DbFactory.Get<UserTempProgress>();
                userProgress = await userTempDb.SimpleDb.AsQueryable().Where(it => it.uid == userSession.uid).FirstAsync();

                if (userProgress == null)
                {
                    answerLog.status = 5;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("没有进度，请返回首页重新开始。");
                    return;
                }

                prologueSaveData = userProgress.prologue_data;
                if (prologueSaveData == null)
                {
                    answerLog.status = 5;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("未找到可用存档，请联系管理员。");
                    return;
                }
            }
            else
            {
                //组队
                gid = groupBindItem.gid;
                answerLog.gid = gid;

                //取得进度
                var progressDb = DbFactory.Get<Progress>();
                progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
                if (progress == null)
                {
                    answerLog.status = 5;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("没有进度，请返回首页重新开始。");
                    return;
                }

                prologueSaveData = progress.prologue_data;
                if (prologueSaveData == null)
                {
                    answerLog.status = 5;
                    await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                    await response.BadRequest("未找到可用存档，请联系管理员。");
                    return;
                }
            }

            if (prologueSaveData == null)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            answerLog.template = prologueSaveData.TemplateBag[prologueSaveData.CurrentTemplateIndex];
            answerLog.correct_answer = prologueSaveData.CurrentAnswer;

            if (prologueSaveData.CurrentProblem != requestJson.pid)
            {
                answerLog.status = 5;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.BadRequest("存档进度不一致，请联系管理员。");
                return;
            }

            var answer = requestJson.answer.ToLower().Replace(" ", "");

            var result = new AnswerResponse
            {
                status = 1
            };

            if (answer == "infiniteloop")
            {
                //跳出循环
                result.answer_status = 1;

                if (gid == 0)
                {
                    //单人
                    result.extend_flag = 4; //跳转到单人剧情结局
                    result.message = "恭喜你解出Meta，跳出了这个循环，未报名用户体验到此就结束了。" +
                        "但比赛才刚刚开始，你可以继续关注排行榜和公告，以及微信公众号【密码菌】随时发出的CCBC 12资讯。比赛结束后，会在【密码菌】上第一时间公布比赛结果。" +
                        "CCBC 12结束后，你可以回到本网站，正赛题目在赛后将对所有用户开放。";

                    //回写存档
                    var userTempDb = DbFactory.Get<UserTempProgress>();
                    userProgress.prologue_data.IsFinished = true;
                    await userTempDb.SimpleDb.AsUpdateable(userProgress).ExecuteCommandAsync();
                }
                else
                {
                    //组队
                    result.extend_flag = 1; //跳转到本篇序章

                    //判断是否为初次完赛
                    if (prologueSaveData.IsFinished == false)
                    {
                        progress.prologue_data.IsFinished = true;

                        //初次完赛，初始化本篇存档
                        var puzzleDb = DbFactory.Get<Puzzle>();
                        var puzzleList = await puzzleDb.SelectAllFromCache();

                        var area1 = puzzleList.Where(it => it.pgid == 1 && it.answer_type == 0).OrderBy(it => it.second_key).ToList();
                        if (area1?.Count <= 0)
                        {
                            throw new Exception("区域1题目不存在，初始化失败。");
                        }
                        var area1ids = area1.Select(it => it.second_key).ToList();

                        var now = DateTime.Now;
                        progress.data = new SaveData
                        {
                            IsOpenMainProject = true,
                            VisibleProblems = new HashSet<int>(area1ids),
                            UnlockedYears = new HashSet<int>(area1ids),
                        };
                        progress.score = 0;
                        progress.update_time = now;
                        progress.is_finish = 0;
                        progress.power_point = await RedisNumberCenter.GetInt("initial_power_point"); //初始能量点
                        progress.power_point_update_time = now;

                        //回写存档
                        var progressDb = DbFactory.Get<Progress>();
                        await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time }).ExecuteCommandAsync();
                    }
                }

                answerLog.status = 6;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                await response.JsonResponse(200, result);
                return;
            }

            var trueAnswer = prologueSaveData.CurrentAnswer.ToLower().Replace(" ", "");
            if (!string.Equals(trueAnswer, answer, StringComparison.CurrentCultureIgnoreCase))
            {
                //答案错误
                answerLog.status = 2;
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

                result.answer_status = 2;
                result.message = "答案错误。";

                await response.JsonResponse(406, result); // 406 Not Acceptable 为本系统答案错误的专用返回码
                return;
            }

            //答案正确
            answerLog.status = 1;
            await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

            //首杀逻辑
            var tempAnnoDb = DbFactory.Get<TempPrologueAnno>();
            var c = await tempAnnoDb.SimpleDb.AsQueryable().Where(x => x.pid == requestJson.pid).CountAsync();
            if (c == 0)
            {
                var name = "";
                if (gid == 0)
                {
                    name = $"选手 {userSession.username} （未报名通道）";
                }
                else
                {
                    //取得队伍名称
                    var groupDb = DbFactory.Get<UserGroup>();
                    var groupList = await groupDb.SelectAllFromCache();

                    var groupItem = groupList.FirstOrDefault(it => it.gid == gid);

                    name = $"队伍 {groupItem?.groupname ?? ""}";
                }

                //触发首杀
                var newTempAnno = new temp_prologue_anno
                {
                    pid = requestJson.pid,
                    create_time = DateTime.Now,
                    content = $"【首杀公告】 恭喜{name} 于 {DateTime.Now:yyyy-MM-dd HH:mm:ss} 首个解出了题目 #{requestJson.pid} 。"
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

            //更新存档
            if (gid == 0)
            {
                var userTempDb = DbFactory.Get<UserTempProgress>();
                userProgress.prologue_data = RandomProblem.GetNext(userProgress.prologue_data);
                await userTempDb.SimpleDb.AsUpdateable(userProgress).ExecuteCommandAsync();
            }
            else
            {
                var progressDb = DbFactory.Get<Progress>();
                progress.prologue_data = RandomProblem.GetNext(progress.prologue_data);
                await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time }).ExecuteCommandAsync();
            }

            result.answer_status = 1;
            result.extend_flag = 0;
            result.message = "OK";
            await response.JsonResponse(200, result);
        }

        /// <summary>
        /// 序章阶段排行榜
        /// </summary>
        [HttpHandler("POST", "/play/get-puzzles-scoreboard")]
        public async Task GetPrologueScoreboard(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal, true);
            if (userSession == null) return;

            //载入所有组队信息
            var groupDb = DbFactory.Get<UserGroup>();
            var allGroup = (await groupDb.SelectAllFromCache()).ToDictionary(x => x.gid, x => x);

            //载入所有用户信息
            var userDb = DbFactory.Get<User>();
            var allUser = (await userDb.SelectAllFromCache()).ToDictionary(x => x.uid, x => x);


            //序章阶段排行榜信息需混合组队和个人结果
            var progressDb = DbFactory.Get<Progress>();
            var allProgress = await progressDb.SimpleDb.AsQueryable().ToListAsync();

            var userTempProgressDb = DbFactory.Get<UserTempProgress>();
            var allUserProgress = await userTempProgressDb.SimpleDb.AsQueryable().ToListAsync();

            var resultList = new List<PrologueScoreboardItem>();
            if (allProgress?.Count > 0)
            {
                var groupResultList = allProgress.Select(it =>
                {
                    if (allGroup.ContainsKey(it.gid))
                    {
                        var group = allGroup[it.gid];

                        var result = new PrologueScoreboardItem
                        {
                            type = 0,
                            name = group.groupname,
                            desc = group.profile,
                            number = 0,
                            last_correct_time = DateTime.MinValue
                        };

                        if (it.prologue_data != null)
                        {
                            result.number = it.prologue_data.CurrentProblem - 1;
                            result.last_correct_time = it.prologue_data.LastAcceptTime;
                        }

                        return result;
                    }

                    return null;
                }).Where(it => it != null);
                if (groupResultList?.Count() > 0)
                {
                    resultList.AddRange(groupResultList);
                }
            }

            if (allUserProgress?.Count > 0)
            {
                var userResultList = allUserProgress.Select(it =>
                {
                    if (allUser.ContainsKey(it.uid))
                    {
                        var user = allUser[it.uid];

                        var result = new PrologueScoreboardItem
                        {
                            type = 1,
                            name = user.username,
                            desc = user.profile,
                            number = 0,
                            last_correct_time = DateTime.MinValue
                        };

                        if (it.prologue_data != null)
                        {
                            result.number = it.prologue_data.CurrentProblem - 1;
                            result.last_correct_time = it.prologue_data.LastAcceptTime;
                        }

                        return result;
                    }

                    return null;
                }).Where(it => it != null);
                if (userResultList?.Count() > 0)
                {
                    resultList.AddRange(userResultList);
                }
            }

            //排序（按number倒序，如果相同就按last_correct_time升序）
            var result = resultList.OrderByDescending(x => x.number).ThenBy(x => x.last_correct_time).ToList();

            await response.JsonResponse(200, new PrologueScoreboardResponse
            {
                status = 1,
                data = result
            });
        }

        /// <summary>
        /// 序章阶段公告列表
        /// </summary>
        [HttpHandler("POST", "/play/get-puzzles-anno")]
        public async Task GetPrologueAnnouncements(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal, true);
            if (userSession == null) return;

            var requestJson = request.Json<PrologueAnnoRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            if (requestJson.page_num == 0) requestJson.page_num = 1;
            if (requestJson.page_size == 0) requestJson.page_size = 20;

            var tempAnnoDb = DbFactory.Get<TempPrologueAnno>();
            var sum = new SqlSugar.RefAsync<int>();
            var resultData = await tempAnnoDb.SimpleDb.AsQueryable().OrderBy(x => x.create_time, SqlSugar.OrderByType.Desc)
                .ToPageListAsync(requestJson.page_num, requestJson.page_size, sum);

            await response.JsonResponse(200, new PrologueAnnoResponse
            {
                status = 1,
                data = resultData,
                sum_rows = sum.Value
            });
        }

        
        [HttpHandler("POST", "/puzzle-backend/2021")]
        public async Task PuzzleBackend2021(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                await response.BadRequest("未确定组队？");
                return;
            }

            var gid = groupBindItem.gid;

            //取得进度
            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            if (progress == null)
            {
                await response.BadRequest("没有进度，请返回首页重新开始。");
                return;
            }

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            if (progressData.IsOpenMainProject == false)
            {
                await response.BadRequest("请求的部分还未解锁");
                return;
            }

            //检查2021题目是否打开
            if (!progressData.UnlockedProblems.Contains(2021))
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            //取得题目数据
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzle = await puzzleDb.SimpleDb.AsQueryable().Where(it => it.second_key == 10000001).FirstAsync();
            if (puzzle == null)
            {
                await response.BadRequest("未找到数据源。");
                return;
            }

            //取出验证信息中的时间戳
            IDictionary<string, object> headers = request.Header;
            var xAuthToken = headers["x-auth-token"].ToString();
            var xAuth = xAuthToken?.Split(" ").Select(it => it.Trim()).ToList();
            var ts = xAuth[1];

            _ = long.TryParse(ts, out long clientLocalTimeStamp);
            var localTime = Ccxc.Core.Utils.UnixTimestamp.FromTimestamp(clientLocalTimeStamp);

            string res;
            if ((localTime.Hour == 1 && localTime.Minute == 5) || (localTime.Hour == 1 && localTime.Minute == 6))
            {
                //01:05~01:06 输出音频
                res = puzzle.html;
            }
            else
            {
                //其他时间输出海报
                res = puzzle.content;
            }

            await response.JsonResponse(200, new
            {
                status = 1,
                content = res
            });
        }

        [HttpHandler("POST", "/puzzle-backend/calc")]
        public async Task PuzzleBackend2040(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<Backend2040Request>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }


            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                await response.BadRequest("未确定组队？");
                return;
            }

            var gid = groupBindItem.gid;

            //取得进度
            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            if (progress == null)
            {
                await response.BadRequest("没有进度，请返回首页重新开始。");
                return;
            }

            var progressData = progress.data;
            if (progressData == null)
            {
                await response.BadRequest("未找到可用存档，请联系管理员。");
                return;
            }

            if (progressData.IsOpenMainProject == false)
            {
                await response.BadRequest("请求的部分还未解锁");
                return;
            }

            //检查2040题目是否打开
            if (!progressData.UnlockedProblems.Contains(2040))
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            var context = requestJson.context;
            if (string.IsNullOrEmpty(requestJson.current_input))
            {
                //输入为空的时候原样返回
                await response.JsonResponse(200, new Backend2040Response
                {
                    status = 1,
                    context = context
                });
                return;
            }

            var input = requestJson.current_input[0];
            if (input == PartNumber.C_AC)
            {
                //AC-返回空结果
                await response.JsonResponse(200, new Backend2040Response
                {
                    status = 1,
                    context = CalcContext.Empty
                });
                return;
            }
            else if (context.error != 0)
            {
                //有错误时原样返回
                await response.JsonResponse(200, new
                {
                    status = 1,
                    context = context
                });
                return;
            }
            else if (input == PartNumber.C_SPACE)
            {
                //如果输入为空格：如果inputBuffer为空，则不执行任何操作。否则将inputBuffer压入buffer
                if (string.IsNullOrEmpty(context.input_buffer))
                {
                    await response.JsonResponse(200, new Backend2040Response
                    {
                        status = 1,
                        context = context
                    });
                    return;
                }
                else
                {
                    if (context.buffer.Count == 8) //如果暂存栈已经满了，就不让继续输入了
                    {
                        context.error = 4;
                        await response.JsonResponse(200, new Backend2040Response
                        {
                            status = 1,
                            context = context
                        });
                        return;
                    }
                    else
                    {
                        //将inputBuffer压入buffer，然后清空inputBuffer
                        context.buffer.Add(new PartNumber
                        {
                            type = 0,
                            content = context.input_buffer
                        });
                        context.input_buffer = "";
                        await response.JsonResponse(200, new Backend2040Response
                        {
                            status = 1,
                            context = context
                        });
                        return;
                    }
                }
            }
            else if (input == PartNumber.C_NEGATIVE)
            {
                //如果输入为负号：如果inputBuffer为空，就在inputBuffer中填入一个负号
                if (string.IsNullOrEmpty(context.input_buffer))
                {
                    context.input_buffer.Append(PartNumber.C_NEGATIVE);
                    context.screen = context.input_buffer;
                    await response.JsonResponse(200, new Backend2040Response
                    {
                        status = 1,
                        context = context
                    });
                    return;
                }
                else
                {
                    //否则，判断inputBuffer的第一个字符是否为负号
                    if (context.input_buffer[0] == PartNumber.C_NEGATIVE)
                    {
                        //如果是，则将inputBuffer的第一个字符去掉。
                        context.input_buffer = context.input_buffer[1..];
                        context.screen = context.input_buffer;
                        await response.JsonResponse(200, new Backend2040Response
                        {
                            status = 1,
                            context = context
                        });
                    }
                    else
                    {
                        //否则，在inputBuffer的开头填入一个负号
                        context.input_buffer = PartNumber.C_NEGATIVE + context.input_buffer;
                        context.screen = context.input_buffer;
                        await response.JsonResponse(200, new Backend2040Response
                        {
                            status = 1,
                            context = context
                        });
                    }
                }
            }
            else if (input == PartNumber.C_PLUS || input == PartNumber.C_MINUS || input == PartNumber.C_MULTIPLY || input == PartNumber.C_MOD)
            {
                // 如果输入为运算符：检查inputBuffer是否为空。如果inputBuffer不为空，先将inputBuffer压入buffer
                if (!string.IsNullOrEmpty(context.input_buffer))
                {
                    context.buffer.Add(new PartNumber
                    {
                        type = 0,
                        content = context.input_buffer
                    });
                    context.input_buffer = "";
                }
                // 检查buffer是否有至少两个数字，如果有，则弹出2个作为操作数
                if (context.buffer.Count >= 2)
                {
                    var num1 = context.buffer[context.buffer.Count - 2];
                    var num2 = context.buffer[context.buffer.Count - 1];
                    context.buffer.RemoveAt(context.buffer.Count - 1);
                    context.buffer.RemoveAt(context.buffer.Count - 1);

                    //执行计算
                    PartNumber result;
                    if (input == PartNumber.C_PLUS)
                    {
                        result = num1 + num2;
                    }
                    else if (input == PartNumber.C_MINUS)
                    {
                        result = num1 - num2;
                    }
                    else if (input == PartNumber.C_MULTIPLY)
                    {
                        result = num1 * num2;
                    }
                    else if (input == PartNumber.C_MOD)
                    {
                        result = num1 % num2;
                    }
                    else
                    {
                        throw new Exception("不可能发生的情况");
                    }
                    //判断结果是否存在错误
                    if (result.type == 2 || result.type == 3)
                    {
                        context.error = 1;
                    }
                    else if (result.type == 4)
                    {
                        context.error = 2;
                    }
                    else
                    {
                        context.buffer.Add(result);
                        context.screen = result.content;
                    }

                    await response.JsonResponse(200, new Backend2040Response
                    {
                        status = 1,
                        context = context
                    });
                    return;
                }
                else
                {
                    //buffer中数字不足，报错
                    context.error = 3;
                    await response.JsonResponse(200, new Backend2040Response
                    {
                        status = 1,
                        context = context
                    });
                    return;
                }
            }
            else if (PartNumber.CharDict.ContainsKey(input))
            {
                //不能输入“1”
                if (input == '0')
                {
                    context.error = 5;
                }
                //输入的是数字的新一位：追加到input_buffer中，然后让screen保持同步
                context.input_buffer += input;
                context.screen = context.input_buffer;
                await response.JsonResponse(200, new Backend2040Response
                {
                    status = 1,
                    context = context
                });
                return;
            }
            else
            {
                //输入的是非法字符，报错
                context.error = 5;
                await response.JsonResponse(200, new Backend2040Response
                {
                    status = 1,
                    context = context
                });
                return;
            }
        }
    }
}

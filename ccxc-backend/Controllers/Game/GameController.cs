using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using ccxc_backend.Functions;
using ccxc_backend.Functions.PowerPoint;
using ccxc_backend.Functions.PrologueGames;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Game
{
    [Export(typeof(HttpController))]
    public class GameController : HttpController
    {
        [HttpHandler("POST", "/start")]
        public async Task Start(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal, true);
            if (userSession == null) return;

            //尝试取得该用户组队信息
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);

            var isFirst = 0;
            if (groupBindItem == null)
            {
                //该用户无组队

                //取得进度
                var userTempProgressDb = DbFactory.Get<UserTempProgress>();
                var progress = await userTempProgressDb.SimpleDb.AsQueryable().Where(it => it.uid == userSession.uid).FirstAsync();
                if (progress == null)
                {
                    isFirst = 1;
                    //初始化
                    progress = new user_temp_progress
                    {
                        uid = userSession.uid,
                        prologue_data = RandomProblem.Init()
                    };

                    await userTempProgressDb.SimpleDb.AsInsertable(progress).ExecuteCommandAsync();
                }
            }
            else
            {
                //有组队
                var gid = groupBindItem.gid;

                //取得进度
                var progressDb = DbFactory.Get<Progress>();
                var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
                if (progress == null)
                {
                    isFirst = 1;
                    var now = DateTime.Now;
                    //初始化
                    progress = new progress
                    {
                        gid = gid,
                        data = new SaveData(),
                        score = 0,
                        update_time = now,
                        is_finish = 0,
                        penalty = 0,
                        power_point = await RedisNumberCenter.GetInt("initial_power_point"), //初始能量点
                        power_point_update_time = now,
                        prologue_data = RandomProblem.Init() //生成序章数据
                    };

                    await progressDb.SimpleDb.AsInsertable(progress).IgnoreColumns(it => new { it.finish_time }).ExecuteCommandAsync();
                    await progressDb.InvalidateCache();
                }
            }

            //登录信息存入Redis
            var ticket = $"0x{Guid.NewGuid():n}";

            var cache = DbFactory.GetCache();
            var ticketKey = cache.GetTempTicketKey(ticket);
            var ticketSession = new PuzzleLoginTicketSession
            {
                token = userSession.token
            };
            await cache.Put(ticketKey, ticketSession, 15000); //15秒内登录完成有效

            await response.JsonResponse(200, new PuzzleStartResponse
            {
                status = 1,
                ticket = ticket,
                start_prefix = Config.Config.Options.GamePrefix,
                is_first = isFirst
            });
        }

        [HttpHandler("POST", "/play/get-prologue")]
        public async Task GetPrologue(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal, true);
            if (userSession == null) return;

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
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

                var userTempData = userProgress.prologue_data;
                if (userTempData == null)
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

                var progressData = progress.prologue_data;
                if (progressData == null)
                {
                    await response.BadRequest("未找到可用存档，请联系管理员。");
                    return;
                }
            }

            var groupDb = DbFactory.Get<PuzzleGroup>();
            var prologueGroup = (await groupDb.SelectAllFromCache()).First(it => it.pg_name == "prologue");

            var prologueResult = "";
            if (prologueGroup != null)
            {
                prologueResult = prologueGroup.pg_desc;
            }

            await response.JsonResponse(200, new BasicResponse
            {
                status = 1,
                message = prologueResult
            });
        }

        [HttpHandler("POST", "/play/get-preface")]
        public async Task GetPreface(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                await response.BadRequest("用户所属队伍不存在。");
                return;
            }
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


            var groupDb = DbFactory.Get<PuzzleGroup>();
            var prologueGroup = (await groupDb.SelectAllFromCache()).First(it => it.pg_name == "preface");

            var prologueResult = "";
            if (prologueGroup != null)
            {
                prologueResult = prologueGroup.pg_desc;
            }

            await response.JsonResponse(200, new BasicResponse
            {
                status = 1,
                message = prologueResult
            });
        }

        [HttpHandler("POST", "/play/get-main-help")]
        public async Task GetCorridor(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                await response.BadRequest("用户所属队伍不存在。");
                return;
            }
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


            var groupDb = DbFactory.Get<PuzzleGroup>();
            var prologueGroup = (await groupDb.SelectAllFromCache()).First(it => it.pg_name == "main-help");

            var prologueResult = "";
            if (prologueGroup != null)
            {
                prologueResult = prologueGroup.pg_desc;
            }

            await response.JsonResponse(200, new BasicResponse
            {
                status = 1,
                message = prologueResult
            });
        }

        [HttpHandler("POST", "/play/get-year-list")]
        public async Task GetClueMatrix(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem == null)
            {
                await response.BadRequest("用户所属队伍不存在。");
                return;
            }
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

            var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
            var puzzleGroupList = await puzzleGroupDb.SelectAllFromCache();
            var resultDataDict = puzzleGroupList.Where(it => it.pgid <= 6).Select(it => new SimplePuzzleGroup
            {
                pgid = it.pgid,
                group_name = it.pg_name,
                puzzles = new List<SimplePuzzle>()
            }).ToDictionary(it => it.pgid, it => it);

            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleDbList = await puzzleDb.SelectAllFromCache();
            var puzzleDict = puzzleDbList.ToDictionary(x => x.second_key, x => x);

            //取出存档中所有可见题目
            if (progressData.VisibleProblems?.Count > 0)
            {
                foreach (var year in progressData.VisibleProblems)
                {
                    if (puzzleDict.ContainsKey(year))
                    {
                        var puzzle = puzzleDict[year];
                        resultDataDict[puzzle.pgid].puzzles.Add(new SimplePuzzle
                        {
                            year = year,
                            type = progressData.FinishedProblems.Contains(year) ? 2 : (progressData.UnlockedProblems.Contains(year) ? 1 : 0)
                        });
                    }
                }
            }

            //对每个分组中的小题排序，并插入Meta完成情况
            foreach (var (areaKey, areaItem) in resultDataDict)
            {
                areaItem.puzzles.Sort((a, b) => a.year.CompareTo(b.year));
                areaItem.meta_type = progressData.FinishedGroups.Contains(areaKey) ? 2 : (progressData.UnlockedMetaGroups.Contains(areaKey) ? 1 : 0);
                if (areaItem.meta_type != 0)
                {
                    var meta = puzzleDbList.Where(x => x.pgid == areaKey && x.answer_type == 1).FirstOrDefault();
                    if (meta != null)
                    {
                        areaItem.meta_name = meta.title;
                    }
                }
                areaItem.unlock_cost = areaKey switch
                {
                    1 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_a"),
                    2 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_b"),
                    3 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_c"),
                    4 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_d"),
                    5 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_e"),
                    6 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_f"),
                    _ => throw new Exception("不支持的题目分组"),
                };
            }

            var res = new GetYearListResponse
            {
                status = 1,
                data = resultDataDict.Select(it => it.Value).OrderBy(it => it.pgid).ToList(),
                final_meta_type = progress.is_finish == 1 ? 2 : (progressData.IsOpenFinalPart1 ? 1 : 0),
                power_point = progress.power_point,
                power_point_calc_time = progress.power_point_update_time,
                power_point_increase_rate = await RedisNumberCenter.GetInt("power_increase_rate"),
                time_probe_cost = await RedisNumberCenter.GetInt("time_probe_cost")
            };
            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/get-year-detail")]
        public async Task GetPuzzleDetail(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetPuzzleDetailRequest>();

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

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleItem = (await puzzleDb.SelectAllFromCache()).FirstOrDefault(it => it.second_key == requestJson.year);
            if (puzzleItem == null)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            bool isFinished; //是否已完成

            //检查是否可见
            //题目组需要是1~6
            if (puzzleItem.pgid < 1 || puzzleItem.pgid > 6)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }
            
            //检查是否为小Meta
            if (puzzleItem.answer_type == 1)
            {
                //小Meta需要对应分组开放
                if (!progressData.UnlockedMetaGroups.Contains(puzzleItem.pgid))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                isFinished = progressData.FinishedGroups.Contains(puzzleItem.pgid);
            }
            else
            {
                //小题需要已解锁
                if (!progressData.UnlockedProblems.Contains(puzzleItem.second_key))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                isFinished = progressData.FinishedProblems.Contains(puzzleItem.second_key);
            }

            var res = new GetPuzzleDetailResponse
            {
                status = 1,
                puzzle = new PuzzleView(puzzleItem)
                {
                    extend_content = isFinished ? puzzleItem.extend_content : "",
                    is_finish = isFinished ? 1 : 0
                },
                power_point = progress.power_point,
                power_point_calc_time = progress.power_point_update_time,
                power_point_increase_rate = await RedisNumberCenter.GetInt("power_increase_rate"),
            };

            await response.JsonResponse(200, res);
        }

        [Obsolete("目前没有地方调用此API")]
        [HttpHandler("POST", "/play/get-meta-list")]
        public async Task GetMetaList(Request request, Response response)
        {
            //var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            //if (userSession == null) return;

            ////取得该用户GID
            //var groupBindDb = DbFactory.Get<UserGroupBind>();
            //var groupBindList = await groupBindDb.SelectAllFromCache();

            //var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            //if (groupBindItem == null)
            //{
            //    await response.BadRequest("未确定组队？");
            //    return;
            //}

            //var gid = groupBindItem.gid;

            ////取得进度
            //var progressDb = DbFactory.Get<Progress>();
            //var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            //if (progress == null)
            //{
            //    await response.BadRequest("没有进度，请返回首页重新开始。");
            //    return;
            //}

            //var progressData = progress.data;
            //if (progressData == null)
            //{
            //    await response.BadRequest("未找到可用存档，请联系管理员。");
            //    return;
            //}

            //var cache = DbFactory.GetCache();
            //var openedGroupKey = cache.GetDataKey("opened-groups");

            //var openedGroup = await cache.Get<int>(openedGroupKey);
            //if (openedGroup < 1) openedGroup = 1;

            //var puzzleDb = DbFactory.Get<Puzzle>();
            //var avaliablePuzzleList = await puzzleDb.SimpleDb.AsQueryable().Where(it => it.pgid <= openedGroup && it.answer_type == 1).ToListAsync();


            //if (progressData.IsOpenPreFinal)
            //{
            //    var addList = await puzzleDb.SimpleDb.AsQueryable().Where(it => it.pgid == 4).ToListAsync();
            //    avaliablePuzzleList.AddRange(addList);
            //}

            //if (progressData.IsOpenFinalStage)
            //{
            //    var addList = await puzzleDb.SimpleDb.AsQueryable().Where(it => it.pgid == 5).ToListAsync();
            //    avaliablePuzzleList.AddRange(addList);
            //}


            //var simpleList = avaliablePuzzleList.Select(it =>
            //{
            //    var sectionType = 0;
            //    if (it.answer_type == 2 || it.answer_type == 3)
            //    {
            //        sectionType = 1;
            //    }
            //    if (it.pgid == 4)
            //    {
            //        sectionType = 1;
            //    }

            //    var r = new SimplePuzzle
            //    {
            //        pid = it.pid,
            //        title = it.title,
            //        x = sectionType,
            //        is_finished = progressData.FinishedPuzzles.Contains(it.pid) ? 1 : 0
            //    };

            //    return r;
            //}).ToList();

            //var res = new GetClueMatrixResponse
            //{
            //    status = 1,
            //    simple_puzzles = simpleList
            //};
            //await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/get-final-info")]
        public async Task GetFinalInfo(Request request, Response response)
        {
            //var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            //if (userSession == null) return;

            //var requestJson = request.Json<GetPuzzleDetailRequest>();

            ////判断请求是否有效
            //if (!Validation.Valid(requestJson, out string reason))
            //{
            //    await response.BadRequest(reason);
            //    return;
            //}

            ////取得该用户GID
            //var groupBindDb = DbFactory.Get<UserGroupBind>();
            //var groupBindList = await groupBindDb.SelectAllFromCache();

            //var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            //if (groupBindItem == null)
            //{
            //    await response.BadRequest("未确定组队？");
            //    return;
            //}

            //var gid = groupBindItem.gid;

            ////取得进度
            //var progressDb = DbFactory.Get<Progress>();
            //var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == gid).FirstAsync();
            //if (progress == null)
            //{
            //    await response.BadRequest("没有进度，请返回首页重新开始。");
            //    return;
            //}

            //var progressData = progress.data;
            //if (progressData == null)
            //{
            //    await response.BadRequest("未找到可用存档，请联系管理员。");
            //    return;
            //}

            ////取得Final题目
            //var puzzleDb = DbFactory.Get<Puzzle>();
            //var puzzleItem = (await puzzleDb.SelectAllFromCache()).First(it => it.answer_type == 3);

            //var finalInfo = "";
            //var rankTemp = 0;

            //if (puzzleItem != null)
            //{
            //    //确定该队已完成Final
            //    if (progressData.FinishedPuzzles.Contains(puzzleItem.pid))
            //    {
            //        var progressList = await progressDb.SimpleDb.AsQueryable().Where(x => x.is_finish == 1).OrderBy(x => x.finish_time, SqlSugar.OrderByType.Asc).ToListAsync();
            //        rankTemp = progressList.FindIndex(it => it.gid == gid) + 1;


            //        //题目组信息
            //        var puzzleGroupDb = DbFactory.Get<PuzzleGroup>();
            //        var finalGroup = (await puzzleGroupDb.SelectAllFromCache()).First(it => it.pgid == puzzleItem.pgid);

            //        if (finalGroup != null)
            //        {
            //            finalInfo = finalGroup.pg_desc;
            //        }
            //    }
            //}

            //await response.JsonResponse(200, new GetFinalInfoResponse
            //{
            //    status = 1,
            //    desc = finalInfo,
            //    rank_temp = rankTemp
            //});
        }

        [HttpHandler("POST", "/play/get-tips")]
        public async Task GetTips(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetPuzzleDetailRequest>();

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

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleItem = (await puzzleDb.SelectAllFromCache()).FirstOrDefault(it => it.second_key == requestJson.year);

            if (puzzleItem == null)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            int unlockTipCost; //解锁提示消耗

            //检查是否可见
            //题目组需要是1~6或是7
            if (puzzleItem.pgid < 1 || puzzleItem.pgid > 7)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            //检查是否为FinalMeta、小Meta
            if (puzzleItem.answer_type == 3)
            {
                //FinalMeta需要已解锁
                if (!progressData.IsOpenFinalPart1)
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = await RedisNumberCenter.GetInt("unlock_final_tip_cost");
            }
            else if (puzzleItem.answer_type == 1)
            {
                //小Meta需要对应分组开放
                if (!progressData.UnlockedMetaGroups.Contains(puzzleItem.pgid))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = puzzleItem.pgid switch
                {
                    1 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_a"),
                    2 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_b"),
                    3 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_c"),
                    4 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_d"),
                    5 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_e"),
                    6 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_f"),
                    _ => throw new Exception("不支持的题目分组")
                };
            }
            else
            {
                //小题需要已解锁
                if (!progressData.UnlockedProblems.Contains(puzzleItem.second_key))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = puzzleItem.pgid switch
                {
                    1 => await RedisNumberCenter.GetInt("unlock_tip_cost_a"),
                    2 => await RedisNumberCenter.GetInt("unlock_tip_cost_b"),
                    3 => await RedisNumberCenter.GetInt("unlock_tip_cost_c"),
                    4 => await RedisNumberCenter.GetInt("unlock_tip_cost_d"),
                    5 => await RedisNumberCenter.GetInt("unlock_tip_cost_e"),
                    6 => await RedisNumberCenter.GetInt("unlock_tip_cost_f"),
                    _ => throw new Exception("不支持的题目分组")
                };
            }

            //提取解锁时间
            if (!progressData.ProblemUnlockTime.ContainsKey(puzzleItem.second_key))
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }
            var unlockTime = progressData.ProblemUnlockTime[puzzleItem.second_key];

            var avaliableDelayMinute = await RedisNumberCenter.GetInt("unlock_tip_function_after");
            var avaliableTime = unlockTime.AddMinutes(avaliableDelayMinute);

            //判断当前时间是否已经达到可见时间
            var now = DateTime.Now;
            if (now < avaliableTime)
            {
                //当前时间还未到可见时间，返回不可见
                await response.JsonResponse(200, new GetPuzzleTipsResponse
                {
                    status = 1,
                    is_tip_available = 0,
                    tip_available_time = avaliableTime,
                    tip_available_progress = 100.0 * (now - unlockTime).TotalMinutes / avaliableDelayMinute
                });
                return;
            }

            //提取本题提示信息
            var tipOpened = new HashSet<int>();
            if (progressData.OpenedHints.ContainsKey(puzzleItem.second_key))
            {
                tipOpened = progressData.OpenedHints[puzzleItem.second_key];
            }
            var puzzle_tips = new List<PuzzleTip>();
            for (var i = 1; i <= 3; i++)
            {
                if (i == 1 && string.IsNullOrEmpty(puzzleItem.tips1title)) continue;
                if (i == 2 && string.IsNullOrEmpty(puzzleItem.tips2title)) continue;
                if (i == 3 && string.IsNullOrEmpty(puzzleItem.tips3title)) continue;

                var puzzleTip = new PuzzleTip
                {
                    tips_id = $"{puzzleItem.second_key}_{i}",
                    tip_num = i,
                    title = i switch
                    {
                        1 => puzzleItem.tips1title,
                        2 => puzzleItem.tips2title,
                        3 => puzzleItem.tips3title,
                        _ => null
                    },
                };

                if (tipOpened.Contains(i))
                {
                    puzzleTip.is_open = 1;
                    puzzleTip.content = i switch
                    {
                        1 => puzzleItem.tips1,
                        2 => puzzleItem.tips2,
                        3 => puzzleItem.tips3,
                        _ => null
                    };
                }

                puzzle_tips.Add(puzzleTip);
            }

            //提取人工提示信息
            var oracleDb = DbFactory.Get<DataModels.Oracle>();
            var oracleList = await oracleDb.SimpleDb.AsQueryable().Where(x => x.gid == gid && x.pid == puzzleItem.second_key).OrderBy(x => x.create_time).ToListAsync();
            var unlockDelay = await RedisNumberCenter.GetInt("manual_tip_reply_delay");

            var oracleItem = oracleList.Select(it => new OracleSimpleItem
            {
                oracle_id = it.oracle_id,
                is_reply = it.is_reply,
                unlock_time = it.create_time.AddMinutes(unlockDelay)
            }).ToList();

            var res = new GetPuzzleTipsResponse
            {
                status = 1,
                is_tip_available = 1,
                tip_available_time = avaliableTime,
                tip_available_progress = 100.0,
                unlock_cost = unlockTipCost,
                unlock_delay = unlockDelay,
                puzzle_tips = puzzle_tips,
                oracles = oracleItem
            };

            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/unlock-tips")]
        public async Task UnlockTips(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<UnlockPuzzleTipRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            if (requestJson.tip_num < 1 || requestJson.tip_num > 3)
            {
                await response.BadRequest("参数不正确");
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

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleItem = (await puzzleDb.SelectAllFromCache()).FirstOrDefault(it => it.second_key == requestJson.year);            
            if (puzzleItem == null)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            int unlockTipCost; //解锁提示消耗

            //检查是否可见
            //题目组需要是1~6或是7
            if (puzzleItem.pgid < 1 || puzzleItem.pgid > 7)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            //检查是否为FinalMeta、小Meta
            if (puzzleItem.answer_type == 3)
            {
                //FinalMeta需要已解锁
                if (!progressData.IsOpenFinalPart1)
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = await RedisNumberCenter.GetInt("unlock_final_tip_cost");
            }
            else if (puzzleItem.answer_type == 1)
            {
                //小Meta需要对应分组开放
                if (!progressData.UnlockedMetaGroups.Contains(puzzleItem.pgid))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = puzzleItem.pgid switch
                {
                    1 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_a"),
                    2 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_b"),
                    3 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_c"),
                    4 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_d"),
                    5 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_e"),
                    6 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_f"),
                    _ => throw new Exception("不支持的题目分组")
                };
            }
            else
            {
                //小题需要已解锁
                if (!progressData.UnlockedProblems.Contains(puzzleItem.second_key))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = puzzleItem.pgid switch
                {
                    1 => await RedisNumberCenter.GetInt("unlock_tip_cost_a"),
                    2 => await RedisNumberCenter.GetInt("unlock_tip_cost_b"),
                    3 => await RedisNumberCenter.GetInt("unlock_tip_cost_c"),
                    4 => await RedisNumberCenter.GetInt("unlock_tip_cost_d"),
                    5 => await RedisNumberCenter.GetInt("unlock_tip_cost_e"),
                    6 => await RedisNumberCenter.GetInt("unlock_tip_cost_f"),
                    _ => throw new Exception("不支持的题目分组")
                };
            }

            //提取解锁时间
            if (!progressData.ProblemUnlockTime.ContainsKey(puzzleItem.second_key))
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }
            var unlockTime = progressData.ProblemUnlockTime[puzzleItem.second_key];

            var avaliableDelayMinute = await RedisNumberCenter.GetInt("unlock_tip_function_after");
            var avaliableTime = unlockTime.AddMinutes(avaliableDelayMinute);

            //判断当前时间是否已经达到可见时间
            var now = DateTime.Now;
            if (now < avaliableTime)
            {
                //当前时间还未到可见时间，返回不可见
                await response.Unauthorized("分析未完成，不能提取");
                return;
            }

            //判断能量是否足够提取并扣减能量
            var isHintOpened = false;
            if (progressData.OpenedHints.ContainsKey(requestJson.year))
            {
                var openedHint = progressData.OpenedHints[requestJson.year];
                if (openedHint.Contains(requestJson.tip_num)) {
                    isHintOpened = true;
                }
            }

            if (isHintOpened)
            {
                await response.Unauthorized("您已经提取过该提示");
                return;
            }

            //未解锁，扣除能量点
            var currentPp = await PowerPoint.GetPowerPoint(progressDb, gid);

            if (currentPp < unlockTipCost)
            {
                await response.Forbidden("能量点不足");
                return;
            }
            await PowerPoint.UpdatePowerPoint(progressDb, gid, -unlockTipCost);

            //记录指定提示为open状态
            if (!progress.data.OpenedHints.ContainsKey(requestJson.year))
            {
                progress.data.OpenedHints.Add(requestJson.year, new HashSet<int>());
            }
            progress.data.OpenedHints[requestJson.year].Add(requestJson.tip_num);


            //写入日志
            var answerLogDb = DbFactory.Get<AnswerLog>();
            var answerLog = new answer_log
            {
                create_time = DateTime.Now,
                uid = userSession.uid,
                gid = gid,
                pid = requestJson.year,
                answer = $"[解锁提示 {requestJson.tip_num}]",
                status = 7
            };
            await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

            //回写进度
            await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time, it.power_point, it.power_point_update_time }).ExecuteCommandAsync();

            //返回
            await response.OK();
        }

        [HttpHandler("POST", "/play/add-oracle")]
        public async Task AddOracle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetPuzzleDetailRequest>();

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

            //取得题目详情
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleItem = (await puzzleDb.SelectAllFromCache()).FirstOrDefault(it => it.second_key == requestJson.year);
            if (puzzleItem == null)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            int unlockTipCost; //解锁提示消耗

            //检查是否可见
            //题目组需要是1~6或是7
            if (puzzleItem.pgid < 1 || puzzleItem.pgid > 7)
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }

            //检查是否为FinalMeta、小Meta
            if (puzzleItem.answer_type == 3)
            {
                //FinalMeta需要已解锁
                if (!progressData.IsOpenFinalPart1)
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = await RedisNumberCenter.GetInt("unlock_final_tip_cost");
            }
            else if (puzzleItem.answer_type == 1)
            {
                //小Meta需要对应分组开放
                if (!progressData.UnlockedMetaGroups.Contains(puzzleItem.pgid))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = puzzleItem.pgid switch
                {
                    1 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_a"),
                    2 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_b"),
                    3 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_c"),
                    4 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_d"),
                    5 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_e"),
                    6 => await RedisNumberCenter.GetInt("unlock_meta_tip_cost_f"),
                    _ => throw new Exception("不支持的题目分组")
                };
            }
            else
            {
                //小题需要已解锁
                if (!progressData.UnlockedProblems.Contains(puzzleItem.second_key))
                {
                    await response.Unauthorized("不能访问您未打开的区域");
                    return;
                }

                unlockTipCost = puzzleItem.pgid switch
                {
                    1 => await RedisNumberCenter.GetInt("unlock_tip_cost_a"),
                    2 => await RedisNumberCenter.GetInt("unlock_tip_cost_b"),
                    3 => await RedisNumberCenter.GetInt("unlock_tip_cost_c"),
                    4 => await RedisNumberCenter.GetInt("unlock_tip_cost_d"),
                    5 => await RedisNumberCenter.GetInt("unlock_tip_cost_e"),
                    6 => await RedisNumberCenter.GetInt("unlock_tip_cost_f"),
                    _ => throw new Exception("不支持的题目分组")
                };
            }

            //提取解锁时间
            if (!progressData.ProblemUnlockTime.ContainsKey(puzzleItem.second_key))
            {
                await response.Unauthorized("不能访问您未打开的区域");
                return;
            }
            var unlockTime = progressData.ProblemUnlockTime[puzzleItem.second_key];

            var avaliableDelayMinute = await RedisNumberCenter.GetInt("unlock_tip_function_after");
            var avaliableTime = unlockTime.AddMinutes(avaliableDelayMinute);

            //判断当前时间是否已经达到可见时间
            var now = DateTime.Now;
            if (now < avaliableTime)
            {
                //当前时间还未到可见时间，返回不可见
                await response.Unauthorized("分析未完成，不能提取");
                return;
            }

            //判断能量是否足够提取并扣减能量
            var currentPp = await PowerPoint.GetPowerPoint(progressDb, gid);

            if (currentPp < unlockTipCost)
            {
                await response.Forbidden("能量点不足");
                return;
            }
            await PowerPoint.UpdatePowerPoint(progressDb, gid, -unlockTipCost);

            //添加Oracle数据库
            var oracleDb = DbFactory.Get<DataModels.Oracle>();
            var oracleItem = new oracle
            {
                gid = gid,
                pid = puzzleItem.second_key,
                update_time = now,
                create_time = now,
                is_reply = 0
            };
            await oracleDb.SimpleDb.AsInsertable(oracleItem).ExecuteCommandAsync();

            //返回
            await response.OK();
        }

        [HttpHandler("POST", "/play/open-oracle")]
        public async Task OpenOracle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<OpenOracleRequest>();

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

            var oracleDb = DbFactory.Get<DataModels.Oracle>();
            var oracleItem = await oracleDb.SimpleDb.AsQueryable().Where(x => x.gid == gid && x.oracle_id == requestJson.oracle_id).FirstAsync();
            if (oracleItem == null)
            {
                await response.BadRequest("未找到该Oracle");
                return;
            }

            await response.JsonResponse(200, new OpenOracleResponse
            {
                status = 1,
                data = oracleItem
            });
        }
    }
}

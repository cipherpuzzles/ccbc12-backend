using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using ccxc_backend.Functions;
using ccxc_backend.Functions.PowerPoint;
using SqlSugar;

namespace ccxc_backend.Controllers.Game
{
    [Export(typeof(HttpController))]
    public class GameInfoController : HttpController
    {
        [HttpHandler("POST", "/play/get-last-answer-log")]
        public async Task GetLastAnswerLog(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetLastAnswerLogRequest>();

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

            //取得答题历史
            var answerLogDb = DbFactory.Get<AnswerLog>();
            var answerList = await answerLogDb.SimpleDb.AsQueryable()
                .Where(it =>
                    it.gid == gid && it.pid == requestJson.pid && (it.status == 1 || it.status == 2 || it.status == 3 || it.status == 6))
                .OrderBy(it => it.create_time, OrderByType.Desc)
                .Take(10)
                .ToListAsync();

            //取得用户名缓存
            var userDb = DbFactory.Get<User>();
            var userNameDict = (await userDb.SelectAllFromCache()).ToDictionary(it => it.uid, it => it.username);

            var resultList = answerList.Select(it => new AnswerLogView(it)
            {
                user_name = userNameDict.ContainsKey(it.uid) ? userNameDict[it.uid] : ""
            }).ToList();

            await response.JsonResponse(200, new GetLastAnswerLogResponse
            {
                status = 1,
                answer_log = resultList
            });
        }

        [HttpHandler("POST", "/play/probe-year")]
        public async Task ProbeYear(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<YearProbeRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            if (requestJson.year < 1701 || requestJson.year > 2100)
            {
                await response.BadRequest("探测的时间只能在 1701 ~ 2100 年之间。");
                return;
            }

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

            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();
            var groupName = groupList.Where(it => it.gid == gid).Select(it => it.groupname).FirstOrDefault();

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

            //检查待探测年份是否已解锁
            var usePp = false;
            if (!progressData.UnlockedYears.Contains(requestJson.year))
            {
                //未解锁，扣除能量点
                var ppCost = await RedisNumberCenter.GetInt("time_probe_cost");
                var currentPp = await PowerPoint.GetPowerPoint(progressDb, gid);

                if (currentPp < ppCost)
                {
                    await response.Forbidden("能量点不足");
                    return;
                }

                usePp = true;
                await PowerPoint.UpdatePowerPoint(progressDb, gid, -ppCost);
            }

            var message = "";
            var extraMessage = "";
            if (!usePp)
            {
                extraMessage += "本次探测未消耗能量。";
            }

            var answerLog = new answer_log
            {
                create_time = DateTime.Now,
                uid = userSession.uid,
                gid = gid,
                pid = 0,
                answer = "【探测年份】",
                status = 8
            };

            //判断待探测年份是否为题目
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = await puzzleDb.SelectAllFromCache();
            var puzzleDict = puzzleList.ToDictionary(it => it.second_key, it => it);
            if (puzzleDict.ContainsKey(requestJson.year))
            {
                message = "发现了时空奇点。";

                //更新data
                progress.data.VisibleProblems.Add(requestJson.year);
                answerLog.pid = requestJson.year;
                answerLog.answer = "[探测到题目]";
            }
            else
            {
                var yearDataDb = DbFactory.Get<TempExtendData>();
                var yearItem = await yearDataDb.SimpleDb.AsQueryable().Where(x => x.year == requestJson.year).FirstAsync();
                if (yearItem != null)
                {
                    message = yearItem.content;
                }
                answerLog.answer = $"[探测了 {requestJson.year}]";
            }

            progress.data.UnlockedYears.Add(requestJson.year);

            //回写存档
            await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time, it.power_point, it.power_point_update_time }).ExecuteCommandAsync();

            //写入日志
            if (usePp)
            {
                var answerLogDb = DbFactory.Get<AnswerLog>();
                await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();
            }

            //返回
            await response.JsonResponse(200, new YearProbeResponse
            {
                status = 1,
                message = message.Replace("{{groupName}}", groupName),
                extra_message = extraMessage
            });
        }

        [HttpHandler("POST", "/play/get-probed-year")]
        public async Task GetProbedYear(Request request, Response response)
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

            var data = progressData.UnlockedYears.OrderBy(it => it).Select(it => new ProbedYear
            {
                year = it,
                is_puzzle = progressData.VisibleProblems.Contains(it) ? 1 : 0
            }).ToList();

            await response.JsonResponse(200, new GetProbedYearsListResponse
            {
                status = 1,
                data = data
            });
        }

        [HttpHandler("POST", "/play/unlock-puzzle")]
        public async Task UnlockPuzzle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<YearProbeRequest>();

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

            if (progressData.UnlockedProblems.Contains(requestJson.year))
            {
                await response.BadRequest($"{requestJson.year} 这个年份已经扫描过了。");
                return;
            }

            //检查待解锁题目是不是可见的
            if (!progressData.VisibleProblems.Contains(requestJson.year))
            {
                await response.Forbidden($"{requestJson.year} 这个年份还未探测过，无法执行扫描。");
                return;
            }

            //读入题目列表
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleList = await puzzleDb.SelectAllFromCache();
            var puzzle = puzzleList.Where(it => it.second_key == requestJson.year).FirstOrDefault();
            if (puzzle == default)
            {
                await response.Forbidden($"{requestJson.year} 这个年份作为请求有点问题。");
                return;
            }

            //消费能量
            var ppCost = puzzle.pgid switch
            {
                1 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_a"),
                2 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_b"),
                3 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_c"),
                4 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_d"),
                5 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_e"),
                6 => await RedisNumberCenter.GetInt("unlock_puzzle_cost_f"),
                _ => throw new Exception("不应该有这种问题。")
            };
            var currentPp = await PowerPoint.GetPowerPoint(progressDb, gid);
            if (currentPp < ppCost)
            {
                await response.Forbidden("能量点不足");
                return;
            }
            await PowerPoint.UpdatePowerPoint(progressDb, gid, -ppCost);

            //更新存档
            var now = DateTime.Now;
            progress.data.UnlockedProblems.Add(requestJson.year);
            progress.data.ProblemUnlockPowerPoint[requestJson.year] = ppCost;
            progress.data.ProblemUnlockTime[requestJson.year] = now;

            //回写存档
            await progressDb.SimpleDb.AsUpdateable(progress).IgnoreColumns(it => new { it.finish_time, it.power_point, it.power_point_update_time }).ExecuteCommandAsync();

            //记录日志
            var answerLogDb = DbFactory.Get<AnswerLog>();
            var answerLog = new answer_log
            {
                create_time = now,
                uid = userSession.uid,
                gid = gid,
                pid = requestJson.year,
                answer = "[解锁题目]",
                status = 9
            };
            await answerLogDb.SimpleDb.AsInsertable(answerLog).ExecuteCommandAsync();

            //返回
            await response.OK();
        }

        [HttpHandler("POST", "/play/get-puzzle-board")]
        public async Task GetPuzzleBoard(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Member, true);
            if (userSession == null) return;

            var requestJson = request.Json<GetPuzzleBoardRequest>();

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

            //查询Redis缓存
            var now = DateTime.Now;
            var cache = DbFactory.GetCache();

            var puzzleBoardKey = cache.GetCacheKey($"puzzle_board_cache_{requestJson.type}");
            var cacheData = await cache.Client.GetObject<GetPuzzleBoardResponse>(puzzleBoardKey);
            if (cacheData != null && cacheData.cache_time > now.AddMinutes(-1))
            {
                await response.JsonResponse(200, cacheData);
                return;
            }

            //无缓存或缓存过期，重新加载数据
            var puzzleDb = DbFactory.Get<Puzzle>();
            var puzzleTitleDict = (await puzzleDb.SelectAllFromCache()).ToDictionary(it => it.second_key, it => it.title);

            var userGroupDb = DbFactory.Get<UserGroup>();
            var userNameDict = (await userGroupDb.SelectAllFromCache()).ToDictionary(it => it.gid, it => it.groupname);

            //各队题目解答数
            var progressList = await progressDb.SimpleDb.AsQueryable().ToListAsync();
            var puzzleSolveCountDict = new Dictionary<int, int>();
            foreach (var groupProgress in progressList)
            {
                var groupProgressData = groupProgress.data;
                if (groupProgressData == null) continue;
                if (groupProgressData.IsOpenMainProject == false) continue;
                if (groupProgressData.FinishedProblems?.Count > 0)
                {
                    foreach (var year in groupProgressData.FinishedProblems)
                    {
                        if (!puzzleSolveCountDict.ContainsKey(year))
                        {
                            puzzleSolveCountDict[year] = 0;
                        }
                        puzzleSolveCountDict[year]++;
                    }
                }
            }

            //统计赞踩数据
            var puzzleVoteDb = DbFactory.Get<PuzzleVote>();
            var puzzleVoteList = await puzzleVoteDb.SimpleDb.AsQueryable().ToListAsync();
            var likeVoteDict = puzzleVoteList.Where(it => it.vote == 1).GroupBy(it => it.pid).ToDictionary(it => it.Key, it => it.Count());
            var dislikeVoteDict = puzzleVoteList.Where(it => it.vote == 2).GroupBy(it => it.pid).ToDictionary(it => it.Key, it => it.Count());

            var annoDb = DbFactory.Get<TempAnno>();
            var annoList = await annoDb.SimpleDb.AsQueryable().ToListAsync();

            var resultList = new List<PuzzleBoardItem>();
            foreach (var anno in annoList)
            {
                var year = anno.pid;
                var puzzleTitle = "";
                if (puzzleTitleDict.ContainsKey(year))
                {
                    puzzleTitle = puzzleTitleDict[year];
                }

                var firstSolverGid = anno.first_solver_gid;
                var firstSolverGroupName = "";
                if (userNameDict.ContainsKey(firstSolverGid))
                {
                    firstSolverGroupName = userNameDict[firstSolverGid];
                }

                var r = new PuzzleBoardItem
                {
                    title = puzzleTitle,
                    first_solve_group_name = firstSolverGroupName,
                    first_solve_time = anno.first_solve_time,
                    solved_group_count = puzzleSolveCountDict.ContainsKey(year) ? puzzleSolveCountDict[year] : 0,
                    like_count = likeVoteDict.ContainsKey(year) ? likeVoteDict[year] : 0,
                    dislike_count = dislikeVoteDict.ContainsKey(year) ? dislikeVoteDict[year] : 0
                };

                resultList.Add(r);
            }

            //根据请求排序
            if (requestJson.type == 0)
            {
                resultList = resultList.OrderBy(it => it.first_solve_time).ToList();
            }
            else if (requestJson.type == 1)
            {
                resultList = resultList.OrderByDescending(it => it.solved_group_count).ThenBy(it => it.first_solve_time).ToList();
            }
            else if (requestJson.type == 2)
            {
                resultList = resultList.OrderByDescending(it => it.like_count).ThenBy(it => it.dislike_count).ThenByDescending(it => it.solved_group_count).ThenBy(it => it.first_solve_time).ToList();
            }
            else
            {
                resultList = resultList.OrderByDescending(it => it.dislike_count).ThenBy(it => it.like_count).ThenByDescending(it => it.solved_group_count).ThenBy(it => it.first_solve_time).ToList();
            }

            var res = new GetPuzzleBoardResponse
            {
                status = 1,
                cache_time = DateTime.Now,
                data = resultList
            };

            //存入缓存
            await cache.Client.PutObject(puzzleBoardKey, res, 65000);

            //返回
            await response.JsonResponse(200, res);
        }

        [HttpHandler("POST", "/play/get-final-end")]
        public async Task GetFinalEnd(Request request, Response response)
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

            //确定该队已经完成Final
            var rankTemp = 0;
            var finalEnd = "";
            if (progress.is_finish == 1)
            {
                var progressList = await progressDb.SimpleDb.AsQueryable().Where(x => x.is_finish == 1).OrderBy(x => x.finish_time, OrderByType.Asc).ToListAsync();
                rankTemp = progressList.FindIndex(it => it.gid == gid) + 1;

                //判断是否已完成全部6个Meta
                var finalEndKeys = "final-bad-end";
                if (progressData.FinishedGroups.Count >= 6)
                {
                    finalEndKeys = "final-true-end";
                }

                var groupDb = DbFactory.Get<PuzzleGroup>();
                var finalGroup = (await groupDb.SelectAllFromCache()).FirstOrDefault(it => it.pg_name == finalEndKeys);

                if (finalGroup != null)
                {
                    finalEnd = finalGroup.pg_desc;
                }
            }

            await response.JsonResponse(200, new GetFinalEndResponse
            {
                status = 1,
                message = finalEnd,
                rank_temp = rankTemp
            });
        }
    }
}

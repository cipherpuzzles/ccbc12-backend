using Ccxc.Core.HttpServer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System.Linq;
using Ccxc.Core.Utils;

namespace ccxc_backend.Controllers.System
{
    [Export(typeof(HttpController))]
    public class SystemController : HttpController
    {
        [HttpHandler("POST", "/get-default-setting")]
        public async Task GetDefaultSetting(Request request, Response response)
        {
            await response.JsonResponse(200, new DefaultSettingResponse
            {
                status = 1,
                start_time = Config.Config.Options.StartTime
            });
        }

        [HttpHandler("POST", "/heartbeat")]
        public async Task HeartBeat(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            await response.JsonResponse(200, new
            {
                status = 1,
            });
        }

        [HttpHandler("POST", "/heartbeat-puzzle")]
        public async Task HeartBeatPuzzle(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Normal);
            if (userSession == null) return;

            var cache = DbFactory.GetCache();
            var maxIdKey = "/ccxc-backend/datacache/last_announcement_id";
            var maxId = await cache.Get<int>(maxIdKey);

            var userReadKey = cache.GetCacheKey($"max_read_anno_id_for_{userSession.uid}");
            var userRead = await cache.Get<int>(userReadKey);

            var unread = maxId - userRead;

            var newMessage = 0; //新消息数目
            //取得该用户GID
            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            var groupBindItem = groupBindList.FirstOrDefault(it => it.uid == userSession.uid);
            if (groupBindItem != null)
            {
                var gid = groupBindItem.gid;
                var messageDb = DbFactory.Get<Message>();
                newMessage = await messageDb.SimpleDb.AsQueryable()
                    .Where(it => it.gid == gid && it.direction == 1 && it.is_read == 0).CountAsync();
            }



            await response.JsonResponse(200, new
            {
                status = 1,
                unread,
                new_message = newMessage
            });
        }


        [HttpHandler("POST", "/get-scoreboard-info")]
        public async Task GetScoreBoardInfo(Request request, Response response)
        {
            //判断是否已开赛
            var now = DateTime.Now;
            var cache = DbFactory.GetCache();

            var getNumber = false;
            var isBetaUser = false;
            if (now < UnixTimestamp.FromTimestamp(Config.Config.Options.StartTime))
            {
                //未开赛

                //判断当前用户是否登录
                IDictionary<string, object> headers = request.Header;
                if (headers.ContainsKey("user-token"))
                {
                    var userToken = headers["user-token"].ToString();

                    //从缓存中取出Session
                    var sessionKey = cache.GetUserSessionKey(userToken);
                    var session = await cache.Get<UserSession>(sessionKey);

                    if (session != null)
                    {
                        //已登录
                        if (session.is_betaUser == 1)
                        {
                            //是Beta用户
                            getNumber = true;
                            isBetaUser = true;
                        }
                    }
                }
            }
            else
            {
                //已开赛
                getNumber = true;
            }


            //取得redis缓存的排行榜数据
            var scoreboardKey = cache.GetCacheKey("scoreboard_cache");
            if (!isBetaUser) //对beta user禁用缓存
            {
                var cacheData = await cache.Client.GetObject<ScoreBoardResponse>(scoreboardKey);
                if (cacheData != null && cacheData.cache_time > now.AddMinutes(-1))
                {
                    //缓存有效，直接返回
                    await response.JsonResponse(200, cacheData);
                    return;
                }
            }

            //缓存无效，重新加载数据
            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            var progressDb = DbFactory.Get<Progress>();
            var progressList = await progressDb.SimpleDb.AsQueryable().ToListAsync();
            var progressDict = progressList.ToDictionary(it => it.gid, it => it);

            var scoreBoardList = groupList.Select(it =>
            {
                var r = new ScoreBoardItem
                {
                    gid = it.gid,
                    group_name = it.groupname,
                    group_profile = it.profile
                };

                if (getNumber && progressDict.ContainsKey(it.gid))
                {
                    var progress = progressDict[it.gid];
                    r.is_finish = progress.is_finish;

                    if (r.is_finish == 1)
                    {
                        r.total_time = (progress.finish_time - UnixTimestamp.FromTimestamp(Config.Config.Options.StartTime)).TotalHours;
                    }

                    r.finished_group_count = progress.data.FinishedGroups.Count;
                    r.finished_puzzle_count = progress.data.FinishedProblems.Count;
                    r.u = progress.data.IsOpenMainProject ? 1 : 0;
                    r.a = progress.prologue_data.CurrentProblem - 1;
                }

                return r;
            }).ToList();

            var res = new ScoreBoardResponse
            {
                status = 1,
                cache_time = now,
                finished_groups = scoreBoardList.Where(it => it.is_finish == 1)
                    .OrderBy(it => it.total_time).ThenByDescending(it => it.finished_group_count)
                    .ThenByDescending(it => it.finished_puzzle_count).ThenBy(it => it.gid).ToList(),
                groups = scoreBoardList.Where(it => it.is_finish != 1).OrderByDescending(it => it.finished_group_count)
                    .ThenByDescending(it => it.finished_puzzle_count).ThenBy(it => it.u).ThenBy(it => it.a).ThenBy(it => it.gid).ToList()
            };

            //存入缓存
            if (!isBetaUser) //对beta user禁用缓存
            {
                await cache.Client.PutObject(scoreboardKey, res, 65000);
            }

            await response.JsonResponse(200, res);
        }
    }
}

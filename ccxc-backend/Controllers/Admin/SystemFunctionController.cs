using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class SystemFunctionController : HttpController
    {
        [HttpHandler("POST", "/admin/overview")]
        public async Task Overview(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var result = new List<string>
            {
                $"服务器时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}"
            };

            var userDb = DbFactory.Get<User>();
            var userList = await userDb.SelectAllFromCache();

            result.Add($"注册用户数：{userList.Count}");

            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();

            result.Add($"有效报名人数：{groupBindList.Count}");

            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            result.Add($"报名队伍数：{groupList.Count}");

            var cache = DbFactory.GetCache();
            //登录成功
            var keyPattern = cache.GetUserSessionKey("*");
            var sessions = cache.FindKeys(keyPattern);
            var lastActionList = (await Task.WhenAll(sessions.Select(async it => await cache.Get<UserSession>(it))))
                .Where(it => it != null && it.is_active == 1)
                .GroupBy(it => it.uid)
                .Select(it => it.First() == null ? DateTime.MinValue : it.First().last_update)
                .Where(it => Math.Abs((DateTime.Now - it).TotalMinutes) < 1.1);

            result.Add($"在线人数：{lastActionList.Count()}");

            var resultString = string.Join("", result.Select(it => "<p>" + it + "</p>"));

            await response.JsonResponse(200, new
            {
                status = 1,
                result = resultString
            });
        }

        [HttpHandler("POST", "/admin/purge-cache")]
        public async Task CachePurge(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Administrator);
            if (userSession == null) return;

            var requestJson = request.Json<PurgeCacheRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            switch (requestJson.op_key)
            {
                case "anno": //公告
                    {
                        var db1 = DbFactory.Get<Announcement>();
                        var db2 = DbFactory.Get<TempAnno>();
                        var db3 = DbFactory.Get<TempPrologueAnno>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await db3.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "invi": //邀请
                    {
                        var db = DbFactory.Get<Invite>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "mess": //站内信
                    {
                        var db = DbFactory.Get<Message>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "prog": //进度
                    {
                        var db = DbFactory.Get<Progress>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "puzz": //题目
                    {
                        var db1 = DbFactory.Get<Puzzle>();
                        var db2 = DbFactory.Get<AdditionalAnswer>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "puzg": //题目组
                    {
                        var db = DbFactory.Get<PuzzleGroup>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "user": //用户
                    {
                        var db = DbFactory.Get<User>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "useg": //用户组
                    {
                        var db = DbFactory.Get<UserGroup>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "usgb": //用户绑定
                    {
                        var db = DbFactory.Get<UserGroupBind>();
                        await db.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "uall": //用户相关全部
                    {
                        var db1 = DbFactory.Get<User>();
                        var db2 = DbFactory.Get<UserGroup>();
                        var db3 = DbFactory.Get<UserGroupBind>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await db3.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "pall": //题目相关全部
                    {
                        var db1 = DbFactory.Get<Puzzle>();
                        var db2 = DbFactory.Get<PuzzleGroup>();
                        var db3 = DbFactory.Get<AdditionalAnswer>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await db3.InvalidateCache();
                        await response.OK();
                        return;
                    }
                case "aall": //全部
                    {
                        var db1 = DbFactory.Get<AdditionalAnswer>();
                        var db2 = DbFactory.Get<Announcement>();
                        var db3 = DbFactory.Get<AnswerLog>();
                        var db4 = DbFactory.Get<Invite>();
                        var db5 = DbFactory.Get<LoginLog>();
                        var db6 = DbFactory.Get<Message>();
                        var db7 = DbFactory.Get<Progress>();
                        var db8 = DbFactory.Get<Puzzle>();
                        var db9 = DbFactory.Get<PuzzleGroup>();
                        var db10 = DbFactory.Get<TempAnno>();
                        var db11 = DbFactory.Get<TempExtendData>();
                        var db12 = DbFactory.Get<TempPrologueAnno>();
                        var db13 = DbFactory.Get<TempPrologueAnswerLog>();
                        var db14 = DbFactory.Get<User>();
                        var db15 = DbFactory.Get<UserGroup>();
                        var db16 = DbFactory.Get<UserGroupBind>();
                        var db17 = DbFactory.Get<UserTempProgress>();
                        await db1.InvalidateCache();
                        await db2.InvalidateCache();
                        await db3.InvalidateCache();
                        await db4.InvalidateCache();
                        await db5.InvalidateCache();
                        await db6.InvalidateCache();
                        await db7.InvalidateCache();
                        await db8.InvalidateCache();
                        await db9.InvalidateCache();
                        await db10.InvalidateCache();
                        await db11.InvalidateCache();
                        await db12.InvalidateCache();
                        await db13.InvalidateCache();
                        await db14.InvalidateCache();
                        await db15.InvalidateCache();
                        await db16.InvalidateCache();
                        await db17.InvalidateCache();
                        await response.OK();
                        return;
                    }
                default:
                    break;
            }

            await response.BadRequest("wrong op_key");
        }
    }
}

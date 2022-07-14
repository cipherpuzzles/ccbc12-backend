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

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class GroupAdminController : HttpController
    {
        [HttpHandler("POST", "/admin/list-group-name")]
        public async Task ListGroupName(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var userGroupDb = DbFactory.Get<UserGroup>();
            var groupList = (await userGroupDb.SelectAllFromCache()).Select(it => new UserGroupNameInfo
            {
                gid = it.gid,
                groupname = it.groupname
            }).ToList();

            await response.JsonResponse(200, new UserGroupNameListResponse
            {
                status = 1,
                group_name_list = groupList
            });
        }

        [HttpHandler("POST", "/admin/add-penalty")]
        public async Task AddPenalty(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GroupAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var penaltyIncrement = Config.Config.Options.PenaltyDefault;
            var progressDb = DbFactory.Get<Progress>();

            var groupProgress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == requestJson.gid).FirstAsync();

            if (groupProgress == null)
            {
                await response.BadRequest("找不到指定队伍");
                return;
            }

            groupProgress.penalty += penaltyIncrement;

            await progressDb.SimpleDb.AsUpdateable(groupProgress).UpdateColumns(it => new {it.penalty})
                .ExecuteCommandAsync();
            await response.OK();
        }

        [HttpHandler("POST", "/admin/del-penalty")]
        public async Task DelPenalty(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GroupAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var penaltyDecrement = Config.Config.Options.PenaltyDefault;
            var progressDb = DbFactory.Get<Progress>();

            var groupProgress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == requestJson.gid).FirstAsync();

            if (groupProgress == null)
            {
                await response.BadRequest("找不到指定队伍");
                return;
            }

            groupProgress.penalty -= penaltyDecrement;

            await progressDb.SimpleDb.AsUpdateable(groupProgress).UpdateColumns(it => new { it.penalty })
                .ExecuteCommandAsync();
            await response.OK();
        }

        [HttpHandler("POST", "/admin/get-penalty")]
        public async Task GetPenalty(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GroupAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var progressDb = DbFactory.Get<Progress>();

            var groupProgress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == requestJson.gid).FirstAsync();

            if (groupProgress == null)
            {
                await response.BadRequest("找不到指定队伍");
                return;
            }

            await response.JsonResponse(200, new GetPenaltyResponse
            {
                status = 1,
                penalty = groupProgress.penalty
            });
        }

        [HttpHandler("POST", "/admin/get-group-overview")]
        public async Task GetGroupOverview(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GetGroupRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var groupDb = DbFactory.Get<UserGroup>();
            var groupList = await groupDb.SelectAllFromCache();

            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindList = await groupBindDb.SelectAllFromCache();
            var groupBindCountDict = groupBindList.GroupBy(it => it.gid).ToDictionary(it => it.Key, it => it.Count());

            var progressDb = DbFactory.Get<Progress>();
            var progressList = await progressDb.SimpleDb.AsQueryable().ToListAsync();
            var progressDict = progressList.ToDictionary(it => it.gid, it => it);

            var ppRate = await RedisNumberCenter.GetInt("power_increase_rate");

            var resList = groupList.Select(it =>
            {
                var r = new GetGroupOverview
                {
                    gid = it.gid,
                    groupname = it.groupname,
                    profile = it.profile,
                    create_time = it.create_time,
                };

                if (groupBindCountDict.ContainsKey(it.gid))
                {
                    r.member_count = groupBindCountDict[it.gid];
                }

                if (progressDict.ContainsKey(it.gid))
                {
                    var progress = progressDict[it.gid];
                    r.is_finish_prologue = progress.data.IsOpenMainProject ? 1 : 0;
                    r.prologue_progress = progress.prologue_data.CurrentProblem - 1;

                    r.finished_group_count = progress.data.FinishedGroups.Count();
                    r.finished_puzzle_count = progress.data.FinishedProblems.Count();

                    r.unlock_year_count = progress.data.UnlockedYears.Count();
                    r.unlock_puzzle_count = progress.data.UnlockedProblems.Count();
                    r.visible_puzzle_count = progress.data.VisibleProblems.Count();

                    r.is_finish = progress.is_finish;
                    r.finish_time = progress.finish_time;

                    r.power_point = progress.power_point + ppRate * (int)Math.Floor((DateTime.Now - progress.power_point_update_time).TotalMinutes);
                }

                return r;
            });

            List<GetGroupOverview> res;
            if (requestJson.order == 0)
            {
                res = resList.OrderBy(it => it.gid).ToList();
            }
            else
            {
                res = resList.OrderByDescending(it => it.is_finish).ThenBy(it => it.finish_time).ThenByDescending(it => it.finished_group_count).ThenByDescending(it => it.finished_puzzle_count)
                    .ThenByDescending(it => it.is_finish_prologue).ThenByDescending(it => it.prologue_progress).ToList();
            }

            await response.JsonResponse(200, new GetGroupOverviewResponse
            {
                status = 1,
                groups = res
            });
        }

        [HttpHandler("POST", "/admin/get-p-user-list")]
        public async Task GetPuzzleUserList(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var puzzleDb = DbFactory.Get<Puzzle>();
            var pidItems = (await puzzleDb.SelectAllFromCache()).Select(it => new PidItem
            {
                pid = it.pid,
                pgid = it.pgid,
                second_key = it.second_key,
                title = it.title
            }).OrderBy(it => it.pgid).ThenBy(it => it.pid).ToList();

            await response.JsonResponse(200, new GetUserListResponse
            {
                status = 1,
                pid_item = pidItems
            });
        }

        [HttpHandler("POST", "/admin/get-group-detail")]
        public async Task GetGroupDetail(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GroupAdminRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var groupBindDb = DbFactory.Get<UserGroupBind>();
            var groupBindUsers = new HashSet<int>((await groupBindDb.SelectAllFromCache())
                .Where(it => it.gid == requestJson.gid)
                .Select(it => it.uid));

            var userDb = DbFactory.Get<User>();
            var userList = (await userDb.SelectAllFromCache()).Where(it => groupBindUsers.Contains(it.uid))
                .Select(it => new UserNameInfoItem(it)).ToList();

            var progressDb = DbFactory.Get<Progress>();
            var progress = await progressDb.SimpleDb.AsQueryable().Where(it => it.gid == requestJson.gid).FirstAsync();

            var res = new AdminGroupDetailResponse
            {
                status = 1,
                users = userList,
                progress = progress
            };

            await response.JsonResponse(200, res);
        }
    }
}

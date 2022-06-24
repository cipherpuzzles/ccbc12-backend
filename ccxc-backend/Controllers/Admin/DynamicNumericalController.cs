using Ccxc.Core.HttpServer;
using ccxc_backend.DataModels;
using ccxc_backend.DataServices;
using ccxc_backend.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Admin
{
    [Export(typeof(HttpController))]
    public class DynamicNumericalController : HttpController
    {
        [HttpHandler("POST", "/admin/get-dynamic-numerical-set")]
        public async Task GetDynamicNumericalSet(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var result = new DynamicNumerical
            {
                initial_power_point = await RedisNumberCenter.GetInt("initial_power_point"),
                power_increase_rate = await RedisNumberCenter.GetInt("power_increase_rate"),
                unlock_puzzle_cost_a = await RedisNumberCenter.GetInt("unlock_puzzle_cost_a"),
                unlock_puzzle_cost_b = await RedisNumberCenter.GetInt("unlock_puzzle_cost_b"),
                unlock_puzzle_cost_c = await RedisNumberCenter.GetInt("unlock_puzzle_cost_c"),
                unlock_puzzle_cost_d = await RedisNumberCenter.GetInt("unlock_puzzle_cost_d"),
                unlock_puzzle_cost_e = await RedisNumberCenter.GetInt("unlock_puzzle_cost_e"),
                unlock_puzzle_cost_f = await RedisNumberCenter.GetInt("unlock_puzzle_cost_f"),
                time_probe_cost = await RedisNumberCenter.GetInt("time_probe_cost"),
                try_answer_cost = await RedisNumberCenter.GetInt("try_answer_cost"),
                try_meta_answer_cost = await RedisNumberCenter.GetInt("try_meta_answer_cost"),
                unlock_tip_function_after = await RedisNumberCenter.GetInt("unlock_tip_function_after"),
                unlock_tip_cost_a = await RedisNumberCenter.GetInt("unlock_tip_cost_a"),
                unlock_tip_cost_b = await RedisNumberCenter.GetInt("unlock_tip_cost_b"),
                unlock_tip_cost_c = await RedisNumberCenter.GetInt("unlock_tip_cost_c"),
                unlock_tip_cost_d = await RedisNumberCenter.GetInt("unlock_tip_cost_d"),
                unlock_tip_cost_e = await RedisNumberCenter.GetInt("unlock_tip_cost_e"),
                unlock_tip_cost_f = await RedisNumberCenter.GetInt("unlock_tip_cost_f"),
                unlock_meta_tip_cost_a = await RedisNumberCenter.GetInt("unlock_meta_tip_cost_a"),
                unlock_meta_tip_cost_b = await RedisNumberCenter.GetInt("unlock_meta_tip_cost_b"),
                unlock_meta_tip_cost_c = await RedisNumberCenter.GetInt("unlock_meta_tip_cost_c"),
                unlock_meta_tip_cost_d = await RedisNumberCenter.GetInt("unlock_meta_tip_cost_d"),
                unlock_meta_tip_cost_e = await RedisNumberCenter.GetInt("unlock_meta_tip_cost_e"),
                unlock_meta_tip_cost_f = await RedisNumberCenter.GetInt("unlock_meta_tip_cost_f"),
                unlock_final_a_tip_cost = await RedisNumberCenter.GetInt("unlock_final_a_tip_cost"),
                unlock_final_b_tip_cost = await RedisNumberCenter.GetInt("unlock_final_b_tip_cost"),
            };

            await response.JsonResponse(200, new GetDynamicNumericalResponse
            {
                status = 1,
                data = result
            });
        }

        [HttpHandler("POST", "/admin/update-dynamic-numerical-set")]
        public async Task UpdateDynamicNumericalSet(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<DynamicNumerical>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            await RedisNumberCenter.SetInt("initial_power_point", requestJson.initial_power_point);
            await RedisNumberCenter.SetInt("unlock_puzzle_cost_a", requestJson.unlock_puzzle_cost_a);
            await RedisNumberCenter.SetInt("unlock_puzzle_cost_b", requestJson.unlock_puzzle_cost_b);
            await RedisNumberCenter.SetInt("unlock_puzzle_cost_c", requestJson.unlock_puzzle_cost_c);
            await RedisNumberCenter.SetInt("unlock_puzzle_cost_d", requestJson.unlock_puzzle_cost_d);
            await RedisNumberCenter.SetInt("unlock_puzzle_cost_e", requestJson.unlock_puzzle_cost_e);
            await RedisNumberCenter.SetInt("unlock_puzzle_cost_f", requestJson.unlock_puzzle_cost_f);
            await RedisNumberCenter.SetInt("time_probe_cost", requestJson.time_probe_cost);
            await RedisNumberCenter.SetInt("try_answer_cost", requestJson.try_answer_cost);
            await RedisNumberCenter.SetInt("try_meta_answer_cost", requestJson.try_meta_answer_cost);
            await RedisNumberCenter.SetInt("unlock_tip_function_after", requestJson.unlock_tip_function_after);
            await RedisNumberCenter.SetInt("unlock_tip_cost_a", requestJson.unlock_tip_cost_a);
            await RedisNumberCenter.SetInt("unlock_tip_cost_b", requestJson.unlock_tip_cost_b);
            await RedisNumberCenter.SetInt("unlock_tip_cost_c", requestJson.unlock_tip_cost_c);
            await RedisNumberCenter.SetInt("unlock_tip_cost_d", requestJson.unlock_tip_cost_d);
            await RedisNumberCenter.SetInt("unlock_tip_cost_e", requestJson.unlock_tip_cost_e);
            await RedisNumberCenter.SetInt("unlock_tip_cost_f", requestJson.unlock_tip_cost_f);
            await RedisNumberCenter.SetInt("unlock_meta_tip_cost_a", requestJson.unlock_meta_tip_cost_a);
            await RedisNumberCenter.SetInt("unlock_meta_tip_cost_b", requestJson.unlock_meta_tip_cost_b);
            await RedisNumberCenter.SetInt("unlock_meta_tip_cost_c", requestJson.unlock_meta_tip_cost_c);
            await RedisNumberCenter.SetInt("unlock_meta_tip_cost_d", requestJson.unlock_meta_tip_cost_d);
            await RedisNumberCenter.SetInt("unlock_meta_tip_cost_e", requestJson.unlock_meta_tip_cost_e);
            await RedisNumberCenter.SetInt("unlock_meta_tip_cost_f", requestJson.unlock_meta_tip_cost_f);
            await RedisNumberCenter.SetInt("unlock_final_a_tip_cost", requestJson.unlock_final_a_tip_cost);
            await RedisNumberCenter.SetInt("unlock_final_b_tip_cost", requestJson.unlock_final_b_tip_cost);

            await response.OK();
        }

        [HttpHandler("POST", "/admin/update-power-increase-rate")]
        public async Task UpdatePowerIncreaseRate(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Administrator);
            if (userSession == null) return;

            var requestJson = request.Json<DynamicNumerical>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            //刷新全员当前能量点
            //遍历所有progress，提取所有通过了序章的progress
            var progressDb = DbFactory.Get<Progress>();
            var progressList = await progressDb.SimpleDb.AsQueryable().ToListAsync();

            var mainPartList = progressList.Where(it => it.data.IsOpenMainProject);
            foreach (var progressItem in mainPartList)
            {
                await Functions.PowerPoint.PowerPoint.UpdatePowerPoint(progressDb, progressItem.gid, 0);
            }

            //更新能量增速
            await RedisNumberCenter.SetInt("power_increase_rate", requestJson.power_increase_rate);

            await response.OK();
        }
    }
}

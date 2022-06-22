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
    public class TempExtendController : HttpController
    {
        [HttpHandler("POST", "/admin/get-temp-extend-data")]
        public async Task GetExtendController(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<GetTempExtendRequest>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var extendDataDb = DbFactory.Get<TempExtendData>();
            var extendDataList = await extendDataDb.SimpleDb.AsQueryable().Where(x => x.year >= requestJson.start && x.year < requestJson.start + 100).OrderBy(x => x.year).ToListAsync();

            await response.JsonResponse(200, new GetTempExtendResponse
            {
                status = 1,
                data = extendDataList,
            });
        }

        [HttpHandler("POST", "/admin/update-temp-extend-data")]
        public async Task UpdateExtendController(Request request, Response response)
        {
            var userSession = await CheckAuth.Check(request, response, AuthLevel.Organizer);
            if (userSession == null) return;

            var requestJson = request.Json<temp_extend_data>();

            //判断请求是否有效
            if (!Validation.Valid(requestJson, out string reason))
            {
                await response.BadRequest(reason);
                return;
            }

            var extendDataDb = DbFactory.Get<TempExtendData>();
            var updateDb = extendDataDb.Db.Storageable(requestJson).ToStorage();
            await updateDb.AsInsertable.ExecuteCommandAsync();
            await updateDb.AsUpdateable.ExecuteCommandAsync();

            await response.OK();
        }
    }
}

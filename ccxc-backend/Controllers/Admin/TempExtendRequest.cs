using ccxc_backend.DataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccxc_backend.Controllers.Admin
{
    public class GetTempExtendRequest
    {
        public int start { get; set; }
    }

    public class GetTempExtendResponse : BasicResponse
    {
        public List<temp_extend_data> data { get; set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Text;
using Ccxc.Core.Utils.ExtensionFunctions;
using ccxc_backend.DataModels;
using Newtonsoft.Json;

namespace ccxc_backend.Controllers.Admin
{
    public class UserGroupNameListResponse : BasicResponse
    {
        public List<UserGroupNameInfo> group_name_list { get; set; }
    }

    public class UserGroupNameInfo
    {
        public int gid { get; set; }
        public string groupname { get; set; }
    }

    public class GroupAdminRequest
    {
        public int gid { get; set; }
    }

    public class GetPenaltyResponse : BasicResponse
    {
        public double penalty { get; set; }
    }

    public class GetGroupOverviewResponse : BasicResponse
    {
        public List<GetGroupOverview> groups { get; set; }
        public int sum_rows { get; set; }
    }

    public class GetGroupOverview
    {
        public int gid { get; set; }
        public string groupname { get; set; }
        public string profile { get; set; }
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime create_time { get; set; }

        public int member_count { get; set; }

        public int is_finish_prologue { get; set; }
        public int prologue_progress { get; set; }
        
        public int finished_group_count { get; set; }
        public int finished_puzzle_count { get; set; }
        public int unlock_year_count { get; set; }
        public int unlock_puzzle_count { get; set; }
        public int visible_puzzle_count { get; set; }
        
        public int power_point { get; set; }
        public int is_finish { get; set; }
        [JsonConverter(typeof(UnixTimestampConverter))]
        public DateTime finish_time { get; set; }
    }

    public class GetGroupRequest
    {
        /// <summary>
        /// 0-默认顺序（GID顺序） 1-排行榜顺序（分数）
        /// </summary>
        public int order { get; set; }
        
        public string groupname { get; set; }
        public int page_num { get; set; }
        public int page_size { get; set; }
    }

    public class AdminGroupDetailResponse : BasicResponse
    {
        public List<UserNameInfoItem> users { get; set; }
        public progress progress { get; set; }
    }

    public class UserNameInfoItem
    {
        public UserNameInfoItem(user u)
        {
            uid = u.uid;
            username = u.username;
            roleid = u.roleid;
        }

        public int uid { get; set; }
        public string username { get; set; }
        public int roleid { get; set; }
    }
}

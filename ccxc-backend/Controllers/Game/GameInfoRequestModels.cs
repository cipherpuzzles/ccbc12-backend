﻿using System;
using System.Collections.Generic;
using System.Text;
using ccxc_backend.DataModels;

namespace ccxc_backend.Controllers.Game
{
    public class GetLastAnswerLogRequest
    {
        public int pid { get; set; }
    }

    public class GetLastAnswerLogResponse : BasicResponse
    {
        public List<AnswerLogView> answer_log { get; set; }
    }

    public class AnswerLogView : answer_log
    {
        public AnswerLogView(answer_log a)
        {
            id = a.id;
            create_time = a.create_time;
            uid = a.uid;
            gid = a.gid;
            pid = a.pid;
            answer = a.answer;
            status = a.status;
        }

        public string user_name { get; set; }
    }

    public class GetClueMatrixResponse : BasicResponse
    {
        public List<SimplePuzzle> simple_puzzles { get; set; }
    }

    public class SimplePuzzle
    {
        public int pid { get; set; }
        public string title { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int is_finished { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Work1.Models
{
    public class LineLoginToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string id_token { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
    }

    public class LineProfile
    {
        public string access_token { get; set; }
        public string userId { get; set; }
        public string displayName { get; set; }
        public string pictureUrl { get; set; }
        public string status { get; set; }
    }

    public class LineNotifyToken
    {
        public string status { get; set; }
        public string message { get; set; }
        public string access_token { get; set; }
    }
}
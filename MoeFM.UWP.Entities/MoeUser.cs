using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities
{
    public class MoeUser
    {
        public string uid { get; set; }
        public string user_name { get; set; }
        public string user_nickname { get; set; }
        public string user_sex { get; set; }
        public int user_registered { get; set; }
        public int user_lastactivity { get; set; }
        public string user_status { get; set; }
        public string user_level { get; set; }
        public string user_icon { get; set; }
        public string user_desc { get; set; }
        public string user_url { get; set; }
        public string user_fm_url { get; set; }
        public MoeCover user_avatar { get; set; }
        public int follower_count { get; set; }
        public int following_count { get; set; }
        public int group_count { get; set; }
        public string about { get; set; }
    }
}

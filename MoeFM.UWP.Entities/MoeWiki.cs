using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities
{
    public class MoeWiki
    {
        public int wiki_id { get; set; }

        public string wiki_title { get; set; }

        public string wiki_title_encode { get; set; }

        public string wiki_name { get; set; }

        public string wiki_type { get; set; }

        public int wiki_parent { get; set; }

        public int wiki_date { get; set; }

        public int wiki_modified { get; set; }

        public int wiki_modified_user { get; set; }

        public List<Dictionary<object, object>> wiki_meta { get; set; }

        public string fav_count { get; set; }

        public string wiki_fm_url { get; set; }

        public string wiki_url { get; set; }

        public MoeCover wiki_cover { get; set; }

        public int wiki_fav_count { get; set; }

        public object wiki_user_fav { get; set; }
    }
}

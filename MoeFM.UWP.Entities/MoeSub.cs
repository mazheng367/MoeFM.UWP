using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities
{
    public class MoeSub
    {
        public int sub_id { get; set; }
        public int sub_parent_wiki { get; set; }
        public int sub_parent { get; set; }
        public string sub_title { get; set; }
        public string sub_title_encode { get; set; }
        public string sub_type { get; set; }
        public string sub_order { get; set; }
        public object sub_meta { get; set; }
        public string sub_about { get; set; }
        public string sub_comment_count { get; set; }
        public object sub_data { get; set; }
        public int sub_date { get; set; }
        public int sub_modified { get; set; }
        public string sub_url { get; set; }
        public string sub_fm_url { get; set; }
        public string sub_view_title { get; set; }
        public List<MoeSubUpload> sub_upload { get; set; }
        public MoeWiki wiki { get; set; }
    }
}

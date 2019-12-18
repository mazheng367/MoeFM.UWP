using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities
{
    public class MoeSubUpload
    {
        public int up_id { get; set; }
        public int up_uid { get; set; }
        public int up_obj_id { get; set; }
        public string up_obj_type { get; set; }
        public string up_uri { get; set; }
        public string up_type { get; set; }
        public string up_md5 { get; set; }
        public int up_size { get; set; }
        public string up_quality { get; set; }
        public MoeUpData up_data { get; set; }
        public int up_date { get; set; }
        public string up_url { get; set; }
    }
}

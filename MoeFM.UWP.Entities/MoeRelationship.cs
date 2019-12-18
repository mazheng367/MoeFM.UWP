using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming
namespace MoeFM.UWP.Entities
{
    public class MoeRelationship
    {
        public int? wr_id { get; set; }
        public int? wr_obj1 { get; set; }
        public string wr_obj1_type { get; set; }
        public int? wr_obj2 { get; set; }
        public string wr_obj2_type { get; set; }
        public int? wr_order { get; set; }
        public string wr_about { get; set; }

        public MoeSub obj { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities.MoeFMEntity
{
    public class MoeRelationshipEntity : MoeBaseEntity
    {
        public List<MoeRelationship> relationships { get; set; }
    }
}

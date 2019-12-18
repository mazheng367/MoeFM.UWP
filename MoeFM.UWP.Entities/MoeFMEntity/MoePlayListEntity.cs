using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities.MoeFMEntity
{
    public class MoePlayListEntity : MoeBaseEntity
    {
        public List<PlayListItem> playlist { get; set; }
    }
}

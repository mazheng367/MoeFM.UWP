using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities.MoeFMEntity
{
    public class MoeFavSubEntity<T> : MoeBaseEntity
    {
        public List<MoeFavSub<T>> favs { get; set; }
    }
}
using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities.MoeFMEntity
{
    public class MoeHomePageEntity : MoeBaseEntity
    {
        public List<MoeWiki> hot_radios { get; set; }

        public List<MoeWiki> hot_musics { get; set; }
    }
}

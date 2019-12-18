using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities
{
    public class MoeInformation
    {
        public Dictionary<string, string> parameters { get; set; }

        public List<string> msg { get; set; }

        public bool has_error { get; set; }

        public int error { get; set; }

        public string request { get; set; }

        public int page { get; set; }

        public int item_count { get; set; }

        public int perpage { get; set; }

        public int count { get; set; }

        public bool is_target { get; set; }

        public bool may_have_next { get; set; }

        public string next_url { get; set; }
    }
}
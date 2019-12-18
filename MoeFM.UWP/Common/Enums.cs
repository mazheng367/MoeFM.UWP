using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFM.UWP.Common
{
    public enum WikiType
    {
        Music = 0,
        Radio = 1,
        Song = 2
    }

    public class ClickInfo
    {
        public string Id;
        public WikiType WikiType;
        public object Data;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoeFM.UWP.Entities.MoeFMEntity;

// ReSharper disable InconsistentNaming

namespace MoeFM.UWP.Entities.Response
{
    public class MoeResponse<T>
        where T : MoeBaseEntity
    {
        public T response { get; set; }
    }
}

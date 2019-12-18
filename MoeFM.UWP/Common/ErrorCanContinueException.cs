using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoeFM.UWP.Common
{
    internal class ErrorCanContinueException : MoeException
    {
        public override string Message { get; }

        public ErrorCanContinueException(string message)
        {
            Message = message;
        }
    }
}

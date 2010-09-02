using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NuPackConsole
{
    class EventArgs<T> : EventArgs
    {
        public T Arg { get; private set; }

        public EventArgs(T arg)
        {
            this.Arg = arg;
        }
    }
}

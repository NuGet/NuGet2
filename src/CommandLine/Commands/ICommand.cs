using System.Collections.Generic;

namespace NuGet {

    public interface ICommand {
        List<string> Arguments { get; set; }
        void Execute();
    }
}

namespace NuGet {

    using System.Collections.Generic;

    public interface ICommand {
        List<string> Arguments { get; set; }
        void Execute();
    }
}

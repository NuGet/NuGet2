using System.Collections.Generic;

namespace NuGet {
    public interface IBatchProcessor<T> {
        void Begin(IEnumerable<T> batch);
        void End();
    }
}

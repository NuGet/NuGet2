using System.Collections.Generic;

namespace NuGet
{
    public interface IBatchProcessor<T>
    {
        void BeginProcessing(IEnumerable<T> batch);
        void EndProcessing();
    }
}

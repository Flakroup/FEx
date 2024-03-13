using FEx.Asyncx.Collections;
using FEx.MVVM.Subjects;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;

namespace FEx.MVVM.Utilities;

public class ProgressChangesBuffer : AsyncBuffer<Timestamped<IProgressChange>>
{
    protected override IList<Timestamped<IProgressChange>> RetrieveFromBuffer(
        IList<Timestamped<IProgressChange>> buffer)
    {
        var changes = buffer.OrderBy(change => change.Timestamp).ToList();

        buffer.Clear();

        return changes;
    }
}
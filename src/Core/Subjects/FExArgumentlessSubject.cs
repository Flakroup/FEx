using FEx.Core.Abstractions.Subjects;

namespace FEx.Core.Subjects;

public class FExArgumentlessSubject : FExSubject<bool>
{
    public void OnNext() => base.OnNext(false);
}
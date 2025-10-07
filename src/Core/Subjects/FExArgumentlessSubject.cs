namespace FEx.Rx.Subjects;

public class FExArgumentlessSubject : FExSubject<bool>
{
    public void OnNext() => base.OnNext(false);
}
namespace FEx.Rx;

public class FExArgumentlessSubject : FExSubject<bool>
{
    public void OnNext()
    {
        base.OnNext(false);
    }
}
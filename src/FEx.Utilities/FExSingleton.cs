namespace FEx.Utilities;

public abstract class FExSingleton
    : IDisposable
{
    private static readonly List<FExSingleton> Singletons = new();

    public static void ClearAllSingletons()
    {
        lock (Singletons)
        {
            foreach (FExSingleton s in Singletons)
            {
                s.Dispose();
            }

            Singletons.Clear();
        }
    }

    protected FExSingleton()
    {
        lock (Singletons)
        {
            Singletons.Add(this);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected abstract void Dispose(bool isDisposing);

    ~FExSingleton()
    {
        Dispose(false);
    }
}

public abstract class FExSingleton<T>
    : FExSingleton
    where T : class, new()
{
    private static volatile T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (SyncRoot)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }

            return _instance;
        }
    }

    private static object SyncRoot { get; } = new();

    protected override void Dispose(bool isDisposing)
    {
        if (isDisposing)
        {
            _instance = null;
        }
    }
}
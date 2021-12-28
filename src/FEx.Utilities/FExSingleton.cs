namespace FEx.Utilities
{
    public abstract class FExSingleton
        : IDisposable
    {
        ~FExSingleton()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool isDisposing);

        private static readonly List<FExSingleton> Singletons = new List<FExSingleton>();

        protected FExSingleton()
        {
            lock (Singletons)
            {
                Singletons.Add(this);
            }
        }

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
    }

    public abstract class FExSingleton<T>
        : FExSingleton
        where T : class, new()
    {
        private static object SyncRoot { get; } = new object();

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

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _instance = null;
            }
        }
    }
}

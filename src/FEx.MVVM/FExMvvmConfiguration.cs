using System;

namespace FEx.MVVM
{
    public class FExMvvmConfiguration
    {
        public static TimeSpan DefaultUIRefreshInterval { get; set; } = TimeSpan.FromMilliseconds(250);
    }
}

using FEx.Abstractions;
using System;
using System.Diagnostics;

namespace FEx.Utilities
{
    public class DebugExceptionHandler : IExceptionHandler
    {
        public bool CanHandle(Exception exception) => true;

        public void Handle(Exception exception)
        {
            Debug.WriteLine(exception.ToString());
        }
    }
}

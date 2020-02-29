using System;
using System.Collections.Generic;
using System.Text;

namespace Unicord
{
    public interface IVersionProvider
    {
        string GetVersionString();
    }

    public static class VersionHelper
    {
        private static IVersionProvider _versionProvider;

        public static T RegisterVersionProvider<T>() where T : IVersionProvider, new()
        {
            if (_versionProvider != null)
                throw new Exception("Version Provider has already been registered!");

            return (T)(_versionProvider = new T());
        }

        public static string VersionString => _versionProvider?.GetVersionString() ?? "";
    }
}

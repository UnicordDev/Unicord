using System;
using System.Threading;
using Windows.UI.Composition;

namespace Unicord.Universal.Services
{
    internal abstract class BaseService<T> where T : BaseService<T>, new()
    {
        private static ThreadLocal<T> _serviceStore
            = new ThreadLocal<T>(() => new T(), true);

        public static T GetForCurrentView()
        {
            var val = _serviceStore.Value;
            if (!val._isInitialised)
            {
                val.InitializeCore();
            }

            return val;
        }

        public static void Reset()
        {
            foreach (var val in _serviceStore.Values)
            {
                if (val is IDisposable disposable)
                    disposable.Dispose();
            }

            _serviceStore.Dispose();
            _serviceStore = new ThreadLocal<T>(() => new T(), true);
        }

        private void InitializeCore()
        {
            _isInitialised = true;
            Initialise();
        }

        protected bool _isInitialised;
        protected virtual void Initialise() { }
    }
}

using System.Threading;

namespace Unicord.Universal.Services
{
    internal abstract class BaseService<T> where T: BaseService<T>, new()
    {
        private static ThreadLocal<T> _serviceStore
            = new ThreadLocal<T>(() => new T(), true);

        public static T GetForCurrentView()
        {
            var val = _serviceStore.Value;
            if (!val._isInitialised)
            {
                val.Initialise();
            }

            return val;
        }

        public static void Reset()
        {
            _serviceStore.Dispose();
            _serviceStore = new ThreadLocal<T>(() => new T(), true);
        }

        protected bool _isInitialised;
        protected virtual void Initialise()
        {
            _isInitialised = true;
        }
    }
}

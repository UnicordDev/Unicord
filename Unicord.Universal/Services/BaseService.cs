using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unicord.Universal.Services
{
    internal abstract class BaseService<T> where T: BaseService<T>, new()
    {
        private static ThreadLocal<T> _serviceStore
            = new ThreadLocal<T>(() => new T(), true);

        public static T GetForCurrentView()
        {
            var val = _serviceStore.Value;
            if (!val.isInitialised)
            {
                val.Initialise();
            }

            return val;
        }

        protected bool isInitialised;
        protected virtual void Initialise()
        {
            isInitialised = true;
        }
    }
}

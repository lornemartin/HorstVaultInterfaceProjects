using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PrintPDF
{
    // Single persistent STA thread for all Inventor COM calls.
    // Creating a new STA thread per job destroys the COM apartment on exit,
    // invalidating any stubs still held in the process and crashing the JP delegate.
    internal sealed class StaDispatcher
    {
        private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>();

        public StaDispatcher()
        {
            var thread = new Thread(() =>
            {
                foreach (var action in _queue.GetConsumingEnumerable())
                    action();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Invoke(Action action)
        {
            ExceptionDispatchInfo captured = null;
            var done = new ManualResetEventSlim(false);
            _queue.Add(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
                finally { done.Set(); }
            });
            done.Wait();
            captured?.Throw();
        }
    }
}

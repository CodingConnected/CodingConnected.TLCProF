using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mono.Unix.Native;

namespace CodingConnected.TLCProF.Generic
{
    // From: https://stackoverflow.com/questions/37814505/mono-high-resolution-timer-on-linux
    public class PosixHiPrecTimer
    {
        #region Fields

        private const uint SafeDelay = 0; // millisecond (for slightly early wakeup)
        private readonly Stopwatch _watch = new Stopwatch();
        private bool _enabled;
        private Timespec _pendingNanosleepParams;
        private Timespec _threadNanosleepParams;
        private readonly object _lockObject = new object();

        #endregion // Fields

        #region Properties

        internal bool Enabled
        {
            get => _enabled;
            set
            {
                if (value && !_enabled)
                {
                    _watch.Start();
                    _enabled = true;
                    Task.Run(TickGenerator); // fire up new thread
                }
                else
                {
                    lock (_lockObject)
                    {
                        _enabled = false;
                    }
                }
            }
        }

        public long Interval
        {
            get
            {
                double totalNanoseconds;
                lock (_lockObject)
                {
                    totalNanoseconds = 1e9 * _pendingNanosleepParams.tv_sec + _pendingNanosleepParams.tv_nsec;
                }
                return (int)(totalNanoseconds * 1e-6); // return value in ms
            }
            set
            {
                lock (_lockObject)
                {
                    _pendingNanosleepParams.tv_sec = value / 1000;
                    _pendingNanosleepParams.tv_nsec = (long)((value % 1000) * 1e6); //set value in ns
                }
            }
        }

        #endregion // Properties

        #region Events

        public event EventHandler Tick; // Tick event 

        #endregion // Events

        #region Private Methods

        private Task TickGenerator()
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            while (true)
            {
                // Check if thread has been told to halt
                lock (_lockObject)
                {
                    if (!_enabled) break;
                }
                var curTime = _watch.ElapsedMilliseconds;
                if (curTime >= Interval)
                {
                    _watch.Restart();
                    Tick?.Invoke(this, new EventArgs());
                }
                else
                {
                    var iTimeLeft = (Interval - curTime); // How long to delay for 
                    if (iTimeLeft < SafeDelay) continue;
                    // Task.Delay has resolution 15ms//await Task.Delay(TimeSpan.FromMilliseconds(iTimeLeft - safeDelay));
                    _threadNanosleepParams.tv_nsec = (int)((iTimeLeft - SafeDelay) * 1e6);
                    _threadNanosleepParams.tv_sec = 0;
                    Syscall.nanosleep(ref _threadNanosleepParams, ref _threadNanosleepParams);
                }

            }
            _watch.Stop();
            return null;
        }

        #endregion // Private Methods
    }
}

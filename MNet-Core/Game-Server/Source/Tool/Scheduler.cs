using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

using System.Threading;
using System.Threading.Tasks;

namespace MNet
{
    public class Scheduler
    {
        public long Interval { get; protected set; }

        public long DeltaTime { get; protected set; }

        public long ElapsedTime { get; protected set; }

        Thread thread;
        Stopwatch stopwatch;

        public bool Running { get; protected set; } = true;

        public void Start()
        {
            Running = true;

            thread.Start();
        }

        void Procedure()
        {
            stopwatch = new Stopwatch();

            while (Running) Tick();
        }

        public delegate void Delegate();
        Delegate callback;
        void Tick()
        {
            stopwatch.Start();

            callback?.Invoke();

            var elapsed = stopwatch.ElapsedMilliseconds;

            if (Interval > elapsed) Sleep(Interval - elapsed);

            DeltaTime = stopwatch.ElapsedMilliseconds;

            ElapsedTime += DeltaTime;

            stopwatch.Reset();
        }

        void Sleep(long duration) => Thread.Sleep((int)duration);

        public void Stop()
        {
            Running = false;
        }

        public Scheduler(long interval, Delegate callback)
        {
            this.Interval = interval;

            ElapsedTime = 0;
            DeltaTime = interval;

            this.callback = callback;

            thread = new Thread(Procedure);
        }
    }
}
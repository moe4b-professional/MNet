using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

namespace Backend
{
    public class Schedule
    {
        public float ElapsedTime { get; protected set; }
        public float DeltaTime { get; protected set; }

        public long Interval { get; protected set; }

        Thread thread;
        Stopwatch stopwatch;

        bool run = true;

        public void Start()
        {
            run = true;

            thread.Start();
        }

        void Procedure()
        {
            stopwatch = new Stopwatch();

            while (run) Tick();
        }

        public delegate void Delegate();
        Delegate callback;
        void Tick()
        {
            stopwatch.Start();

            callback?.Invoke();

            var elapsed = stopwatch.ElapsedMilliseconds;

            if (Interval > elapsed) Sleep(Interval - elapsed);

            DeltaTime = stopwatch.ElapsedMilliseconds / 1000f;

            ElapsedTime += DeltaTime;

            stopwatch.Reset();
        }

        void Sleep(long duration) => Thread.Sleep((int)duration);

        public void Stop()
        {
            run = false;
        }

        public Schedule(long interval, Delegate callback)
        {
            this.Interval = interval;

            ElapsedTime = 0f;
            DeltaTime = interval / 1000;

            this.callback = callback;

            thread = new Thread(Procedure);
        }
    }
}
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

        public readonly long interval;

        Thread thread;

        bool run = true;

        public void Start()
        {
            thread.Start();
        }

        void Connect()
        {
            Tick();
        }

        public delegate void TickDelegate();
        TickDelegate tickCallback;
        void Tick()
        {
            var stopwatch = new Stopwatch();

            while (run)
            {
                stopwatch.Start();

                tickCallback?.Invoke();

                var elapsed = stopwatch.ElapsedMilliseconds;

                if (interval > elapsed) Sleep(interval - elapsed);

                DeltaTime = stopwatch.ElapsedMilliseconds / 1000f;

                ElapsedTime += DeltaTime;

                stopwatch.Reset();
            }
        }

        void Sleep(long duration) => Thread.Sleep((int)duration);

        public void Stop()
        {
            run = false;
        }

        public Schedule(long interval, TickDelegate tickCallback)
        {
            this.interval = interval;

            ElapsedTime = 0f;
            DeltaTime = interval / 1000;

            this.tickCallback = tickCallback;

            thread = new Thread(Connect);
        }
    }
}
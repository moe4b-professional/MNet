using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using System.Threading;
using System.Threading.Tasks;

namespace Game.Server
{
    public class Schedule
    {
        public float ElapsedTime { get; protected set; }
        public float DeltaTime { get; protected set; }

        public readonly long interval;

        Thread thread;

        void Start()
        {
            Init();

            Tick();
        }

        public delegate void InitDelegate();
        public event InitDelegate OnInit;
        void Init()
        {
            OnInit?.Invoke();
        }

        public delegate void TickDelegate();
        public event TickDelegate OnTick;
        void Tick()
        {
            var stopwatch = new Stopwatch();

            long elapsed = 0;

            while (true)
            {
                stopwatch.Start();
                {
                    OnTick?.Invoke();

                    elapsed = stopwatch.ElapsedMilliseconds;

                    if (interval > elapsed) Sleep(interval - elapsed);

                    DeltaTime = stopwatch.ElapsedMilliseconds / 1000f;

                    ElapsedTime += DeltaTime;
                }
                stopwatch.Reset();
            }
        }

        void Sleep(long duration) => Thread.Sleep((int)duration);

        public Schedule(long interval)
        {
            this.interval = interval;

            ElapsedTime = 0f;
            DeltaTime = interval / 1000;

            thread = new Thread(Start);
            thread.Start();
        }
    }
}
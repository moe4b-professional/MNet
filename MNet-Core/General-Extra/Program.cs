using MNet;
using System;

#if DEBUG
Sandbox.Execute();
#else
    Benchmarks.Execute();
#endif
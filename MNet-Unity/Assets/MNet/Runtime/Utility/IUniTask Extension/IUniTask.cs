using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Cysharp.Threading.Tasks
{
    public interface IUniTask
    {
        UniTaskStatus Status { get; }

        Type Type { get; }
        object Result { get; }
    }

    partial struct UniTask : IUniTask
    {
        public Type Type => null;
        public object Result => null;
    }

    partial struct UniTask<T> : IUniTask
    {
        public Type Type => typeof(T);
        public object Result => source.GetResult(token);
    }
}
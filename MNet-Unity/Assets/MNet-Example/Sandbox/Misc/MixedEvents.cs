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

using MB;
using UnityEngine.Events;

namespace MNet
{
    [Serializable]
    public abstract class BaseMixedEvent
    {
        public virtual void Clear()
        {
            ClearUnity();
            ClearManaged();
        }
        protected abstract void ClearUnity();
        protected abstract void ClearManaged();

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(BaseMixedEvent), true)]
        public class Drawer : PersistantPropertyDrawer
        {
            SerializedProperty unity;

            protected override void Init()
            {
                base.Init();

                unity = Property.FindPropertyRelative("unity");
            }

            public override float CalculateHeight()
            {
                return EditorGUI.GetPropertyHeight(unity);
            }

            public override void Draw(Rect rect)
            {
                EditorGUI.PropertyField(rect, unity);
            }
        }
#endif
    }

    [Serializable]
    public abstract class BaseMixedEvent<TUnity> : BaseMixedEvent
        where TUnity : UnityEventBase
    {
        [SerializeField]
        protected TUnity unity = default;
        public TUnity Unity => unity;

        protected override void ClearUnity()
        {
            unity.RemoveAllListeners();
        }
    }

    [Serializable]
    public class MixedEvent : BaseMixedEvent<UnityEvent>
    {
        public event UnityAction Managed;

        public virtual void Invoke()
        {
            Unity.Invoke();
            Managed.Invoke();
        }

        protected override void ClearManaged() => Managed = null;
    }

    [Serializable]
    public class MixedEvent<T1> : BaseMixedEvent<UnityEvent<T1>>
    {
        public event UnityAction<T1> Managed;

        public virtual void Invoke(T1 arg1)
        {
            Unity.Invoke(arg1);
            Managed.Invoke(arg1);
        }

        protected override void ClearManaged() => Managed = null;
    }

    [Serializable]
    public class MixedEvent<T1, T2> : BaseMixedEvent<UnityEvent<T1, T2>>
    {
        public event UnityAction<T1, T2> Managed;

        public virtual void Invoke(T1 arg1, T2 arg2)
        {
            Unity.Invoke(arg1, arg2);
            Managed.Invoke(arg1, arg2);
        }

        protected override void ClearManaged() => Managed = null;
    }

    [Serializable]
    public class MixedEvent<T1, T2, T3> : BaseMixedEvent<UnityEvent<T1, T2, T3>>
    {
        public event UnityAction<T1, T2, T3> Managed;

        public virtual void Invoke(T1 arg1, T2 arg2, T3 arg3)
        {
            Unity.Invoke(arg1, arg2, arg3);
            Managed.Invoke(arg1, arg2, arg3);
        }

        protected override void ClearManaged() => Managed = null;
    }

    [Serializable]
    public class MixedEvent<T1, T2, T3, T4> : BaseMixedEvent<UnityEvent<T1, T2, T3, T4>>
    {
        public event UnityAction<T1, T2, T3, T4> Managed;

        public virtual void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            Unity.Invoke(arg1, arg2, arg3, arg4);
            Managed.Invoke(arg1, arg2, arg3, arg4);
        }

        protected override void ClearManaged() => Managed = null;
    }
}
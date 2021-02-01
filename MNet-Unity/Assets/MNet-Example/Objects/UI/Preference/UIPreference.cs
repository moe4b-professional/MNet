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

namespace MNet.Example
{
	public abstract class UIPreference<TComponent, TData> : MonoBehaviour
        where TComponent : Component
	{
        [SerializeField]
        protected string ID;

        [SerializeField]
        TData _default = default;
        public TData Default => _default;

        public TData Data { get; protected set; }

        public TComponent Component { get; protected set; }

        void Reset()
        {
            ID = name;
        }

        protected void Start()
        {
            Component = GetComponent<TComponent>();

            Data = Load();

            RegisterCallback();

            ApplyData(Data);
        }

        protected abstract void ApplyData(TData data);

        protected abstract void RegisterCallback();

        protected virtual void ChangeCallback(TData data) => Save(data);

        protected abstract void Save(TData data);
        protected abstract TData Load();
    }
}
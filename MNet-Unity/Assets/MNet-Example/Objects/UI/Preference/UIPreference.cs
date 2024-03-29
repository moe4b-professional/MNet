﻿using System;
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

using UnityEngine.PlayerLoop;

using MB;

namespace MNet.Example
{
    public abstract class UIPreference : MonoBehaviour
    {
        [SerializeField]
        protected string ID;

        protected virtual void Reset()
        {
            ID = name;
        }
    }

    public abstract class UIPreference<TComponent, TData> : UIPreference
        where TComponent : Component
    {
        [SerializeField]
        TData _default = default;
        public TData Default => _default;

        public TComponent Component { get; protected set; }

        void Awake()
        {
            Component = GetComponent<TComponent>();

            ManualLateStart.Register(LateStart);
        }

        void LateStart()
        {
            gameObject.SetActive(false); //Used to hide UI transitions

            var data = Load();

            Apply(data);

            RegisterCallback();

            gameObject.SetActive(true);
        }

        protected virtual void Save(TData data) => AutoPreferences.Set(ID, data);

        protected virtual TData Load() => AutoPreferences.Read<TData>(ID);

        protected abstract void Apply(TData data);

        protected abstract void RegisterCallback();

        protected virtual void ChangeCallback(TData data) => Save(data);
    }
}
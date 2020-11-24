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
	public abstract class UITemplate : UIElement
	{
        public void SetParent(Transform target) => transform.SetParent(target);

        public void Rename(string name) => gameObject.name = name;

        public delegate void ProcessDelegate<TTemplate>(TTemplate template, int index);
    }

    public abstract class UITemplate<TSelf, TData> : UITemplate
        where TSelf : UITemplate<TSelf, TData>
    {
        public TData Data { get; protected set; }

        public virtual void SetData(TData data)
        {
            this.Data = data;

            UpdateState();
        }

        public virtual void UpdateState()
        {

        }

        //Static Utility
        public static List<TSelf> CreateAll(GameObject prefab, ICollection<TData> collection, ProcessDelegate<TSelf> action)
        {
            return CreateAll(prefab, collection, Selector, action);

            TData Selector(TData data) => data;
        }

        public static List<TSelf> CreateAll<T>(GameObject prefab, ICollection<T> collection, Func<T, TData> selector, ProcessDelegate<TSelf> action)
        {
            var templates = new List<TSelf>(collection.Count);

            var index = 0;

            foreach (var item in collection)
            {
                var data = selector(item);

                var instance = Create(prefab, data);

                action(instance, index);

                templates.Add(instance);

                index += 1;
            }

            return templates;
        }

        public static TSelf Create(GameObject prefab, TData data)
        {
            var gameObject = Instantiate(prefab);

            var script = gameObject.GetComponent<TSelf>();
            script.SetData(data);

            return script;
        }

        public static void Destroy(TSelf self) => Destroy(self.gameObject);
    }
}
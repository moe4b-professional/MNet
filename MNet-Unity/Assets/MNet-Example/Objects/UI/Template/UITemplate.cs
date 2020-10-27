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

        public virtual void Set(TData data)
        {
            this.Data = data;

            UpdateState();
        }

        public virtual void UpdateState()
        {

        }

        //Static Utility
        public static TSelf[] CreateAll(GameObject prefab, Transform parent, IList<TData> list, ProcessDelegate<TSelf> action)
        {
            var templates = new TSelf[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                var instance = Instantiate(prefab);

                var script = instance.GetComponent<TSelf>();
                script.Set(list[i]);
                script.SetParent(parent);
                action(script, i);

                templates[i] = script;
            }

            return templates;
        }

        public static void Destroy(TSelf self) => Destroy(self.gameObject);
    }
}
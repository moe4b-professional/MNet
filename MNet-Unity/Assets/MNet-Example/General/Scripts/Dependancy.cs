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
	public static class Dependancy
	{
        public static TComponent Get<TComponent>(GameObject target)
            where TComponent : class
        {
            return Get<TComponent>(target, Scope.CurrentToChildern);
        }
        public static TComponent Get<TComponent>(GameObject target, Scope scope)
            where TComponent : class
        {
            TComponent component;

            if(scope == Scope.Childern)
            {
                component = null;

                scope = Scope.CurrentToChildern;
            }
            else if(scope == Scope.Parents)
            {
                component = null;

                scope = Scope.CurrentToParents;
            }
            else
            {
                component = target.GetComponent<TComponent>();
            }

            if (IsNull(component))
            {
                if (scope == Scope.CurrentToChildern)
                {
                    for (int i = 0; i < target.transform.childCount; i++)
                    {
                        component = Get<TComponent>(target.transform.GetChild(i).gameObject, scope);

                        if (!IsNull(component)) break;
                    }
                }

                if (scope == Scope.CurrentToParents && target.transform.parent != null)
                    component = Get<TComponent>(target.transform.parent.gameObject, scope);
            }

            return component;
        }
        
        public static List<TComponent> GetAll<TComponent>(GameObject target)
            where TComponent : class
        {
            return GetAll<TComponent>(target, Scope.CurrentToChildern);
        }
		public static List<TComponent> GetAll<TComponent>(GameObject target, Scope scope)
            where TComponent : class
        {
            var list = new List<TComponent>();

            list.AddRange(target.GetComponents<TComponent>());

            if (scope == Scope.CurrentToChildern)
                for (int i = 0; i < target.transform.childCount; i++)
                    list.AddRange(GetAll<TComponent>(target.transform.GetChild(i).gameObject, scope));

            if (scope == Scope.CurrentToParents)
                if (target.transform.parent != null)
                    list.AddRange(GetAll<TComponent>(target.transform.parent.gameObject, scope));

            return list;
        }

        public enum Scope
        {
            Local, Childern, CurrentToChildern, CurrentToParents, Parents
        }

        public static NullReferenceException FormatException(string dependancy, object dependant)
        {
            var text = "No " + dependancy + " found for " + dependant.GetType().Name;

            var componentDependant = dependant as Component;

            if (componentDependant != null)
                text += " On gameObject: " + componentDependant.gameObject;

            return new NullReferenceException(text);
        }
        public static string FormatExceptionText(string dependancy, object dependant)
        {
            var text = "No " + dependancy + " specified for " + dependant.GetType().Name;

            var componentDependant = dependant as Component;

            if (componentDependant != null)
                text += " On gameObject: " + componentDependant.gameObject;

            return text;
        }

        public static bool IsNull(object target)
        {
            if (target == null) return true;

            if (target.Equals(null)) return true;

            return false;
        }
    }

    public static class DependancyExtensions
    {
        public static TComponent GetDependancy<TComponent>(this Component target)
            where TComponent : class
        {
            return Dependancy.Get<TComponent>(target.gameObject);
        }
        public static TComponent GetDependancy<TComponent>(this Component target, Dependancy.Scope scope)
            where TComponent : class
        {
            return Dependancy.Get<TComponent>(target.gameObject, scope);
        }

        public static List<TComponent> GetAllDependancies<TComponent>(this Component target)
            where TComponent : class
        {
            return Dependancy.GetAll<TComponent>(target.gameObject);
        }
        public static List<TComponent> GetAllDependancies<TComponent>(this Component target, Dependancy.Scope scope)
            where TComponent : class
        {
            return Dependancy.GetAll<TComponent>(target.gameObject, scope);
        }
    }
}
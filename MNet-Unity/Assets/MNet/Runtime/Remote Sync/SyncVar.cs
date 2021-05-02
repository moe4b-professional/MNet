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

using System.Reflection;

namespace MNet
{
    public class SyncVarBind
    {
        public NetworkEntity.Behaviour Behaviour { get; protected set; }
        public Component Component => Behaviour.Component;
        public NetworkEntity Entity => Behaviour.Entity;

        #region Attribute
        public SyncVarAttribute Attribute { get; protected set; }

        public RemoteAuthority Authority => Attribute.Authority;
        #endregion

        #region Field
        public FieldInfo FieldInfo { get; protected set; }
        public bool IsField => FieldInfo != null;

        public PropertyInfo PropertyInfo { get; protected set; }
        public bool IsProperty => PropertyInfo != null;

        public SyncVarFieldID FieldID { get; protected set; }

        public string Name { get; protected set; }

        public Type Type
        {
            get
            {
                if (IsField) return FieldInfo.FieldType;

                if (IsProperty) return PropertyInfo.PropertyType;

                throw new NotImplementedException();
            }
        }
        #endregion

        #region Hooks
        public List<Delegate> Hooks { get; protected set; }

        public void InvokeHooks(object oldValue, object newValue, SyncVarInfo info)
        {
            for (int i = 0; i < Hooks.Count; i++)
                Hooks[i].DynamicInvoke(oldValue, newValue, info);
        }
        #endregion

        #region Value Accessors
        public object GetValue()
        {
            if (IsField)
                return FieldInfo.GetValue(Component);

            if (IsProperty)
                return PropertyInfo.GetValue(Component);

            throw new NotImplementedException();
        }

        public void SetValue(object value)
        {
            if (IsField)
            {
                FieldInfo.SetValue(Component, value);
                return;
            }

            if (IsProperty)
            {
                PropertyInfo.SetValue(Component, value);
                return;
            }

            throw new NotImplementedException();
        }
        #endregion

        public void ParseCommand(ISyncVarCommand command, out object value, out SyncVarInfo info)
        {
            value = command.Read(Type);

            NetworkAPI.Room.Clients.TryGet(command.Sender, out var sender);
            info = new SyncVarInfo(sender);
        }

        public override string ToString() => $"{Behaviour}->{Name}";

        public SyncVarBind(NetworkEntity.Behaviour behaviour, SyncVarAttribute attribute, MemberInfo member, byte index)
        {
            this.Behaviour = behaviour;

            this.Attribute = attribute;

            FieldInfo = member as FieldInfo;
            PropertyInfo = member as PropertyInfo;

            Name = GetName(member);
            FieldID = new SyncVarFieldID(index);

            Hooks = new List<Delegate>();

            if (IsProperty)
            {
                if (PropertyInfo.SetMethod == null) throw FormatInvalidPropertyAcessorException(behaviour, PropertyInfo, "Setter");
                if (PropertyInfo.GetMethod == null) throw FormatInvalidPropertyAcessorException(behaviour, PropertyInfo, "Getter");
            }
        }

        //Static Utility

        public static string GetName(MemberInfo info) => info.Name;

        public static Exception FormatInvalidPropertyAcessorException<T>(T type, PropertyInfo property, string missing)
        {
            var text = $"{type.GetType().Name}->{property.Name}' Property Cannot be Used as a SyncVar " +
                    $"as it does not have a {missing}";

            throw new Exception(text);
        }
    }

    public struct SyncVarInfo
    {
        public NetworkClient Sender { get; private set; }

        /// <summary>
        /// Is this RPC Request the Result of the Room's Buffer
        /// </summary>
        public bool IsBuffered { get; private set; }

        public SyncVarInfo(NetworkClient sender)
        {
            this.Sender = sender;

            this.IsBuffered = NetworkAPI.Realtime.IsOnBuffer;
        }
    }

    public delegate void SyncVarHook<T>(T oldValue, T newValue, SyncVarInfo info);

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class SyncVarAttribute : Attribute
    {
        public RemoteAuthority Authority { get; set; } = RemoteAuthority.Any;

        public SyncVarAttribute()
        {

        }

        public static SyncVarAttribute Retrieve(MemberInfo info) => info.GetCustomAttribute<SyncVarAttribute>(true);

        public static bool Defined(MemberInfo info) => Retrieve(info) != null;
    }
}
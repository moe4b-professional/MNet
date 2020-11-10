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
        public NetworkBehaviour Behaviour { get; protected set; }
        public NetworkEntity Entity => Behaviour.Entity;

        public SyncVarAttribute Attribute { get; protected set; }
        public RemoteAutority Authority => Attribute.Authority;
        public DeliveryChannel Channel => Attribute.Channel;

        public FieldInfo FieldInfo { get; protected set; }
        public bool IsField => FieldInfo != null;

        public PropertyInfo PropertyInfo { get; protected set; }
        public bool IsProperty => PropertyInfo != null;

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

        public SyncVarRequest CreateRequest(object value)
        {
            var request = SyncVarRequest.Write(Entity.ID, Behaviour.ID, Name, value);

            return request;
        }

        public object ParseValue(SyncVarCommand command)
        {
            var value = command.Read(Type);

            return value;
        }

        public void Set(object value)
        {
            if (IsField)
                FieldInfo.SetValue(Behaviour, value);
            else if (IsProperty)
                PropertyInfo.SetValue(Behaviour, value);
            else
                throw new NotImplementedException();
        }

        public object Get()
        {
            if (IsField) return FieldInfo.GetValue(Behaviour);

            if (IsProperty) return PropertyInfo.GetValue(Behaviour);

            throw new NotImplementedException();
        }

        public override string ToString() => $"{Entity}->{Name}";

        SyncVarBind(NetworkBehaviour behaviour, SyncVarAttribute attribute, FieldInfo field, PropertyInfo property)
        {
            this.Behaviour = behaviour;

            this.Attribute = attribute;

            this.FieldInfo = field;
            this.PropertyInfo = property;

            if (IsField) Name = FieldInfo.Name;
            if (IsProperty) Name = PropertyInfo.Name;
        }
        public SyncVarBind(NetworkBehaviour behaviour, SyncVarAttribute attribute, FieldInfo field) : this(behaviour, attribute, field, null)
        {

        }
        public SyncVarBind(NetworkBehaviour behaviour, SyncVarAttribute attribute, PropertyInfo property) : this(behaviour, attribute, null, property)
        {
            if (property.SetMethod == null || property.GetMethod == null)
            {
                var text = $"{behaviour.GetType().Name}'s '{Name}' Property Cannot be Used as a SyncVar, " +
                    $"Please Ensure The Property Has Both a Setter & Getter";

                throw new Exception(text);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class SyncVarAttribute : Attribute
    {
        public RemoteAutority Authority { get; private set; }
        public DeliveryChannel Channel { get; private set; }

        public SyncVarAttribute(RemoteAutority authotity = RemoteAutority.Any, DeliveryChannel channel = DeliveryChannel.Reliable)
        {
            this.Authority = authotity;
            this.Channel = channel;
        }
        public SyncVarAttribute(RemoteAutority authority) : this(authority, DeliveryChannel.Reliable) { }
        public SyncVarAttribute(DeliveryChannel channel) : this(RemoteAutority.Any, channel) { }
    }
}
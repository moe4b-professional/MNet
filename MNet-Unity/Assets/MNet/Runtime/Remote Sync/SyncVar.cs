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
        public RemoteAuthority Authority => Attribute.Authority;
        public DeliveryMode DeliveryMode => Attribute.Mode;

        public PropertyInfo PropertyInfo { get; protected set; }

        public string Name { get; protected set; }

        public Type Type => PropertyInfo.PropertyType;

        public object Get() => PropertyInfo.GetValue(Behaviour);

        public void Set(object value) => PropertyInfo.SetValue(Behaviour, value);

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

        public override string ToString() => $"{Entity}->{Name}";

        public SyncVarBind(NetworkBehaviour behaviour, SyncVarAttribute attribute, PropertyInfo property)
        {
            this.Behaviour = behaviour;

            this.Attribute = attribute;

            this.PropertyInfo = property;

            Name = PropertyInfo.Name;

            if (property.SetMethod == null) throw FormatInvalidPropertyException(behaviour, property, "Setter");

            if (property.GetMethod == null) throw FormatInvalidPropertyException(behaviour, property, "Getter");
        }

        //Static Utility

        public static Exception FormatInvalidPropertyException<T>(T type, PropertyInfo property, string missing)
        {
            var text = $"{type.GetType().Name}->{property.Name}' Property Cannot be Used as a SyncVar " +
                    $"as it does not have a {missing}";

            throw new Exception(text);
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class SyncVarAttribute : Attribute
    {
        public RemoteAuthority Authority { get; set; } = RemoteAuthority.Any;
        public DeliveryMode Mode { get; set; } = DeliveryMode.Reliable;

        public SyncVarAttribute()
        {

        }
    }
}
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
using Backend;

using System.Reflection;

namespace Game
{
	public class SyncVarBind
	{
        public NetworkBehaviour Behaviour { get; protected set; }

        public NetworkEntity Entity => Behaviour.Entity;

        public SyncVarAttribute Attribute { get; protected set; }

        public EntityAuthorityType Authority => Attribute.Authority;

        public string ID { get; protected set; }

        public FieldInfo FieldInfo { get; protected set; }
        public bool IsField => FieldInfo != null;

        public PropertyInfo PropertyInfo { get; protected set; }
        public bool IsProperty => PropertyInfo != null;

        public Type Type
        {
            get
            {
                if (IsField) return FieldInfo.FieldType;

                if (IsProperty) return PropertyInfo.PropertyType;

                throw new NotImplementedException();
            }
        }

        public object ParseValue(SyncVarCommand command)
        {
            var value = command.Read(Type);

            return value;
        }

        public SyncVarRequest CreateRequest(object value)
        {
            var request = SyncVarRequest.Write(Entity.ID, Behaviour.ID, ID, value);

            return request;
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

        SyncVarBind(NetworkBehaviour behaviour, SyncVarAttribute attribute, FieldInfo field, PropertyInfo property)
        {
            this.Behaviour = behaviour;

            this.Attribute = attribute;

            this.FieldInfo = field;
            this.PropertyInfo = property;

            if (IsField) ID = FieldInfo.Name;
            if (IsProperty) ID = PropertyInfo.Name;
        }

        public SyncVarBind(NetworkBehaviour behaviour, SyncVarAttribute attribute, FieldInfo field)
            : this(behaviour, attribute, field, null)
        {

        }

        public SyncVarBind(NetworkBehaviour behaviour, SyncVarAttribute attribute, PropertyInfo property)
            : this(behaviour, attribute, null, property)
        {
            if (property.SetMethod == null || property.GetMethod == null)
            {
                var text = $"{behaviour.GetType().Name}'s '{ID}' Property Cannot be Used as a SyncVar, " +
                    $"Please Ensure The Property Has Both a Setter & Getter";

                throw new Exception(text);
            }
        }
	}

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class SyncVarAttribute : Attribute
    {
        public EntityAuthorityType Authority { get; private set; }

        public SyncVarAttribute() : this(EntityAuthorityType.Any) { }
        public SyncVarAttribute(EntityAuthorityType authotity)
        {
            this.Authority = authotity;
        }
    }
}
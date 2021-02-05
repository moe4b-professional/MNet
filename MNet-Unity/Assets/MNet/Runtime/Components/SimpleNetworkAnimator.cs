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
using UnityEditor.Animations;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MNet
{
	[RequireComponent(typeof(Animator))]
	public class SimpleNetworkAnimator : NetworkBehaviour
	{
		[SerializeField]
        NetworkEntitySyncTimer syncTimer = default;
        public NetworkEntitySyncTimer SyncTimer => syncTimer;

        [SerializeField]
		ParametersProperty parameters;
		public ParametersProperty Parameters => parameters;
		[Serializable]
		public class ParametersProperty
		{
			[SerializeField]
			List<TriggerProperty> triggers;
			public List<TriggerProperty> Triggers => triggers;
			[Serializable]
			public class TriggerProperty : Property<bool>
			{
				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Trigger;

				public override void ClearDirty()
				{
					base.ClearDirty();

					value = false;
				}

				public override void Read(Animator animator)
				{
					base.Read(animator);

					value = animator.GetBool(Hash);
				}

				public TriggerProperty(AnimatorControllerParameter parameter) : base(parameter)
				{

				}
			}

			[SerializeField]
			List<BoolProperty> bools;
			public List<BoolProperty> Bools => bools;
			[Serializable]
			public class BoolProperty : Property<bool>
			{
				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Bool;

				public override void Read(Animator animator)
				{
					base.Read(animator);

					value = animator.GetBool(Hash);
				}

				public BoolProperty(AnimatorControllerParameter parameter) : base(parameter)
				{

				}
			}

			[SerializeField]
			List<IntegerProperty> integers;
			public List<IntegerProperty> Integers => integers;
			[Serializable]
			public class IntegerProperty : Property<int>
			{
				[SerializeField]
				[Tooltip("Serializes the parameter as a Short that uses 2 bytes instead of 4")]
				bool useShort = true;
				public bool UseShort => useShort;

				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Int;

				public override void Read(Animator animator)
				{
					base.Read(animator);

					value = animator.GetInteger(Hash);
				}

				public IntegerProperty(AnimatorControllerParameter parameter) : base(parameter)
				{

				}
			}

			[SerializeField]
			List<FloatProperty> floats;
			public List<FloatProperty> Floats => floats;
			[Serializable]
			public class FloatProperty : Property<float>
			{
				[SerializeField]
				[Tooltip("Serializes the parameter as a Half that uses 2 bytes instead of 4")]
				bool useHalf = true;
				public bool UseHalf => useHalf;

				[Header("Translation")]
				[SerializeField]
				bool smooth = true;
				public bool Smooth => smooth;

				[SerializeField]
				float speed = 10;
				public float Speed => speed;

				public float Target { get; protected set; }
				internal void SetTarget(float value) => Target = value;

				public bool Translate()
				{
					if (Target == value) return false;

					//TODO use a translation method better than lerp
					value = Mathf.Lerp(value, Target, speed * Time.deltaTime);

					return true;
				}

				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Float;

				public override void Read(Animator animator)
				{
					base.Read(animator);

					value = animator.GetFloat(Hash);
				}

				public override bool Compare(float a, float b) => Mathf.Approximately(a, b);

				public FloatProperty(AnimatorControllerParameter parameter) : base(parameter)
				{

				}
			}

			[Serializable]
			public abstract class Property
			{
				[SerializeField]
				protected string name;
				public string Name => name;

				public byte ID { get; protected set; }
				public void SetID(byte value) => ID = value;

				public int Hash { get; protected set; }

				public bool IsDirty { get; protected set; }
				public virtual void ClearDirty() => IsDirty = false;

				public abstract AnimatorControllerParameterType Type { get; }

				public virtual void Read(Animator animator)
				{
					Hash = Animator.StringToHash(name);
				}

				public virtual void Parse(AnimatorControllerParameter parameter)
				{
					name = parameter.name;
				}

				public Property(AnimatorControllerParameter parameter)
				{
					Parse(parameter);
				}

				internal static T Create<T>(AnimatorControllerParameter parameter)
					where T : Property
				{
					var type = typeof(T);

					switch (type)
					{
						case Type target when type == typeof(TriggerProperty):
							return new TriggerProperty(parameter) as T;

						case Type target when type == typeof(BoolProperty):
							return new BoolProperty(parameter) as T;

						case Type target when type == typeof(IntegerProperty):
							return new IntegerProperty(parameter) as T;

						case Type target when type == typeof(FloatProperty):
							return new FloatProperty(parameter) as T;
					}

					throw new NotImplementedException();
				}
			}

			[Serializable]
			public abstract class Property<TValue> : Property
			{
				[SerializeField]
				protected TValue value;
				public TValue Value => value;

				public bool Update(TValue data)
				{
					if (Compare(data, value)) return false;

					value = data;
					IsDirty = true;
					return true;
				}

				public virtual bool Compare(TValue a, TValue b) => Equals(a, b);

				public Property(AnimatorControllerParameter parameter) : base(parameter)
				{

				}
			}

			public List<Property> All { get; protected set; }

			public DualDictionary<string, byte, Property> Dictionary { get; protected set; }

			public bool TryGet<TProperty>(string name, out TProperty property)
				where TProperty : Property
			{
				if (TryGet(name, out var target) == false)
				{
					property = default;
					return false;
				}

				property = target as TProperty;

				if (property == null)
					return false;

				return true;
			}
			public bool TryGet(string name, out Property property)
			{
				if (Dictionary.TryGetValue(name, out property)) return true;

				if (TryGetFrom(name, triggers, out property)) return true;
				if (TryGetFrom(name, bools, out property)) return true;
				if (TryGetFrom(name, integers, out property)) return true;
				if (TryGetFrom(name, floats, out property)) return true;

				return false;
			}

			public bool TryGet<TProperty>(byte id, out TProperty property)
				where TProperty : Property
			{
				if (TryGet(id, out var target) == false)
				{
					property = default;
					return false;
				}

				property = target as TProperty;

				if (property == null)
					return false;

				return true;
			}
			public bool TryGet(byte id, out Property property)
			{
				if (Dictionary.TryGetValue(id, out property)) return true;

				return false;
			}

			public void ForAll(Action<Property> action)
			{
				for (int i = 0; i < triggers.Count; i++) action(triggers[i]);
				for (int i = 0; i < bools.Count; i++) action(bools[i]);
				for (int i = 0; i < integers.Count; i++) action(integers[i]);
				for (int i = 0; i < floats.Count; i++) action(floats[i]);
			}

			SimpleNetworkAnimator animator;

			internal void Configure(SimpleNetworkAnimator reference)
			{
				animator = reference;

				All = new List<Property>(triggers.Count + bools.Count + integers.Count + floats.Count);

				for (int i = 0; i < triggers.Count; i++) All.Add(triggers[i]);
				for (int i = 0; i < bools.Count; i++) All.Add(bools[i]);
				for (int i = 0; i < integers.Count; i++) All.Add(integers[i]);
				for (int i = 0; i < floats.Count; i++) All.Add(floats[i]);

				for (byte i = 0; i < All.Count; i++)
				{
					All[i].SetID(i);
					All[i].Read(animator.Component);

					Dictionary.Add(All[i].Name, i, All[i]);
				}
			}

#if UNITY_EDITOR
			internal void Refresh(Animator animator)
			{
				var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

				IList<AnimatorControllerParameter> QueryParameters(AnimatorControllerParameterType type)
				{
					return controller.parameters.Where(x => x.type == type).ToArray();
				}

				Refresh(ref triggers, QueryParameters(AnimatorControllerParameterType.Trigger));
				Refresh(ref bools, QueryParameters(AnimatorControllerParameterType.Bool));
				Refresh(ref integers, QueryParameters(AnimatorControllerParameterType.Int));
				Refresh(ref floats, QueryParameters(AnimatorControllerParameterType.Float));
			}

			internal void Refresh<T>(ref List<T> list, IList<AnimatorControllerParameter> parameters)
				where T : Property
			{
				for (int i = 0; i < parameters.Count; i++)
				{
					if (TryGetFrom(parameters[i].name, list, out var existing))
					{
						existing.Parse(parameters[i]);
					}
					else
					{
						var property = Property.Create<T>(parameters[i]);
						list.Add(property);
					}
				}

				list.RemoveAll(NameIsNotInList);

				bool NameIsNotInList(T argument) => parameters.Any(x => x.name == argument.Name) == false;
			}
#endif

			public ParametersProperty()
			{
				triggers = new List<TriggerProperty>();
				bools = new List<BoolProperty>();
				integers = new List<IntegerProperty>();
				floats = new List<FloatProperty>();

				Dictionary = new DualDictionary<string, byte, Property>();
			}

			//Static Utility

			static bool TryGetFrom<T>(string name, IList<T> list, out Property property)
				where T : Property
			{
				for (byte i = 0; i < list.Count; i++)
				{
					if (list[i].Name == name)
					{
						property = list[i];
						return true;
					}
				}

				property = default;
				return false;
			}
		}

		[SerializeField]
		LayersProperty layers;
		public LayersProperty Layers => layers;
		[Serializable]
		public class LayersProperty
		{
			[SerializeField]
			List<Property> list;
			public List<Property> List => list;

			[Serializable]
			public class Property
			{
				[SerializeField]
				protected string name;
				public string Name => name;

				[SerializeField]
				float value;
				public float Value => value;

				[SerializeField]
				[Tooltip("Serializes the parameter as a Half that uses 2 bytes instead of 4")]
				bool useHalf = true;
				public bool UseHalf => useHalf;

				[Header("Translation")]
				[SerializeField]
				bool smooth = true;
				public bool Smooth => smooth;

				[SerializeField]
				float speed = 10;
				public float Speed => speed;

				public float Target { get; protected set; }
				internal void SetTarget(float value) => Target = value;

				public bool Translate()
				{
					if (Target == value) return false;

					//TODO use a translation method better than lerp
					value = Mathf.Lerp(value, Target, speed * Time.deltaTime);

					return true;
				}

				public byte ID { get; protected set; }
				public void SetID(byte value) => ID = value;

				public int Index { get; protected set; }

				public bool IsDirty { get; protected set; }
				public virtual void ClearDirty() => IsDirty = false;

				internal void Read(Animator component)
				{
					Index = component.GetLayerIndex(Name);

					value = component.GetLayerWeight(Index);
				}

				public bool Update(float data)
				{
					if (Mathf.Approximately(data, value)) return false;

					value = data;
					IsDirty = true;
					return true;
				}

				public Property(string name)
				{
					this.name = name;
				}
			}

			public DualDictionary<string, byte, Property> Dictionary { get; protected set; }

			public bool TryGet(string name, out Property property)
			{
				if (Dictionary.TryGetValue(name, out property)) return true;

				if (TryGetFrom(name, list, out property)) return true;

				return false;
			}

			public bool TryGet(byte id, out Property property)
			{
				if (Dictionary.TryGetValue(id, out property)) return true;

				return false;
			}

			SimpleNetworkAnimator animator;

			internal void Configure(SimpleNetworkAnimator reference)
			{
				animator = reference;

				for (byte i = 0; i < list.Count; i++)
				{
					list[i].SetID(i);
					list[i].Read(animator.Component);

					Dictionary.Add(list[i].Name, i, list[i]);
				}
			}

#if UNITY_EDITOR
			internal void Refresh(Animator animator)
			{
				var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

				var layers = controller.layers;

				for (int i = 0; i < layers.Length; i++)
				{
					if (TryGetFrom(layers[i].name, list, out var property))
					{
						continue;
					}
					else
					{
						property = new Property(layers[i].name);
						list.Add(property);
					}
				}

				list.RemoveAll(NameIsNotInList);
				bool NameIsNotInList(Property argument) => layers.Any(x => x.name == argument.Name) == false;
			}
#endif

			public LayersProperty()
			{
				list = new List<Property>();

				Dictionary = new DualDictionary<string, byte, Property>();
			}

			//Static Utility

			static bool TryGetFrom<T>(string name, IList<T> list, out Property property)
				where T : Property
			{
				for (byte i = 0; i < list.Count; i++)
				{
					if (list[i].Name == name)
					{
						property = list[i];
						return true;
					}
				}

				property = default;
				return false;
			}
		}

		public Animator Component { get; protected set; }

        protected override void Reset()
        {
            base.Reset();

#if UNITY_EDITOR
			syncTimer = NetworkEntitySyncTimer.Resolve(Entity);
#endif
		}

        void Awake()
		{
			Component = GetComponent<Animator>();

			parameters.Configure(this);
			layers.Configure(this);
		}

		void Start()
		{
			syncTimer.OnInvoke += Sync;
		}

		#region Trigger Parameter
		public void SetTrigger(string name, bool instant = true)
		{
			if (Parameters.TryGet<ParametersProperty.TriggerProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			Component.SetTrigger(parameter.Hash);

			if (instant) SyncParameter(parameter);
		}

		void SyncParameter(ParametersProperty.TriggerProperty parameter)
		{
			BroadcastRPC(TriggerRPC, parameter.ID, exception: NetworkAPI.Client.Self);
			parameter.ClearDirty();
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void TriggerRPC(byte id, RpcInfo info)
		{
			if (parameters.TryGet<ParametersProperty.TriggerProperty>(id, out var parameter) == false)
			{
				Debug.LogWarning($"Recieved Network Animator RPC for {this} with Invalid Parameter ID of {id}");
				return;
			}

			Component.SetTrigger(parameter.Hash);
		}
		#endregion

		#region Bool Parameter
		public void SetBool(string name, bool value, bool instant = true)
		{
			if (Parameters.TryGet<ParametersProperty.BoolProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			Component.SetBool(parameter.Hash, value);

			if (parameter.Update(value) == false) return;

			if (instant) SyncParameter(parameter);
		}

		void SyncParameter(ParametersProperty.BoolProperty parameter)
		{
			BroadcastRPC(BoolRPC, parameter.ID, parameter.Value, exception: NetworkAPI.Client.Self);
			parameter.ClearDirty();
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void BoolRPC(byte id, bool value, RpcInfo info)
		{
			if (parameters.TryGet<ParametersProperty.BoolProperty>(id, out var parameter) == false)
			{
				Debug.LogWarning($"Recieved Network Animator RPC for {this} with Invalid Parameter ID of {id}");
				return;
			}

			Component.SetBool(parameter.Hash, value);
		}
		#endregion

		#region Integer Parameter
		public void SetInteger(string name, int value, bool instant = true)
		{
			if (Parameters.TryGet<ParametersProperty.IntegerProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			Component.SetInteger(parameter.Hash, value);

			if (parameter.Update(value) == false) return;

			if (instant) SyncParameter(parameter);
		}

		void SyncParameter(ParametersProperty.IntegerProperty parameter)
		{
			if (parameter.UseShort)
				BroadcastRPC(ShortRPC, parameter.ID, (short)parameter.Value, exception: NetworkAPI.Client.Self);
			else
				BroadcastRPC(IntergerRPC, parameter.ID, parameter.Value, exception: NetworkAPI.Client.Self);

			parameter.ClearDirty();
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void IntergerRPC(byte id, int value, RpcInfo info)
		{
			if (parameters.TryGet<ParametersProperty.IntegerProperty>(id, out var parameter) == false)
			{
				Debug.LogWarning($"Recieved Network Animator RPC for {this} with Invalid Parameter ID of {id}");
				return;
			}

			Component.SetInteger(parameter.Hash, value);
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void ShortRPC(byte id, short value, RpcInfo info) => IntergerRPC(id, value, info);
		#endregion

		#region Float Parameter
		public void SetFloat(string name, float value, bool instant = false)
		{
			if (Parameters.TryGet<ParametersProperty.FloatProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			Component.SetFloat(parameter.Hash, value);

			if (parameter.Update(value) == false) return;

			if (instant) SyncParameter(parameter);
		}

		void SyncParameter(ParametersProperty.FloatProperty parameter)
		{
			if (parameter.UseHalf)
				BroadcastRPC(HalfRPC, parameter.ID, (Half)parameter.Value, exception: NetworkAPI.Client.Self);
			else
				BroadcastRPC(FloatRPC, parameter.ID, parameter.Value, exception: NetworkAPI.Client.Self);

			parameter.ClearDirty();
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void FloatRPC(byte id, float value, RpcInfo info)
		{
			if (parameters.TryGet<ParametersProperty.FloatProperty>(id, out var parameter) == false)
			{
				Debug.LogWarning($"Recieved Network Animator RPC for {this} with Invalid Parameter ID of {id}");
				return;
			}

			if (parameter.Smooth)
				parameter.SetTarget(value);
			else
				Component.SetFloat(parameter.Hash, value);
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void HalfRPC(byte id, Half value, RpcInfo info) => FloatRPC(id, value, info);
		#endregion

		#region Layer Weight
		public void SetLayerWeight(string name, float value, bool instant = false)
		{
			if (layers.TryGet(name, out var layer) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			Component.SetLayerWeight(layer.Index, value);

			if (layer.Update(value) == false) return;

			if (instant) SyncLayerWeight(layer);
		}

		void SyncLayerWeight(LayersProperty.Property layer)
		{
			if (layer.UseHalf)
				BroadcastRPC(LayerWeightHalfRPC, layer.ID, (Half)layer.Value, exception: NetworkAPI.Client.Self);
			else
				BroadcastRPC(LayerWeightFloatRPC, layer.ID, layer.Value, exception: NetworkAPI.Client.Self);

			layer.ClearDirty();
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void LayerWeightFloatRPC(byte id, float value, RpcInfo info)
		{
			if (layers.TryGet(id, out var layer) == false)
			{
				Debug.LogWarning($"Recieved Network Animator RPC for {this} with Invalid Layer ID of {id}");
				return;
			}

			if (layer.Smooth)
				layer.SetTarget(value);
			else
				Component.SetLayerWeight(layer.Index, value);
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void LayerWeightHalfRPC(byte id, Half value, RpcInfo info) => LayerWeightFloatRPC(id, value, info);
		#endregion

		void Sync()
		{
			foreach (var parameter in parameters.Triggers)
			{
				if (parameter.IsDirty == false) continue;

				SyncParameter(parameter);
			}

			foreach (var parameter in parameters.Bools)
			{
				if (parameter.IsDirty == false) continue;

				SyncParameter(parameter);
			}

			foreach (var parameter in parameters.Integers)
			{
				if (parameter.IsDirty == false) continue;

				SyncParameter(parameter);
			}

			foreach (var parameter in parameters.Floats)
			{
				if (parameter.IsDirty == false) continue;

				SyncParameter(parameter);
			}

			foreach (var layer in layers.List)
			{
				if (layer.IsDirty == false) continue;

				SyncLayerWeight(layer);
			}
		}

		void Update()
		{
			foreach (var parameter in parameters.Floats)
			{
				if (parameter.Smooth == false) continue;

				if (parameter.Translate() == false) continue;

				Component.SetFloat(parameter.Hash, parameter.Value);
			}

			foreach (var layer in layers.List)
			{
				if (layer.Smooth == false) continue;

				if (layer.Translate() == false) continue;

				Component.SetLayerWeight(layer.Index, layer.Value);
			}
		}

		public SimpleNetworkAnimator()
		{
			parameters = new ParametersProperty();
			layers = new LayersProperty();
		}

#if UNITY_EDITOR
		void Refresh()
		{
			Component = GetComponent<Animator>();

			if (Component.runtimeAnimatorController == null)
			{
				Debug.LogWarning($"No Animator Controller Located on {this}");
				return;
			}

			parameters.Refresh(Component);
			layers.Refresh(Component);

			EditorUtility.SetDirty(this);
		}

		[CustomEditor(typeof(SimpleNetworkAnimator))]
		public class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();

				EditorGUILayout.Space();

				if (GUILayout.Button("Referesh"))
				{
					var target = base.target as SimpleNetworkAnimator;

					target.Refresh();
				}
			}
		}
#endif
	}
}
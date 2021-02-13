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

		public Animator Component { get; protected set; }

		NetworkWriter writer;
		NetworkReader reader;

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

			writer = new NetworkWriter(100);
			reader = new NetworkReader();

			parameters.Configure(this);
			layers.Configure(this);
		}

		void Start()
		{
			syncTimer.OnInvoke += Sync;
		}

		#region Parameters
		[SerializeField]
		ParametersProperty parameters = default;
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
				public override bool Value
				{
					get
					{
						return false;
					}
					internal set
					{
						if (value) animator.Component.SetTrigger(Hash);
					}
				}

				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Trigger;

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
				public override bool Value
				{
					get
					{
						return animator.Component.GetBool(Hash);
					}
					internal set
					{
						animator.Component.SetBool(Hash, value);
					}
				}

				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Bool;

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
				public override int Value
				{
					get
					{
						return animator.Component.GetInteger(Hash);
					}
					internal set
					{
						animator.Component.SetInteger(Hash, value);
					}
				}

				[SerializeField]
				[Tooltip("Serializes the parameter as a Short that uses 2 bytes instead of 4")]
				bool useShort = true;
				public bool UseShort => useShort;

				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Int;

				public override void WriteBinary(NetworkWriter writer)
				{
					if (useShort)
						writer.Write((short)Value);
					else
						writer.Write(Value);
				}

				public override void ReadBinary(NetworkReader reader)
				{
					if (useShort)
						Value = reader.Read<short>();
					else
						Value = reader.Read<int>();
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
				public override float Value
				{
					get
					{
						return animator.Component.GetFloat(Hash);
					}
					internal set
					{
						animator.Component.SetFloat(Hash, value);
					}
				}

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
					if (Compare(Value, Target)) return false;

					//TODO use a translation method better than lerp
					Value = Mathf.Lerp(Value, Target, speed * Time.deltaTime);

					return true;
				}

				public override AnimatorControllerParameterType Type => AnimatorControllerParameterType.Float;

				public override bool Compare(float a, float b) => Mathf.Approximately(a, b);

				public override void WriteBinary(NetworkWriter writer)
				{
					if (useHalf)
						writer.Write((Half)Value);
					else
						writer.Write(Value);
				}

				public override void ReadBinary(NetworkReader reader)
				{
					if (useHalf)
						Value = reader.Read<Half>();
					else
						Value = reader.Read<float>();

					Target = Value;
				}

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

				[SerializeField]
				bool ignore = false;
				public bool Ignore => ignore;

				public byte ID { get; protected set; }
				public void SetID(byte value) => ID = value;

				public int Hash { get; protected set; }

				public bool IsDirty { get; protected set; }
				public virtual void ClearDirty() => IsDirty = false;

				public abstract AnimatorControllerParameterType Type { get; }

				protected SimpleNetworkAnimator animator;
				internal void Set(SimpleNetworkAnimator reference)
				{
					animator = reference;

					Hash = Animator.StringToHash(name);
				}

				public abstract void WriteBinary(NetworkWriter writer);
				public abstract void ReadBinary(NetworkReader reader);

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
				public abstract TValue Value { get; internal set; }

				public bool Update(TValue data)
				{
					if (Compare(data, Value)) return false;

					Value = data;
					IsDirty = true;
					return true;
				}

				public virtual bool Compare(TValue a, TValue b) => Equals(a, b);

				public override void WriteBinary(NetworkWriter writer) => writer.Write(Value);
				public override void ReadBinary(NetworkReader reader) => Value = reader.Read<TValue>();

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
					All[i].Set(animator);

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

		#region Trigger
		public void SetTrigger(string name, bool instant = true)
		{
			if (Parameters.TryGet<ParametersProperty.TriggerProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			if (parameter.Ignore)
			{
				Debug.LogWarning($"Recieved Set For Ignored Animator Parameter {name}, This will be ignored");
				return;
			}

			if (parameter.Update(true) == false) return;

			if (instant) SendTrigger(parameter);
		}

		void SendTrigger(ParametersProperty.TriggerProperty parameter)
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

			parameter.Value = true;
		}

		void SyncTriggers()
		{
			foreach (var parameter in parameters.Triggers)
			{
				if (parameter.Ignore) continue;
				if (parameter.IsDirty == false) continue;

				SendTrigger(parameter);
			}
		}
		#endregion

		#region Bool
		public void SetBool(string name, bool value, bool instant = true)
		{
			if (Parameters.TryGet<ParametersProperty.BoolProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			if (parameter.Ignore)
			{
				Debug.LogWarning($"Recieved Set For Ignored Animator Parameter {name}, This will be ignored");
				return;
			}

			if (parameter.Update(value) == false) return;

			if (instant)
			{
				SendBool(parameter);
				BufferBools();
			}
		}

		void SendBool(ParametersProperty.BoolProperty parameter)
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

			parameter.Value = value;
		}

		void BufferBools()
		{
			var binary = WriteAll(parameters.Floats);
			BufferRPC(BufferBools, binary);
		}
		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void BufferBools(byte[] binary, RpcInfo info)
		{
			ReadAll(parameters.Bools, binary);
		}

		void SyncBools()
		{
			var dirty = false;

			foreach (var parameter in parameters.Bools)
			{
				if (parameter.Ignore) continue;
				if (parameter.IsDirty == false) continue;

				SendBool(parameter);
				dirty = true;
			}

			if (dirty) BufferBools();
		}
		#endregion

		#region Integer
		public void SetInteger(string name, int value, bool instant = true)
		{
			if (Parameters.TryGet<ParametersProperty.IntegerProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			if (parameter.Ignore)
			{
				Debug.LogWarning($"Recieved Set For Ignored Animator Parameter {name}, This will be ignored");
				return;
			}

			if (parameter.Update(value) == false) return;

			if (instant)
			{
				SendInteger(parameter);
				BufferIntergers();
			}
		}

		void SendInteger(ParametersProperty.IntegerProperty parameter)
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

			parameter.Value = value;
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void ShortRPC(byte id, short value, RpcInfo info) => IntergerRPC(id, value, info);

		void BufferIntergers()
		{
			var binary = WriteAll(parameters.Floats);
			BufferRPC(BufferIntergers, binary);
		}
		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void BufferIntergers(byte[] binary, RpcInfo info)
		{
			ReadAll(parameters.Integers, binary);
		}

		void SyncIntegers()
		{
			var dirty = false;

			foreach (var parameter in parameters.Integers)
			{
				if (parameter.Ignore) continue;
				if (parameter.IsDirty == false) continue;

				SendInteger(parameter);
				dirty = true;
			}

			if (dirty) BufferIntergers();
		}
		#endregion

		#region Float
		public void SetFloat(string name, float value, bool instant = false)
		{
			if (Parameters.TryGet<ParametersProperty.FloatProperty>(name, out var parameter) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			if (parameter.Ignore)
			{
				Debug.LogWarning($"Recieved Set For Ignored Animator Parameter {name}, This will be ignored");
				return;
			}

			if (parameter.Update(value) == false) return;

			if (instant)
			{
				SendFloat(parameter);
				BufferFloats();
			}
		}

		void SendFloat(ParametersProperty.FloatProperty parameter)
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
				parameter.Value = value;
		}

		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void HalfRPC(byte id, Half value, RpcInfo info) => FloatRPC(id, value, info);

		void BufferFloats()
		{
			var binary = WriteAll(parameters.Floats);

			BufferRPC(BufferFloats, binary);
		}
		[NetworkRPC(Authority = RemoteAuthority.Owner)]
		void BufferFloats(byte[] binary, RpcInfo info)
		{
			ReadAll(parameters.Floats, binary);
		}

		void SyncFloats()
		{
			var dirty = false;

			foreach (var parameter in parameters.Floats)
			{
				if (parameter.Ignore) continue;
				if (parameter.IsDirty == false) continue;

				SendFloat(parameter);
				dirty = true;
			}

			if (dirty) BufferFloats();
		}
		#endregion

		byte[] WriteAll<T>(IList<T> list)
			where T : ParametersProperty.Property
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Ignore) continue;

				list[i].WriteBinary(writer);
			}

			return writer.Flush();
		}
		void ReadAll<T>(IList<T> list, byte[] binary)
			where T : ParametersProperty.Property
		{
			reader.Set(binary);

			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Ignore) continue;

				list[i].ReadBinary(reader);
			}
		}
		#endregion

		#region Layers
		[SerializeField]
		LayersProperty layers = default;
		public LayersProperty Layers => layers;
		[Serializable]
		public class LayersProperty
		{
			[SerializeField]
			List<Property> list;
			public List<Property> List => list;

			public int Count => list.Count;

			public Property this[int index] => list[index];

			[Serializable]
			public class Property
			{
				[SerializeField]
				protected string name;
				public string Name => name;

				[SerializeField]
				bool ignore = false;
				public bool Ignore => ignore;

				public float Value
				{
					get
					{
						return animator.Component.GetLayerWeight(Index);
					}
					internal set
					{
						animator.Component.SetLayerWeight(Index, value);
					}
				}

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

				public byte ID { get; protected set; }
				public void SetID(byte value) => ID = value;

				public int Index { get; protected set; }

				public bool IsDirty { get; protected set; }
				public virtual void ClearDirty() => IsDirty = false;

				SimpleNetworkAnimator animator;
				internal void Set(SimpleNetworkAnimator reference)
				{
					animator = reference;

					Index = animator.Component.GetLayerIndex(Name);
				}

				public bool Update(float data)
				{
					if (Mathf.Approximately(data, Value)) return false;

					Value = data;
					IsDirty = true;
					return true;
				}

				public bool Translate()
				{
					if (Mathf.Approximately(Target, Value)) return false;

					//TODO use a translation method better than lerp
					Value = Mathf.Lerp(Value, Target, speed * Time.deltaTime);

					return true;
				}

				public void WriteBinary(NetworkWriter writer)
				{
					if (useHalf)
						writer.Write((Half)Value);
					else
						writer.Write(Value);
				}

				public void ReadBinary(NetworkReader reader)
				{
					if (useHalf)
						Value = reader.Read<Half>();
					else
						Value = reader.Read<float>();

					Target = Value;
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
					list[i].Set(animator);

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

		#region Set Weight
		public void SetLayerWeight(string name, float value, bool instant = false)
		{
			if (layers.TryGet(name, out var layer) == false)
			{
				Debug.LogWarning($"No Network Animator Parameter Found with name {name}");
				return;
			}

			if (layer.Ignore)
			{
				Debug.LogWarning($"Recieved Set For Ignored Animator Layer {name}, This will be ignored");
				return;
			}

			Component.SetLayerWeight(layer.Index, value);

			if (layer.Update(value) == false) return;

			if (instant)
			{
				SendLayerWeight(layer);
				BufferLayerWeights();
			}
		}

		void SendLayerWeight(LayersProperty.Property layer)
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

		void BufferLayerWeights()
		{
			for (int i = 0; i < layers.Count; i++)
			{
				if (layers[i].Ignore) continue;

				layers[i].WriteBinary(writer);
			}

			var binary = writer.Flush();

			BufferRPC(BufferLayerWeights, binary);
		}
		[NetworkRPC]
		void BufferLayerWeights(byte[] binary, RpcInfo info)
		{
			reader.Set(binary);

			for (int i = 0; i < layers.Count; i++)
			{
				if (layers[i].Ignore) continue;

				layers[i].ReadBinary(reader);
			}
		}

		void SyncLayerWeights()
		{
			var dirty = false;

			foreach (var layer in layers.List)
			{
				if (layer.Ignore == false) continue;
				if (layer.IsDirty == false) continue;

				SendLayerWeight(layer);
				dirty = true;
			}

			if (dirty) BufferLayerWeights();
		}
		#endregion
		#endregion

		void Sync()
		{
			SyncTriggers();
			SyncBools();
			SyncIntegers();
			SyncFloats();

			SyncLayerWeights();
		}

		void Update()
		{
			if (Entity.IsReady == false) return;

			if (Entity.IsMine == false) Translate();
		}

		void Translate()
		{
			foreach (var parameter in parameters.Floats)
			{
				if (parameter.Ignore) continue;
				if (parameter.Smooth == false) continue;

				if (parameter.Translate() == false) continue;
			}

			foreach (var layer in layers.List)
			{
				if (layer.Ignore) continue;
				if (layer.Smooth == false) continue;

				if (layer.Translate() == false) continue;
			}
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
using UnityEngine;
using UnityEngine.Scripting;
using System.Runtime.CompilerServices;

[assembly : AssemblySymbolDefine("MNet_Generated_AOT_Code")]

[Preserve]
[CompilerGenerated]
public static class GeneratedNetworkCode
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	static void OnLoad()
	{

		//RPCs
		{
			//MNet.SimpleNetworkAnimator
			MNet.RpcBindVoid<System.Byte, System.Boolean>.Register();
			MNet.RpcBindVoid<MNet.ByteChunk>.Register();
			MNet.RpcBindVoid<MNet.ByteChunk>.Register();
			MNet.RpcBindVoid<MNet.ByteChunk>.Register();
			MNet.RpcBindVoid<MNet.ByteChunk>.Register();
			MNet.RpcBindVoid<System.Byte, System.Single>.Register();
			MNet.RpcBindVoid<System.Byte, Half>.Register();
			MNet.RpcBindVoid<System.Byte, System.Int32>.Register();
			MNet.RpcBindVoid<System.Byte, System.Single>.Register();
			MNet.RpcBindVoid<System.Byte, Half>.Register();
			MNet.RpcBindVoid<System.Byte, System.Int16>.Register();
			MNet.RpcBindVoid<System.Byte>.Register();
			
			//MNet.SimpleNetworkTransform
			MNet.RpcBindVoid<MNet.SimpleNetworkTransform.CoordinatesPacket>.Register();
			MNet.RpcBindVoid<MNet.SimpleNetworkTransform.CoordinatesPacket>.Register();
			
			//MNet.RpcTest
			MNet.RpcBindVoid.Register();
			MNet.RpcBindVoid<System.Int32>.Register();
			MNet.RpcBindVoid<System.Int32, System.Int32>.Register();
			MNet.RpcBindVoid<System.Int32, System.Int32, System.Int32>.Register();
			MNet.RpcBindVoid<System.Int32, System.Int32, System.Int32, System.Int32>.Register();
			MNet.RpcBindVoid<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32>.Register();
			MNet.RpcBindVoid<System.Int32, System.Int32, System.Int32, System.Int32, System.Int32, System.Int32>.Register();
			
			//MNet.RprTest
			MNet.RpcBindReturn<System.String>.Register();
			
			//MNet.SerializationTest
			MNet.RpcBindVoid<MNet.NetworkEntity, MNet.SerializationTest>.Register();
			
			//MNet.Example.RoomLog
			MNet.RpcBindVoid<System.String>.Register();
			
			//MNet.Example.StressBehaviour
			MNet.RpcBindVoid<System.String>.Register();
			
			//MNet.Example.StressEntity
			MNet.RpcBindVoid<UnityEngine.Vector3, System.Single>.Register();
			
			
		}
		
		//Resolvers
		{
			new MNet.BlittableNetworkSerializationResolver<MNet.PingResponse>();
			new MNet.BlittableNetworkSerializationResolver<MNet.PingRequest>();
			new MNet.INetworkSerializableResolver<MNet.NetworkClientProfile>();
			new MNet.BlittableNetworkSerializationResolver<MNet.TimeRequest>();
			new MNet.IManualNetworkSerializableResolver<MNet.ByteChunk>();
			new MNet.BlittableNetworkSerializationResolver<MNet.NetworkClientID>();
			new MNet.INetworkSerializableResolver<MNet.RoomInfo>();
			new MNet.ArrayNetworkSerializationResolver<MNet.NetworkClientInfo>();
			new MNet.INetworkSerializableResolver<MNet.NetworkClientInfo>();
			new MNet.BlittableNetworkSerializationResolver<MNet.TimeResponse>();
			new MNet.EnumNetworkSerializationResolver<MNet.EntityType, System.Byte>();
			new MNet.BlittableNetworkSerializationResolver<MNet.EntitySpawnToken>();
			new MNet.EnumNetworkSerializationResolver<MNet.PersistanceFlags, System.Byte>();
			new MNet.IManualNetworkSerializableResolver<MNet.AttributesCollection>();
			new MNet.BlittableNetworkSerializationResolver<MNet.NetworkEntityID>();
			new MNet.EnumNetworkSerializationResolver<MNet.RoomInfoTarget, System.Int32>();
			new MNet.ArrayNetworkSerializationResolver<System.UInt16>();
			new MNet.ArrayNetworkSerializationResolver<MNet.NetworkGroupID>();
			new MNet.BlittableNetworkSerializationResolver<MNet.NetworkGroupID>();
			new MNet.EnumNetworkSerializationResolver<MNet.Log.Level, System.Int32>();
			new MNet.INetworkSerializableResolver<MNet.GameServerID>();
			new MNet.ListNetworkSerializationResolver<MNet.RoomInfo>();
			new MNet.INetworkSerializableResolver<MNet.ServerLogPayload>();
			new MNet.INetworkSerializableResolver<MNet.RegisterClientResponse>();
			new MNet.INetworkSerializableResolver<MNet.RegisterClientRequest>();
			new MNet.INetworkSerializableResolver<MNet.JoinNetworkGroupsPayload>();
			new MNet.INetworkSerializableResolver<MNet.LeaveNetworkGroupsPayload>();
			new MNet.BlittableNetworkSerializationResolver<MNet.SpawnEntityResponse>();
			new MNet.INetworkSerializableResolver<MNet.SpawnEntityRequest>();
			new MNet.BlittableNetworkSerializationResolver<MNet.DestroyEntityPayload>();
			new MNet.INetworkSerializableResolver<MNet.RprCommand>();
			new MNet.INetworkSerializableResolver<MNet.RprResponse>();
			new MNet.INetworkSerializableResolver<MNet.RprRequest>();
			new MNet.INetworkSerializableResolver<MNet.SystemMessagePayload>();
			new MNet.IManualNetworkSerializableResolver<MNet.BroadcastSyncVarRequest>();
			new MNet.IManualNetworkSerializableResolver<MNet.BufferSyncVarRequest>();
			new MNet.BlittableNetworkSerializationResolver<MNet.NetworkBehaviourID>();
			new MNet.BlittableNetworkSerializationResolver<MNet.RpcID>();
			new MNet.EnumNetworkSerializationResolver<MNet.RemoteBufferMode, System.Byte>();
			new MNet.NullableNetworkSerializationResolver<MNet.NetworkClientID>();
			new MNet.BlittableNetworkSerializationResolver<MNet.RprChannelID>();
			new MNet.INetworkSerializableResolver<MNet.MasterServerSchemeRequest>();
			new MNet.INetworkSerializableResolver<MNet.MasterServerSchemeResponse>();
			new MNet.INetworkSerializableResolver<MNet.MasterServerInfoRequest>();
			new MNet.INetworkSerializableResolver<MNet.MasterServerInfoResponse>();
			new MNet.INetworkSerializableResolver<MNet.BroadcastRpcRequest>();
			new MNet.INetworkSerializableResolver<MNet.TargetRpcRequest>();
			new MNet.INetworkSerializableResolver<MNet.QueryRpcRequest>();
			new MNet.INetworkSerializableResolver<MNet.BufferRpcRequest>();
			new MNet.INetworkSerializableResolver<MNet.AppID>();
			new MNet.BlittableNetworkSerializationResolver<MNet.Version>();
			new MNet.INetworkSerializableResolver<MNet.RoomOptions>();
			new MNet.INetworkSerializableResolver<MNet.AppConfig>();
			new MNet.INetworkSerializableResolver<MNet.RemoteConfig>();
			new MNet.ArrayNetworkSerializationResolver<MNet.GameServerInfo>();
			new MNet.INetworkSerializableResolver<MNet.GameServerInfo>();
			new MNet.INetworkSerializableResolver<MNet.CreateRoomRequest>();
			new MNet.INetworkSerializableResolver<MNet.ChangeRoomInfoPayload>();
			new MNet.BlittableNetworkSerializationResolver<MNet.ChangeMasterCommand>();
			new MNet.INetworkSerializableResolver<MNet.ClientConnectedPayload>();
			new MNet.BlittableNetworkSerializationResolver<MNet.ClientDisconnectPayload>();
			new MNet.BlittableNetworkSerializationResolver<MNet.LoadScenePayload>();
			new MNet.INetworkSerializableResolver<MNet.UnloadScenePayload>();
			new MNet.INetworkSerializableResolver<MNet.SpawnEntityCommand>();
			new MNet.BlittableNetworkSerializationResolver<MNet.TransferEntityPayload>();
			new MNet.BlittableNetworkSerializationResolver<MNet.TakeoverEntityCommand>();
			new MNet.INetworkSerializableResolver<MNet.BroadcastRpcCommand>();
			new MNet.INetworkSerializableResolver<MNet.TargetRpcCommand>();
			new MNet.INetworkSerializableResolver<MNet.QueryRpcCommand>();
			new MNet.INetworkSerializableResolver<MNet.BufferRpcCommand>();
			new MNet.IManualNetworkSerializableResolver<MNet.SyncVarCommand>();
			new MNet.BlittableNetworkSerializationResolver<MNet.TakeoverEntityRequest>();
			new MNet.EnumNetworkSerializationResolver<MNet.GameServerRegion, System.Byte>();
			new MNet.INetworkSerializableResolver<MNet.GetLobbyInfoRequest>();
			new MNet.INetworkSerializableResolver<MNet.LobbyInfo>();
			new MNet.BlittableNetworkSerializationResolver<MNet.RoomID>();
			new MNet.EnumNetworkSerializationResolver<MNet.MigrationPolicy, System.Byte>();
			new MNet.NullableNetworkSerializationResolver<System.Byte>();
			new MNet.EnumNetworkSerializationResolver<MNet.NetworkEntityNetworkSerializationResolver.State, System.Byte>();
			new MNet.BlittableNetworkSerializationResolver<MNet.SyncVarID>();
			new MNet.EnumNetworkSerializationResolver<MNet.SimpleNetworkTransform.ChangeFlags, System.Int32>();
			new MNet.EnumNetworkSerializationResolver<MNet.NetworkTransportType, System.Byte>();
			new MNet.EnumNetworkSerializationResolver<MNet.RemoteResponseType, System.Byte>();
			new MNet.INetworkSerializableResolver<MNet.SimpleNetworkTransform.CoordinatesPacket>();
			new MNet.NetworkBehaviourNetworkSerializationResolver<MNet.SerializationTest>();
			
		}
		
	}
}
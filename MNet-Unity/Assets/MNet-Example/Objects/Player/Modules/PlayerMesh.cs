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
	[RequireComponent(typeof(MeshRenderer))]
	public class PlayerMesh : NetworkBehaviour
	{
		[SyncVar(Authority = RemoteAuthority.Owner)]
		Color Color
		{
			get => mesh.material.color;
			set => mesh.material.color = value;
		}

		MeshRenderer mesh;

		void Awake()
		{
			mesh = GetComponent<MeshRenderer>();
		}

		void Start()
		{
			if (IsMine) SyncVar(nameof(Color), Random.ColorHSV());
		}
	}
}
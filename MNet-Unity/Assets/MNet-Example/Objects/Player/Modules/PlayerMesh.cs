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
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
	public class PlayerMesh : NetworkBehaviour
	{
		[SerializeField]
		Renderer[] renderers = default;

		void SetColor(Color color)
        {
			var block = new MaterialPropertyBlock();

			block.SetColor("_Color", color);

			for (int i = 0; i < renderers.Length; i++)
				renderers[i].SetPropertyBlock(block);
		}

        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSetup += SetupCallback;
        }

        void SetupCallback()
        {
			ReadAttributes(Entity.Attributes, out var color);

			SetColor(color);
		}

		//Static Utility

		public static void WriteAttributes(AttributesCollection attributes, Color color)
		{
			attributes.Set(2, color);
		}

		public static void ReadAttributes(AttributesCollection attributes, out Color color)
		{
			attributes.TryGetValue(2, out color);
		}
	}
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
}
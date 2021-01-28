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

using Cysharp.Threading.Tasks;

using System.Threading;
using System.Threading.Tasks;

namespace MNet.Example
{
    public class Sandbox : NetworkBehaviour
    {
        public SimpleNetworkAnimator animator;

        public float speed = 5;

        public float acceleration = 20;

        public float value;

        void Update()
        {
            if (Entity == null) return;
            if (Entity.IsReady == false) return;
            if (Entity.IsMine == false) return;

            var target = Input.GetAxisRaw("Vertical") * speed;

            target = Mathf.Abs(target);

            value = Mathf.MoveTowards(value, target, acceleration * Time.deltaTime);

            animator.SetFloat("Move", value / speed * 2f);
            animator.SetLayerWeight("Locomotion", value / speed);
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {

        }
    }
}
﻿using System;
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
	public class NetworkTimeSinExample : MonoBehaviour
	{
        public float range = 5f;

        void Update()
        {
            var sin = Mathf.Sin(NetworkAPI.Time.Seconds);

            var position = transform.position;
            position.x = sin * range;
            transform.position = position;
        }
    }
}
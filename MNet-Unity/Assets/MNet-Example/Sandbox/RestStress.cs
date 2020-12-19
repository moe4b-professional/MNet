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
	public class RestStress : MonoBehaviour
	{
        IEnumerator Start()
        {
            bool HasNoSelection() => NetworkAPI.Server.Game.Selection == null;

            yield return new WaitWhile(HasNoSelection);

            while(true)
            {
                for (int i = 0; i < 25; i++)
                {
                    NetworkAPI.Lobby.GetInfo();
                }

                yield return new WaitForSeconds(1f);
            }
        }
	}
}
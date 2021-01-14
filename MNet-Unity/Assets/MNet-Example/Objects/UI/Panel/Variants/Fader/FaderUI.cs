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

namespace MNet.Example
{
	[RequireComponent(typeof(Image), typeof(CanvasGroup))]
	public class FaderUI : UIPanel
	{
		Image image;

		CanvasGroup canvasGroup;

		Color Color
		{
			get => image.color;
			set => image.color = value;
		}

		float Alpha
        {
			get => canvasGroup.alpha;
			set => canvasGroup.alpha = value;
        }

		bool BlockRaycasts
        {
			get => canvasGroup.blocksRaycasts;
			set => canvasGroup.blocksRaycasts = value;
        }

		CancellationToken AsyncDestoryToken;

		void Awake()
        {
			image = GetComponent<Image>();
			canvasGroup = GetComponent<CanvasGroup>();
        }

		void Start()
        {
			BlockRaycasts = false;

			AsyncDestoryToken = gameObject.GetCancellationTokenOnDestroy();
		}

		public async UniTask Transition(float target, float duration)
        {
			if (target > 0f) BlockRaycasts = true;

			var initial = Alpha;

			var timer = 0f;

			while (timer != duration)
            {
				timer = Mathf.MoveTowards(timer, duration, Time.unscaledDeltaTime);

				Alpha = Mathf.Lerp(initial, target, timer / duration);

				await UniTask.NextFrame(cancellationToken: AsyncDestoryToken);
			}

			if (Alpha == 0f) BlockRaycasts = false;
        }
	}
}
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

using UnityEngine.EventSystems;

namespace MNet.Example
{
	public class RoomLogPanel : UIPanel
	{
        [SerializeField]
        RoomLog log = default;

        [SerializeField]
        GameObject template = default;

        [SerializeField]
        ScrollRect scroll = default;

        Queue<RoomLogUITemplate> stack = new Queue<RoomLogUITemplate>();

        [SerializeField]
        int max = 40;

        [SerializeField]
        Broadcast broadcast = new Broadcast();
        [Serializable]
        public class Broadcast
        {
            [SerializeField]
            UIElement element = default;

            public bool Visible => element.Visible;

            [SerializeField]
            InputField field = default;

            public string Text
            {
                get => field.text;
                set => field.text = value;
            }

            [SerializeField]
            Button button = default;

            RoomLog log;

            LevelPause Pause => Level.Instance.Pause;

            public void Configure(RoomLog reference)
            {
                log = reference;

                element.Hide();

                button.onClick.AddListener(Send);
            }

            public void Send()
            {
                if (string.IsNullOrEmpty(Text)) return;

                log.Broadcast(Text);

                Text = string.Empty;

                Hide();
            }

            public void Show()
            {
                Pause.Set(LevelPauseMode.Soft);
                element.Show();
                UIElement.Selection = field.gameObject;
            }
            public void Hide()
            {
                Pause.Set(LevelPauseMode.None);
                UIElement.Selection = null;
                element.Hide();
            }
        }

        void Start()
        {
            broadcast.Configure(log);

            log.OnAdd += Add;
        }

        void Update()
        {
            if(broadcast.Visible)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    broadcast.Hide();
                }
                else if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (string.IsNullOrEmpty(broadcast.Text))
                        broadcast.Hide();
                    else
                        broadcast.Send();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Return))
                    broadcast.Show();
            }
        }

        void Add(RoomLog.Entry log)
		{
            var instance = RoomLogUITemplate.Create(template, log);
            instance.SetParent(scroll.content);
            stack.Enqueue(instance);

            while (stack.Count > max) Release();

            ScrollToLast();
        }

        void Release()
        {
            var element = stack.Dequeue();
            scroll.content.localPosition -= Vector3.up * (element.Rect.sizeDelta.y + 10);
            Destroy(element.gameObject);
        }

        Coroutine coroutine;
        void ScrollToLast()
        {
            if (coroutine != null) StopCoroutine(coroutine);

            coroutine = StartCoroutine(Procedure());

            IEnumerator Procedure()
            {
                yield return new WaitForEndOfFrame();

                var initial = scroll.verticalNormalizedPosition;
                var final = 0f;

                var duration = 0.2f;
                var timer = duration;

                while (timer > 0f)
                {
                    timer = Mathf.MoveTowards(timer, 0f, Time.deltaTime);

                    scroll.verticalNormalizedPosition = Mathf.Lerp(final, initial, timer / duration);

                    yield return new WaitForEndOfFrame();
                }

                coroutine = null;
            }
        }

        void OnDestroy()
        {
            log.OnAdd -= Add;
        }
	}
}
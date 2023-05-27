using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using KomeTube.Emums;
using KomeTube.Kernel;
using KomeTube.Kernel.YtLiveChatDataModel;
using UnityEngine;

namespace VTSYoutubeDropper
{
    [BepInPlugin("murabitob.vts.youtube.dropper", "youtube.dropper", "1.0")]
    public class VTSYoutubeDropper: BaseUnityPlugin
    {
        private CommentLoader _loader;
        private bool _isGuiOpen;
        
        private void Awake()
        {
            Logger.LogInfo("VTSYoutubeDropper Started.");
            _loader = GenerateLoader();
        }

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(VTSYoutubeDropper));
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                _isGuiOpen = !_isGuiOpen;
            }
        }


        private IEnumerable<string> FilterEmojis(IEnumerable<CommentData> data)
        {

            var result = data.Select(
                comment => comment?.addChatItemAction?.item?.liveChatTextMessageRenderer?.message?.runs
                    ?.Where(run => run.type == CommentDetailType.emoji).Select(x => x.content));
            
            return result.SelectMany(x => x).Where(x => !x.EndsWith(".svg"));
        }

        private CommentLoader GenerateLoader()
        {
            _loader = new CommentLoader();
            _loader.OnCommentsReceive += (sender, comments) =>
            {
                DropEmojiOnReceive(comments);
            };

            return _loader;
        }

        private void DropEmojiOnReceive(IEnumerable<CommentData> data)
        {
            var filteredData = FilterEmojis(data);
            foreach (var s in filteredData)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(delegate()
                {
                    this.StartCoroutine(TwitchDropper.Instance().GetTextureAndDrop(s, null));
                });
            }
        }


        private Rect windowRect = new Rect(50, 50, 300, 200);
        private string url = "";
        
        // GUI
        private void OnGUI()
        {
            if (_isGuiOpen)
            {
                GUILayout.Window(19961008, windowRect, WindowFunc, "MurabitoB Youtube Dropper Plugin v1.0b");
            }
        }

        public void WindowFunc(int id)
        {
            if (!string.IsNullOrEmpty(_loader.VideoUrl))
            {
                GUILayout.Label($"Current listening url: {_loader.VideoUrl}");
                if (GUILayout.Button("Cancel"))
                {
                    _loader.Stop();
                    _loader = GenerateLoader();
                }
            }
            else
            {
                GUILayout.Label("Please input youtube stream url");
                url = GUILayout.TextField(url);
                if (GUILayout.Button("Confirm"))
                {
                    _loader.Start(url);
                }
            }
            
            GUI.DragWindow(windowRect);
        }

        [HarmonyPatch(typeof(TwitchDropper), "Update")]
        [HarmonyPrefix]
        public static void TwitchDropper_Update_Patch()
        {
            TwitchDropper.on = true;
            TwitchConfigItem.on = true;
        }
    }
}
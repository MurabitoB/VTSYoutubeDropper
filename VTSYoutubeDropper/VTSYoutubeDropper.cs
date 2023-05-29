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
    [BepInPlugin("murabitob.vts.youtube.dropper", "youtube.dropper", "1.1")]
    public class VTSYoutubeDropper: BaseUnityPlugin
    {
        private CommentLoader _loader;
        private bool _isGuiOpen;
        private bool _isDropEmote = true;
        private bool _isDropThumbnail = true;
        private bool _isPaidOnly = false;
        private bool _isMemberOnly = false;
        
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


        private IEnumerable<string> FilterEmojis(IEnumerable<CommentData> data, bool isPaidOnly = false)
        {

            var result = data.Select(
                comment => comment?.addChatItemAction?.item?.liveChatTextMessageRenderer?.message?.runs
                    ?.Where(run => run.type == CommentDetailType.emoji).Select(x => x.content));
            
            var paidEmojis = data.Select(
                comment => comment?.addChatItemAction?.item?.liveChatPaidMessageRenderer?.message?.runs
                    ?.Where(run => run.type == CommentDetailType.emoji).Select(x => x.content));
            
            if(isPaidOnly)
                return paidEmojis.SelectMany(x => x).Where(x => !x.EndsWith(".svg"));
            
            return result.SelectMany(x => x).Concat(paidEmojis.SelectMany(x => x)).Where(x => !x.EndsWith(".svg"));
        }
        
        private IEnumerable<string> FilterThumbnails(IEnumerable<CommentData> data, bool isPaidOnly = false)
        {

            var normalEmojis = data.Select(
                comment => comment?.addChatItemAction?.item?.liveChatTextMessageRenderer?.authorPhoto?.thumbnails
                    ?.Select(x => x.url).LastOrDefault());
            
            
            var paidEmojis = data.Select(
                comment => comment?.addChatItemAction?.item?.liveChatPaidMessageRenderer?.authorPhoto?.thumbnails
                    ?.Select(x => x.url).LastOrDefault());
            
            
            if(isPaidOnly)
                return paidEmojis.Where(x => !string.IsNullOrEmpty(x));
            
            

            return normalEmojis.Concat(paidEmojis).Where(x => !string.IsNullOrEmpty(x));
        }

        private CommentLoader GenerateLoader()
        {
            _loader = new CommentLoader();
            _loader.OnCommentsReceive += (sender, comments) =>
            {
                if (_isDropEmote)
                {
                    DropEmoteOnReceive(comments, _isPaidOnly);
                }
                
                if (_isDropThumbnail)
                {
                    DropThumbnailOnReceive(comments, _isPaidOnly);
                }
                
            };

            return _loader;
        }

        private void DropEmoteOnReceive(IEnumerable<CommentData> data, bool isPaidOnly = false)
        {
            var filteredData = FilterEmojis(data, isPaidOnly);
            foreach (var s in filteredData)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(delegate()
                {
                    this.StartCoroutine(TwitchDropper.Instance().GetTextureAndDrop(s, null));
                });
            }
        }
        
        private void DropThumbnailOnReceive(IEnumerable<CommentData> data, bool isPaidOnly = false)
        {
            var filteredData = FilterThumbnails(data, isPaidOnly);
            foreach (var s in filteredData)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(delegate()
                {
                    this.StartCoroutine(TwitchDropper.Instance().GetTextureAndDrop(s, null));
                });
            }
        }


        private Rect windowRect = new Rect(50, 50, 300, 100);
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
            _isPaidOnly = GUILayout.Toggle(_isPaidOnly, "Only Drop Paid Message");
            _isDropEmote = GUILayout.Toggle(_isDropEmote, "Drop Emote");
            _isDropThumbnail = GUILayout.Toggle(_isDropThumbnail, "Drop AuthorPhoto");
            
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
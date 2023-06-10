using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        private Rect _windowRect = new Rect(50, 50, 300, 100);
        private string _url = "";
        private string _language = "";
        private Dictionary<string, string> _textureCache = new Dictionary<string, string>();
        
        
        // i18n value
        private string _i18nPluginName;
        private string _i18nDropEmote;
        private string _i18nDropThumbnail;
        private string _i18nPaidOnly;
        private string _i18nMemberOnly;
        private string _i18nUrl;
        private string _i18nTrackingUrl;
        private string _i18nConfirm;
        private string _i18nCancel;

        private void Awake()
        {
            Logger.LogInfo("VTSYoutubeDropper Started.");
            _loader = GenerateLoader();
        }

        private void Start()
        {
            Harmony.CreateAndPatchAll(typeof(VTSYoutubeDropper));
            _language = ConfigManager.GetString(ConfigManager.C_MAIN_LANGUAGE);
            Logger.LogInfo("[VTSYoutubeDropper]: Started.");
            Logger.LogInfo("[VTSYoutubeDropper]: " + _language);
            
            // i18n
            _i18nPluginName = I18nManager.Translate(_language, "plugin_name");
            _i18nDropEmote = I18nManager.Translate(_language, "drop_emote");
            _i18nDropThumbnail = I18nManager.Translate(_language, "drop_thumbnail");
            _i18nPaidOnly = I18nManager.Translate(_language, "paid_only");
            _i18nMemberOnly = I18nManager.Translate(_language, "member_only");
            _i18nUrl = I18nManager.Translate(_language, "url");
            _i18nTrackingUrl = I18nManager.Translate(_language, "tracking_url");
            _i18nConfirm = I18nManager.Translate(_language, "confirm");
            _i18nCancel = I18nManager.Translate(_language, "cancel");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Delete))
            {
                _isGuiOpen = !_isGuiOpen;
            }
        }

        
        private CommentLoader GenerateLoader()
        {
            _loader = new CommentLoader();
            _loader.OnCommentsReceive += (sender, comments) =>
            {
                if (_isDropEmote)
                {
                    DropEmoteOnReceive(comments, _isPaidOnly, _isMemberOnly);
                }
                
                if (_isDropThumbnail)
                {
                    DropThumbnailOnReceive(comments, _isPaidOnly, _isMemberOnly);
                }
                
            };

            return _loader;
        }

        private void DropEmoteOnReceive(IEnumerable<CommentData> data, bool isPaidOnly = false, bool isMemberOnly = false)
        {
            var filteredData = Utils
                .FilterEmojis(data, isPaidOnly, isMemberOnly, TwitchDropper.emotesPerMessage)
                .Select(url => GetCache(24, 128, url));
            foreach (var s in filteredData)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(delegate()
                {
                    this.StartCoroutine(TwitchDropper.Instance().GetTextureAndDrop(s, null));
                });
            }
        }
        
        private void DropThumbnailOnReceive(IEnumerable<CommentData> data, bool isPaidOnly = false, bool isMemberOnly = false)
        {
            var filteredData = Utils
                .FilterThumbnails(data, isPaidOnly, isMemberOnly)
                .Select(url => GetCache(64, 128, url, true));
                
            foreach (var s in filteredData)
            {
                UnityMainThreadDispatcher.Instance().Enqueue(delegate()
                {
                    this.StartCoroutine(TwitchDropper.Instance().GetTextureAndDrop(s, null));
                });
            }
        }

        private string GetCache(int originRes, int upperRes, string url, bool isSize = false)
        {
            if(_textureCache.ContainsKey(url))
            {
                return _textureCache[url];
            }

            string result = "";
            if(!isSize)
                result = url.Replace($"w{originRes}-h{originRes}", $"w{upperRes}-h{upperRes}");
            else
                result = url.Replace($"=s{originRes}-c-k", $"=s{upperRes}-c-k");
            _textureCache.Add(url, result);
            
            return result;
        }
        
        // GUI
        private void OnGUI()
        {
            if (_isGuiOpen)
            {
                GUILayout.Window(19961008, _windowRect, WindowFunc, $"{_i18nPluginName} v1.1b");
            }
        }

        public void WindowFunc(int id)
        {
            _isPaidOnly = GUILayout.Toggle(_isPaidOnly, $"{_i18nPaidOnly}");
            _isMemberOnly = GUILayout.Toggle(_isMemberOnly, $"{_i18nMemberOnly}");
            _isDropEmote = GUILayout.Toggle(_isDropEmote, $"{_i18nDropEmote}");
            _isDropThumbnail = GUILayout.Toggle(_isDropThumbnail, $"{_i18nDropThumbnail}");
            
            if (!string.IsNullOrEmpty(_loader.VideoUrl))
            {
                GUILayout.Label($"{_i18nTrackingUrl}: {_loader.VideoUrl}");
                if (GUILayout.Button($"{_i18nCancel}"))
                {
                    _loader.Stop();
                    _loader = GenerateLoader();
                }
            }
            else
            {
                GUILayout.Label($"{_i18nUrl}");
                _url = GUILayout.TextField(_url);
                if (GUILayout.Button($"{_i18nConfirm}"))
                {
                    _loader.Start(_url);
                }
            }
            
            GUI.DragWindow(_windowRect);
        }

        // public void LoadAssetBundle()
        // {
        //     Assembly assembly = Assembly.GetExecutingAssembly();
        //     var stream = assembly.GetManifestResourceStream("VTSYoutubeDropper.vtsyoutubedropper");
        //     var ab = AssetBundle.LoadFromStream(stream);
        //     if (ab != null)
        //     {
        //         var uiPrefab = ab.LoadAsset<GameObject>("VTSYoutubeDropper");
        //         var root = GameObject.Find("Front UI/--- ConfigWindow/ - [1] General Config/Viewport/Content");
        //         var instance = GameObject.Instantiate(uiPrefab, root.transform, false);
        //     }
        //     else
        //     {
        //         Logger.LogError("AssetBundle not found!");
        //     }
        // }

        [HarmonyPatch(typeof(TwitchDropper), "Update")]
        [HarmonyPrefix]
        public static void TwitchDropper_Update_Patch()
        {
            TwitchDropper.on = true;
            TwitchConfigItem.on = true;
        }
        
    }
}
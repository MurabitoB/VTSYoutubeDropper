using System.Collections.Generic;

namespace VTSYoutubeDropper
{
    public class I18nManager
    {
        private static readonly string _defaultLanguage = "English";
        
        private static readonly Dictionary<string, Dictionary<string, string>> i18n = new Dictionary<string, Dictionary<string, string>>()
        {
            // Traditional Chinese
            {
                "Traditional Chinese", new Dictionary<string, string>()
                {
                    {"plugin_name", "MurabitoB Youtube 貼圖掉落器"},
                    {"drop_emote", "掉落表情貼圖"},
                    {"drop_thumbnail", "掉落頭像"},
                    {"paid_only", "僅掉落付費留言"},
                    {"member_only", "僅掉落頻道會員及管理員"},
                    {"url", "Youtube直播網址"},
                    {"tracking_url", "監聽直播中網址"},
                    {"confirm", "確認"},
                    {"cancel", "取消"}
                }
            },
            // English 
            {
                "English", new Dictionary<string, string>()
                {
                    {"plugin_name", "MurabitoB Youtube Emote Dropper"},
                    {"drop_emote", "Drop Emote"},
                    {"drop_thumbnail", "Drop Thumbnail"},
                    {"paid_only", "Paid Message Only"},
                    {"member_only", "Member / Admin Only"},
                    {"url", "Youtube Live URL"},
                    {"tracking_url", "Tracking URL"},
                    {"confirm", "Confirm"},
                    {"cancel", "Cancel"}
                }
            },
            // Japanese
            {
                "Japanese", new Dictionary<string, string>()
                {
                    {"plugin_name", "MurabitoB Youtube Emote Dropper"},
                    {"drop_emote", "ドロップエモート"},
                    {"drop_thumbnail", "ドロップサムネイル"},
                    {"paid_only", "有料のみ"},
                    {"member_only", "メンバーのみ"},
                    {"url", "YoutubeライブURL"},
                    {"tracking_url", "トラッキングURL"},
                    {"confirm", "確認"},
                    {"cancel", "キャンセル"}
                }
            },
        };

        public static string Translate(string language, string key)
        {
            if (i18n.ContainsKey(language))
            {
                if (i18n[language].ContainsKey(key))
                {
                    return i18n[language][key];
                }
                else if (i18n[_defaultLanguage].ContainsKey(key))
                {
                    return i18n[_defaultLanguage][key];
                }
            }
            else if (i18n.ContainsKey(_defaultLanguage))
            {
                if (i18n[_defaultLanguage].ContainsKey(key))
                {
                    return i18n[_defaultLanguage][key];
                }
            }

            return key;
        }
    }
}
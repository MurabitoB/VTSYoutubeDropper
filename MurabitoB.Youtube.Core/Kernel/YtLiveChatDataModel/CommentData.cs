using KomeTube.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace KomeTube.Kernel.YtLiveChatDataModel
{
    public class Run
    {
        [JsonProperty("type")]
        public CommentDetailType Type { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class Message
    {
        public Message()
        {
            Runs = new List<Run>();
        }

        [JsonProperty("runs")]
        public List<Run> Runs { get; set; }

        [JsonProperty("simpleText")]
        public string SimpleText { get; set; }
    }

    public class AuthorName
    {
        [JsonProperty("simpleText")]
        public string SimpleText { get; set; }
    }

    public class AuthorBadge
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("iconType")]
        public string IconType { get; set; }

        [JsonProperty("tooltip")]
        public string Tooltip { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Thumbnail
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class AuthorPhoto
    {
        public AuthorPhoto()
        {
            Thumbnails = new List<Thumbnail>();
        }

        [JsonProperty("thumbnails")]
        public List<Thumbnail> Thumbnails { get; set; }
    }

    public class WebCommandMetadata
    {
        [JsonProperty("ignoreNavigation")]
        public bool IgnoreNavigation { get; set; }
    }

    public class CommandMetadata
    {
        public CommandMetadata()
        {
            WebCommandMetadata = new WebCommandMetadata();
        }

        [JsonProperty("webCommandMetadata")]
        public WebCommandMetadata WebCommandMetadata { get; set; }
    }

    public class LiveChatItemContextMenuEndpoint
    {
        [JsonProperty("parameters")]
        public string Parameters { get; set; }
    }

    public class ContextMenuEndpoint
    {
        public ContextMenuEndpoint()
        {
            CommandMetadata = new CommandMetadata();
            LiveChatItemContextMenuEndpoint = new LiveChatItemContextMenuEndpoint();
        }

        [JsonProperty("clickTrackingParams")]
        public string ClickTrackingParams { get; set; }

        [JsonProperty("commandMetadata")]
        public CommandMetadata CommandMetadata { get; set; }

        [JsonProperty("liveChatItemContextMenuEndpoint")]
        public LiveChatItemContextMenuEndpoint LiveChatItemContextMenuEndpoint { get; set; }
    }

    public class AccessibilityData
    {
        [JsonProperty("label")]
        public string Label { get; set; }
    }

    public class Accessibility
    {
        public Accessibility()
        {
            AccessibilityData = new AccessibilityData();
        }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("accessibilityData")]
        public AccessibilityData AccessibilityData { get; set; }
    }

    public class PurchaseAmountText
    {
        [JsonProperty("simpleText")]
        public string SimpleText { get; set; }
    }

    public class ContextMenuAccessibility
    {
        public ContextMenuAccessibility()
        {
            AccessibilityData = new AccessibilityData();
        }

        [JsonProperty("accessibilityData")]
        public AccessibilityData AccessibilityData { get; set; }
    }

    public class LiveChatPaidMessageRenderer : LiveChatTextMessageRenderer
    {
        public LiveChatPaidMessageRenderer()
        {
            PurchaseAmountText = new PurchaseAmountText();
        }

        [JsonProperty("purchaseAmountText")]
        public PurchaseAmountText PurchaseAmountText { get; set; }

        [JsonProperty("headerBackgroundColor")]
        public long HeaderBackgroundColor { get; set; }

        [JsonProperty("headerTextColor")]
        public long HeaderTextColor { get; set; }

        [JsonProperty("bodyBackgroundColor")]
        public long BodyBackgroundColor { get; set; }

        [JsonProperty("bodyTextColor")]
        public long BodyTextColor { get; set; }

        [JsonProperty("authorNameTextColor")]
        public long AuthorNameTextColor { get; set; }

        [JsonProperty("timestampColor")]
        public long TimestampColor { get; set; }
    }

    public class Sticker
    {
        public Sticker()
        {
            Thumbnails = new List<Thumbnail>();
            Accessibility = new Accessibility();
        }

        [JsonProperty("thumbnails")]
        public List<Thumbnail> Thumbnails { get; set; }

        [JsonProperty("accessibility")]
        public Accessibility Accessibility { get; set; }
    }

    public class LiveChatPaidStickerRenderer : LiveChatTextMessageRenderer
    {
        public LiveChatPaidStickerRenderer()
        {
            Sticker = new Sticker();
            PurchaseAmountText = new PurchaseAmountText();
        }

        [JsonProperty("sticker")]
        public Sticker Sticker { get; set; }

        [JsonProperty("moneyChipBackgroundColor")]
        public long MoneyChipBackgroundColor { get; set; }

        [JsonProperty("moneyChipTextColor")]
        public long MoneyChipTextColor { get; set; }

        [JsonProperty("purchaseAmountText")]
        public PurchaseAmountText PurchaseAmountText { get; set; }

        [JsonProperty("stickerDisplayWidth")]
        public int StickerDisplayWidth { get; set; }

        [JsonProperty("stickerDisplayHeight")]
        public int StickerDisplayHeight { get; set; }

        [JsonProperty("backgroundColor")]
        public long BackgroundColor { get; set; }

        [JsonProperty("authorNameTextColor")]
        public long AuthorNameTextColor { get; set; }

        [JsonProperty("trackingParams")]
        public string TrackingParams { get; set; }
    }

    public class LiveChatMembershipItemRenderer : LiveChatTextMessageRenderer
    {

    }

    public class LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer : LiveChatTextMessageRenderer
    {

    }

    public class LiveChatTextMessageRenderer
    {
        public LiveChatTextMessageRenderer()
        {
            Message = new Message();
            AuthorName = new AuthorName();
            AuthorPhoto = new AuthorPhoto();
            AuthorBadges = new List<AuthorBadge>();
            ContextMenuEndpoint = new ContextMenuEndpoint();
            ContextMenuAccessibility = new ContextMenuAccessibility();
        }

        [JsonProperty("message")]
        public Message Message { get; set; }

        [JsonProperty("authorName")]
        public AuthorName AuthorName { get; set; }

        [JsonProperty("authorPhoto")]
        public AuthorPhoto AuthorPhoto { get; set; }

        [JsonProperty("contextMenuEndpoint")]
        public ContextMenuEndpoint ContextMenuEndpoint { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("timestampUsec")]
        public long TimestampUsec { get; set; }

        [JsonProperty("authorExternalChannelId")]
        public string AuthorExternalChannelId { get; set; }

        [JsonProperty("contextMenuAccessibility")]
        public ContextMenuAccessibility ContextMenuAccessibility { get; set; }

        [JsonProperty("authorBadges")]
        public List<AuthorBadge> AuthorBadges { get; set; }
    }

    public class Item
    {
        public Item()
        {
            LiveChatTextMessageRenderer = new LiveChatTextMessageRenderer();
            LiveChatPaidMessageRenderer = new LiveChatPaidMessageRenderer();
            LiveChatPaidStickerRenderer = new LiveChatPaidStickerRenderer();
            LiveChatMembershipItemRenderer = new LiveChatMembershipItemRenderer();
            LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer = new LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer();
        }

        [JsonProperty("liveChatTextMessageRenderer")]
        public LiveChatTextMessageRenderer LiveChatTextMessageRenderer { get; set; }

        [JsonProperty("liveChatPaidMessageRenderer")]
        public LiveChatPaidMessageRenderer LiveChatPaidMessageRenderer { get; set; }

        [JsonProperty("liveChatPaidStickerRenderer")]
        public LiveChatPaidStickerRenderer LiveChatPaidStickerRenderer { get; set; }

        [JsonProperty("liveChatMembershipItemRenderer")]
        public LiveChatMembershipItemRenderer LiveChatMembershipItemRenderer { get; set; }

        [JsonProperty("liveChatSponsorshipsGiftPurchaseAnnouncementRenderer")]
        public LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer { get; set; }

        public bool IsPaidMessage
        {
            get
            {
                return !string.IsNullOrEmpty(LiveChatPaidMessageRenderer.PurchaseAmountText.SimpleText) ||
                    !string.IsNullOrEmpty(LiveChatPaidStickerRenderer.PurchaseAmountText.SimpleText);
            }
        }
    }

    public class AddChatItemAction
    {
        public AddChatItemAction()
        {
            Item = new Item();
        }

        [JsonProperty("item")]
        public Item Item { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }

    public class CommentData
    {
        public CommentData()
        {
            AddChatItemAction = new AddChatItemAction();
        }

        [JsonProperty("addChatItemAction")]
        public AddChatItemAction AddChatItemAction { get; set; }

        public override string ToString()
        {
            string ret = string.Format("{0}:{1}", AddChatItemAction.Item.LiveChatTextMessageRenderer.AuthorName.SimpleText, AddChatItemAction.Item.LiveChatTextMessageRenderer.Message.SimpleText);

            if (!string.IsNullOrEmpty(AddChatItemAction.Item.LiveChatPaidMessageRenderer.PurchaseAmountText.SimpleText))
            {
                ret += string.Format(" {0}", AddChatItemAction.Item.LiveChatPaidMessageRenderer.PurchaseAmountText.SimpleText);
            }

            return ret;
        }
    }
}
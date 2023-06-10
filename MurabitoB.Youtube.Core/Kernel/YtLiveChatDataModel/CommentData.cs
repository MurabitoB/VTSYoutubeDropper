﻿using KomeTube.Emums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KomeTube.Kernel.YtLiveChatDataModel
{
    public class Run 
    {
        public CommentDetailType type { get; set; }

        public string content { get; set; }
    }

    public class Message
    {
        public Message()
        {
            runs = new List<Run>();
        }

        public List<Run> runs { get; set; }
        public string simpleText { get; set; }
    }

    public class AuthorName
    {
        public string simpleText { get; set; }
    }

    public class AuthorBadge
    {
        public String tooltip { get; set; }
    }

    public class Thumbnail
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class AuthorPhoto
    {
        public AuthorPhoto()
        {
            this.thumbnails = new List<Thumbnail>();
        }

        public List<Thumbnail> thumbnails { get; set; }
    }

    public class WebCommandMetadata
    {
        public bool ignoreNavigation { get; set; }
    }

    public class CommandMetadata
    {
        public CommandMetadata()
        {
            this.webCommandMetadata = new WebCommandMetadata();
        }

        public WebCommandMetadata webCommandMetadata { get; set; }
    }

    public class LiveChatItemContextMenuEndpoint
    {
        public string parameters { get; set; }
    }

    public class ContextMenuEndpoint
    {
        public ContextMenuEndpoint()
        {
            this.commandMetadata = new CommandMetadata();
            this.liveChatItemContextMenuEndpoint = new LiveChatItemContextMenuEndpoint();
        }

        public string clickTrackingParams { get; set; }
        public CommandMetadata commandMetadata { get; set; }
        public LiveChatItemContextMenuEndpoint liveChatItemContextMenuEndpoint { get; set; }
    }

    public class AccessibilityData
    {
        public string label { get; set; }
    }

    public class PurchaseAmountText
    {
        public string simpleText { get; set; }
    }

    public class ContextMenuAccessibility
    {
        public ContextMenuAccessibility()
        {
            this.accessibilityData = new AccessibilityData();
        }

        public AccessibilityData accessibilityData { get; set; }
    }

    public class LiveChatPaidMessageRenderer : LiveChatTextMessageRenderer
    {
        public LiveChatPaidMessageRenderer()
        {
            this.purchaseAmountText = new PurchaseAmountText();
        }

        public PurchaseAmountText purchaseAmountText { get; set; }
        public long headerBackgroundColor { get; set; }
        public long headerTextColor { get; set; }
        public long bodyBackgroundColor { get; set; }
        public long bodyTextColor { get; set; }
        public long authorNameTextColor { get; set; }
        public long timestampColor { get; set; }
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
            this.message = new Message();
            this.authorName = new AuthorName();
            this.authorPhoto = new AuthorPhoto();
            this.authorBadges = new List<AuthorBadge>();
            this.contextMenuEndpoint = new ContextMenuEndpoint();
            this.contextMenuAccessibility = new ContextMenuAccessibility();
        }

        public Message message { get; set; }
        public AuthorName authorName { get; set; }
        public AuthorPhoto authorPhoto { get; set; }
        public ContextMenuEndpoint contextMenuEndpoint { get; set; }
        public string id { get; set; }
        public long timestampUsec { get; set; }
        public string authorExternalChannelId { get; set; }
        public ContextMenuAccessibility contextMenuAccessibility { get; set; }
        public List<AuthorBadge> authorBadges { get; set; }
    }

    public class Item
    {
        public Item()
        {
            this.liveChatTextMessageRenderer = new LiveChatTextMessageRenderer();
            this.liveChatPaidMessageRenderer = new LiveChatPaidMessageRenderer();
            this.liveChatMembershipItemRenderer = new LiveChatMembershipItemRenderer();
            this.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer = new LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer();
        }

        public LiveChatTextMessageRenderer liveChatTextMessageRenderer { get; set; }
        public LiveChatPaidMessageRenderer liveChatPaidMessageRenderer { get; set; }
        public LiveChatMembershipItemRenderer liveChatMembershipItemRenderer { get; set; }
        
        public LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer liveChatSponsorshipsGiftPurchaseAnnouncementRenderer { get; set; }

        public bool IsPaidMessage
        {
            get
            {
                if (liveChatPaidMessageRenderer.purchaseAmountText.simpleText != null
                    && liveChatPaidMessageRenderer.purchaseAmountText.simpleText != "")
                {
                    return true;
                }
                return false;
            }
        }
    }

    public class AddChatItemAction
    {
        public AddChatItemAction()
        {
            this.item = new Item();
        }

        public Item item { get; set; }
        public string clientId { get; set; }
    }

    public class CommentData
    {
        public CommentData()
        {
            this.addChatItemAction = new AddChatItemAction();
        }

        public AddChatItemAction addChatItemAction { get; set; }

        public override string ToString()
        {
            String ret = String.Format("{0}:{1}", this.addChatItemAction.item.liveChatTextMessageRenderer.authorName.simpleText, this.addChatItemAction.item.liveChatTextMessageRenderer.message.simpleText);
            if (this.addChatItemAction.item.liveChatPaidMessageRenderer.purchaseAmountText.simpleText != "")
            {
                ret += String.Format(" {0}", this.addChatItemAction.item.liveChatPaidMessageRenderer.purchaseAmountText.simpleText);
            }
            return ret;
        }
    }
}
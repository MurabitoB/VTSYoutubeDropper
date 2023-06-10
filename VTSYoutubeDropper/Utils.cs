using System;
using System.Collections.Generic;
using System.Linq;
using KomeTube.Emums;
using KomeTube.Kernel.YtLiveChatDataModel;

namespace VTSYoutubeDropper
{
    public class Utils
    {
        public static IEnumerable<string> FilterEmojis(
            IEnumerable<CommentData> data, 
            bool isPaidOnly = false, 
            bool isMemberOnly = false,
            int emotesPerMessage = Int32.MaxValue
            )
        {
            var normalEmojis = data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatTextMessageRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                .Select(
                renderer => renderer?.message?.runs
                    ?.Where(run => run.type == CommentDetailType.emoji)
                    .Take(emotesPerMessage)
                    .Select(x => x.content));
            
            var paidEmojis = data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatPaidMessageRenderer )
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                .Select(
                    renderer => renderer?.message?.runs
                        ?.Where(run => run.type == CommentDetailType.emoji)
                        .Take(emotesPerMessage)
                        .Select(x => x.content));
            
            var memberShip =  data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatMembershipItemRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                .Select(
                    renderer => renderer?.message?.runs
                        ?.Where(run => run.type == CommentDetailType.emoji)
                        .Take(emotesPerMessage)
                        .Select(x => x.content));

            var gift =  data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                .Select(
                    renderer => renderer?.message?.runs
                        ?.Where(run => run.type == CommentDetailType.emoji)
                        .Take(emotesPerMessage)
                        .Select(x => x.content));
            
            if(isPaidOnly)
                return paidEmojis.SelectMany(x => x)
                    .Concat(memberShip.SelectMany(x => x))
                    .Concat(gift.SelectMany(x => x))
                    .Where(x => !x.EndsWith(".svg"))
                    ;

            return normalEmojis.SelectMany(x => x)
                .Concat(paidEmojis.SelectMany(x => x))
                .Concat(memberShip.SelectMany(x => x))
                .Concat(gift.SelectMany(x => x))
                .Where(x => !x.EndsWith(".svg"))
                ;
        }
        
        public static IEnumerable<string> FilterThumbnails(
            IEnumerable<CommentData> data, 
            bool isPaidOnly = false, 
            bool isMemberOnly = false
        )
        {

            var normalEmojis = data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatTextMessageRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                renderer => renderer?.authorPhoto?.thumbnails
                    ?.Select(x => x.url).LastOrDefault());
            
            
            var paidEmojis = data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatPaidMessageRenderer  ??　comment?.addChatItemAction?.item?.liveChatMembershipItemRenderer as LiveChatTextMessageRenderer ?? comment?.addChatItemAction?.item?.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                renderer => renderer?.authorPhoto?.thumbnails
                    ?.Select(x => x.url).LastOrDefault());
            
            var memberShip = data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatMembershipItemRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                    renderer => renderer?.authorPhoto?.thumbnails
                        ?.Select(x => x.url).LastOrDefault());
            
            var gift = data
                .Select(comment => comment?.addChatItemAction?.item?.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.authorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                    renderer => renderer?.authorPhoto?.thumbnails
                        ?.Select(x => x.url).LastOrDefault());
            
            
            if(isPaidOnly)
                return paidEmojis
                    .Concat(memberShip)
                    .Concat(gift)
                    .Where(x => !string.IsNullOrEmpty(x));
            
            
            return normalEmojis
                .Concat(paidEmojis)
                .Concat(memberShip)
                .Concat(gift)
                .Where(x => !string.IsNullOrEmpty(x));
        }
    }
}
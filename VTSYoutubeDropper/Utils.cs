using System.Collections.Generic;
using System.Linq;
using KomeTube.Enums;
using KomeTube.Kernel.YtLiveChatDataModel;

namespace VTSYoutubeDropper
{
    public class Utils
    {
        public static IEnumerable<string> FilterEmojis(
            IEnumerable<CommentData> data,
            bool isPaidOnly = false,
            bool isMemberOnly = false,
            int emotesPerMessage = int.MaxValue
            )
        {
            var normalEmojis = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatTextMessageRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                .Select(
                    renderer => renderer?.Message?.Runs
                        ?.Where(run => run.Type == CommentDetailType.Emoji)
                        .Take(emotesPerMessage)
                        .Select(x => x.Content));

            var paidEmojis = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatPaidMessageRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                .Select(
                    renderer => renderer?.Message?.Runs
                        ?.Where(run => run.Type == CommentDetailType.Emoji)
                        .Take(emotesPerMessage)
                        .Select(x => x.Content));

            var memberShip = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatMembershipItemRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                .Select(
                    renderer => renderer?.Message?.Runs
                        ?.Where(run => run.Type == CommentDetailType.Emoji)
                        .Take(emotesPerMessage)
                        .Select(x => x.Content));

            var gift = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                .Select(
                    renderer => renderer?.Message?.Runs
                        ?.Where(run => run.Type == CommentDetailType.Emoji)
                        .Take(emotesPerMessage)
                        .Select(x => x.Content));

            if (isPaidOnly)
                return paidEmojis.SelectMany(x => x)
                    .Concat(memberShip.SelectMany(x => x))
                    .Concat(gift.SelectMany(x => x))
                    .Where(x => !x.EndsWith(".svg"));

            return normalEmojis.SelectMany(x => x)
                .Concat(paidEmojis.SelectMany(x => x))
                .Concat(memberShip.SelectMany(x => x))
                .Concat(gift.SelectMany(x => x))
                .Where(x => !x.EndsWith(".svg"));
        }

        public static IEnumerable<string> FilterThumbnails(
            IEnumerable<CommentData> data,
            bool isPaidOnly = false,
            bool isMemberOnly = false)
        {

            var normalEmojis = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatTextMessageRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                    renderer => renderer?.AuthorPhoto?.Thumbnails
                        ?.Select(x => x.Url).LastOrDefault());

            var paidEmojis = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatPaidMessageRenderer ?? comment?.AddChatItemAction?.Item?.LiveChatMembershipItemRenderer as LiveChatTextMessageRenderer ?? comment?.AddChatItemAction?.Item?.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                    renderer => renderer?.AuthorPhoto?.Thumbnails
                        ?.Select(x => x.Url).LastOrDefault());

            var memberShip = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatMembershipItemRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                    renderer => renderer?.AuthorPhoto?.Thumbnails
                        ?.Select(x => x.Url).LastOrDefault());

            var gift = data
                .Select(comment => comment?.AddChatItemAction?.Item?.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer)
                // if member only, filter out non-member comments
                .Where(renderer => !isMemberOnly || renderer?.AuthorBadges?.Count > 0)
                // youtube thumbnails resolution is order by size, the last one has the highest resolution
                .Select(
                    renderer => renderer?.AuthorPhoto?.Thumbnails
                        ?.Select(x => x.Url).LastOrDefault());

            if (isPaidOnly)
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
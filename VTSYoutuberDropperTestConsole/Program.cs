using System.Linq;
using KomeTube.Emums;
using KomeTube.Kernel;

namespace VTSYoutuberDropperTestConsole
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var loader = new CommentLoader();
            loader.OnCommentsReceive += (sender, comments) =>
            {
                foreach (var comment in comments)
                {
                    var message = comment?.addChatItemAction?.item?.liveChatTextMessageRenderer?.message?.runs
                        ?.Where(run => run.type == CommentDetailType.emoji).Select(x => x.content);
                    foreach (var s in message)
                    {
                        System.Console.WriteLine(s);
                    }
                }
            };
            
            loader.Start("https://www.youtube.com/watch?v=bUVK1wysJpk");

            while (true)
            {
                // do nothing
                // Sleep
                System.Threading.Thread.Sleep(1000);
            }
            
        }
    }
}
using System;
using System.Linq;
using System.Threading;
using KomeTube.Emums;
using KomeTube.Kernel;
using Newtonsoft.Json;

namespace DebugConsole
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var loader = new CommentLoader();
            loader.OnCommentsReceive += (x, d) =>
            {
                Console.WriteLine(JsonConvert.SerializeObject(
                    d.Select(
                        comment => comment?.addChatItemAction?.item?.liveChatTextMessageRenderer?.message?.runs?.Where(run => run.type == CommentDetailType.emoji))));
            };
            
            loader.Start("https://www.youtube.com/watch?v=8lsNMo80Vk4");
                
            while (true)
            {
                // Thread.Sleep(1000);
            }
        }
    }
}
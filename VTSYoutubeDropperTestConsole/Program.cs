using System.Linq;
using KomeTube.Enums;
using KomeTube.Kernel;
using VTSYoutubeDropper;

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
                    var thumbnails = Utils.FilterThumbnails(comments, false, false);
                    var emojis = Utils.FilterEmojis(comments, false, false);
                    foreach (var s in thumbnails)
                    {
                        System.Console.WriteLine(s);
                    }
                    foreach (var s in emojis)
                    {
                        System.Console.WriteLine(s);
                    }
                }
            };
            
            loader.Start("http://localhost:3000/watch?v=7TWmO0Y0lIU");

            while (true)
            {
                // do nothing
                // Sleep
                System.Threading.Thread.Sleep(1000);
            }
            
        }
    }
}
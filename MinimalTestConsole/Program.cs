using KomeTube.Kernel;
using KomeTube.Kernel.Utils;
using KomeTube.Kernel.YtLiveChatDataModel;
using System;

namespace MinimalTestConsole
{
    internal class Program
    {
        /// <summary>
        /// CommentLoader
        /// </summary>
        private static CommentLoader commentLoader;

        static void Main(string[] args)
        {
            if (args is null)
            {
                // Do nothing.
            }

            CustomInit(showLogInConsole: false);

            do
            {
                if (commentLoader.Status == CommentLoaderStatus.Null)
                {
                    Console.WriteLine("Press the ESC key to stop fetch comments.");
                    Console.WriteLine(string.Empty);

                    WebBrowserUtil.BrowserType? browserType = null;

                    string videoId = "",
                        url = $"https://www.youtube.com/watch?v={videoId}",
                        profileFolderName = "";

                    StartFetchComments(
                        url: url,
                        browserType: browserType,
                        profileFolderName: profileFolderName);
                }

                if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    StopFetchComments();

                    break;
                }
            }
            while (true);

            Pause();
        }

        /// <summary>
        /// Custom init
        /// </summary>
        /// <param name="showLogInConsole">Boolean, Determine to show IHttpClientFactory logs in the console, The default value is true</param>
        private static void CustomInit(bool showLogInConsole = true)
        {
            try
            {
                commentLoader = new CommentLoader(showLogInConsole: showLogInConsole);

                commentLoader.OnCommentsReceive += (sender, comments) =>
                {
                    foreach (CommentData comment in comments)
                    {
                        Console.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss}] {comment}");
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Start to fetch comments
        /// </summary>
        /// <param name="url">String, The YouTube video URL</param>
        /// <param name="browserType">WebBrowserUtil.BrowserType, The default value is null</param>
        /// <param name="profileFolderName">String, The profile folder name or path to the browser, The default value is empty</param>
        private static void StartFetchComments(
            string url,
            WebBrowserUtil.BrowserType? browserType = null,
            string profileFolderName = "")
        {
            try
            {
                Console.WriteLine("Starting fetch comments...");
                Console.WriteLine(string.Empty);

                commentLoader.Start(
                    url: url,
                    browserType: browserType,
                    profileFolderName: profileFolderName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                StopFetchComments();
            }
        }

        /// <summary>
        /// Stop to fetch comments
        /// </summary>
        private static void StopFetchComments()
        {
            try
            {
                commentLoader?.Stop();

                Console.WriteLine("Stopped fetch comments.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Pause
        /// </summary>
        private static void Pause()
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
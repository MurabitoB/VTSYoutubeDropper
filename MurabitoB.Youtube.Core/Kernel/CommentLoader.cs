using KomeTube.Enums;
using KomeTube.Kernel.Models;
using KomeTube.Kernel.Utils;
using KomeTube.Kernel.YtLiveChatDataModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KomeTube.Kernel
{
    public class CommentLoader
    {
        private readonly static string BaseUrl = "www.youtube.com";

        //private readonly static string BaseUrl = "localhost:3000";

        #region Private Member

        private string _videoUrl;
        private InnerTubeContextData _innerContextData;
        private readonly object _lockContinuation;
        private readonly bool _showLogInConsole = true;

        private Task _mainTask;
        private CancellationTokenSource _mainTaskCancelTS;

        #endregion Private Member

        #region Constructor

        /// <summary>
        /// CommentLoader
        /// </summary>
        /// <param name="showLogInConsole">Boolean, Determine to show IHttpClientFactory logs in the console, The default value is true</param>
        public CommentLoader(bool showLogInConsole = true)
        {
            _showLogInConsole = showLogInConsole;

            // Avoid exceptions.
            SQLitePCL.Batteries.Init();

            _lockContinuation = new object();
        }

        #endregion Constructor

        #region Public Member

        /// <summary>
        /// 使用者輸入的直播影片網址
        /// </summary>
        public string VideoUrl
        {
            get
            {
                return _videoUrl;
            }

            set
            {
                _videoUrl = value;
                // 生成標題。
            }
        }

        /// <summary>
        /// 當前的 continuation 資料，用來取得下次留言列表
        /// </summary>
        public string CurrentContinuation
        {
            get
            {
                lock (_lockContinuation)
                {
                    return _innerContextData.Continuation;
                }
            }

            set
            {
                lock (_lockContinuation)
                {
                    _innerContextData.Continuation = value;
                }
            }
        }

        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// CommentLoader 當前執行狀態
        /// </summary>
        public CommentLoaderStatus Status { get; private set; }

        #endregion Public Member

        #region Public Method

        /// <summary>
        /// 開始讀取留言
        /// <para>請監聽 OnCommentsReceive 事件取得留言列表</para>
        /// </summary>
        /// <param name="url">YouTube 直播影片位址</param>
        /// <param name="browserType">WebBrowserUtil.BrowserType, The default value is null</param>
        /// <param name="profileFolderName">String, The profile folder name or path, The default value is empty</param>
        public void Start(string url,
            WebBrowserUtil.BrowserType? browserType = null,
            string profileFolderName = "")
        {
            if (_mainTask != null && !_mainTask.IsCompleted)
            {
                // 若任務仍在執行則不再重複啟動。
                return;
            }

            _mainTaskCancelTS = new CancellationTokenSource();
            _mainTask = Task.Factory.StartNew(() => StartGetComments(url, browserType, profileFolderName), _mainTaskCancelTS.Token);
            _mainTask.ContinueWith(StartGetCommentsCompleted, _mainTaskCancelTS.Token);

            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 停止取得留言
        /// </summary>
        public void Stop()
        {
            if (_mainTaskCancelTS != null)
            {
                // 停止取得留言。
                _mainTaskCancelTS.Cancel();

                RaiseStatusChanged(CommentLoaderStatus.StopRequested);
            }
        }

        #endregion Public Method

        #region Private Method

        /// <summary>
        /// 從影片位址解析 vid 後取得聊天室位址
        /// </summary>
        /// <param name="videoUrl">影片位址</param>
        /// <returns>回傳聊天室位址，若失敗則發出 CanNotGetLiveChatUrl Error 事件，並回傳空字串</returns>
        private string GetLiveChatRoomUrl(string videoUrl)
        {
            string baseUrl = $"{BaseUrl}/watch?",
                vid = string.Empty,
                ret,
                urlParamStr = videoUrl.Substring(videoUrl.IndexOf(baseUrl) + baseUrl.Length);

            string[] urlParamArr = urlParamStr.Split('&');

            // 取得 vid。
            foreach (string param in urlParamArr)
            {
                if (param.IndexOf("v=") == 0)
                {
                    vid = param.Substring(2);

                    break;
                }
            }

            if (string.IsNullOrEmpty(vid))
            {
                Debug.WriteLine(string.Format("[GetLiveChatRoomUrl] 無法取得聊天室位址。 URL={0}", videoUrl));

                RaiseError(CommentLoaderErrorCode.CanNotGetLiveChatUrl, videoUrl);

                return string.Empty;
            }
            else
            {
                ret = string.Format($"https://{BaseUrl}/live_chat?v={{0}}&is_popout=1", vid);
            }

            return ret;
        }

        /// <summary>
        /// 取得 Youtube API 'get_live_chat' 的位址
        /// </summary>
        /// <param name="apiKey">YtCfg 資料中 INNERTUBE_API_KEY 參數。此參數應從 ParseLiveChatHtml 或 GetComment 方法取得</param>
        /// <returns>回傳 Youtube API 'get_live_chat' 的位址</returns>
        private string GetLiveChatUrl(string apiKey)
        {
            return $@"https://{BaseUrl}/youtubei/v1/live_chat/get_live_chat?key={apiKey}";
        }

        /// <summary>
        /// 開始取得留言，此方法將會進入長時間迴圈，若要停止請使用 _mainTaskCancelTS 發出 cancel 請求
        /// </summary>
        /// <param name="url">直播影片位址</param>
        /// <param name="browserType">WebBrowserUtil.BrowserType, The default value is null</param>
        /// <param name="profileFolderName">String, The profile folder name or path, The default value is empty</param>
        private void StartGetComments(
            string url,
            WebBrowserUtil.BrowserType? browserType = null,
            string profileFolderName = "")
        {
            RaiseStatusChanged(CommentLoaderStatus.Started);

            string continuation = "";

            // 取得聊天室位址。
            string liveChatRoomUrl = GetLiveChatRoomUrl(url);

            if (string.IsNullOrEmpty(liveChatRoomUrl))
            {
                Debug.WriteLine(string.Format("[StartGetComments] GetLiveChatRoomUrl 無法取得 html 內容。"));

                return;
            }

            #region Obtaining and setting cookies and YTConfigData

            string cookies = string.Empty;

            if (browserType != null)
            {
                cookies = CustomFunction.GetCookies(browserType.Value, profileFolderName);

                CookieConfigData cookieConfigData = HttpClientUtil.GetCookieConfigData(_showLogInConsole);

                if (cookieConfigData.UseCookies)
                {
                    CookieContainer cookieContainer = cookieConfigData.CookieContainer;

                    #region First remove cookies from the YouTube site from CookieContainer

                    CookieCollection cookieCollection = cookieContainer
                        .GetCookies(new Uri("https://www.youtube.com"));

                    if (cookieCollection.Count > 0)
                    {
                        foreach (Cookie cookie in cookieCollection)
                        {
                            cookie.Expired = true;
                        }
                    }

                    #endregion

                    #region Add cookies from the YouTube website to CookieContainer

                    List<WebBrowserUtil.CookieData> cookiesList = CustomFunction
                        .GetCookiesList(browserType.Value, profileFolderName);

                    foreach (WebBrowserUtil.CookieData cookieData in cookiesList)
                    {
                        cookieContainer.Add(new Cookie()
                        {
                            Name = cookieData.Name,
                            Value = cookieData.Value,
                            Domain = cookieData.HostKey
                        });
                    }

                    #endregion
                }
            }

            #endregion

            ContinuationData continuationData = new ContinuationData();

            // 取得 continuation 和第一次訪問的留言列表。
            List<CommentData> firstCommentList = ParseLiveChatHtml(
                liveChatRoomUrl,
                ref continuation,
                ref continuationData,
                cookies,
                _innerContextData);

            if (string.IsNullOrEmpty(continuation))
            {
                Debug.WriteLine(string.Format("[StartGetComments] ParseLiveChatHtml 無法取得 continuation 參數。"));

                return;
            }

            VideoUrl = url;

            RaiseCommentsReceive(firstCommentList);

            // 持續取得留言。
            while (!_mainTaskCancelTS.IsCancellationRequested)
            {
                List<CommentData> comments = GetComments(
                    ref continuation,
                    ref continuationData,
                    cookies,
                    _innerContextData);

                if (comments != null && comments.Count > 0)
                {
                    RaiseCommentsReceive(comments);
                }

                if (string.IsNullOrEmpty(continuation))
                {
                    Debug.WriteLine(string.Format("[StartGetComments] GetComments 無法取得 continuation 參數。"));

                    return;
                }

                // When the browserType is not null, the timeoutMs value is respected to reduce
                // the chance of being judged as malicious or abusive.
                int millisecondsTimeout = browserType == null ? 1000 : continuationData.TimeoutMs;

                SpinWait.SpinUntil(() => false, millisecondsTimeout);
            }
        }

        /// <summary>
        /// 取得留言的 Task 結束 (StartGetComments 方法結束)
        /// </summary>
        /// <param name="sender">已完成的 Task</param>
        /// <param name="obj">object</param>
        private void StartGetCommentsCompleted(Task sender, object obj)
        {
            if (sender.IsFaulted)
            {
                // 取得留言時發生其他 exception 造成 Task 結束。
                RaiseError(CommentLoaderErrorCode.GetCommentsError, sender.Exception.Message);
            }

            RaiseStatusChanged(CommentLoaderStatus.Completed);
        }

        /// <summary>
        /// 取得第一次訪問的 cookie 資料，並在 html 中取出 ytcfg 資料與 window["ytInitialData"] 後方的 json code，並解析出 continuation
        /// </summary>
        /// <param name="liveChatUrl">聊天室位址</param>
        /// <param name="continuation">Continuation 參數</param>
        /// <param name="continuationData">ContinuationData</param>
        /// <param name="cookies">String, The cookies string, The default value is empty</param>
        /// <param name="innerTubeContextData">InnerTubeContextData, The default value is null</param>
        /// <returns>回傳 continuation 參數值</returns>
        private List<CommentData> ParseLiveChatHtml(
            string liveChatUrl,
            ref string continuation,
            ref ContinuationData continuationData,
            string cookies = "",
            InnerTubeContextData innerTubeContextData = null)
        {
            string htmlContent = string.Empty;

            List<CommentData> initComments;

            RaiseStatusChanged(CommentLoaderStatus.GetLiveChatHtml);

            // 取得 HTML 內容。
            using (HttpClient httpClient = HttpClientUtil.CreateClient(_showLogInConsole))
            {
                try
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, liveChatUrl);

                    YTAuthUtil.SetHttpRequestMessageHeader(
                        httpRequestMessage,
                        cookies,
                        innerTubeContextData,
                        _showLogInConsole);

                    HttpResponseMessage httpResponseMessage = httpClient
                        .SendAsync(httpRequestMessage)
                        .GetAwaiter()
                        .GetResult();

                    htmlContent = httpResponseMessage.Content
                        .ReadAsStringAsync()
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(string.Format("[ParseLiveChatHtml] 無法取得聊天室 HTML 內容。 Excetion: {0}", e.Message));

                    RaiseError(CommentLoaderErrorCode.CanNotGetLiveChatHtml, e.Message);

                    return null;
                }
            }

            // 解析 YtCfg。
            RaiseStatusChanged(CommentLoaderStatus.ParseYtCfgData);

            string strCfg = ParseYtCfg(htmlContent);

            if (strCfg == null)
            {
                Debug.WriteLine(string.Format("[ParseLiveChatHtml] 無法解析 YtCfg。 HTML content: {0}", htmlContent));

                RaiseError(CommentLoaderErrorCode.CanNotParseYtCfg, htmlContent);

                return null;
            }

            // 解析 inner context data。
            _innerContextData = ParseInnerContextData(strCfg);

            // 解析 HTML。
            RaiseStatusChanged(CommentLoaderStatus.ParseLiveChatHtml);

            Match match = Regex.Match(htmlContent, "window\\[\"ytInitialData\"\\] = ({.+});\\s*</script>", RegexOptions.Singleline);

            if (!match.Success)
            {
                Debug.WriteLine(string.Format("[ParseLiveChatHtml] 無法解析HTML。 HTML content: {0}", htmlContent));

                RaiseError(CommentLoaderErrorCode.CanNotParseLiveChatHtml, htmlContent);

                return null;
            }

            // 解析 json data。
            string ytInitialData = match.Groups[1].Value;

            dynamic jsonData;

            try
            {
                jsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(ytInitialData);

                // 解析 continuation 供下次取得留言使用。
                ParseContinuation(
                    jsonData,
                    ref continuation,
                    ref continuationData,
                    isFirstTime: true);

                dynamic actions = jsonData["contents"]["liveChatRenderer"]["actions"];

                initComments = ParseComment(actions);
            }
            catch (Exception e)
            {
                Debug.WriteLine(string.Format("[ParseLiveChatHtml] 無法解析 json data: {0}", e.Message));

                RaiseError(CommentLoaderErrorCode.CanNotParseLiveChatHtml, ytInitialData);

                return null;
            }

            return initComments;
        }

        /// <summary>
        /// 從第一次訪問的 html 內容解析出 ytcfg 字串
        /// </summary>
        /// <param name="liveChatHtml">字串，HTML 內容</param>
        /// <returns>字串，ytcfg 字串</returns>
        private string ParseYtCfg(string liveChatHtml)
        {
            Match match = Regex.Match(liveChatHtml, "ytcfg\\.set\\(({.+?})\\);", RegexOptions.Singleline);

            if (!match.Success)
            {
                return null;
            }

            try
            {
                string ytCfg = match.Groups[1].Value;

                dynamic dynamicObj = JsonConvert.DeserializeObject(ytCfg);

                MatchCollection matches = Regex.Matches(liveChatHtml, "ytcfg\\.set\\(\"([^\"]+)\",\\s*(.+?)\\);?\\r?\n", RegexOptions.Singleline);

                foreach (Match singleMatch in matches)
                {
                    string key = singleMatch.Groups[1].Value,
                        value = singleMatch.Groups[2].Value,
                        jsonString = $"{{\"{key}\":\"{value}\"}}";

                    object obj = JsonConvert.DeserializeObject(jsonString);

                    dynamicObj.Merge(obj);
                }

                return dynamicObj.ToString(Formatting.None);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format($"[ParseYtCfg] 無法解析 YtCfg 資料：{ex.Message}"));

                return null;
            }
        }

        /// <summary>
        /// 從 YtCfg 字串解析 inner context 資料供取得留言使用
        /// </summary>
        /// <param name="strCfg">字串，YtCfg 字串</param>
        /// <returns>InnerTubeContextData</returns>
        private InnerTubeContextData ParseInnerContextData(string strCfg)
        {
            InnerTubeContextData ret = new InnerTubeContextData();

            dynamic jsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(strCfg);

            ret.InnetrubeApiKey = Convert.ToString(JsonHelper.TryGetValue(jsonData, "INNERTUBE_API_KEY", string.Empty));
            ret.IDToken = Convert.ToString(JsonHelper.TryGetValue(jsonData, "ID_TOKEN", string.Empty));
            ret.SessionIndex = Convert.ToString(JsonHelper.TryGetValue(jsonData, "SESSION_INDEX", string.Empty));
            ret.InnertubeContextClientName = Convert.ToInt32(JsonHelper.TryGetValue(jsonData, "INNERTUBE_CONTEXT_CLIENT_NAME", 0));
            ret.InnertubeContextClientVersion = Convert.ToString(JsonHelper.TryGetValue(jsonData, "INNERTUBE_CONTEXT_CLIENT_VERSION", string.Empty));
            ret.InnertubeClientVersion = Convert.ToString(JsonHelper.TryGetValue(jsonData, "INNERTUBE_CLIENT_VERSION", string.Empty));

            #region DataSyncID

            // Source: https://github.com/xenova/chat-downloader/blob/master/chat_downloader/sites/youtube.py#L1629
            // 
            // Copyright(c) 2021, xenova
            //
            // MIT License
            // https://github.com/xenova/chat-downloader/blob/master/LICENSE

            bool useDelegatedSessionID = false;

            ret.DataSyncID = Convert.ToString(JsonHelper.TryGetValue(jsonData, "DATASYNC_ID", string.Empty));

            string[] arrayDataSyncID = ret.DataSyncID.Split("||".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (arrayDataSyncID.Length >= 2 && !string.IsNullOrEmpty(arrayDataSyncID[1]))
            {
                ret.DataSyncID = arrayDataSyncID[0];
            }
            else
            {
                useDelegatedSessionID = true;
            }

            ret.DelegatedSessionID = Convert.ToString(JsonHelper.TryGetValue(jsonData, "DELEGATED_SESSION_ID", string.Empty));

            if (useDelegatedSessionID)
            {
                ret.DataSyncID = ret.DelegatedSessionID;
            }

            #endregion 

            ret.Context.ClickTracking.ClickTrackingParams = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.clickTracking.clickTrackingParams", string.Empty));

            ret.Context.Request.UseSsl = Convert.ToBoolean(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.request.useSsl", false));

            ret.Context.Client.BrowserName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.browserName", string.Empty));
            ret.Context.Client.BrowserVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.browserVersion", string.Empty));
            ret.Context.Client.ClientFormFactor = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientFormFactor", string.Empty));
            ret.Context.Client.ClientName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientName", string.Empty));
            ret.Context.Client.ClientVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientVersion", string.Empty));
            ret.Context.Client.DeviceMake = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.deviceMake", string.Empty));
            ret.Context.Client.DeviceModel = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.deviceModel", string.Empty));
            ret.Context.Client.Gl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.gl", string.Empty));
            ret.Context.Client.Hl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.hl", string.Empty));
            ret.Context.Client.OriginalUrl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.originalUrl", string.Empty));
            ret.Context.Client.OsName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.osName", string.Empty));
            ret.Context.Client.OsVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.osVersion", string.Empty));
            ret.Context.Client.Platform = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.platform", string.Empty));
            ret.Context.Client.RemoteHost = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.remoteHost", string.Empty));
            ret.Context.Client.UserAgent = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.userAgent", string.Empty));
            ret.Context.Client.VisitorData = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.visitorData", string.Empty));

            return ret;
        }

        /// <summary>
        /// 利用 Youtube API 'get_live_chat' 取得聊天室留言，並解析 continuation 參數供下次取得留言使用
        /// </summary>
        /// <param name="continuation">Continuation 參數</param>
        /// <param name="continuationData">ContinuationData</param>
        /// <param name="cookies">String, The cookies string, The default value is empty</param>
        /// <param name="innerTubeContextData">InnerTubeContextData, The default value is null</param>
        /// <returns>成功時回傳留言資料，失敗則回傳 null。</returns>
        private List<CommentData> GetComments(
            ref string continuation,
            ref ContinuationData continuationData,
            string cookies = "",
            InnerTubeContextData innerTubeContextData = null)
        {
            if (continuation == null || continuation == "")
            {
                RaiseError(CommentLoaderErrorCode.GetCommentsError, new Exception("continuation 參數錯誤"));

                return null;
            }

            RaiseStatusChanged(CommentLoaderStatus.GetComments);

            string chatUrl = GetLiveChatUrl(_innerContextData.InnetrubeApiKey);

            List<CommentData> ret = null;

            string resp;

            using (HttpClient httpClient = HttpClientUtil.CreateClient(_showLogInConsole))
            {
                try
                {
                    HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, chatUrl);

                    YTAuthUtil.SetHttpRequestMessageHeader(
                        httpRequestMessage,
                        cookies,
                        innerTubeContextData,
                        _showLogInConsole);

                    HttpContent dataContent = new StringContent(
                        _innerContextData.ToString(),
                        Encoding.UTF8,
                        "application/json");

                    httpRequestMessage.Content = dataContent;

                    // 取得聊天室留言。
                    HttpResponseMessage respMsg = httpClient.SendAsync(httpRequestMessage)
                        .GetAwaiter()
                        .GetResult();

                    resp = respMsg.Content.ReadAsStringAsync()
                        .GetAwaiter()
                        .GetResult();

                    dynamic jsonData = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(resp);

                    // 解析 continuation 供下次取得留言使用。
                    ParseContinuation(
                        jsonData,
                        ref continuation,
                        ref continuationData,
                        isFirstTime: false);

                    // 解析留言資料。
                    dynamic commentActions = jsonData["continuationContents"]["liveChatContinuation"]["actions"];

                    ret = ParseComment(commentActions);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(string.Format("[GetComments] 無法取得聊天室 HTML 內容。 Excetion: {0}", ex.Message));

                    RaiseError(CommentLoaderErrorCode.GetCommentsError, ex.Message);

                    return null;
                }
            }

            return ret;
        }

        /// <summary>
        /// Parse continuation data
        /// </summary>
        /// <param name="jsonData">Dynamic, The JSON data</param>
        /// <param name="continuation">String, The continuation string</param>
        /// <param name="continuationData">ContinuationData</param>
        /// <param name="isFirstTime">Boolean, Determine if this is the first execution, The default value is true</param>
        private void ParseContinuation(
            dynamic jsonData,
            ref string continuation,
            ref ContinuationData continuationData,
            bool isFirstTime = true)
        {
            try
            {
                dynamic data = isFirstTime ? (jsonData["contents"]["liveChatRenderer"]["continuations"][0]["invalidationContinuationData"] ??
                    jsonData["contents"]["liveChatRenderer"]["continuations"][0]["timedContinuationData"]) :
                    (jsonData["continuationContents"]["liveChatContinuation"]["continuations"][0]["invalidationContinuationData"] ??
                    jsonData["continuationContents"]["liveChatContinuation"]["continuations"][0]["timedContinuationData"]);

                continuation = Convert.ToString(JsonHelper.TryGetValue(data, "continuation", string.Empty));

                _innerContextData.Continuation = continuation;

                int timeoutMs = Convert.ToInt32(JsonHelper.TryGetValue(data, "timeoutMs", 0));

                continuationData.Continuation = continuation;
                continuationData.TimeoutMs = timeoutMs;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("[ParseContinuation] 無法解析 continuation 內容。 Excetion: {0}", ex.Message));

                RaiseError(CommentLoaderErrorCode.GetContinuationError, ex.Message);
            }
        }

        /// <summary>
        /// 解析留言資訊
        /// </summary>
        /// <param name="commentActions">json data.</param>
        /// <returns>成功則回傳留言列表，失敗則回傳 null</returns>
        private List<CommentData> ParseComment(dynamic commentActions)
        {
            if (commentActions == null)
            {
                return null;
            }

            List<CommentData> ret = new List<CommentData>();

            for (int i = 0; i < commentActions.Count; i++)
            {
                CommentData cmt = new CommentData();

                cmt.AddChatItemAction.ClientId = Convert.ToString(JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.clientId", string.Empty));

                dynamic txtMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatTextMessageRenderer", null);

                if (txtMsgRd != null)
                {
                    ParseTextMessage(cmt.AddChatItemAction.Item.LiveChatTextMessageRenderer, txtMsgRd);
                }
                else
                {
                    dynamic paidMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatPaidMessageRenderer", null);
                    dynamic paidStickerRd = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatPaidStickerRenderer", null);
                    dynamic membershipMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatMembershipItemRenderer", null);
                    dynamic gift = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer", null);

                    if (paidMsgRd != null)
                    {
                        ParsePaidMessage(cmt.AddChatItemAction.Item.LiveChatPaidMessageRenderer, paidMsgRd);
                    }
                    else if (paidStickerRd != null)
                    {
                        ParsePaidSticker(cmt.AddChatItemAction.Item.LiveChatPaidStickerRenderer, paidStickerRd);
                    }
                    else if (membershipMsgRd != null)
                    {
                        ParseMemberMilestoneMessage(cmt.AddChatItemAction.Item.LiveChatMembershipItemRenderer, membershipMsgRd);
                    }
                    else if (gift != null)
                    {
                        ParseGiftMessage(cmt.AddChatItemAction.Item.LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer, gift);
                    }
                    else
                    {
                        continue;
                    }
                }

                ret.Add(cmt);
            }

            return ret;
        }

        /// <summary>
        /// 解析付費留言資訊
        /// </summary>
        /// <param name="liveChatPaidMessageRenderer">付費留言</param>
        /// <param name="paidMsgRd">json data.</param>
        private void ParsePaidMessage(LiveChatPaidMessageRenderer liveChatPaidMessageRenderer, dynamic paidMsgRd)
        {
            // 解析留言內容。
            ParseTextMessage(liveChatPaidMessageRenderer, paidMsgRd);

            // 解析付費留言內容。
            liveChatPaidMessageRenderer.PurchaseAmountText.SimpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(paidMsgRd, "purchaseAmountText.simpleText", string.Empty));
            liveChatPaidMessageRenderer.HeaderBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "headerBackgroundColor", 0));
            liveChatPaidMessageRenderer.HeaderTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "headerTextColor", 0));
            liveChatPaidMessageRenderer.BodyBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "bodyBackgroundColor", 0));
            liveChatPaidMessageRenderer.BodyTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "bodyTextColor", 0));
            liveChatPaidMessageRenderer.AuthorNameTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "authorNameTextColor", 0));
            liveChatPaidMessageRenderer.TimestampColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "timestampColor", 0));
        }

        /// <summary>
        /// Parse paid sticker
        /// </summary>
        /// <param name="liveChatPaidStickerRenderer">LiveChatPaidStickerRenderer</param>
        /// <param name="paidStickerRd">Dynamic, The JSON data</param>
        private void ParsePaidSticker(LiveChatPaidStickerRenderer liveChatPaidStickerRenderer, dynamic paidStickerRd)
        {
            // Parse text message.
            ParseTextMessage(liveChatPaidStickerRenderer, paidStickerRd);

            // Parse paid sticker.
            liveChatPaidStickerRenderer.MoneyChipBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStickerRd, "moneyChipBackgroundColor", 0));
            liveChatPaidStickerRenderer.MoneyChipTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStickerRd, "moneyChipTextColor", 0));
            liveChatPaidStickerRenderer.PurchaseAmountText.SimpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(paidStickerRd, "purchaseAmountText.simpleText", string.Empty));
            liveChatPaidStickerRenderer.StickerDisplayWidth = Convert.ToInt32(JsonHelper.TryGetValueByXPath(paidStickerRd, "stickerDisplayWidth", 0));
            liveChatPaidStickerRenderer.StickerDisplayHeight = Convert.ToInt32(JsonHelper.TryGetValueByXPath(paidStickerRd, "stickerDisplayHeight", 0));
            liveChatPaidStickerRenderer.BackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStickerRd, "backgroundColor", 0));
            liveChatPaidStickerRenderer.AuthorNameTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidStickerRd, "authorNameTextColor", 0));
            liveChatPaidStickerRenderer.TrackingParams = Convert.ToString(JsonHelper.TryGetValueByXPath(paidStickerRd, "trackingParams", string.Empty));

            dynamic stickerObj = JsonHelper.TryGetValueByXPath(paidStickerRd, "sticker", null);

            if (stickerObj != null)
            {
                liveChatPaidStickerRenderer.Sticker.Thumbnails = ParseThumbnails(stickerObj);
                liveChatPaidStickerRenderer.Sticker.Accessibility.AccessibilityData.Label = Convert.ToString(JsonHelper.TryGetValueByXPath(stickerObj, "accessibility.accessibilityData.label", string.Empty));
                liveChatPaidStickerRenderer.Sticker.Accessibility.Label = Convert.ToString(JsonHelper.TryGetValueByXPath(stickerObj, "accessibility.label", string.Empty));

                liveChatPaidStickerRenderer.Message.Runs.Add(new Run()
                {
                    Type = CommentDetailType.Sticker,
                    Content = liveChatPaidStickerRenderer.Sticker.Accessibility.Label
                });
            }
        }

        /// <summary>
        /// 解析會員里程碑資訊
        /// </summary>
        /// <param name="liveChatMembershipItemRenderer">LiveChatMembershipItemRenderer</param>
        /// <param name="memberMileStoneRd">json data.</param>
        private void ParseMemberMilestoneMessage(LiveChatMembershipItemRenderer liveChatMembershipItemRenderer, dynamic memberMileStoneRd)
        {
            ParseTextMessage(liveChatMembershipItemRenderer, memberMileStoneRd);
        }

        /// <summary>
        /// 解析會員贈禮資訊
        /// </summary>
        /// <param name="liveChatPaidMessageRenderer">LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer</param>
        /// <param name="giftMsgRd">json data.</param>
        private void ParseGiftMessage(LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer liveChatPaidMessageRenderer, dynamic giftMsgRd)
        {
            ParseTextMessage(liveChatPaidMessageRenderer, giftMsgRd);
        }

        /// <summary>
        /// 解析留言內容
        /// </summary>
        /// <param name="liveChatTextMessageRenderer">LiveChatTextMessageRenderer</param>
        /// <param name="txtMsgRd">json data.</param>
        private void ParseTextMessage(LiveChatTextMessageRenderer liveChatTextMessageRenderer, dynamic txtMsgRd)
        {
            liveChatTextMessageRenderer.AuthorExternalChannelId = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorExternalChannelId", string.Empty));
            liveChatTextMessageRenderer.AuthorName.SimpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorName.simpleText", string.Empty));
            liveChatTextMessageRenderer.AuthorPhoto.Thumbnails = ParseThumbnails(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorPhoto.thumbnails", null));
            liveChatTextMessageRenderer.ContextMenuAccessibility.AccessibilityData.Label = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "contextMenuAccessibility.accessibilityData.label", string.Empty));
            liveChatTextMessageRenderer.Id = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "id", string.Empty));
            liveChatTextMessageRenderer.TimestampUsec = Convert.ToInt64(JsonHelper.TryGetValueByXPath(txtMsgRd, "timestampUsec", 0));

            // 留言包含自訂表情符號或空格時 runs 陣列會分割成多元素。
            dynamic runs = JsonHelper.TryGetValueByXPath(txtMsgRd, "message.runs");

            if (runs != null)
            {
                for (int i = 0; i < runs.Count; i++)
                {
                    // 文字。
                    string xPath = string.Format($"message.runs.{i}.text"),
                        // emoji.
                        emojiPath = string.Format($"message.runs.{i}.emoji.image.thumbnails.0.url"),
                        text = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, xPath, string.Empty)),
                        emoji = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, emojiPath, string.Empty));

                    if (!string.IsNullOrEmpty(text))
                    {
                        liveChatTextMessageRenderer.Message.Runs.Add(new Run
                        {
                            Type = CommentDetailType.Text,
                            Content = text
                        });
                    }
                    else
                    {
                        liveChatTextMessageRenderer.Message.Runs.Add(new Run
                        {
                            Type = CommentDetailType.Emoji,
                            Content = emoji
                        });
                    }

                    liveChatTextMessageRenderer.Message.SimpleText += text;
                }
            }
            else
            {
                liveChatTextMessageRenderer.Message.SimpleText = string.Empty;
            }

            dynamic authorBadges = JsonHelper.TryGetValueByXPath(txtMsgRd, "authorBadges", null);

            if (authorBadges != null)
            {
                // 留言者可能擁有多個徽章 (EX: 管理員、會員)
                for (int i = 0; i < authorBadges.Count; i++)
                {
                    AuthorBadge badge = new AuthorBadge
                    {
                        Url = Convert.ToString(JsonHelper.TryGetValueByXPath(authorBadges[i], "liveChatAuthorBadgeRenderer.customThumbnail.thumbnails.0.url")),
                        IconType = Convert.ToString(JsonHelper.TryGetValueByXPath(authorBadges[i], "liveChatAuthorBadgeRenderer.icon.iconType")),
                        Tooltip = Convert.ToString(JsonHelper.TryGetValueByXPath(authorBadges[i], "liveChatAuthorBadgeRenderer.tooltip")),
                        Label = Convert.ToString(JsonHelper.TryGetValueByXPath(authorBadges[i], "liveChatAuthorBadgeRenderer.accessibility.accessibilityData.label"))
                    };

                    liveChatTextMessageRenderer.AuthorBadges.Add(badge);
                }
            }
        }

        /// <summary>
        /// Parse thumbnails
        /// </summary>
        /// <param name="thumbnails">Dynamic, thumbnails</param>
        private List<Thumbnail> ParseThumbnails(dynamic thumbnails)
        {
            if (thumbnails == null)
            {
                return null;
            }

            List<Thumbnail> ret = new List<Thumbnail>();

            for (int i = 0; i < thumbnails.Count; i++)
            {
                Thumbnail thumb = new Thumbnail
                {
                    Url = Convert.ToString(JsonHelper.TryGetValue(thumbnails[i], "url", string.Empty)),
                    Width = Convert.ToInt32(JsonHelper.TryGetValue(thumbnails[i], "width", 0)),
                    Height = Convert.ToInt32(JsonHelper.TryGetValue(thumbnails[i], "height", 0))
                };

                ret.Add(thumb);
            }

            return ret;
        }

        #endregion Private Method

        #region Event

        public delegate void ErrorHandleMethod(CommentLoader sender, CommentLoaderErrorCode errCode, object obj);

        /// <summary>
        /// CommentLoader 發生錯誤事件
        /// </summary>
        public event ErrorHandleMethod OnError;

        /// <summary>
        /// 發出錯誤事件
        /// </summary>
        /// <param name="errCode">錯誤碼</param>
        /// <param name="obj">附帶的錯誤資訊</param>
        private void RaiseError(CommentLoaderErrorCode errCode, object obj)
        {
            Debug.WriteLine(string.Format("[RaiseError] errCode: {0}, {1}", errCode.ToString(), obj));

            OnError?.Invoke(this, errCode, obj);
        }

        public delegate void CommentsReceiveMethod(CommentLoader sender, List<CommentData> lsComments);

        /// <summary>
        /// CommentLoader 取得新留言事件
        /// </summary>
        public event CommentsReceiveMethod OnCommentsReceive;

        /// <summary>
        /// 發出收到留言事件
        /// </summary>
        /// <param name="lsComments">收到的留言資料列表</param>
        private void RaiseCommentsReceive(List<CommentData> lsComments)
        {
            OnCommentsReceive?.Invoke(this, lsComments);
        }

        public delegate void StatusChangedMethod(CommentLoader sender, CommentLoaderStatus status);

        /// <summary>
        /// CommentLoader 執行時發生的各階段事件
        /// <para>GetComments 狀態會持續發生</para>
        /// </summary>
        public event StatusChangedMethod OnStatusChanged;

        /// <summary>
        /// 發出執行階段狀態事件
        /// </summary>
        /// <param name="status">正在執行的狀態</param>
        private void RaiseStatusChanged(CommentLoaderStatus status)
        {
            Status = status;

            //Debug.WriteLine(string.Format("[OnStatusChanged] {0}", Status.ToString()));

            OnStatusChanged?.Invoke(this, status);
        }

        #endregion Event
    }
}
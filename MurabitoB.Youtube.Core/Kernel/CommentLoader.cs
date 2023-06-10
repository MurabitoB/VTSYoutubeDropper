﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

using KomeTube.Kernel.YtLiveChatDataModel;
using KomeTube.Emums;

namespace KomeTube.Kernel
{
    public class CommentLoader
    {

        private readonly static string BaseUrl = "www.youtube.com";

        // private readonly static string BaseUrl = "localhost:3000";
        
        #region Private Member

        private String _videoUrl;
        private CookieContainer _cookieContainer;
        private InnerTubeContextData _innerContextData;
        private readonly Object _lockContinuation;

        private Task _mainTask;
        private CancellationTokenSource _mainTaskCancelTS;

        #endregion Private Member

        #region Constructor

        public CommentLoader()
        {
            _lockContinuation = new object();
        }

        #endregion Constructor

        #region Public Member

        /// <summary>
        /// 使用者輸入的直播影片網址
        /// </summary>


        public String VideoUrl
        {
            get
            {
                return _videoUrl;
            }

            set
            {
                _videoUrl = value;
                // 生成標題
            }
        }

        /// <summary>
        /// 當前的continuation資料，用來取得下次留言列表
        /// </summary>
        public String CurrentContinuation
        {
            get
            {
                lock (_lockContinuation)
                {
                    return _innerContextData.continuation;
                }
            }

            set
            {
                lock (_lockContinuation)
                {
                    _innerContextData.continuation = value;
                }
            }
        }

        public DateTime UpdateTime { get; set; }


        /// <summary>
        /// CommentLoader當前執行狀態
        /// </summary>
        public CommentLoaderStatus Status { get; private set; }

        #endregion Public Member

        #region Public Method

        /// <summary>
        /// 開始讀取留言
        /// <para>請監聽OnCommentsReceive事件取得留言列表</para>
        /// </summary>
        /// <param name="url">Youtube直播影片位址</param>
        public void Start(String url)
        {
            if (_mainTask != null
                && !_mainTask.IsCompleted)
            {
                //若任務仍在執行則不再重複啟動
                return;
            }

            _mainTaskCancelTS = new CancellationTokenSource();
            _mainTask = Task.Factory.StartNew(() => StartGetComments(url), _mainTaskCancelTS.Token);
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
                //停止取得留言
                _mainTaskCancelTS.Cancel();
                RaiseStatusChanged(CommentLoaderStatus.StopRequested);
            }
        }

        #endregion Public Method

        #region Private Method

        /// <summary>
        /// 從影片位址解析vid後取得聊天室位址
        /// </summary>
        /// <param name="videoUrl">影片位址</param>
        /// <returns>回傳聊天室位址，若失敗則發出CanNotGetLiveChatUrl Error事件，並回傳空字串</returns>
        private String GetLiveChatRoomUrl(String videoUrl)
        {
            String baseUrl = $"{BaseUrl}/watch?";
            String ret = "";
            String urlParamStr = videoUrl.Substring(videoUrl.IndexOf(baseUrl) + baseUrl.Length);
            String[] urlParamArr = urlParamStr.Split('&');
            String vid = "";

            //取得vid
            foreach (String param in urlParamArr)
            {
                if (param.IndexOf("v=") == 0)
                {
                    vid = param.Substring(2);
                    break;
                }
            }

            if (vid == "")
            {
                Debug.WriteLine(String.Format("[GetLiveChatRoomUrl] 無法取得聊天室位址. URL={0}", videoUrl));
                RaiseError(CommentLoaderErrorCode.CanNotGetLiveChatUrl, videoUrl);
                return "";
            }
            else
            {
                ret = String.Format($"https://{BaseUrl}/live_chat?v={{0}}&is_popout=1", vid);
            }

            return ret;
        }

        /// <summary>
        /// 取得Youtube API 'get_live_chat'的位址
        /// </summary>
        /// <param name="apiKey">YtCfg資料中INNERTUBE_API_KEY參數。此參數應從ParseLiveChatHtml或GetComment方法取得</param>
        /// <returns>回傳Youtube API 'get_live_chat'的位址</returns>
        private String GetLiveChatUrl(String apiKey)
        {
            string ret = $@"https://{BaseUrl}/youtubei/v1/live_chat/get_live_chat?key=" + apiKey;

            return ret;
        }

        /// <summary>
        /// 開始取得留言，此方法將會進入長時間迴圈，若要停止請使用_mainTaskCancelTS發出cancel請求
        /// </summary>
        /// <param name="url">直播影片位址</param>
        private void StartGetComments(String url)
        {
            RaiseStatusChanged(CommentLoaderStatus.Started);

            String continuation = "";

            //取得聊天室位址
            String liveChatRoomUrl = GetLiveChatRoomUrl(url);
            if (liveChatRoomUrl == "")
            {
                Debug.WriteLine(String.Format("[StartGetComments] GetLiveChatRoomUrl無法取得html內容"));
                return;
            }

            //取得continuation和第一次訪問的留言列表
            List<CommentData> firstCommentList = ParseLiveChatHtml(liveChatRoomUrl, ref continuation);
            if (continuation == "")
            {
                Debug.WriteLine(String.Format("[StartGetComments] ParseLiveChatHtml無法取得continuation參數"));
                return;
            }

            this.VideoUrl = url;
            RaiseCommentsReceive(firstCommentList);

            //持續取得留言
            while (!_mainTaskCancelTS.IsCancellationRequested)
            {
                List<CommentData> comments = GetComments(ref continuation);

                if (comments != null
                    && comments.Count > 0)
                {
                    RaiseCommentsReceive(comments);
                }

                if (continuation == "")
                {
                    Debug.WriteLine(String.Format("[StartGetComments] GetComments無法取得continuation參數"));
                    return;
                }

                SpinWait.SpinUntil(() => false, 1000);
            }
        }

        /// <summary>
        /// 取得留言的Task結束(StartGetComments方法結束)
        /// </summary>
        /// <param name="sender">已完成的Task</param>
        /// <param name="obj"></param>
        private void StartGetCommentsCompleted(Task sender, object obj)
        {
            if (sender.IsFaulted)
            {
                //取得留言時發生其他exception造成Task結束
                RaiseError(CommentLoaderErrorCode.GetCommentsError, sender.Exception.Message);
            }

            RaiseStatusChanged(CommentLoaderStatus.Completed);
        }

        /// <summary>
        /// 取得第一次訪問的cookie資料，並在html中取出ytcfg資料與window["ytInitialData"] 後方的json code，並解析出continuation
        /// </summary>
        /// <param name="liveChatUrl">聊天室位址</param>
        /// <returns>回傳continuation參數值</returns>
        private List<CommentData> ParseLiveChatHtml
            (String liveChatUrl, ref String continuation)
        {
            String htmlContent = "";
            List<CommentData> initComments = new List<CommentData>();

            RaiseStatusChanged(CommentLoaderStatus.GetLiveChatHtml);
            CookieContainer cc = new CookieContainer();
            var cookie = new CookieCollection();

            //cookie.Add(new Cookie
            //{
            //   Domain = "youtube.com",
            //   Name = "LOGIN_INFO",
            //   Value = "AFmmF2swRgIhAJMbCYTyIwvmeIy2rcbbo7lxweiYdg8V49p6Q4HSrnMiAiEAnoeeMQ7LxvEFRmQv2-pphvuJr6t0f2Z_jnFRWCFUrjs:QUQ3MjNmeFd2eGRXTzRSdVl0NVJTUWphNlJoU0xQNDlZS3dqT2NEYU1EekhuRS1ZdXljd0VsMm9MREFPVWVlbVh6TlNsM2N3Z2lEWDhQOWFtYmdLWm5EeS1WRkdEdGMzb1FBc1dHQUtmS1V2VUJoWU54NlFiZG5McEtyZ19FYkVQMktuQzBtNU8tSlFfWUsxelJjcHFDVEszSGwwNGpQVHRn"
            //});

            //cookie.Add(new Cookie
            //{
            //    Domain = "youtube.com",
            //    Name = "APISID",
            //    Value = "hgUh1jzTKibHgDZ9/AdqjTMGcvSj0ZKkP_"
            //});

            //cookie.Add(new Cookie
            //{
            //    Domain = "youtube.com",
            //    Name = "NID",
            //    Value = "217=SqXJUe2IqXu1Xhz8r1o73t2PKDqoeoFIvxSTRTcliGuKya3lxnd5lfcu0e7GAmm_5xbxT5HjNmMSFtB8e-26_ucvZ9tAR2fuBb-wjRGFTwdtQd40fvnlfgmU63e8PMb_9qvupFGKKAoB9kXxvCFkHbx7NLY1tKBkpELa9r3dvgk/AdqjTMGcvSj0ZKkP_"
            //});

            //cookie.Add(new Cookie
            //{
            //    Domain = "youtube.com",
            //    Name = "SAPISID",
            //    Value = "E8zXvzXoRm6-GNQ4/AJCIHpoGPx2XOZbWz"
            //});

            cookie.Add(new Cookie
            {
                Domain = "youtube.com",
                Name = "HSID",
                Value = "Aqjq0ncax6Vj0b1Sg"
            });

            //cookie.Add(new Cookie
            //{
            //    Domain = "youtube.com",
            //    Name = "YSC",
            //    Value = "g7Oe9ru2pNo"
            //});

            cookie.Add(new Cookie
            {
                Domain = "youtube.com",
                Name = "SID",
                Value = "-wfPhZz_a6AfVoM58pFgVEeUwZQO8rXbz_UMmy_6fHW8rdTB6fvSblG-KDatVKnHhZvcpw."
            });

            cookie.Add(new Cookie
            {
                Domain = "youtube.com",
                Name = "SSID",
                Value = "ATZtANlESJ21OsZtD"
            });

            cc.Add(cookie);

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cc,
            };

            //取得HTML內容
            using (HttpClient client = new HttpClient(handler))
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:59.0) Gecko/20100101 Firefox/59.0");
                    htmlContent = client.GetStringAsync(liveChatUrl).Result;
                    _cookieContainer = handler.CookieContainer;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法取得聊天室HTML內容. Excetion:{0}", e.Message));
                    RaiseError(CommentLoaderErrorCode.CanNotGetLiveChatHtml, e.Message);
                    return null;
                }
            }

            //解析YtCfg
            RaiseStatusChanged(CommentLoaderStatus.ParseYtCfgData);
            string strCfg = ParseYtCfg(htmlContent);
            if (strCfg == null)
            {
                Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法解析YtCfg. HTML content:{0}", htmlContent));
                RaiseError(CommentLoaderErrorCode.CanNotParseYtCfg, htmlContent);
                return null;
            }
            //解析inner context data
            _innerContextData = ParseInnerContextData(strCfg);

            //解析HTML
            RaiseStatusChanged(CommentLoaderStatus.ParseLiveChatHtml);
            Match match = Regex.Match(htmlContent, "window\\[\"ytInitialData\"\\] = ({.+});\\s*</script>", RegexOptions.Singleline);
            if (!match.Success)
            {
                Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法解析HTML. HTML content:{0}", htmlContent));
                RaiseError(CommentLoaderErrorCode.CanNotParseLiveChatHtml, htmlContent);
                return null;
            }

            //解析json data
            String ytInitialData = match.Groups[1].Value;
            dynamic jsonData;
            try
            {
                jsonData = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(ytInitialData);

                var data = jsonData["contents"]["liveChatRenderer"]["continuations"][0]["invalidationContinuationData"];
                if (data == null)
                {
                    data = jsonData["contents"]["liveChatRenderer"]["continuations"][0]["timedContinuationData"];
                }
                continuation = Convert.ToString(JsonHelper.TryGetValue(data, "continuation", ""));
                _innerContextData.continuation = continuation;

                var actions = jsonData["contents"]["liveChatRenderer"]["actions"];
                initComments = ParseComment(actions);

            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("[ParseLiveChatHtml] 無法解析json data:{0}", e.Message));
                RaiseError(CommentLoaderErrorCode.CanNotParseLiveChatHtml, ytInitialData);
                return null;
            }

            return initComments;
        }

        /// <summary>
        /// 從第一次訪問的html內容解析出ytcfg字串
        /// </summary>
        /// <param name="liveChatHtml"></param>
        /// <returns></returns>
        private string ParseYtCfg(string liveChatHtml)
        {
            var match = Regex.Match(liveChatHtml, "ytcfg\\.set\\(({.+?})\\);", RegexOptions.Singleline);
            if (!match.Success)
            {
                return null;
            }

            try
            {
                var ytCfg = match.Groups[1].Value;
                dynamic d = JsonConvert.DeserializeObject(ytCfg);
                var matches = Regex.Matches(liveChatHtml, "ytcfg\\.set\\(\"([^\"]+)\",\\s*(.+?)\\);?\\r?\n", RegexOptions.Singleline);
                foreach (Match m in matches)
                {
                    var key = m.Groups[1].Value;
                    var value = m.Groups[2].Value;
                    var s = "{\"" + key + "\":" + value + "}";
                    var obb = JsonConvert.DeserializeObject(s);
                    d.Merge(obb);
                }
                return d.ToString(Formatting.None);
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format($"[ParseYtCfg] 無法解析YtCfg資料:{e.Message}"));
                return null;
            }
        }

        /// <summary>
        /// 從YtCfg字串解析inner context資料供取得留言使用
        /// </summary>
        /// <param name="strCfg"></param>
        /// <returns></returns>
        private InnerTubeContextData ParseInnerContextData(string strCfg)
        {
            InnerTubeContextData ret = new InnerTubeContextData();
            dynamic jsonData = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(strCfg);

            ret.INNERTUBE_API_KEY = Convert.ToString(JsonHelper.TryGetValue(jsonData, "INNERTUBE_API_KEY", ""));

            ret.context.clickTracking.clickTrackingParams = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.clickTracking.clickTrackingParams", ""));

            ret.context.request.useSsl = Convert.ToBoolean(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.request.useSsl", false));

            ret.context.client.browserName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.browserName", ""));
            ret.context.client.browserVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.browserVersion", ""));
            ret.context.client.clientFormFactor = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientFormFactor", ""));
            ret.context.client.clientName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientName", ""));
            ret.context.client.clientVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.clientVersion", ""));
            ret.context.client.deviceMake = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.deviceMake", ""));
            ret.context.client.deviceModel = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.deviceModel", ""));
            ret.context.client.gl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.gl", ""));
            ret.context.client.hl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.hl", ""));
            ret.context.client.originalUrl = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.originalUrl", ""));
            ret.context.client.osName = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.osName", ""));
            ret.context.client.osVersion = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.osVersion", ""));
            ret.context.client.platform = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.platform", ""));
            ret.context.client.remoteHost = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.remoteHost", ""));
            ret.context.client.userAgent = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.userAgent", ""));
            ret.context.client.visitorData = Convert.ToString(JsonHelper.TryGetValueByXPath(jsonData, "INNERTUBE_CONTEXT.client.visitorData", ""));

            return ret;
        }

        /// <summary>
        /// 利用Youtube API 'get_live_chat'取得聊天室留言，並解析continuation參數供下次取得留言使用
        /// </summary>
        /// <param name="continuation">Continuation參數</param>
        /// <returns>成功時回傳留言資料，失敗則回傳null。</returns>
        private List<CommentData> GetComments(ref String continuation)
        {
            if (continuation == null || continuation == "")
            {
                RaiseError(CommentLoaderErrorCode.GetCommentsError, new Exception("continuation參數錯誤"));
                return null;
            }

            RaiseStatusChanged(CommentLoaderStatus.GetComments);

            String chatUrl = GetLiveChatUrl(_innerContextData.INNERTUBE_API_KEY);
            List<CommentData> ret = null;
            String resp = "";
            HttpClientHandler handler = new HttpClientHandler()
            {
                UseCookies = true,
                CookieContainer = _cookieContainer,
            };

            using (HttpClient client = new HttpClient(handler))
            {
                try
                {
                    StringContent dataContent = new StringContent(_innerContextData.ToString(), Encoding.UTF8, "application/json");

                    //取得聊天室留言
                    client.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:59.0) Gecko/20100101 Firefox/59.0");
                    client.DefaultRequestHeaders.Add("Origin", "https://www.youtube.com");

                    HttpResponseMessage respMsg = client.PostAsync(chatUrl, dataContent).Result;
                    resp = respMsg.Content.ReadAsStringAsync().Result;

                    //解析continuation供下次取得留言使用
                    dynamic jsonData = JsonConvert.DeserializeObject<Dictionary<String, dynamic>>(resp);
                    var data = jsonData["continuationContents"]["liveChatContinuation"]["continuations"][0]["invalidationContinuationData"];
                    if (data == null)
                    {
                        data = jsonData["continuationContents"]["liveChatContinuation"]["continuations"][0]["timedContinuationData"];
                    }
                    continuation = Convert.ToString(JsonHelper.TryGetValue(data, "continuation", ""));
                    _innerContextData.continuation = continuation;
                    _cookieContainer = handler.CookieContainer;

                    //解析留言資料
                    var commentActions = jsonData["continuationContents"]["liveChatContinuation"]["actions"];
                    ret = ParseComment(commentActions);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(String.Format("[GetComments] 無法取得聊天室HTML內容. Excetion:{0}", e.Message));
                    RaiseError(CommentLoaderErrorCode.GetCommentsError, e.Message);
                    return null;
                }
            }

            return ret;
        }

        /// <summary>
        /// 解析留言資訊
        /// </summary>
        /// <param name="commentActions">json data.</param>
        /// <returns>成功則回傳留言列表，失敗則回傳null</returns>
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
                cmt.addChatItemAction.clientId = Convert.ToString(JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.clientId", ""));

                var txtMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatTextMessageRenderer", null);
                if (txtMsgRd != null)
                {
                    ParseTextMessage(cmt.addChatItemAction.item.liveChatTextMessageRenderer, txtMsgRd);
                }
                else
                {
                    dynamic paidMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatPaidMessageRenderer", null);
                    dynamic membershipMsgRd = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatMembershipItemRenderer", null);
                    dynamic gift = JsonHelper.TryGetValueByXPath(commentActions[i], "addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer", null);
                    
                    if (paidMsgRd != null)
                    {
                        ParsePaidMessage(cmt.addChatItemAction.item.liveChatPaidMessageRenderer, paidMsgRd);
                    }
                    
                    else if (membershipMsgRd != null)
                    {
                        ParseMemberMilestoneMessage(cmt.addChatItemAction.item.liveChatMembershipItemRenderer, membershipMsgRd);
                    }
                    else if (gift != null)
                    {
                        ParseGiftMessage(cmt.addChatItemAction.item.liveChatSponsorshipsGiftPurchaseAnnouncementRenderer, gift);
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
            //解析留言內容
            ParseTextMessage(liveChatPaidMessageRenderer, paidMsgRd);

            //解析付費留言內容
            liveChatPaidMessageRenderer.purchaseAmountText.simpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(paidMsgRd, "purchaseAmountText.simpleText", ""));
            liveChatPaidMessageRenderer.headerBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "headerBackgroundColor", 0));
            liveChatPaidMessageRenderer.headerTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "headerTextColor", 0));
            liveChatPaidMessageRenderer.bodyBackgroundColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "bodyBackgroundColor", 0));
            liveChatPaidMessageRenderer.bodyTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "bodyTextColor", 0));
            liveChatPaidMessageRenderer.authorNameTextColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "authorNameTextColor", 0));
            liveChatPaidMessageRenderer.timestampColor = Convert.ToInt64(JsonHelper.TryGetValueByXPath(paidMsgRd, "timestampColor", 0));
        }

        private void ParseMemberMilestoneMessage(LiveChatMembershipItemRenderer liveChatMembershipItemRenderer, dynamic memberMileStoneRd)
        {
            ParseTextMessage(liveChatMembershipItemRenderer, memberMileStoneRd);
        }
        
        private void ParseGiftMessage(LiveChatSponsorshipsGiftPurchaseAnnouncementRenderer liveChatPaidMessageRenderer, dynamic giftMsgRd)
        {
            ParseTextMessage(liveChatPaidMessageRenderer, giftMsgRd);
        }
        

        /// <summary>
        /// 解析留言內容
        /// </summary>
        /// <param name="liveChatTextMessageRenderer"></param>
        /// <param name="txtMsgRd">json data.</param>
        private void ParseTextMessage(LiveChatTextMessageRenderer liveChatTextMessageRenderer, dynamic txtMsgRd)
        {
            liveChatTextMessageRenderer.authorExternalChannelId = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorExternalChannelId", ""));
            liveChatTextMessageRenderer.authorName.simpleText = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorName.simpleText", ""));
            liveChatTextMessageRenderer.authorPhoto.thumbnails = ParseAuthorPhotoThumb(JsonHelper.TryGetValueByXPath(txtMsgRd, "authorPhoto.thumbnails", null));
            liveChatTextMessageRenderer.contextMenuAccessibility.accessibilityData.label = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "contextMenuAccessibility.accessibilityData.label", ""));
            liveChatTextMessageRenderer.id = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, "id", ""));
            liveChatTextMessageRenderer.timestampUsec = Convert.ToInt64(JsonHelper.TryGetValueByXPath(txtMsgRd, "timestampUsec", 0));


            //留言包含自訂表情符號或空格時runs陣列會分割成多元素
            dynamic runs = JsonHelper.TryGetValueByXPath(txtMsgRd, "message.runs");
            if (runs != null)
            {
                for (int i = 0; i < runs.Count; i++)
                {
                    string xPath = String.Format($"message.runs.{i.ToString()}.text"); // 文字
                    string emojiPath = String.Format($"message.runs.{i.ToString()}.emoji.image.thumbnails.0.url");// emoji
                    var text = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, xPath, ""));
                    var emoji = Convert.ToString(JsonHelper.TryGetValueByXPath(txtMsgRd, emojiPath, ""));

                    if (!string.IsNullOrEmpty(text))
                    {
                        liveChatTextMessageRenderer.message.runs.Add(new Run
                        {
                            type = CommentDetailType.text,
                            content = text
                        });
                    }
                    else
                    {
                        liveChatTextMessageRenderer.message.runs.Add(new Run
                        {
                            type = CommentDetailType.emoji,
                            content = emoji
                        });
                    }

                    liveChatTextMessageRenderer.message.simpleText += text;
                }
            }
            else
                liveChatTextMessageRenderer.message.simpleText = "";

            var authorBadges = JsonHelper.TryGetValueByXPath(txtMsgRd, "authorBadges", null);
            if (authorBadges != null)
            {
                //留言者可能擁有多個徽章 (EX:管理員、會員)
                for (int i = 0; i < authorBadges.Count; i++)
                {
                    AuthorBadge badge = new AuthorBadge();
                    badge.tooltip = Convert.ToString(JsonHelper.TryGetValueByXPath(authorBadges[i], "liveChatAuthorBadgeRenderer.tooltip"));
                    liveChatTextMessageRenderer.authorBadges.Add(badge);
                }
            }
        }

        /// <summary>
        /// 解析留言者縮圖
        /// </summary>
        /// <param name="authorPhotoData">json data.</param>
        private List<Thumbnail> ParseAuthorPhotoThumb(dynamic authorPhotoData)
        {
            if (authorPhotoData == null)
            {
                return null;
            }

            List<Thumbnail> ret = new List<Thumbnail>();

            for (int i = 0; i < authorPhotoData.Count; i++)
            {
                Thumbnail thumb = new Thumbnail();
                thumb.url = JsonHelper.TryGetValue(authorPhotoData[i], "url", "");
                thumb.width = JsonHelper.TryGetValue(authorPhotoData[i], "width", "");
                thumb.height = JsonHelper.TryGetValue(authorPhotoData[i], "height", "");
                ret.Add(thumb);
            }

            return ret;
        }

        #endregion Private Method

        #region Event

        public delegate void ErrorHandleMethod(CommentLoader sender, CommentLoaderErrorCode errCode, object obj);

        /// <summary>
        /// CommentLoader發生錯誤事件
        /// </summary>
        public event ErrorHandleMethod OnError;

        /// <summary>
        /// 發出錯誤事件
        /// </summary>
        /// <param name="errCode">錯誤碼</param>
        /// <param name="obj">附帶的錯誤資訊</param>
        private void RaiseError(CommentLoaderErrorCode errCode, object obj)
        {
            Debug.WriteLine(String.Format("[RaiseError] errCode:{0}, {1}", errCode.ToString(), obj));
            if (OnError != null)
            {
                OnError(this, errCode, obj);
            }
        }

        public delegate void CommentsReceiveMethod(CommentLoader sender, List<CommentData> lsComments);

        /// <summary>
        /// CommentLoader取得新留言事件
        /// </summary>
        public event CommentsReceiveMethod OnCommentsReceive;

        /// <summary>
        /// 發出收到留言事件
        /// </summary>
        /// <param name="lsComments">收到的留言資料列表</param>
        private void RaiseCommentsReceive(List<CommentData> lsComments)
        {
            if (OnCommentsReceive != null)
            {
                OnCommentsReceive(this, lsComments);
            }
        }

        public delegate void StatusChangedMethod(CommentLoader sender, CommentLoaderStatus status);

        /// <summary>
        /// CommentLoader執行時發生的各階段事件
        /// <para>GetComments狀態會持續發生</para>
        /// </summary>
        public event StatusChangedMethod OnStatusChanged;

        /// <summary>
        /// 發出執行階段狀態事件
        /// </summary>
        /// <param name="status">正在執行的狀態</param>
        private void RaiseStatusChanged(CommentLoaderStatus status)
        {
            this.Status = status;
            //Debug.WriteLine(String.Format("[OnStatusChanged] {0}", this.Status.ToString()));
            if (OnStatusChanged != null)
            {
                OnStatusChanged(this, status);
            }
        }

        #endregion Event
    }
}
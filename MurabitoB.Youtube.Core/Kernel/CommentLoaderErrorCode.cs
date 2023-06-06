namespace KomeTube.Kernel
{
    public enum CommentLoaderErrorCode
    {
        /// <summary>
        /// 無法取得聊天室位址
        /// <para>發生此錯誤時，附帶的錯誤資訊為使用者輸入的影片位址。 (URL)</para>
        /// </summary>
        CanNotGetLiveChatUrl,
        /// <summary>
        /// 無法取得聊天室 HTML 內容
        /// <para>發生此錯誤時，附帶的錯誤資訊為 Exception message。</para>
        /// </summary>
        CanNotGetLiveChatHtml,
        /// <summary>
        /// 無法解析連線設定
        /// <para>發生此錯誤時，附帶的錯誤資訊為聊天室 HTML 內容。</para>
        /// </summary>
        CanNotParseYtCfg,
        /// <summary>
        /// 無法解析 HTML
        /// <para>發生此錯誤時，附帶的錯誤資訊為聊天室 HTML 內容。</para>
        /// </summary>
        CanNotParseLiveChatHtml,
        /// <summary>
        /// 取得留言時發生錯誤
        /// <para>發生此錯誤時，附帶的錯誤資訊為 Exception message。</para>
        /// </summary>
        GetCommentsError,
        /// <summary>
        /// An error occurred while parsing the continuation
        /// <para>When this error occurs, the attached error message is the Exception message.</para>
        /// </summary>
        GetContinuationError
    }
}
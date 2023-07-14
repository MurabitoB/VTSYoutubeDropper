namespace KomeTube.Kernel
{
    public enum CommentLoaderStatus
    {
        Null,
        Started,
        GetLiveChatHtml,
        ParseYtCfgData,
        ParseLiveChatHtml,
        GetComments,
        StopRequested,
        Completed
    }
}
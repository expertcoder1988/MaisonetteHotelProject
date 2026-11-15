namespace MyBlog.Model
{
    public static class WebConstant
    {
        public const string DateFormate = "MM/dd/yyyy";
        public const string DefaultImage = "/Content/img/default.jpg";
        public const string PostImagePath = "/uploads/post/";
        public const string LogoImagePath = "/uploads/logo/";
        public const string ProfileImagePath = "/uploads/profile/";
    }

    public static class CacheKey
    {
        public const string Setting = "Setting";
        public const int SettingCacheTimeSeconds = 18000; // 1 hours = 3600 second
    }

    public class Alert
    {
        public const string TempDataKey = "TempDataAlerts";

        public string AlertStyle { get; set; }
        public string Message { get; set; }
        public bool Dismissable { get; set; }
    }

    public static class AlertStyles
    {
        public const string Success = "success";
        public const string Information = "info";
        public const string Warning = "warning";
        public const string Danger = "danger";
    }
}

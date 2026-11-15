using HotelWebsite.Models;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MyBlog.Model
{
    public class SettingViewModel
    {
        public SettingViewModel()
        {
            Title = string.Empty;
            Description = string.Empty;
            Keywords = string.Empty;
            Author = string.Empty;
            GoogleAnalytics = string.Empty;
            SharePost = true;
            NoOfPostShow = 5;
            SMTPEnableSsl = true;
        }

        [DataType(DataType.Upload)]
        [Display(Name = "Upload logo")]
        [FileType("jpg, jpeg, png")]
        public HttpPostedFileBase File { get; set; }

        [Display(Name = "Logo")]
        public string Logo { get; set; }

        public string ViewLogo
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Logo))
                    return string.Empty;

                return WebConstant.LogoImagePath + Logo;
            }
        }

        [MaxLength(100)]
        [Display(Name = "Blog Title")]
        public string Title { get; set; }

        [MaxLength(200)]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [MaxLength(100)]
        public string Keywords { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Site URL")]
        [DataType(DataType.Url)]
        public string SiteURL { get; set; }

        [MaxLength(100)]
        public string Author { get; set; }

        [Display(Name = "Custom robots.txt")]
        [DataType(DataType.MultilineText)]
        public string RobotsTxt { get; set; }

        [Display(Name = "Google Analytics Key")]
        [MaxLength(50)]
        public string GoogleAnalytics { get; set; }

        [Required]
        [Range(1, 20)]
        [Display(Name = "No of post show per page")]
        public int NoOfPostShow { get; set; }

        [Display(Name = "Social Share Button")]
        public bool SharePost { get; set; }

        [Display(Name = "Show Footer")]
        public bool ShowFooter { get; set; }

        [Display(Name = "Show Related Posts")]
        public bool ShowRelatedPosts { get; set; }

        [MaxLength(100)]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "SMTP Username/Email")]
        public string SMTPEmail { get; set; }

        [MaxLength(100)]
        [Display(Name = "SMTP Password")]
        public string SMTPPassword { get; set; }

        [MaxLength(50)]
        [Display(Name = "SMTP Host")]
        public string SMTPHost { get; set; }

        [Display(Name = "SMTP Port")]
        public int SMTPPort { get; set; }

        [Display(Name = "SMTP Enable Ssl")]
        public bool SMTPEnableSsl { get; set; }
    }
}

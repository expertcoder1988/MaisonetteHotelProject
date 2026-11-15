using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Web;
using HotelWebsite.Models;
using MyBlog.Model;

namespace HotelWebsite.Models
{
    public class PostViewModel
    {
        public int PostId { get; set; }

        [DataType(DataType.Upload)]
        [Display(Name = "Upload Image")]
        [FileType("jpg, jpeg, png")]
        public HttpPostedFileBase File { get; set; }

        public string DefaultImage { get; set; }

        public string ViewDefaultImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DefaultImage))
                    return WebConstant.DefaultImage;

                return WebConstant.PostImagePath + DefaultImage;
            }
        }

        [Required]
        [MaxLength(150)]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required]
        [MaxLength(160)]
        [Display(Name = "MetaDescription")]
        public string MetaDescription { get; set; }

        [Required]
        [MaxLength(150)]
        [Display(Name = "Keywords")]
        public string Keywords { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Author")]
        public string Author { get; set; }

        [MaxLength(200)]
        [RegularExpression(@"^[a-z][a-z\-]*[a-z]$", ErrorMessage = "Invalid Sug url. Url contain only latters, numbers and hyphen.")]
        public string Slug { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        public bool IsPublished { get; set; }

        [Display(Name = "Published")]
        public DateTime? PublishedDate { get; set; }

        [Display(Name = "Modify Date")]
        public DateTime ModifyDate { get; set; }

        public string DisplayPublishedDate
        {
            get
            {
                if (!PublishedDate.HasValue)
                    return string.Empty;

                return PublishedDate.Value.ToString("ddd, MMM dd, yyyy");
            }
        }

        public string ShortDescription
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Description))
                    return string.Empty;

                var desc = Regex.Replace(Description, "<.*?>", String.Empty);

                if (string.IsNullOrWhiteSpace(desc))
                    return string.Empty;

                return desc.Length > 301 ? desc.Substring(0, 300) + "..." : desc;
            }
        }

        public string Tags { get; set; }

        public int NoOfTimeView { get; set; }

        public string PostUrl
        {
            get
            {
                var requestContext = HttpContext.Current.Request.RequestContext;
                System.Web.Mvc.UrlHelper url = new System.Web.Mvc.UrlHelper(requestContext);
                return url.RouteUrl("postDetailRoute", new { postId = PostId, slug = Slug }, HttpContext.Current.Request.Url.Scheme);
            }
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace MyBlog.Model
{
    public class SocialProfileViewModel
    {
        public int? SocialProfileId { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Button Class")]
        public string ButtonClass { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Icon Class")]
        public string IconClass { get; set; }

        [Required]
        [Display(Name = "Url")]
        public string Url { get; set; }

        [Required]
        [Display(Name = "Order")]
        [RegularExpression("([0-9]+)")]
        [Range(0, 255)]
        public int OrderBy { get; set; }

        [Display(Name = "Visible")]
        public bool IsVisible { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MyBlog.Model
{
    public class SubscribeViewModel
    {
        public int SubscribeId { get; set; }

        [Required]
        [MaxLength(100)]
        [DataType(DataType.EmailAddress)]
        [Remote("IsSubscribeEmail", "Home", ErrorMessage = "This email is already subscribed.", AdditionalFields = "SubscribeId")]
        public string Email { get; set; }

        public bool IsSubscribe { get; set; }
    }

    public class UnSubscribeViewModel
    {
        [Required]
        [MaxLength(100)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}

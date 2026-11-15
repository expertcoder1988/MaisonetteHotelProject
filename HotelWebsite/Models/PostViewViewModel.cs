using System;

namespace MyBlog.Model
{
    public class PostViewViewModel
    {
        public int PostId { get; set; }

        public DateTime ViewDate { get; set; }

        public int NoOfTimeView { get; set; }

        public string DisplayViewDate
        {
            get
            {
                return ViewDate.ToString("MMM dd, yyyy");
            }
        }
    }
}

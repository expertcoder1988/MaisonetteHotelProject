namespace MyBlog.Model
{
    public class Response
    {
        public Response()
        {
            Status = false;
            Message = string.Empty;
        }

        public bool Status { get; set; }

        public string Message { get; set; }

        public object Data { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MyBlog.Model
{
	public class MaxFileSizeAttribute : ValidationAttribute
	{
		private readonly int _maxFileSize;
		public MaxFileSizeAttribute(int maxFileSize)
		{
			_maxFileSize = maxFileSize;
		}

		public override bool IsValid(object value)
		{
			var file = value as HttpPostedFileBase;
			if (file == null)
			{
				return true;
			}
			return file.ContentLength <= _maxFileSize;
		}

		public override string FormatErrorMessage(string name)
		{
			//return base.FormatErrorMessage(_maxFileSize.ToString());
			return base.FormatErrorMessage(((_maxFileSize / 1024) / 1024).ToString());
		}
	}
}

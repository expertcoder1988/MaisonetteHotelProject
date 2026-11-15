using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelWebsite.Controllers
{
    public class ActivitiesController : Controller
    {
        [Route("~/tourist-attractions-places-to-visit-in-murree", Name = "activitiesmaisonettehotel")]
        // GET: Activities
        public ActionResult Index()
        {
            return View();
        }
    }
}
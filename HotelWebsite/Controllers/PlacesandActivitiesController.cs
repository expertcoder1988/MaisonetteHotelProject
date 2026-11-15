using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelWebsite.Controllers
{
    public class PlacesandActivitiesController : Controller
    {
        [Route("~/places-to-visit-in-naran", Name = "placestovisitinnaran")]
        // GET: PopularActivities
        public ActionResult Index()
        {
            return View();
        }
    }
}
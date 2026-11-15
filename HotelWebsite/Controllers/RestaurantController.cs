using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelWebsite.Controllers
{
    public class RestaurantController : Controller
    {
        [Route("~/spice-waadi-murree-restaurant", Name = "maisonetteRestaurant")]

        // GET: Restaurant

        public ActionResult Index()
        {
            return View();
        }

        [Route("~/the-cloud-walk-cafe-in-murree", Name = "maisonetteCafe")]
        public ActionResult Cafe()
        {
            return View();
        }
    }
}
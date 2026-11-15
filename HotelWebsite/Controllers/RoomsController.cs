using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HotelWebsite.Controllers
{
    public class RoomsController : Controller
    {
        // GET: Rooms
        public ActionResult Index()
        {
            return View();
        }
        [Route("~/executive-room-in-murree", Name = "Executive")]
        public ActionResult Executive()
        {
            return View();
        }
        [Route("~/studio-room-in-murree", Name = "Studio")]
        public ActionResult Studio()
        {
            return View();
        }
        [Route("~/honeymoon-suite-in-murree", Name = "Honeymoonsuite")]
        public ActionResult Honeymoonsuite()
        {
            return View();
        }
        [Route("~/queen-suite-in-murree", Name = "QueenSuite")]
        public ActionResult QueenSuite()
        {
            return View();
        }
        [Route("~/presidential-suite-in-murree", Name = "Presidentialsuite")]
        public ActionResult Presidentialsuite()
        {
            return View();
        }
    }
}
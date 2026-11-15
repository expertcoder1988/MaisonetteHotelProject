using HotelWebsite.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace HotelWebsite.Controllers
{

    public class SearchResultController : Controller
    {
        HotelDBEntities2 HDE = new HotelDBEntities2();
        Booking bi = new Booking();

        // GET: SearchResult
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SearchResults(string CheckIn, string CheckOut, int Adults, int Children, int NoOfRooms)
        {
            try
            {
                using (var db = new HotelDBEntities2())
                {
                    // Convert CheckIn to DateTime
                    string[] sDate = CheckIn.Split('-');
                    string startDateStr = sDate[1] + "/" + sDate[0] + "/" + sDate[2];
                    DateTime startDate = Convert.ToDateTime(startDateStr);

                    // Convert CheckOut to DateTime
                    sDate = CheckOut.Split('-');
                    string endDateStr = sDate[1] + "/" + sDate[0] + "/" + sDate[2];
                    DateTime endDate = Convert.ToDateTime(endDateStr);

                    // Calculate number of days between check-in and check-out
                    int NoOfDays = (endDate - startDate).Days;
                    ViewBag.NoOfDays = NoOfDays;

                    int id = 1;

                    // Call stored procedure
                    IEnumerable<PropertySearch> rowsAffected = db.Database.SqlQuery<PropertySearch>(
                        "exec sp_GetSearchResults {0}, {1}, {2}, {3}, {4}",
                        startDate, endDate, Adults, Children, NoOfRooms).ToList();

                    foreach (var itm in rowsAffected)
                    {
                        int v = VacantRooms(itm.RoomCode, itm.RoomCateCode, CheckIn);
                        int b = BlockedRooms(itm.RoomCode, itm.RoomCateCode, CheckIn, CheckOut);
                        itm.NoOfRooms = itm.NoOfRooms - v - b;

                        if (itm.NoOfRooms < 0)
                        {
                            itm.NoOfRooms = 0;
                        }

                        itm.Id = id;
                        id++;

                        itm.RoomPictures = itm.RoomCateCode.HasValue
                            ? RoomImagesCat(itm.RoomCateCode.Value)
                            : new List<string>();
                    }

                    Session["results"] = rowsAffected;
                    return View(rowsAffected);
                }
            }
            catch (Exception ex)
            {
                return Json(new { Error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        public List<string> RoomImagesCat(int roomCategoryId)
        {
            try
            {
                using (var db = new HotelDBEntities2())
                {
                    var roomPictures = db.RoomPictures
                        .Where(a => a.RoomCode == roomCategoryId && a.Active == true)
                        .Select(a => a.Picture)
                        .ToList();

                    return roomPictures;
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as necessary
                return new List<string>(); // Return an empty list if an exception occurs
            }
        }

        public int VacantRooms(int? roomCode, int? RoomCateCode, string startDate)
        {
            try
            {
                int vacantRooms = 0;
                // var BlockedRooms = 0;
                DateTime _startDate = Convert.ToDateTime(startDate);

                using (var db = new HotelDBEntities2())
                {
                    //TotalRooms = db.RoomNumbers.Count(x => x.RoomCode == roomCode && x.Status == false);

                    vacantRooms = db.Bookings.Count(x => x.RoomCode == roomCode && x.CheckIn == _startDate);
                }

                return vacantRooms; // Content(vacantRooms.ToString());
            }
            catch (Exception ex)
            {
                return 0;// Content(ex.Message);
            }
        }

        public int BlockedRooms(int? roomCode, int? RoomCateCode, string startDate, string endDate)
        {
            try
            {
                // Parse the start and end dates
                string[] formats = { "MM/dd/yyyy", "yyyy-MM-dd" };
                DateTime _startDate = DateTime.ParseExact(startDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);
                DateTime _endDate = DateTime.ParseExact(endDate, formats, CultureInfo.InvariantCulture, DateTimeStyles.None);


                int blockedRooms = 0;

                using (var db = new HotelDBEntities2())
                {
                    // Count rooms that have overlapping blocked dates
                    blockedRooms = db.BlockedRooms.Count(x =>
                        x.CateId == RoomCateCode &&
                        x.Status == true &&
                        (
                            (x.StartDate <= _endDate && x.EndDate >= _startDate) // Overlapping check
                        ));
                }

                return blockedRooms;
            }
            catch (Exception ex)
            {
                // Log the exception as needed
                return 0;
            }
        }

        public ActionResult RoomDetails(int Id)
        {
            try
            {
                // Call methods to fetch details
                var roomDetails = RoomDetail(Id);
                var roomPictures = RoomPictures(Id);

                // You can use ViewBag or a ViewModel to pass data to the view
                ViewBag.RoomDetails = roomDetails;
                ViewBag.RoomPictures = roomPictures;

                return View();
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return RedirectToAction("Error", "Home"); // Redirect to an error page if needed
            }
        }

        public JsonResult RoomPics(int Roomcate)
        {
            try
            {
                //int Id = requestRoom.Id; // HttpContext.Request.Form["Id"];
                //string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities2())
                {
                    var room = (from a in db.RoomPictures
                                where (a.RoomCode == Roomcate)
                                select new
                                {
                                    ImageName = a.Picture
                                }).ToList();

                    ;
                    return Json(room, JsonRequestBehavior.AllowGet);
                }



            }
            catch (Exception ex)
            {
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }
        public string RoomDetail(int Id)
        {
            try
            {
                //int Id = requestRoom.Id; // HttpContext.Request.Form["Id"];
                //string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities2())
                {
                    var room = (from a in db.Rooms
                                join b in db.RoomCategories
                                on a.RoomCategory equals b.Id
                                where (b.Id == Id)
                                select new HotelWebsite.Models.RoomDetails
                                {
                                    Id = a.ID,
                                    RoomCode = a.RoomCode,
                                    RoomCategoryId = a.RoomCategory,
                                    RoomCategory = b.Category,
                                    RoomDescription = a.RoomDescription,
                                    PropertyId = a.propertyID,
                                    BedType = a.BedType,
                                    CheckIn = a.CheckIn,
                                    CheckOut = a.CheckOut,
                                    NoOfRooms = a.NoOfRooms,
                                    Capacity = a.Capacity,
                                    Adult = a.Adult,
                                    NoOfChild = a.NoOfChild,
                                    ChildAge = a.ChildAge,
                                    ShortCode = a.ShortCode,
                                    MinimumPrice = a.MinimumPrice,
                                    MinimumStay = a.MinimumStay,
                                    MaximumStay = a.MaximumStay,
                                    Size = a.Size,
                                    Features = a.Features
                                }).FirstOrDefault();

                    //  TempData["room"] = room;
                    ViewBag.room = room;
                }



                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public string RoomPrice(int Id, DateTime checkin)
        {
            try
            {
                using (var db = new HotelDBEntities2())
                {
                    var roomPrice = (from a in db.RateSlabs
                                     where (a.RoomCategoryCode == Id && (a.StartDate < checkin && a.EndDate > checkin))
                                     select new
                                     {
                                         RoomPrice = a.RoomRate
                                     }).FirstOrDefault();
                    TempData["roomPrice"] = roomPrice;
                }

                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public JsonResult RoomPolicy(int CancellationPolicy)
        {
            try
            {
                using (var db = new HotelDBEntities2())
                {
                    // Fetch data from the database
                    var roomPolicyData = (from a in db.RatePlans
                                          join b in db.CancellationPolicies
                                              on a.CancellationPolicy equals b.Id
                                          where (b.Id == CancellationPolicy && a.Status == true)
                                          select new
                                          {
                                              RatePlanName = a.Name,
                                              CancellationPolicyName = b.Name,
                                              FreeCancelBefore = b.PeriodForFreeCharge,
                                              Breakfast = a.Breakfast.HasValue && a.Breakfast.Value ? "Included" : "Not Included", // Map directly to string
                                              ChargedForNoShow = b.ChargedForNoShow,
                                              GuestChargedForCancellation = b.GuestChargedForCancellation,
                                              HowlongFreeOfCharge = b.HowlongFreeOfCharge
                                          }).ToList();

                    // Process data in memory
                    var roomPolicy = roomPolicyData.Select(item => new
                    {
                        item.RatePlanName,
                        item.CancellationPolicyName,
                        item.FreeCancelBefore,
                        Breakfast = item.Breakfast, // Already processed as "Included" or "Not Included"
                        ChargedForNoShow = item.ChargedForNoShow == 1 ? "Same as the cancellation fee" : "100% of the total price",
                        GuestChargedForCancellation = GetGuestChargedForCancellation(item.GuestChargedForCancellation ?? 0),
                        HowlongFreeOfCharge = GetHowLongFreeOfCharge(item.HowlongFreeOfCharge ?? 0)
                    }).ToList();

                    return Json(roomPolicy, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        // Helper method for Breakfast
        private string GetBreakfastString(bool breakfast)
        {
            return breakfast ? "Included" : "Not Included";
        }


        // Helper method for GuestChargedForCancellation
        private string GetGuestChargedForCancellation(int guestCharged)
        {
            switch (guestCharged)
            {
                case 1:
                    return "The cost of the first night";
                case 2:
                    return "50% of the total price";
                case 3:
                    return "100% of the total price";
                default:
                    return "Not specified";
            }
        }

        // Helper method for HowlongFreeOfCharge
        private string GetHowLongFreeOfCharge(int howLong)
        {
            switch (howLong)
            {
                case 1:
                    return "until 6:00PM on the day of arrival";
                case 2:
                    return "until 2:00PM on the day of arrival";
                case 3:
                    return "until 1 day before arrival";
                case 4:
                    return "until 2 days before arrival";
                case 5:
                    return "until 3 days before arrival";
                case 6:
                    return "until 5 days before arrival";
                case 7:
                    return "until 7 days before arrival";
                case 8:
                    return "until 14 days before arrival";
                case 9:
                    return "until 21 days before arrival";
                case 10:
                    return "until 28 days before arrival";
                case 11:
                    return "until 30 days before arrival";
                case 12:
                    return "until 42 days before arrival";
                case 13:
                    return "until 60 days before arrival";
                default:
                    return "Not specified";
            }
        }


        public ActionResult RoomPolicyDetails()
        {
            // Fetch all policy details from the database using HDE
            List<CancellationPolicy> policies = HDE.CancellationPolicies.ToList();

            // Serialize the list of policies to JSON
            string policiesJson = JsonConvert.SerializeObject(policies);

            // Pass the JSON data to the ViewBag
            ViewBag.PoliciesJson = policiesJson;

            // Return the view, passing policies list if needed for other data bindings
            return View(policies);
        }


        public string RoomPictures(int Id)
        {
            try
            {
                // int Id = requestRoom.Id;
                // DateTime checkin = requestRoom.Checkin;
                //  string jsonString = "";
                // var responseBasicProfile = new ResponsePropertySearch();
                using (var db = new HotelDBEntities2())
                {
                    var roomPictures = (from a in db.RoomPictures
                                        join b in db.Rooms
                                        on a.RoomCode equals b.RoomCategory
                                        where (b.RoomCategory == Id && a.Active == true)
                                        select new Pictures
                                        {
                                            Picture = a.Picture
                                        }).ToList();

                    TempData["roomPictures"] = roomPictures;

                    if (roomPictures.Any())
                    {
                        Session["roomPictures"] = roomPictures.Select(rp => rp.Picture).ToList();
                        return "Room pictures fetched successfully.";
                    }
                    else
                    {
                        return "No room pictures found for the given category.";
                    }
                }

                // return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public ActionResult PaymentError(string pp_ResponseCode, string pp_ResponseMessage)
        {
            try
            {
                TempData["pp_ResponseMessage"] = pp_ResponseMessage;
                TempData["pp_ResponseCode"] = pp_ResponseCode;
                return View();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public ActionResult Thankyou(string billReference, string Total)
        {
            try
            {
                string confirmationId = "";
                string pp_ResponseCode = Request.Form["pp_ResponseCode"];
                string pp_ResponseMessage = Request.Form["pp_ResponseMessage"];

                if (Request.Form["pp_BillReference"] != null)
                {
                    confirmationId = Request.Form["pp_BillReference"];
                }
                else
                {
                    confirmationId = billReference;
                }

                if (pp_ResponseCode == "000" || pp_ResponseCode == null)
                {
                    var _booking = (from obj in HDE.Bookings
                                    join b in HDE.RoomCategories
                                     on obj.RoomCatCode equals b.Id
                                    where (obj.ConfirmationNo == confirmationId)
                                    select new FinalBooking
                                    {
                                        BookingID = obj.BookingID,
                                        PaxTitle = obj.PaxTitle,
                                        PaxFName = obj.PaxFName,
                                        PaxLName = obj.PaxLName,
                                        NoOfRooms = obj.NoOfRooms,
                                        RoomCode = obj.RoomCode,
                                        RoomCatCode = obj.RoomCatCode,
                                        RoomCategory = b.Category,
                                        RoomNum = obj.RoomNum,
                                        PaxEmail = obj.PaxEmail,
                                        PaxPhoneNumber = obj.PaxPhoneNumber,
                                        PaxDescription = obj.PaxDescription,
                                        Adults = obj.Adults,
                                        Children = obj.Children,
                                        CheckIn = obj.CheckIn,
                                        CheckOut = obj.CheckOut,
                                        RoomRate = obj.RoomRate,
                                        HotelCode = obj.HotelCode,
                                        ConfirmationNo = obj.ConfirmationNo,
                                        NoofDay = obj.NoofDay,
                                        TotalRoomRate = obj.TotalRoomRate
                                    }).ToList();

                    //RoomCates = RoomCates.Remove(0, 1);
                    //TempData["RoomCates"] = RoomCates;
                    //TempData["noOfDay"] = noOfDay;
                    Session["booking"] = _booking;
                    Session["confirmationId"] = confirmationId;




                    using (var db = new HotelDBEntities2())
                    {
                        var myData = db.Bookings.Where(x => x.ConfirmationNo == confirmationId).ToList();
                        myData.ForEach(m => m.BookingStatus = "Booked");
                        db.SaveChanges();
                    }
                    // Extract the value before the LINQ query
                    var roomCatCode = _booking[0].RoomCatCode;

                    var roomcategory = Convert.ToInt32(roomCatCode);
                    List<string> roomImagePath = GetRoomImagePaths(roomcategory);
                    Session["RoomImagePaths"] = roomImagePath;

                    ViewBag.RoomImages = roomImagePath;
                    // Modify the LINQ query to use the extracted value
                    var rm = (from obj in HDE.RoomPictures
                              join r in HDE.Rooms on obj.RoomCode equals r.RoomCategory
                              where obj.RoomCode == roomCatCode && obj.Active == true
                              select obj.Picture).FirstOrDefault();
                    // Assuming rm is the image filename, set it in ViewBag
                    ViewBag.RoomPicture = rm;
                    Session["ImagePath"] = rm;
                    TimeSpan t = Convert.ToDateTime(_booking[0].CheckOut) - Convert.ToDateTime(_booking[0].CheckIn);
                    int noOfDay = (int)t.TotalDays;
                    var category = _booking[0].RoomCategory;

                    try
                    {
                        int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                        StringBuilder htmlString = new StringBuilder();
                        htmlString.Append("<html lang=\"en\">");
                        htmlString.Append("<head>");
                        htmlString.Append("<meta charset=\"UTF-8\">");
                        htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                        htmlString.Append("<title>Booking Confirmation</title>");
                        htmlString.Append("<style>");
                        htmlString.Append("body {");
                        htmlString.Append("font-family: Arial, sans-serif;");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("padding: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".email-container {");
                        htmlString.Append("width: 100%;");
                        htmlString.Append("max-width: 650px;");
                        htmlString.Append("margin: 0 auto;");
                        htmlString.Append("background-color: #ffffff;");
                        htmlString.Append("border-radius: 8px;");
                        htmlString.Append("overflow: hidden;");
                        htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                        htmlString.Append("}");
                        htmlString.Append(".header {");
                        htmlString.Append("background-color: #F20008;");
                        htmlString.Append("color: white;");
                        htmlString.Append("}");
                        htmlString.Append(".header h1 {");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".content {");
                        htmlString.Append("padding: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".content h2 {");
                        htmlString.Append("color: #333;");
                        htmlString.Append("}");
                        htmlString.Append(".details {");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("background-color: #f9f9f9;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("}");
                        htmlString.Append(".details p {");
                        htmlString.Append("margin: 5px 0;");
                        htmlString.Append("}");
                        htmlString.Append(".footer {");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("text-align: center;");
                        htmlString.Append("font-size: 12px;");
                        htmlString.Append("color: #777;");
                        htmlString.Append("}");
                        htmlString.Append(".footer a {");
                        htmlString.Append("color: #0066cc;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("}");
                        htmlString.Append(".button {");
                        htmlString.Append("background-color: #0066cc;");
                        htmlString.Append("color: white;");
                        htmlString.Append("padding: 10px 20px;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("display: inline-block;");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 20px;");
                        htmlString.Append("}");
                        htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                        htmlString.Append("@media (max-width: 768px) {");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                        htmlString.Append("}");
                        htmlString.Append("}");
                        htmlString.Append("</style>");

                        htmlString.Append("</head>");

                        htmlString.Append("<body>");
                        htmlString.Append("<div class=\"email-container\">");


                        htmlString.Append("<div class=\"header\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td class=\"responsive-padding\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:22px 0 0 0\"><a href=\"https://maisonettefirhillmurree.com/\" style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                        htmlString.Append("<img alt=\"maisonette\" height=\"60\" src=\"https://maisonettefirhillmurree.com/Content/paradise/img/massionatewhite.png\" style=\"display:block\" width=\"144\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                        htmlString.Append("</a></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#ffffff\">");
                        htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"24\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");

                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td align=\"center\" width=\"100%\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                        htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                        htmlString.Append("Thanks <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                        htmlString.Append("Your booking in maisonette Hotel is confirmed.");
                        htmlString.Append("</h1>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Reservation details</td>");

                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"16\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding: 0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-in");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(from 15:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-out");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(until 12:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Your reservation");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("You booked for");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + TotalGuests + " Adults" + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Booking Date");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<address style=\"font-style:normal\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToString("M/d/yyyy") + "");
                        htmlString.Append("</address>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Phone");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Email");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxEmail + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Room");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].RoomCategory + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"32\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                        htmlString.Append("Price details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"16\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                        htmlString.Append("" + _booking[0].RoomCategory + ", Non-Smoking.");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("16 % GST is included.");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("To confirm your booking please make a payment of 50% of the total amount within 1 hour and share a screenshot via WhatsApp");
                        htmlString.Append("<a href=\"https://wa.me/+923111122168? text=Hi! Maisonette Hotel.\" target=\"_blank\">");
                        htmlString.Append("<b>+923111122168</b>");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Yum Hospitality & Holdings");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Bank Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("HABIB BANK LIMITED PAKISTAN (H.B.L)");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Number");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("001517900992803");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("IBAN");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("PK32 HABB 0001517900992803");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");



                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("Total price");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");




                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                        htmlString.Append("Important Details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is  2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                        htmlString.Append("</ul>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");
                        htmlString.Append("</body>");

                        htmlString.Append("</html>");
                        MailMessage mm = new MailMessage();
                        mm.IsBodyHtml = true;
                        mm.To.Add("info@maisonettefirhillmurree.com");

                        mm.From = new MailAddress("info@maisonettefirhillmurree.com");
                        mm.Subject = "Order Confirmation: " + confirmationId;
                        mm.Body = htmlString.ToString();
                        SmtpClient smc = new SmtpClient();
                        smc.Host = "mail.maisonettefirhillmurree.com";
                        smc.Port = 587;
                        smc.UseDefaultCredentials = false;
                        smc.EnableSsl = false;
                        smc.Credentials = new System.Net.NetworkCredential("info@maisonettefirhillmurree.com", "Wc1n5!u98");
                        smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                        System.Net.ServicePointManager.ServerCertificateValidationCallback =
                        delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
                        {
                            return true;  // Bypasses the SSL certificate validation
                        };

                        smc.Send(mm);
                    }
                    catch (Exception ex)
                    {
                        TempData["error"] = ex.Message;
                    }

                    try
                    {
                        int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                        StringBuilder htmlString = new StringBuilder();
                        htmlString.Append("<html lang=\"en\">");
                        htmlString.Append("<head>");
                        htmlString.Append("<meta charset=\"UTF-8\">");
                        htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                        htmlString.Append("<title>Booking Confirmation</title>");
                        htmlString.Append("<style>");
                        htmlString.Append("body {");
                        htmlString.Append("font-family: Arial, sans-serif;");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("padding: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".email-container {");
                        htmlString.Append("width: 100%;");
                        htmlString.Append("max-width: 650px;");
                        htmlString.Append("margin: 0 auto;");
                        htmlString.Append("background-color: #ffffff;");
                        htmlString.Append("border-radius: 8px;");
                        htmlString.Append("overflow: hidden;");
                        htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                        htmlString.Append("}");
                        htmlString.Append(".header {");
                        htmlString.Append("background-color: #F20008;");
                        htmlString.Append("color: white;");
                        htmlString.Append("}");
                        htmlString.Append(".header h1 {");
                        htmlString.Append("margin: 0;");
                        htmlString.Append("}");
                        htmlString.Append(".content {");
                        htmlString.Append("padding: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".content h2 {");
                        htmlString.Append("color: #333;");
                        htmlString.Append("}");
                        htmlString.Append(".details {");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("background-color: #f9f9f9;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("}");
                        htmlString.Append(".details p {");
                        htmlString.Append("margin: 5px 0;");
                        htmlString.Append("}");
                        htmlString.Append(".footer {");
                        htmlString.Append("background-color: #f4f4f4;");
                        htmlString.Append("padding: 15px;");
                        htmlString.Append("text-align: center;");
                        htmlString.Append("font-size: 12px;");
                        htmlString.Append("color: #777;");
                        htmlString.Append("}");
                        htmlString.Append(".footer a {");
                        htmlString.Append("color: #0066cc;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("}");
                        htmlString.Append(".button {");
                        htmlString.Append("background-color: #0066cc;");
                        htmlString.Append("color: white;");
                        htmlString.Append("padding: 10px 20px;");
                        htmlString.Append("text-decoration: none;");
                        htmlString.Append("border-radius: 5px;");
                        htmlString.Append("display: inline-block;");
                        htmlString.Append("margin-top: 20px;");
                        htmlString.Append("}");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 20px;");
                        htmlString.Append("}");
                        htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                        htmlString.Append("@media (max-width: 768px) {");
                        htmlString.Append(".responsive-padding {");
                        htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                        htmlString.Append("}");
                        htmlString.Append("}");
                        htmlString.Append("</style>");

                        htmlString.Append("</head>");

                        htmlString.Append("<body>");
                        htmlString.Append("<div class=\"email-container\">");


                        htmlString.Append("<div class=\"header\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td class=\"responsive-padding\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:22px 0 0 0\"><a href=\"https://maisonettefirhillmurree.com/\" style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                        htmlString.Append("<img alt=\"maisonette\" height=\"60\" src=\"https://maisonettefirhillmurree.com/Content/paradise/img/massionatewhite.png\" style=\"display:block\" width=\"144\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                        htmlString.Append("</a></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#ffffff\">");
                        htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"24\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");

                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td align=\"center\" width=\"100%\">");
                        htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"center\">");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                        htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                        htmlString.Append("Thanks <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                        htmlString.Append("Your booking in maisonette Hotel is confirmed.");
                        htmlString.Append("</h1>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"20\">");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Reservation details</td>");

                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"16\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding: 0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                        htmlString.Append("<tbody>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-in");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(from 15:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Check-out");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                        //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                        //htmlString.Append("(until 12:00)");
                        //htmlString.Append("</span>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Your reservation");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("You booked for");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + TotalGuests + " Adults" + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Booking Date");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<address style=\"font-style:normal\">");
                        htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToString("M/d/yyyy") + "");
                        htmlString.Append("</address>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Phone");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Email");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                        htmlString.Append("" + _booking[0].PaxEmail + "");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");


                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                        htmlString.Append("Room");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("" + _booking[0].RoomCategory + "");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");

                        htmlString.Append("<td height=\"32\">");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");



                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                        htmlString.Append("Price details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"16\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                        htmlString.Append("" + _booking[0].RoomCategory + ", Non-Smoking.");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("16 % GST is included.");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                        htmlString.Append("To confirm your booking please make a payment of 50% of the total amount within 1 hour and share a screenshot via WhatsApp");
                        htmlString.Append("<a href=\"https://wa.me/+923111122168? text=Hi! Maisonette Hotel.\" target=\"_blank\">");
                        htmlString.Append("<b>+923111122168</b>");
                        htmlString.Append("</a>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Yum Hospitality & Holdings");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Bank Name");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("HABIB BANK LIMITED PAKISTAN (H.B.L)");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("Account Number");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("001517900992803");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");

                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("IBAN");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("PK32 HABB 0001517900992803");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");



                        htmlString.Append("<tr>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("Total price");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                        htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");




                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"padding:0px 8px\">");
                        htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                        htmlString.Append("<td>");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                        htmlString.Append("Important Details");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                        htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                        htmlString.Append("<tbody>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                        htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is  2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                        htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                        htmlString.Append("</ul>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("<tr>");
                        htmlString.Append("<td height=\"32\"></td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");

                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</td>");
                        htmlString.Append("</tr>");
                        htmlString.Append("</tbody>");
                        htmlString.Append("</table>");
                        htmlString.Append("</div>");
                        htmlString.Append("</body>");

                        htmlString.Append("</html>");

                        MailMessage mm = new MailMessage();
                        mm.IsBodyHtml = true;
                        mm.To.Add("info@maisonettefirhillmurree.com");

                        mm.From = new MailAddress("info@maisonettefirhillmurree.com");
                        mm.Subject = "Order Confirmation: " + confirmationId;
                        mm.Body = htmlString.ToString();
                        SmtpClient smc = new SmtpClient();
                        smc.Host = "mail.maisonettefirhillmurree.com";
                        smc.Port = 587;
                        smc.UseDefaultCredentials = false;
                        smc.EnableSsl = false;
                        smc.Credentials = new System.Net.NetworkCredential("info@maisonettefirhillmurree.com", "Wc1n5!u98");
                        smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                        System.Net.ServicePointManager.ServerCertificateValidationCallback =
                        delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
                        {
                            return true;  // Bypasses the SSL certificate validation
                        };

                        smc.Send(mm);

                    }
                    catch (Exception ex)
                    {
                        TempData["error"] = ex.Message;
                    }


                    if (_booking[0].PaxEmail != "")
                    {
                        try
                        {
                            int? TotalGuests = _booking[0].Adults + _booking[0].Children;
                            StringBuilder htmlString = new StringBuilder();


                            htmlString.Append("<html lang=\"en\">");
                            htmlString.Append("<head>");
                            htmlString.Append("<meta charset=\"UTF-8\">");
                            htmlString.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                            htmlString.Append("<title>Booking Confirmation</title>");
                            htmlString.Append("<style>");
                            htmlString.Append("body {");
                            htmlString.Append("font-family: Arial, sans-serif;");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("padding: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".email-container {");
                            htmlString.Append("width: 100%;");
                            htmlString.Append("max-width: 650px;");
                            htmlString.Append("margin: 0 auto;");
                            htmlString.Append("background-color: #ffffff;");
                            htmlString.Append("border-radius: 8px;");
                            htmlString.Append("overflow: hidden;");
                            htmlString.Append("box-shadow: 0 0 15px rgba(0, 0, 0, 0.1);");
                            htmlString.Append("}");
                            htmlString.Append(".header {");
                            htmlString.Append("background-color: #F20008;");
                            htmlString.Append("color: white;");
                            htmlString.Append("}");
                            htmlString.Append(".header h1 {");
                            htmlString.Append("margin: 0;");
                            htmlString.Append("}");
                            htmlString.Append(".content {");
                            htmlString.Append("padding: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".content h2 {");
                            htmlString.Append("color: #333;");
                            htmlString.Append("}");
                            htmlString.Append(".details {");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("background-color: #f9f9f9;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("}");
                            htmlString.Append(".details p {");
                            htmlString.Append("margin: 5px 0;");
                            htmlString.Append("}");
                            htmlString.Append(".footer {");
                            htmlString.Append("background-color: #f4f4f4;");
                            htmlString.Append("padding: 15px;");
                            htmlString.Append("text-align: center;");
                            htmlString.Append("font-size: 12px;");
                            htmlString.Append("color: #777;");
                            htmlString.Append("}");
                            htmlString.Append(".footer a {");
                            htmlString.Append("color: #0066cc;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("}");
                            htmlString.Append(".button {");
                            htmlString.Append("background-color: #0066cc;");
                            htmlString.Append("color: white;");
                            htmlString.Append("padding: 10px 20px;");
                            htmlString.Append("text-decoration: none;");
                            htmlString.Append("border-radius: 5px;");
                            htmlString.Append("display: inline-block;");
                            htmlString.Append("margin-top: 20px;");
                            htmlString.Append("}");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 20px;");
                            htmlString.Append("}");
                            htmlString.Append("/* Media query for mobile (max-width: 768px) */");
                            htmlString.Append("@media (max-width: 768px) {");
                            htmlString.Append(".responsive-padding {");
                            htmlString.Append("padding: 0px 5px; /* Remove padding for mobile view */");
                            htmlString.Append("}");
                            htmlString.Append("}");
                            htmlString.Append("</style>");

                            htmlString.Append("</head>");

                            htmlString.Append("<body>");
                            htmlString.Append("<div class=\"email-container\">");


                            htmlString.Append("<div class=\"header\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"min-width:100%\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td class=\"responsive-padding\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:144px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:22px 0 0 0\"><a href=\"https://maisonettefirhillmurree.com/\" style=\"display:inline-block;font-family:Arial,Helvetica,sans-serif;font-size:22px;color:#ffffff;font-weight:600;text-decoration:none\" target=\"_blank\">");
                            htmlString.Append("<img alt=\"maisonette\" height=\"60\" src=\"https://maisonettefirhillmurree.com/Content/paradise/img/massionatewhite.png\" style=\"display:block\" width=\"144\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\">");
                            htmlString.Append("</a></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"right\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:480px\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"padding:40px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:14px;line-height:20px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#ffffff\">");
                            htmlString.Append("Booking Ref : <strong>" + _booking[0].ConfirmationNo + "</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"24\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");

                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td align=\"center\" width=\"100%\">");
                            htmlString.Append("<table align=\"center\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"center\">");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding:0 8px\">");
                            htmlString.Append("<h1 style=\"font-size:22px;line-height:32px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;margin:0\">");
                            htmlString.Append("Thanks <span class=\"il\">" + _booking[0].PaxTitle + " " + _booking[0].PaxFName + " " + _booking[0].PaxLName + "</span>! ");
                            htmlString.Append("Your booking in maisonette Hotel is confirmed.");
                            htmlString.Append("</h1>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"20\">");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">Reservation details</td>");

                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"16\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding: 0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");

                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");

                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");

                            htmlString.Append("<tbody>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-in");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckIn).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(from 15:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Check-out");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].CheckOut).ToShortDateString() + "");
                            //htmlString.Append("<span style=\"color:#6b6b6b;white-space:nowrap\">");
                            //htmlString.Append("(until 12:00)");
                            //htmlString.Append("</span>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Your reservation");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].NoOfRooms + " Room" + "," + _booking[0].NoofDay + " Nights");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("You booked for");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + TotalGuests + " Adults" + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Booking Date");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<address style=\"font-style:normal\">");
                            htmlString.Append("" + Convert.ToDateTime(_booking[0].ReservationDate).ToString("M/d/yyyy") + "");
                            htmlString.Append("</address>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Phone");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a  style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxPhoneNumber + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Email");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<a style=\"color:#0071c2\" target=\"_blank\">");
                            htmlString.Append("" + _booking[0].PaxEmail + "");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");


                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#6b6b6b;padding:16px\">");
                            htmlString.Append("Room");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("" + _booking[0].RoomCategory + "");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");

                            htmlString.Append("<td height=\"32\">");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");



                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:30px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 8px\">");
                            htmlString.Append("Price details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"16\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"border-collapse:separate\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"border:1px solid #e6e6e6;border-radius:4px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:8px 16px\">");
                            htmlString.Append("" + _booking[0].RoomCategory + ", Non-Smoking.");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                            htmlString.Append("16 % GST is included.");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:0 16px 8px\">");
                            htmlString.Append("To confirm your booking please make a payment of 50% of the total amount within 1 hour and share a screenshot via WhatsApp");
                            htmlString.Append("<a href=\"https://wa.me/+923111122168? text=Hi! Maisonette Hotel.\" target=\"_blank\">");
                            htmlString.Append("<b>+923111122168</b>");
                            htmlString.Append("</a>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Account Name");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Yum Hospitality & Holdings");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Bank Name");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("HABIB BANK LIMITED PAKISTAN (H.B.L)");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("Account Number");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("001517900992803");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");

                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("IBAN");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("PK32 HABB 0001517900992803");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");



                            htmlString.Append("<tr>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:622px;border-top:1px solid #e6e6e6\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"background-color:#f5f5f5\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:224px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("Total price");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:382px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"right\" style=\"font-size:16px;line-height:24px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding:16px\">");
                            htmlString.Append("<strong style=\"white-space:nowrap\">" + _booking[0].TotalRoomRate + " PKR</strong>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");




                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" style=\"max-width:640px\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"padding:0px 8px\">");
                            htmlString.Append("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" valign=\"top\" width=\"48\"><img alt=\"\" height=\"auto\" src=\"../Growbiz/info.png\" style=\"display:block\" width=\"32\" data-image-whitelisted=\"\" class=\"CToWUd\" data-bit=\"iit\"></td>");
                            htmlString.Append("<td>");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"font-size:30px;line-height:28px;font-weight:bold;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333;padding-top:2px\">");
                            htmlString.Append("Important Details");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td align=\"left\" style=\"padding-top:8px\">");
                            htmlString.Append("<table align=\"left\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\" role=\"presentation\" width=\"100%\">");
                            htmlString.Append("<tbody>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td style=\"font-size:16px;line-height:24px;font-weight:normal;font-family:BlinkMacSystemFont,-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;color:#333333\">");
                            htmlString.Append("<ul align=\"left\" style=\"margin:0;margin-left:24px;padding:0\" type=\"disc\">");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-in time is  2 PM, guest before 2PM will be entertained in case of room availability.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Check-out time is 12(Noon), Guest will be charged fee after this time.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Any damage to the property will be charged by the guest.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Pets are not allowed in the property.</li>");
                            htmlString.Append("<li style=\"margin:0;padding:4px 0\">Smoking is not allowed in the rooms.</li>");
                            htmlString.Append("</ul>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("<tr>");
                            htmlString.Append("<td height=\"32\"></td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");

                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</tbody>");
                            htmlString.Append("</table>");
                            htmlString.Append("</div>");
                            htmlString.Append("</body>");

                            htmlString.Append("</html>");

                            MailMessage mm = new MailMessage();
                            mm.IsBodyHtml = true;
                            mm.To.Add(_booking[0].PaxEmail);

                            mm.From = new MailAddress("info@maisonettefirhillmurree.com");
                            mm.Subject = "Order Confirmation: " + confirmationId;
                            mm.Body = htmlString.ToString();
                            SmtpClient smc = new SmtpClient();
                            smc.Host = "mail.maisonettefirhillmurree.com";
                            smc.Port = 587;
                            smc.UseDefaultCredentials = false;
                            smc.EnableSsl = false;
                            smc.Credentials = new System.Net.NetworkCredential("info@maisonettefirhillmurree.com", "Wc1n5!u98");
                            smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                            System.Security.Cryptography.X509Certificates.X509Chain chain,
                            System.Net.Security.SslPolicyErrors sslPolicyErrors)
                            {
                                return true;  // Bypasses the SSL certificate validation
                            };

                            smc.Send(mm);


                        }
                        catch (Exception ex)
                        {
                            TempData["error"] = ex.Message;
                        }

                    }

                    return View();
                }
                else
                {
                    return RedirectToAction("PaymentError", "SearchResult", new { @pp_ResponseCode = pp_ResponseCode, @pp_ResponseMessage = pp_ResponseMessage });
                }



            }
            catch (Exception ex)
            {

                return Json(new { status = "error", message = ex.ToString(), JsonRequestBehavior.AllowGet });
            }
        }


        [HttpPost]
        public ActionResult Thankyou(FormCollection frm)
        {
            try
            {

                string PaxTitle = Request.Form["PaxTitle"];
                string PaxFName = Request.Form["PaxFName"];
                string PaxLName = Request.Form["PaxLName"];
                string CheckIn = Convert.ToString(Request.Form["CheckIn"]);
                string CheckOut = Convert.ToString(Request.Form["CheckOut"]);
                int RoomCatCode = 0;  //Convert.ToInt32(Request.Form["RoomCatCode"]);
                string PaxPhoneNumber = Request.Form["PaxPhoneNumber"];
                string PaxEmail = Request.Form["PaxEmail"];
                string PaxDescription = Request.Form["PaxDescription"];
                int Adults = Convert.ToInt32(Request.Form["Adults"]);
                int Children = Convert.ToInt32(Request.Form["Children"]);
                int NoOfRooms = Convert.ToInt32(Request.Form["hdTotalRooms"]);
                int RoomRate = 0; // Convert.ToInt32(Request.Form["RoomRate"]);
                int TotalAmount = Convert.ToInt32(Request.Form["hdTotalAmount"]);
                string RoomCatCodes = Request.Form["RoomCatCode"];
                string RoomCateRoomNums = Request.Form["RoomCateRoomNums"];
                string RatePlanIDs = Request.Form["RatePlanIDs"];
                string RoomRates = Request.Form["RoomRates"];

                string[] _CheckIn = CheckIn.Split('-');
                string[] _CheckOut = CheckOut.Split('-');

                CheckIn = _CheckIn[1] + "/" + _CheckIn[0] + "/" + _CheckIn[2];
                CheckOut = _CheckOut[1] + "/" + _CheckOut[0] + "/" + _CheckOut[2];

                if (Adults == 0)
                {
                    Adults = 1;
                }

                var roomcategory = Convert.ToInt32(RoomCatCodes);
                List<string> roomImagePath = GetRoomImagePaths(roomcategory);
                Session["RoomImagePaths"] = roomImagePath;
                ViewBag.RoomImages = roomImagePath;

                SaveBooking(PaxTitle, PaxFName, PaxLName, Convert.ToDateTime(CheckIn), Convert.ToDateTime(CheckOut), RoomCatCode, PaxPhoneNumber, PaxEmail, PaxDescription, Adults,
                    Children, NoOfRooms, RoomRate, TotalAmount, RoomCatCodes, RoomCateRoomNums, RatePlanIDs, RoomRates);




                //// Retrieve room pictures from Session
                //var roomPictures1 = Session["roomPictures"] as List<string>;
                //if (roomPictures1 != null && roomPictures1.Any())
                //{
                //    ViewBag.RoomPicture = roomPictures1.First(); // Use the first picture for simplicity
                //}
                //else
                //{
                //    ViewBag.RoomPicture = "default-image.jpg"; // Set a default if no image is found
                //}

                return View();
            }
            catch (Exception ex)
            {

                return Json(new { status = "error", message = ex.ToString() });
            }
        }
        public ActionResult Reservation(DateTime checkIn, DateTime checkOut, int Adults, int Children, int NoOfRooms, int RoomRate, int TotalAmount,
                                 string RoomCatCodes, string Quatntity, string RatePlanIDs, string RoomRates, decimal TotalTax)
        {
            var db = new HotelDBEntities2();

            var roomcategory = Convert.ToInt32(RoomCatCodes);
            // var roomCategoryDetails = db.RoomCategories.Where(x => x.Id == roomcategory).FirstOrDefault();
            //  ViewBag.RoomCategory = db.RoomPictures.Where(x => x.RoomCode == roomcategory).FirstOrDefault();
            // Fetch images for the room category
            var roomImages = db.RoomPictures
                .Where(x => x.RoomCode == roomcategory && x.Active == true)
                .Select(x => x.Picture)
                .ToList();

            string imagePathPrefix = "~/uploads/";
            List<string> roomImagePaths = roomImages.Select(image => imagePathPrefix + image).ToList();

            List<string> roomImagePath = GetRoomImagePaths(roomcategory);
            Session["RoomImagePaths"] = roomImagePath;
            ViewBag.RoomImages = roomImagePath;

            // Assuming RoomCatCodes is a string
            // Query the database to get the category name
            var roomCategoryName = (from category in db.RoomCategories
                                    where category.Id == roomcategory
                                    select category.Category).FirstOrDefault();

            // Pass the room category name to ViewBag
            ViewBag.RoomCategoryName = roomCategoryName;

            // Pass data to the View
            ViewBag.RoomImages = roomImagePaths;
            ViewBag.Category = db.RoomCategories.Where(x => x.Id == roomcategory).FirstOrDefault();
            // ViewBag.RoomCategory = roomCategoryDetails;
            ViewBag.checkIn = checkIn;
            ViewBag.checkOut = checkOut;
            ViewBag.Adults = Adults;
            ViewBag.Children = Children;
            ViewBag.NoOfRooms = NoOfRooms;
            ViewBag.RoomRate = RoomRate;
            ViewBag.TotalAmount = TotalAmount;
            ViewBag.RoomCatCodes = RoomCatCodes;
            ViewBag.RoomCateRoomNums = Quatntity;
            ViewBag.RatePlanIDs = RatePlanIDs;
            ViewBag.RoomRates = RoomRates;
            ViewBag.TotalTax = TotalTax;
            // Return the view
            return View();
        }

        [HttpPost]
        public ActionResult ReservationPost()
        {

            string checkIn = Convert.ToString(Request.Form["CheckIn"]);
            string checkOut = Convert.ToString(Request.Form["CheckOut"]);
            int RoomCatCode = 0;  //Convert.ToInt32(Request.Form["RoomCatCode"]);
            int Adults = Convert.ToInt32(Request.Form["Adults"]);
            int Children = Convert.ToInt32(Request.Form["Children"]);
            int NoOfRooms = Convert.ToInt32(Request.Form["hdTotalRooms"]);
            int RoomRate = 0; // Convert.ToInt32(Request.Form["RoomRate"]);
            int TotalAmount = Convert.ToInt32(Request.Form["hdTotalAmount"]);
            string RoomCatCodes = Request.Form["RoomCatCode"];
            string RoomCateRoomNums = Request.Form["RoomCateRoomNums"];
            string RatePlanIDs = Request.Form["RatePlanIDs"];
            string RoomRates = Request.Form["RoomRates"];

            int RoomCategoryCode = Convert.ToInt32(RoomCateRoomNums);
            decimal totalTax = CalculateTotalTax(TotalAmount, RoomCategoryCode);

            string[] _CheckIn = checkIn.Split('-');
            string[] _CheckOut = checkOut.Split('-');

            checkIn = _CheckIn[1] + "/" + _CheckIn[0] + "/" + _CheckIn[2];
            checkOut = _CheckOut[1] + "/" + _CheckOut[0] + "/" + _CheckOut[2];

            DateTime CheckIn = Convert.ToDateTime(checkIn);
            DateTime CheckOut = Convert.ToDateTime(checkOut);
            return RedirectToAction("Reservation", "SearchResult", new { @checkIn = CheckIn, @checkOut = CheckOut, @Adults = Adults, @Children = Children, @NoOfRooms = NoOfRooms, @RoomRate = RoomRate, @TotalAmount = TotalAmount, @RoomCatCodes = RoomCatCodes, @RoomCateRoomNums = RoomCateRoomNums, @RatePlanIDs = RatePlanIDs, @RoomRates = RoomRates, @TotalTax = totalTax });
        }

        [HttpPost]
        public ActionResult ReservationPost2()
        {
            try
            {
                // Input validation
                string checkIn = Convert.ToString(Request.Form["CheckIn"]);
                string checkOut = Convert.ToString(Request.Form["CheckOut"]);
                
                if (string.IsNullOrEmpty(checkIn) || string.IsNullOrEmpty(checkOut))
                {
                    TempData["Error"] = "Check-in and Check-out dates are required.";
                    return RedirectToAction("SearchResults");
                }
                
                int Adults = Convert.ToInt32(Request.Form["Adults"]);
                int Children = Convert.ToInt32(Request.Form["Children"]);

            //int RoomRate = 0; // Convert.ToInt32(Request.Form["RoomRate"]);
            int NoOfRooms = Convert.ToInt32(Request.Form["hdTotalRooms"]);
            string Quatntity = Request.Form["txtQuatntity"];
            int TotalAmount  = Convert.ToInt32(Request.Form["hdTotalAmount"]);
            string RoomCatCodes = Request.Form["RoomCate"];
            string RatePlanIDs = Request.Form["RatePlanIDs"];
            string RoomRates = Request.Form["RoomRates"];

            // ✅ FIXED - Use values directly (JavaScript already calculated correctly)
            // No need to multiply again - JavaScript Calculate function already sends correct values
            // NoOfRooms and TotalAmount are already correct from the form
            // Fetch image data here
            List<string> roomImages = RoomImagesCat(Convert.ToInt32(RoomCatCodes)); // Get the image list based on RoomCatCodes

            // Convert the list of images into a comma-separated string or pass as a list
            string roomImagesString = string.Join(",", roomImages); // or pass the List<string> if preferred
            string[] _CheckIn = checkIn.Split('-');
            string[] _CheckOut = checkOut.Split('-');

            checkIn = _CheckIn[1] + "/" + _CheckIn[0] + "/" + _CheckIn[2];
            checkOut = _CheckOut[1] + "/" + _CheckOut[0] + "/" + _CheckOut[2];

            DateTime CheckIn = Convert.ToDateTime(checkIn);
            DateTime CheckOut = Convert.ToDateTime(checkOut);

            int RoomCategoryCode = Convert.ToInt32(RoomCatCodes);

            int RoomRate = GetRoomPrice(RoomCategoryCode, CheckIn);
            decimal totalTax = CalculateTotalTax(RoomRate, RoomCategoryCode);

            return RedirectToAction("Reservation", "SearchResult", new { @checkIn = CheckIn, @checkOut = CheckOut, @Adults = Adults, @Children = Children, @NoOfRooms = NoOfRooms, @RoomRate = RoomRate, @TotalAmount = TotalAmount, @RoomCatCodes = RoomCatCodes, @Quatntity = Quatntity, @RatePlanIDs = RatePlanIDs, @RoomRates = RoomRates, @totalTax = totalTax, @RoomImages = roomImagesString });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing your booking. Please try again.";
                return RedirectToAction("SearchResults");
            }
        }
        public string SaveBooking(string PaxTitle, string PaxFName, string PaxLName, DateTime CheckIn, DateTime CheckOut, int RoomCatCode,
            string PaxPhoneNumber, string PaxEmail, string PaxDescription, int Adults, int Children, int NoOfRooms, int RoomRate, int TotalAmount,
            string RoomCatCodes, string RoomCateRoomNums, string RatePlanIDs, string RoomRates)
        {
            try
            {
                using (var db = new HotelDBEntities2())
                {
                    var rowsAffected = db.Bookings.Where(x => x.PaxFName == PaxFName && x.PaxLName == PaxLName &&
                                x.CheckIn == CheckIn).FirstOrDefault();

                    if (true == true)
                    {
                        int? bookingID = db.Bookings.DefaultIfEmpty().Max(p => p == null ? 0 : p.BookingID);
                        string confirmationId = "TSH-" + Convert.ToString(bookingID + 1);
                        int? PropertyId = db.Rooms.Where(p => p.RoomCategory == RoomCatCode).Select(p => p.propertyID).FirstOrDefault();
                        int? RoomCode = 0;
                        string RoomCate = "";
                        string RoomCates = "";
                        TimeSpan t = Convert.ToDateTime(CheckOut) - Convert.ToDateTime(CheckIn);
                        int noOfDay = (int)t.TotalDays;
                        string[] _RoomCatCodes;
                        string[] _RoomCateRoomNums;
                        string[] _RatePlanIDs;
                        string[] _RoomRates;

                        RoomRates = RoomRates.Remove(0, 1);
                        _RoomRates = RoomRates.Split(',');

                        RoomCatCodes = RoomCatCodes.Remove(0, 1);
                        _RoomCatCodes = RoomCatCodes.Split(',');

                        RatePlanIDs = RatePlanIDs.Remove(0, 1);
                        _RatePlanIDs = RatePlanIDs.Split(',');

                        RoomCateRoomNums = RoomCateRoomNums.Remove(0, 1);
                        _RoomCateRoomNums = RoomCateRoomNums.Split(',');

                        //foreach (var itm in _RoomCatCodes)
                        for (int itm = 0; itm < _RoomCatCodes.Length; itm++)
                        {
                            int CateCode = Convert.ToInt32(_RoomCatCodes[itm]);
                            RoomCode = db.Rooms.Where(p => p.RoomCategory == CateCode).Select(p => p.RoomCode).FirstOrDefault();
                            RoomCate = db.RoomCategories.Where(p => p.Id == CateCode).Select(p => p.Category).FirstOrDefault();
                            RoomCates = RoomCates + "," + RoomCate;

                            //foreach (var itm1 in _RoomCateRoomNums)
                            {
                                int NoofRooms = Convert.ToInt32(_RoomCateRoomNums[itm]);

                                for (int i = 1; i <= NoofRooms; i++)
                                {
                                    var bookedRooms = db.Bookings.Where(x => x.RoomCode == RoomCode && x.CheckIn == CheckIn).Select(p => p.RoomNum).ToList();
                                    var RoomNum = (db.RoomNumbers.Where(p => p.RoomCode == RoomCode).Select(p => p.RoomNumber1).ToList());
                                    //var roomnum = db.RoomNumbers.Where(p => p.RoomNumber1 == RoomNum).FirstOrDefault();

                                    var roomnum = RoomNum.Except(bookedRooms).FirstOrDefault();

                                    if (roomnum != null)
                                    {
                                        //roomnum.Booked = true;

                                        //db.Entry(roomnum).State = System.Data.EntityState.Modified;
                                        //db.SaveChanges();

                                        Booking booking = new Booking();

                                        booking.PaxTitle = PaxTitle;
                                        booking.PaxFName = PaxFName;
                                        booking.PaxLName = PaxLName;
                                        booking.NoOfRooms = NoOfRooms;
                                        booking.RoomCode = RoomCode;
                                        booking.RoomCatCode = CateCode;
                                        booking.RoomNum = roomnum;
                                        booking.PaxEmail = PaxEmail;
                                        booking.PaxPhoneNumber = PaxPhoneNumber;
                                        booking.PaxDescription = PaxDescription;
                                        booking.Adults = Adults;
                                        booking.Children = Children;
                                        booking.CheckIn = CheckIn;
                                        booking.CheckOut = CheckOut;
                                        booking.RoomRate = Convert.ToInt32(_RoomRates[itm]);
                                        booking.TotalRoomRate = Convert.ToInt32(_RoomRates[itm]);
                                        booking.HotelCode = 5;
                                        booking.ConfirmationNo = confirmationId;
                                        booking.DateModified = DateTime.Now;
                                        booking.BookingStatus = "Booked";
                                        booking.NoofDay = noOfDay;
                                        booking.TotalRoomRate = TotalAmount;
                                        booking.ReservationDate = DateTime.Now;
                                        booking.ThemeColor = "red";
                                        booking.RatePlanId = Convert.ToInt32(_RatePlanIDs[itm]);

                                        db.Entry(booking).State = System.Data.Entity.EntityState.Added;
                                        db.SaveChanges();




                                    }
                                }
                            }
                        }

                        var _booking = (from obj in HDE.Bookings
                                        join b in HDE.RoomCategories
                                         on obj.RoomCatCode equals b.Id
                                        where (obj.ConfirmationNo == confirmationId)
                                        select new FinalBooking
                                        {
                                            BookingID = obj.BookingID,
                                            PaxTitle = obj.PaxTitle,
                                            PaxFName = obj.PaxFName,
                                            PaxLName = obj.PaxLName,
                                            NoOfRooms = obj.NoOfRooms,
                                            RoomCode = obj.RoomCode,
                                            RoomCatCode = obj.RoomCatCode,
                                            RoomCategory = b.Category,
                                            RoomNum = obj.RoomNum,
                                            PaxEmail = obj.PaxEmail,
                                            PaxPhoneNumber = obj.PaxPhoneNumber,
                                            PaxDescription = obj.PaxDescription,
                                            Adults = obj.Adults,
                                            Children = obj.Children,
                                            CheckIn = obj.CheckIn,
                                            CheckOut = obj.CheckOut,
                                            RoomRate = obj.RoomRate,
                                            HotelCode = obj.HotelCode,
                                            ConfirmationNo = obj.ConfirmationNo,
                                            NoofDay = obj.NoofDay,
                                            TotalRoomRate = obj.TotalRoomRate
                                        }).ToList();

                        RoomCates = RoomCates.Remove(0, 1);
                        TempData["RoomCates"] = RoomCates;
                        TempData["noOfDay"] = noOfDay;
                        Session["booking"] = _booking;
                        Session["confirmationId"] = confirmationId;

                        try
                        {
                            StringBuilder htmlString = new StringBuilder();
                            htmlString.Append(@"<html>");
                            htmlString.Append("<body>");
                            htmlString.Append("<h3>Thanks</h3>");
                            htmlString.Append("<p> New Order Received.</p>");
                            htmlString.Append("Confirmation Num.:  <b>" + confirmationId + "</b><br />");
                            htmlString.Append("<table border='1'><tr>");
                            htmlString.Append("<th>Guest Nmae</th><th>Phone Num.</th><th>Room Category</th><th>CheckIn Date/Time</th><th>CheckOut</th></tr>");
                            htmlString.Append("<tr><td>" + PaxTitle + " " + PaxFName + " " + PaxLName + " </td><td> " + PaxPhoneNumber + "</td>");
                            htmlString.Append("<td>" + RoomCates + "</td>");
                            htmlString.Append("<td>" + CheckIn + "</td> ");
                            htmlString.Append("<td>" + CheckOut + "</td>");
                            htmlString.Append("</tr>");
                            htmlString.Append("</table><br />");
                            htmlString.Append("<span><b>Adults:</b></span><span>" + Adults + "</span><br />");
                            htmlString.Append("<span><b>Children:</b></span><span>" + Children + "</span><br />");
                            htmlString.Append("<span><b>No. Of Rooms:</b></span><span>" + NoOfRooms + "</span><br />");
                            htmlString.Append("<span><b>No. Of Night:</b></span><span>" + noOfDay + "</span><br />");
                            htmlString.Append("<span><b>Total Amount:</b></span><span>" + TotalAmount + "</span><br />");
                            htmlString.Append("</body>");
                            htmlString.Append("</html>");

                            MailMessage mm = new MailMessage();
                            mm.IsBodyHtml = true;
                            mm.To.Add("info@thesaraihotel.com");

                            mm.From = new MailAddress("growbiztec@gmail.com");
                            mm.Subject = "Order Confirmation: " + confirmationId;
                            mm.Body = htmlString.ToString();
                            SmtpClient smc = new SmtpClient();
                            smc.Host = "smtp.gmail.com";
                            smc.Port = 587;
                            // smc.UseDefaultCredentials = false;
                            smc.EnableSsl = true;
                            smc.Credentials = new System.Net.NetworkCredential("growbiztec@gmail.com", "growbiz@2022");
                            smc.DeliveryMethod = SmtpDeliveryMethod.Network;

                            smc.Send(mm);


                        }
                        catch (Exception ex)
                        {
                            TempData["error"] = ex.Message;
                        }

                        if (PaxEmail != "")
                        {
                            try
                            {
                                StringBuilder htmlString = new StringBuilder();
                                htmlString.Append(@"<html>");
                                htmlString.Append("<body>");
                                htmlString.Append("<h3>Thanks</h3>");
                                htmlString.Append("<p> We have received your booking order and are working on a confirmation.</p>");
                                htmlString.Append("Your Confirmation Num.:  <b>" + confirmationId + "</b><br />");
                                htmlString.Append("<table border='1'><tr>");
                                htmlString.Append("<th>Guest Nmae</th><th>Phone Num.</th><th>Room Category</th><th>CheckIn Date/Time</th><th>CheckOut</th></tr>");
                                htmlString.Append("<tr><td>" + PaxTitle + " " + PaxFName + " " + PaxLName + " </td><td>" + PaxPhoneNumber + " </td>");
                                htmlString.Append("<td>" + RoomCates + "</td>");
                                htmlString.Append("<td>" + CheckIn + "</td> ");
                                htmlString.Append("<td>" + CheckOut + "</td>");
                                htmlString.Append("</tr>");
                                htmlString.Append("</table><br />");
                                htmlString.Append("<span><b>Adults:</b></span><span>" + Adults + "</span><br />");
                                htmlString.Append("<span><b>Children:</b></span><span>" + Children + "</span><br />");
                                htmlString.Append("<span><b>No. Of Rooms:</b></span><span>" + NoOfRooms + "</span><br />");
                                htmlString.Append("<span><b>No. Of Night:</b></span><span>" + noOfDay + "</span><br />");
                                htmlString.Append("<span><b>Total Amount:</b></span><span>" + TotalAmount + "</span><br />");
                                htmlString.Append("</body>");
                                htmlString.Append("</html>");

                                MailMessage mm = new MailMessage();
                                mm.IsBodyHtml = true;
                                mm.To.Add(PaxEmail);

                                mm.From = new MailAddress("info@thesaraihotel.com");
                                mm.Subject = "Order Confirmation: " + confirmationId;
                                mm.Body = htmlString.ToString();
                                SmtpClient smc = new SmtpClient();
                                smc.Host = "smtp.gmail.com";
                                smc.Port = 587;
                                // smc.UseDefaultCredentials = false;
                                smc.EnableSsl = true;
                                smc.Credentials = new System.Net.NetworkCredential("growbiztec@gmail.com", "growbiz@2022");
                                smc.DeliveryMethod = SmtpDeliveryMethod.Network;
                                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                        delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
                        {
                            return true;  // Bypasses the SSL certificate validation
                        };

                                smc.Send(mm);


                            }
                            catch (Exception ex)
                            {
                                TempData["error"] = ex.Message;
                            }

                        }

                        return ("Booking done succesfully");
                        //jsonString = JsonSerializer.Serialize(confirmationId);

                    }
                    else
                    {
                        // responseBasicProfile.MessageList.Add("Not Record Found.");
                        return ("Something went wrong.");
                    }
                }

            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return ("Something went wrong.");
            }
        }

        [HttpPost]
        public ActionResult SaveBooking(FormCollection frm)
        {
            try
            {
                string PaymentType = Request.Form["PaymentType"];
                string PaxTitle = Request.Form["PaxTitle"];
                string PaxFName = Request.Form["PaxFName"];
                string PaxLName = Request.Form["PaxLName"];
                string checkIn = Convert.ToString(Request.Form["CheckIn"]);
                string checkOut = Convert.ToString(Request.Form["CheckOut"]);
                int RoomCatCode = 0;  //Convert.ToInt32(Request.Form["RoomCatCode"]);
                string PaxPhoneNumber = Request.Form["PaxPhoneNumber"];
                string PaxEmail = Request.Form["PaxEmail"];
                string PaxDescription = Request.Form["PaxDescription"];
                int Adults = Convert.ToInt32(Request.Form["Adults"]);
                int Children = Convert.ToInt32(Request.Form["Children"]);
                int NoOfRooms = Convert.ToInt32(Request.Form["hdTotalRooms"]);
                int RoomRate = 0; // Convert.ToInt32(Request.Form["RoomRate"]);
                int TotalAmount = Convert.ToInt32(Request.Form["TotalAmount"]);
                string RoomCatCodes = Request.Form["RoomCatCode"];
                string RoomCateRoomNums = Request.Form["RoomCateRoomNums"];
                string RatePlanIDs = Request.Form["RatePlanIDs"];
                string RoomRates = Request.Form["RoomRates"];

                int DiscountedAmount = Convert.ToInt32(Request.Form["DiscountedAmount"]);
                string VoucherCode = Request.Form["VoucherCode"];

                // string[] _CheckIn = checkIn.Split('-');
                // string[] _CheckOut = checkOut.Split('-');

                // checkIn = _CheckIn[1] + "/" + _CheckIn[0] + "/" + _CheckIn[2];
                // checkOut = _CheckOut[1] + "/" + _CheckOut[0] + "/" + _CheckOut[2];

                DateTime CheckIn = Convert.ToDateTime(checkIn);
                DateTime CheckOut = Convert.ToDateTime(checkOut);

                if (Adults == 0)
                {
                    Adults = 1;
                }

                using (var db = new HotelDBEntities2())
                {
                    var rowsAffected = db.Bookings.Where(x => x.PaxFName == PaxFName && x.PaxLName == PaxLName &&
                                x.CheckIn == CheckIn).FirstOrDefault();

                    if (true == true)
                    {
                        int? bookingID = db.Bookings.DefaultIfEmpty().Max(p => p == null ? 0 : p.BookingID);
                        string confirmationId = "MFH-" + Convert.ToString(bookingID + 1);
                        int? PropertyId = db.Rooms.Where(p => p.RoomCategory == RoomCatCode).Select(p => p.propertyID).FirstOrDefault();
                        int? RoomCode = 0;
                        string RoomCate = "";
                        string RoomCates = "";
                        TimeSpan t = Convert.ToDateTime(CheckOut) - Convert.ToDateTime(CheckIn);
                        int noOfDay = (int)t.TotalDays;
                        //string[] _RoomCatCodes;
                        //string[] _RoomCateRoomNums;
                        //string[] _RatePlanIDs;
                        //string[] _RoomRates;
                        double GrandTotal = 0;
                        {
                            int CateCode = Convert.ToInt32(RoomCatCodes);
                            RoomCode = db.Rooms.Where(p => p.RoomCategory == CateCode).Select(p => p.RoomCode).FirstOrDefault();
                            RoomCate = db.RoomCategories.Where(p => p.Id == CateCode).Select(p => p.Category).FirstOrDefault();
                            RoomCates = RoomCates + "," + RoomCate;

                            //foreach (var itm1 in _RoomCateRoomNums)
                            {
                                int NoofRooms = Convert.ToInt32(RoomCateRoomNums);

                                // for (int i = 1; i <= NoofRooms; i++)
                                {
                                    var bookedRooms = db.Bookings.Where(x => x.RoomCode == RoomCode && x.CheckIn == CheckIn).Select(p => p.RoomNum).ToList();
                                    var RoomNum = (db.RoomNumbers.Where(p => p.RoomCode == RoomCode).Select(p => p.RoomNumber1).ToList());
                                    //var roomnum = db.RoomNumbers.Where(p => p.RoomNumber1 == RoomNum).FirstOrDefault();

                                    var roomnum = RoomNum.Except(bookedRooms).FirstOrDefault();

                                    if (roomnum != null)
                                    {

                                        GrandTotal = GrandTotal + TotalAmount;

                                        Booking booking = new Booking();

                                        booking.PaxTitle = PaxTitle;
                                        booking.PaxFName = PaxFName;
                                        booking.PaxLName = PaxLName;
                                        booking.NoOfRooms = NoofRooms;
                                        booking.RoomCode = RoomCode;
                                        booking.RoomCatCode = CateCode;
                                        booking.RoomNum = roomnum;
                                        booking.PaxEmail = PaxEmail;
                                        booking.PaxPhoneNumber = PaxPhoneNumber;
                                        booking.PaxDescription = PaxDescription;
                                        booking.Adults = Adults;
                                        booking.Children = Children;
                                        booking.CheckIn = CheckIn;
                                        booking.CheckOut = CheckOut;
                                        booking.RoomRate = Convert.ToInt32(RoomRates);
                                        booking.TotalRoomRate = Convert.ToInt32(RoomRates);
                                        booking.HotelCode = 1;
                                        booking.ConfirmationNo = confirmationId;
                                        booking.ReservationDate = DateTime.Now;
                                        booking.DateModified = DateTime.Now;
                                        booking.BookingStatus = "Pending";
                                        booking.CountryCode = "PK";
                                        booking.NoofDay = noOfDay;
                                        booking.ThemeColor = "red";
                                        booking.RatePlanId = Convert.ToInt32(RatePlanIDs);
                                        // Get the discount amount using the voucher code
                                        decimal discountAmount = GetDiscountAmountDecimal(VoucherCode);

                                        if (discountAmount > 0)
                                        {
                                            booking.VoucherAmount = Convert.ToInt32(discountAmount);
                                        }
                                        else
                                        {
                                            booking.VoucherAmount = 0;
                                        }

                                        GrandTotal -= (double)discountAmount;
                                        DiscountedAmount -= (int)discountAmount;


                                        // Insert logic to set TotalRoomRate
                                        if (discountAmount > 0)
                                        {
                                            booking.TotalRoomRate = Convert.ToInt32(GrandTotal);
                                        }
                                        else
                                        {
                                            booking.TotalRoomRate = TotalAmount;
                                        }
                                        booking.ThemeColor = "red";
                                        booking.RatePlanId = Convert.ToInt32(RatePlanIDs);
                                        //if (PaymentType == "1")
                                        //{
                                        //    booking.DiscountedAmount = 0;
                                        //}
                                        //else
                                        //{
                                        //    booking.DiscountedAmount = Convert.ToInt32(DiscountedAmount);
                                        //}


                                        db.Entry(booking).State = System.Data.Entity.EntityState.Added;
                                        db.SaveChanges();
                                    }
                                }
                            }
                        }

                        // Get the discount amount using the voucher code
                        decimal discAmount = GetDiscountAmountDecimal(VoucherCode);
                        //GrandTotal -= (double)discountAmount;
                        DiscountedAmount -= (int)discAmount;

                        if (PaymentType == "1" || PaymentType == null)
                        {
                            return RedirectToAction("Thankyou", "SearchResult", new { @billReference = confirmationId, @Total = GrandTotal });
                        }
                        else
                        {
                            return RedirectToAction("GetAccessToken", "Payfast", new { @billReference = confirmationId, @GrandTotal = DiscountedAmount });
                        }

                    }
                    else
                    {
                        // responseBasicProfile.MessageList.Add("Not Record Found.");
                        return RedirectToAction("Index", "Home");
                    }
                }

            }
            catch (Exception ex)
            {
                //TempData["error"] = ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        public int GetRoomPrice(int roomCategoryId, DateTime checkIn)
        {
            try
            {
                using (var db = new HotelDBEntities2())
                {
                    var roomPrice = (from a in db.RateSlabs
                                     where (a.RoomCategoryCode == roomCategoryId && a.StartDate <= checkIn && a.EndDate >= checkIn)
                                     select a.Discount).FirstOrDefault();

                    return roomPrice ?? 0; // Return 0 if no rate is found
                }
            }
            catch (Exception)
            {
                return 0; // Handle errors gracefully by returning 0
            }
        }
        private decimal CalculateTotalTax(int totalAmount, int roomCategoryId)
        {
            // Fetch applicable taxes for the room category
            var tax = HDE.Taxes.FirstOrDefault(v => v.RoomCategory == roomCategoryId);

            // Ensure tax is not null and convert Amount to non-nullable, defaulting to 0 if null
            var taxPercentage = tax?.Amount ?? 0;

            // Perform the tax calculation with proper casting
            return totalAmount * (decimal)taxPercentage / 100;
        }


        private List<string> GetRoomImagePaths(int roomCategoryId)
        {
            var db = new HotelDBEntities2();
            var roomImages = db.RoomPictures
                .Where(x => x.RoomCode == roomCategoryId && x.Active == true)
                .Select(x => x.Picture)
                .ToList();

            string imagePathPrefix = "~/uploads/";
            return roomImages.Select(image => imagePathPrefix + image).ToList();
        }

        public JsonResult GetDiscountAmount(string code)
        {
            var voucher = HDE.Vouchers.FirstOrDefault(v => v.Code == code);

            if (voucher != null)
            {
                return Json(new { success = true, discountAmount = voucher.DiscountAmount }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, discountAmount = 0 }, JsonRequestBehavior.AllowGet);
            }
        }

        private decimal GetDiscountAmountDecimal(string code)
        {
            var voucher = HDE.Vouchers.FirstOrDefault(v => v.Code == code);
            return voucher?.DiscountAmount ?? 0;
        }

    }

    public class FinalBooking
    {
        public int? BookingID { get; set; }
        public string PaxTitle { get; set; }
        public string PaxFName { get; set; }
        public string PaxLName { get; set; }
        public int? NoOfRooms { get; set; }
        public int? RoomCode { get; set; }
        public int? RoomCatCode { get; set; }
        public string RoomCategory { get; set; }
        public string RoomNum { get; set; }
        public string PaxEmail { get; set; }
        public string PaxPhoneNumber { get; set; }
        public string PaxDescription { get; set; }
        public int? Adults { get; set; }
        public int? Children { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public DateTime? ReservationDate { get; set; }
        public int? RoomRate { get; set; }
        public int? HotelCode { get; set; }
        public string ConfirmationNo { get; set; }
        public int? NoofDay { get; set; }
        public int? TotalRoomRate { get; set; }
    }

    public class BookedRooms
    {
        public string RoomCate { get; set; }
        public int? Quantity { get; set; }
    }


    public class RoomImages
    {
        public int? Category { get; set; }
        public string ImageName { get; set; }
    }

}

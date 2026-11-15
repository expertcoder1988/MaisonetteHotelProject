using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HotelWebsite.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HotelWebsite.Controllers.Bookings
{
    [Authorize]
    public class BookingController : Controller
    {
        HotelDBEntities2 HDE = new HotelDBEntities2();
        Booking bi = new Booking();

        // GET: Booking
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CheckIn()
        {
            return View();
        }
        public ActionResult BlockBooking()
        {
            return View();
        }
        public ActionResult Booking()
        {
            return View();
        }
        public ActionResult BoookingPayment()
        {
            return View();
        }

        public JsonResult BookingList(int propertyId, string CheckIn, string CheckOut, string BookingStatus, int Roomcate, int flag = -1)
        {
            try
            {
                // Parse user input dates (optional, only if provided)
                DateTime checkInDate = DateTime.MinValue;
                DateTime checkOutDate = DateTime.MaxValue;

                if (!string.IsNullOrEmpty(CheckIn) && !DateTime.TryParseExact(CheckIn, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out checkInDate))
                {
                    return Json("CheckIn date is not in the correct format (MM/dd/yyyy).", JsonRequestBehavior.AllowGet);
                }

                if (!string.IsNullOrEmpty(CheckOut) && !DateTime.TryParseExact(CheckOut, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out checkOutDate))
                {
                    return Json("CheckOut date is not in the correct format (MM/dd/yyyy).", JsonRequestBehavior.AllowGet);
                }

                // Default query for loading data initially
                var res = (from obj in HDE.Bookings
                           join b in HDE.RoomCategories on obj.RoomCatCode equals b.Id into rooms
                           from r in rooms.DefaultIfEmpty()
                           where obj.HotelCode == propertyId
                                 && obj.BookingStatus != "Blocked"
                           orderby obj.BookingID descending
                           select new
                           {
                               BookingID = obj.BookingID,
                               PaxFName = obj.PaxTitle + " " + obj.PaxFName + " " + obj.PaxLName,
                               PaxPhoneNumber = obj.PaxPhoneNumber,
                               RoomCategory = r.Category,
                               RoomNum = obj.RoomNum,
                               Adults = obj.Adults,
                               Children = obj.Children,
                               CheckIn = obj.CheckIn,
                               CheckOut = obj.CheckOut,
                               Rate = obj.RoomRate,
                               status = obj.BookingStatus,
                               roomCateCode = obj.RoomCatCode,
                               ReservationDate = obj.ReservationDate,
                               NoOfRooms = obj.NoOfRooms,
                               TotalAmount = obj.TotalRoomRate,
                               ConfirmationNo = obj.ConfirmationNo,
                               VoucherAmount = obj.VoucherAmount
                           }).ToList();

                // Apply filters only when the `flag` is set
                if (flag != -1)
                {
                    res = (from obj in HDE.Bookings
                           join b in HDE.RoomCategories on obj.RoomCatCode equals b.Id into rooms
                           from r in rooms.DefaultIfEmpty()
                           where obj.HotelCode == propertyId
                                 && obj.CheckIn >= checkInDate
                                 && obj.CheckOut <= checkOutDate
                           orderby obj.BookingID descending
                           select new
                           {
                               BookingID = obj.BookingID,
                               PaxFName = obj.PaxTitle + " " + obj.PaxFName + " " + obj.PaxLName,
                               PaxPhoneNumber = obj.PaxPhoneNumber,
                               RoomCategory = r.Category,
                               RoomNum = obj.RoomNum,
                               Adults = obj.Adults,
                               Children = obj.Children,
                               CheckIn = obj.CheckIn,
                               CheckOut = obj.CheckOut,
                               Rate = obj.RoomRate,
                               status = obj.BookingStatus,
                               roomCateCode = obj.RoomCatCode,
                               ReservationDate = obj.ReservationDate,
                               NoOfRooms = obj.NoOfRooms,
                               TotalAmount = obj.TotalRoomRate,
                               ConfirmationNo = obj.ConfirmationNo,
                               VoucherAmount = obj.VoucherAmount
                           }).ToList();
                }

                // Filter by BookingStatus
                if (!string.IsNullOrEmpty(BookingStatus) && BookingStatus != "-1")
                {
                    res = res.Where(r => r.status == BookingStatus).ToList();
                }

                // Filter by Room Category
                if (Roomcate != -1)
                {
                    res = res.Where(r => r.roomCateCode == Roomcate).ToList();
                }

                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex.Message.ToString(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult ReleaseRoom(int bookingId)
        {
            try
            {
                // Find the booking record
                Booking booking = HDE.Bookings.SingleOrDefault(b => b.BookingID == bookingId);

                if (booking != null)
                {
                    // Update booking status to "Cancelled"
                    booking.BookingStatus = "Cancelled";
                    HDE.SaveChanges(); // Save changes to the database

                    // Return the available room numbers (you can modify this as needed)
                    var availableRooms = HDE.Bookings.Where(b => b.BookingStatus == "Available")
                                                    .Select(b => new { b.RoomNum })
                                                    .ToList();

                    return Json(availableRooms, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Booking not found" }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult LoadRoomNums(int propertyId, DateTime CheckIn, DateTime CheckOut, int Roomcate)
        {
            try
            {
                var list1 = (from a in HDE.RoomNumbers
                             join b in HDE.Rooms
                             on a.RoomCode equals b.RoomCode
                             where (b.RoomCategory == Roomcate)
                             select new
                             {
                                 RoomNums = a.RoomNumber1
                             }).ToList();

                var list2 = (from a in HDE.Bookings
                             where (a.HotelCode == propertyId && a.CheckIn >= CheckIn && a.CheckOut <= CheckOut && a.RoomCatCode == Roomcate)
                             select new
                             {
                                 RoomNums = a.RoomNum
                             }).ToList();

                var res = list1.Except(list2).ToList();

                //if (Roomcate != -1)
                //{
                //    res = res.Where(r => r.roomCateCode == Roomcate);
                //}
                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                //ViewBag.Error = ex.Message;
                return Json(ex.Message.ToString(), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult LoadRoomNumsCheckin(int bookingId)
        {
            try
            {
                var paramList = (from a in HDE.Bookings
                                 where (a.BookingID == bookingId)
                                 select new
                                 {
                                     CheckIn = a.CheckIn,
                                     CheckOut = a.CheckOut,
                                     RoomCatCode = a.RoomCatCode,
                                     PropertyId = a.HotelCode
                                 }).FirstOrDefault();

                var list1 = (from a in HDE.RoomNumbers
                             join b in HDE.Rooms
                             on a.RoomCode equals b.RoomCode
                             where (b.RoomCategory == paramList.RoomCatCode)
                             select new
                             {
                                 RoomNums = a.RoomNumber1
                             }).ToList();

                var list2 = (from a in HDE.Bookings
                             where (a.HotelCode == paramList.PropertyId && a.CheckIn >= paramList.CheckIn && a.CheckOut <= paramList.CheckOut &&
                             a.RoomCatCode == paramList.RoomCatCode)
                             select new
                             {
                                 RoomNums = a.RoomNum
                             }).ToList();

                var res = list1.Except(list2).ToList();

                //if (Roomcate != -1)
                //{
                //    res = res.Where(r => r.roomCateCode == Roomcate);
                //}
                return Json(res, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                //ViewBag.Error = ex.Message;
                return Json(ex.Message.ToString(), JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult LoadRoomRate(int propertyId, DateTime CheckIn, DateTime CheckOut, int Roomcate)
        {
            try
            {
                var dates = new List<DateTime>();
                var days = new List<string>();
                int AdultTotal = 0;
                int ChildTotal = 0;
                DateTime date;
                string day;
                string res = "";

                for (var dt = CheckIn; dt <= CheckOut; dt = dt.AddDays(1))
                {
                    dates.Add(dt);
                    days.Add(dt.ToString("ddd"));
                }

                for (var itm = 0; itm <= dates.Count - 1; itm++)
                {
                    date = dates[itm];
                    day = days[itm];

                    var list1 = (from a in HDE.RateSlabs
                                 where (a.PropertyId == propertyId && a.RoomCategoryCode == Roomcate && a.StartDate <= date && a.EndDate >= date
                                 && a.Days.ToUpper().Contains(day.ToUpper()))
                                 select new
                                 {
                                     AdultPrice = a.ExtraAdultPrice,
                                     ChildPrice = a.ExtraChildPrice
                                 }).FirstOrDefault();

                    if (list1 != null)
                    {
                        AdultTotal = AdultTotal + Convert.ToInt32(list1.AdultPrice);
                        ChildTotal = ChildTotal + Convert.ToInt32(list1.ChildPrice);
                    }

                }
;
                res = AdultTotal.ToString() + "," + ChildTotal.ToString();
                return Json(res, JsonRequestBehavior.AllowGet);


            }
            catch (Exception ex)
            {
                //ViewBag.Error = ex.Message;
                return Json(ex.Message, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public string UpdateRoomNum(int bookingID, string RoomNum)
        {
            try
            {

                using (var context = new HotelDBEntities2())
                {
                    // Retrieve entity by id
                    var entity = context.Bookings.FirstOrDefault(item => item.BookingID == bookingID);

                    // Validate entity is not null
                    if (entity != null)
                    {
                        entity.RoomNum = RoomNum;
                        entity.DateModified = DateTime.Today;

                        // Save changes in database
                        context.SaveChanges();
                    }
                }

                return "Property updated succesfully";
            }
            catch (Exception ex)
            {
                //ViewBag.Error = ex.Message;

                return ex.Message.ToString();
            }
        }

        [HttpPost]
        public string CheckIn(int bookingID, string Status)
        {
            try
            {

                using (var context = new HotelDBEntities2())
                {
                    // Retrieve entity by id
                    var entity = context.Bookings.FirstOrDefault(item => item.BookingID == bookingID);

                    // Validate entity is not null
                    if (entity != null)
                    {
                        entity.BookingStatus = Status;
                        entity.DateModified = DateTime.Today;
                        entity.ThemeColor = "blue";

                        // Save changes in database
                        context.SaveChanges();
                    }
                }

                return "Property updated succesfully";
            }
            catch (Exception ex)
            {
                //ViewBag.Error = ex.Message;

                return ex.Message.ToString();
            }
        }

        [HttpPost]
        public string Payment(int bookingID, string Status)
        {
            try
            {

                using (var context = new HotelDBEntities2())
                {
                    // Retrieve entity by id
                    var entity = context.Bookings.FirstOrDefault(item => item.BookingID == bookingID);

                    // Validate entity is not null
                    if (entity != null)
                    {
                        entity.BookingStatus = Status;
                        entity.DateModified = DateTime.Today;
                        entity.ThemeColor = "green";

                        // Save changes in database
                        context.SaveChanges();
                    }
                }

                return "Property updated succesfully";
            }
            catch (Exception ex)
            {
                //ViewBag.Error = ex.Message;

                return ex.Message.ToString();
            }
        }

        [HttpPost]
        public string Save(Booking pBI)
        {
            try
            {
                int? bookingID = HDE.Bookings.DefaultIfEmpty().Max(p => p == null ? 0 : p.BookingID);
                string confirmationId = "MFH-" + Convert.ToString(bookingID + 1);

                pBI.ServiceType = "RateBasis";
                pBI.RequestTime = DateTime.Now;
                pBI.BookingStatus = "Booked";
                pBI.ConfirmationNo = confirmationId;
                pBI.DestinationID = "ISB";
                pBI.BookingByBO = true;
                pBI.MiscExtrasCharges = 0;
                pBI.AgentCommission = 0;
                pBI.BreakageWaiverFee = 0;
                pBI.BreakageFee = 0;
                pBI.ExtraCleaning = 0;
                pBI.LinenTowelChange = 0;
                pBI.WelcomePack = 0;
                pBI.AirportPickandDrop = 0;
                pBI.TourismTax = 0;
                pBI.VAT = 0;
                pBI.NoOfRooms = 1;
                pBI.RatePlanId = 1;
                pBI.UserID = Convert.ToInt32(Session["CustID"]);
                pBI.DateModified = DateTime.Now;
                pBI.ReservationDate = DateTime.Now;
                pBI.ThemeColor = "red";
                pBI.CountryCode = "PK";
                pBI.VoucherAmount = 0;
                // Ensure CheckIn and CheckOut are not null before calculating NoofDay
                if (pBI.CheckIn.HasValue && pBI.CheckOut.HasValue)
                {
                    pBI.NoofDay = (pBI.CheckOut.Value - pBI.CheckIn.Value).Days;
                }
                else
                {
                    pBI.NoofDay = 0; // Handle as needed if either date is null
                }

                HDE.Entry(pBI).State = System.Data.Entity.EntityState.Added;
                HDE.SaveChanges();

                return "Booking done successfully";
            }
            catch (Exception ex)
            {
                // Log error details
                Console.WriteLine("Error: " + ex.Message);
                return "Error: " + ex.Message;
            }
        }


        public JsonResult RoomCategories()
        {
            var RoomCatCode = HDE.Rooms.Where(s => s.blocked == false).Select(x => new { RoomID = x.ID, RoomCategory = x.RoomCategory }).ToList();
            return this.Json(RoomCatCode, JsonRequestBehavior.AllowGet);
        }
    }
}

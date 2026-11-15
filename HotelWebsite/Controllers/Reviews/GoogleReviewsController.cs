using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace HotelWebsite.Controllers.Reviews
{
    public class GoogleReviewsController : Controller
    {
        string Baseurl = "https://mybusiness.googleapis.com/v4/";
        // GET: GoogleReviews

        public async Task<ActionResult> Index()
        {
            List<Employee> EmpInfo = new List<Employee>();
            using (var client = new HttpClient())
            {
                //Passing service base url
                client.BaseAddress = new Uri(Baseurl);
                client.DefaultRequestHeaders.Clear();
                //Define request data format
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Sending request to find web api REST service resource GetAllEmployees using HttpClient
                HttpResponseMessage Res = await client.GetAsync("accounts/5240659405/locations/ChIJWwQyu-ZR5zgRp0C7MKLaYo8/reviews");
                //Checking the response is successful or not which is sent using HttpClient
                if (Res.IsSuccessStatusCode)
                {
                    //Storing the response details recieved from web api
                    var EmpResponse = Res.Content.ReadAsStringAsync().Result;
                    //Deserializing the response recieved from web api and storing into the Employee list
                    EmpInfo = JsonConvert.DeserializeObject<List<Employee>>(EmpResponse);
                }
                //returning the employee list to view
                return View(EmpInfo);
            }

        }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
    }
}
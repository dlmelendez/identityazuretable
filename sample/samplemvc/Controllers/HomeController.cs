using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Owin;
using System.Web.UI;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace samplemvc.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationUserManager _userManager;

        
        public ApplicationUserManager UserManager {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        [HttpGet]
        public  Task<JArray> GetUsers()
        {
            DateTime start2 = DateTime.UtcNow;
            var usersList2 = UserManager.Users.ToList();
            Debug.WriteLine("GetUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);
            Debug.WriteLine("GetUsers(): {0} count", usersList2.Count);
                        
            JArray result = JArray.Parse(JsonConvert.SerializeObject(usersList2));
            
            return Task.FromResult(result);
        }

        [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
        [HttpGet]
        public Task<int> GetUserCount()
        {
            DateTime start2 = DateTime.UtcNow;
            var count = UserManager.Users.Count();
            Debug.WriteLine("GetUserCount(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);
            Debug.WriteLine("GetUserCount(): {0} count", count);

            return Task.FromResult(count);
        }


    }
}
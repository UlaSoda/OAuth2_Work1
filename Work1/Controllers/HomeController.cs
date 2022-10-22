using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Work1.Models;

namespace Work1.Controllers
{
    public class HomeController : Controller
    {
        public string login_client_id = "login_client_id";
        public string login_client_secret = "login_client_secret";

        public string notify_client_id = "notify_client_id";
        public string notify_client_secret = "notify_client_secret";
        public string direct_url = "http://localhost/Work1/Home/callback";
        //public string direct_url = "https://work120221022145517.azurewebsites.net/Home/callback";
        public ActionResult Index()
        {
            LineProfile ProfileObj = Session["LineProfile"] as LineProfile;
            ViewBag.UserProfile = ProfileObj;


            return View();
        }
        public ActionResult Login()
        {
            string response_type = "code";
            string client_id = login_client_id;
            string redirect_uri = HttpUtility.UrlEncode(direct_url);
            string scope = "openid%20profile";
            //string scope = "profile%20openid%20email";

            string state = "login_state";
            string uri = string.Format("https://access.line.me/oauth2/v2.1/authorize?response_type={0}&client_id={1}&redirect_uri={2}&state={3}&scope={4}&nonce=09876xyz",
                response_type,
                client_id,
                redirect_uri,
                state,
                scope);
            return Redirect(uri);
        }

        public ActionResult Logout()
        {

            using (var wc = new WebClient())
            {
                try
                {
                    LineProfile ProfileObj = Session["LineProfile"] as LineProfile;
                    var bearer = ProfileObj.access_token;
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    NameValueCollection nameValueCollection = new NameValueCollection();
                    nameValueCollection.Add("client_id", login_client_id);
                    nameValueCollection.Add("client_secret", login_client_secret);
                    nameValueCollection.Add("access_token", $"{bearer}");
                    var bResult = wc.UploadValues($"https://api.line.me/oauth2/v2.1/revoke", nameValueCollection);
                    var res = Encoding.UTF8.GetString(bResult);
                 
                    Session["LineProfile"] = null;
                    Session.Remove("LineProfile");
                    
                    return RedirectToAction("callback");

                }
                catch (Exception ex)
                {
                    throw new Exception("無法連接遠端伺服器");
                }
            }
        }
        
        public ActionResult Notify()
        {
            LineProfile ProfileObj = Session["LineProfile"] as LineProfile;
            ViewBag.UserProfile = ProfileObj;
         
            LineNotifyToken access_token3 = Session["access_token"] as LineNotifyToken;
            ViewBag.access_token = access_token3;

            if (access_token3 == null)//未授權
            {
                string response_type = "code";
                string client_id = notify_client_id;
                string redirect_uri = HttpUtility.UrlEncode(direct_url);
                string scope = "notify";

                string state = "notify_state";
                string uri = string.Format("https://notify-bot.line.me/oauth/authorize?response_type={0}&client_id={1}&redirect_uri={2}&state={3}&scope={4}",
                               response_type,
                               client_id,
                               redirect_uri,
                               state,
                               scope);
                return Redirect(uri);
            }
          
            return RedirectToAction("callback");
        }

        public ActionResult Revoke()
        {
            using (var wc = new WebClient())
            {
                try
                {
                    var access_token = Session["access_token"].ToString();

                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    wc.Headers.Add("Authorization", $"Bearer {access_token}");


                    NameValueCollection nameValueCollection = new NameValueCollection();
                    var bResult = wc.UploadValues($"https://notify-api.line.me/api/revoke", nameValueCollection);

                    var res = Encoding.UTF8.GetString(bResult);

                    Session["access_token"] = null;
                    Session.Remove("access_token");


                    ViewBag.UserProfile = Session["LineProfile"] as LineProfile;

                    return RedirectToAction("callback");

                }
                catch (Exception ex)
                {
                    throw new Exception("無法連接遠端伺服器");
                }
            }
        }


        public ActionResult callback(string code, string state)
        {
            if (state == "login_state" && Session["LineProfile"] == null)//按下登入
            {
                using (var wc = new WebClient())
                {
                    try
                    {
                        #region 取回Token POST
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        NameValueCollection nameValueCollection = new NameValueCollection();
                        nameValueCollection.Add("grant_type", "authorization_code");
                        nameValueCollection.Add("code", code);
                        nameValueCollection.Add("redirect_uri", direct_url);
                        nameValueCollection.Add("client_id", login_client_id);
                        nameValueCollection.Add("client_secret", login_client_secret);
                        var bResult = wc.UploadValues($"https://api.line.me/oauth2/v2.1/token", nameValueCollection);
                        var res = Encoding.UTF8.GetString(bResult);
                        LineLoginToken ToKenObj = JsonConvert.DeserializeObject<LineLoginToken>(res);
                        #endregion

                        #region 用Token查狀態 GET
                        //wc.Encoding = Encoding.UTF8;
                        //wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        //wc.QueryString.Add("access_token", ToKenObj.access_token);
                        //NameValueCollection nameValueCollection1 = new NameValueCollection();
                        //nameValueCollection1.Add("access_token", ToKenObj.access_token);
                        //var bResult1 = wc.DownloadString($"https://api.line.me/oauth2/v2.1/verify");
                        //var res1 = Encoding.UTF8.GetString(bResult);
                        //LineLoginToken ToKenObj1 = JsonConvert.DeserializeObject<LineLoginToken>(res1);
                        #endregion

                        #region 取回User Profile Get
                        wc.Headers.Clear();
                        wc.Headers.Add("Authorization", $"Bearer {ToKenObj.access_token}");
                        string UserProfile = wc.DownloadString("https://api.line.me/v2/profile");
                        LineProfile ProfileObj = JsonConvert.DeserializeObject<LineProfile>(UserProfile);
                        ProfileObj.access_token = ToKenObj.access_token;//紀錄
                        ViewBag.UserProfile = ProfileObj;
                        Session["LineProfile"] = ProfileObj;
                        #endregion
                        return View();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("無法連接遠端伺服器");
                    }
                }
            }
            
            else if (state == "notify_state" && Session["access_token"] == null)//按下訂閱的導回
            {
                using (var wc = new WebClient())
                {
                    try
                    {
                        #region 取回Token POST
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        NameValueCollection nameValueCollection = new NameValueCollection();
                        nameValueCollection.Add("grant_type", "authorization_code");
                        nameValueCollection.Add("code", code);
                        nameValueCollection.Add("redirect_uri", direct_url);
                        nameValueCollection.Add("client_id", notify_client_id);
                        nameValueCollection.Add("client_secret", notify_client_secret);
                        var bResult = wc.UploadValues($"https://notify-bot.line.me/oauth/token", nameValueCollection);
                        var res = Encoding.UTF8.GetString(bResult);
                        LineNotifyToken ToKenObj = JsonConvert.DeserializeObject<LineNotifyToken>(res);
                        string access_token = ToKenObj.access_token;
                   
                        Session["access_token"] = access_token;
                        ViewBag.access_token = access_token;
                        #endregion

                        #region 發送訊息 POST
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Clear();
                        wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        wc.Headers.Add("Authorization", $"Bearer {access_token}");
                        NameValueCollection nameValueCollection1 = new NameValueCollection();
                        nameValueCollection1.Add("message", $"{"授權成功!!"}");
                        var bResult1 = wc.UploadValues($"https://notify-api.line.me/api/notify", nameValueCollection1);
                        var res1 = Encoding.UTF8.GetString(bResult);
                        #endregion

                        LineProfile ProfileObj2 = Session["LineProfile"] as LineProfile;
                        ViewBag.UserProfile = ProfileObj2;

                        return View();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("無法連接遠端伺服器");
                    }
                }

            }
            LineProfile ProfileObj3 = Session["LineProfile"] as LineProfile;
            ViewBag.UserProfile = ProfileObj3;

            ViewBag.access_token = Session["access_token"];
            return View();
        }


        #region 傳送訊息
        public ActionResult LineMessage()
        {
            return View();
        }

        [HttpPost]
        public string LineMessage(string Msg)
        {
            using (var wc = new WebClient())
            {
                try
                {
                    var bearer = Session["access_token"];
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    wc.Headers.Add("Authorization", $"Bearer {bearer}");

                    NameValueCollection nameValueCollection = new NameValueCollection();
                    nameValueCollection.Add("message", $"{Msg}");

                    var bResult = wc.UploadValues($"https://notify-api.line.me/api/notify", nameValueCollection);

                    var res = Encoding.UTF8.GetString(bResult);

                    return res;
                }
                catch (Exception ex)
                {
                    throw new Exception("無法連接遠端伺服器");
                }
            }
        }
        #endregion
    }
}
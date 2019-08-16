using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using interview_test.Models;
using MySql.Data.MySqlClient;
using Interview_C.Models;

namespace interview_test.Controllers
{
    public class HomeController : Controller
    {
        //创建command对象	 
        private MySqlCommand cmd = null;
        //创建connection连接对象
        private MySqlConnection conn = null;
        public IActionResult Index()
        {
            /*//数据库连接字符串
            String connstr = "server=localhost;Database =group_pay;uid=root;pwd='apple1993';charset=utf8";
            //建立数据库连接
            conn = new MySqlConnection(connstr);
            List<CollectInstrument> list = new List<CollectInstrument>();
            MySqlDataReader reader = null;
            try
            {
                conn.Open();    //打开数据库连接
                cmd = new MySqlCommand("SELECT * FROM group_pay.user_role", conn);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    CollectInstrument city = new CollectInstrument();
                    city.Id = reader["id"].ToString();
                    city.Memo = reader["memo"].ToString();
                    city.Name = reader["name"].ToString();
                    list.Add(city);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //⑥关闭DataReader 
                reader.Close();
                //⑦关闭连接 
                conn.Close();
            }*/
            return View();
        }

        public IActionResult Background()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Question()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult TestPage1()
        {
            return View();
        }

        public IActionResult FinishPage()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

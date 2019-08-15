using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using interview_test.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace interview_test.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InterviewTestController : ControllerBase
    {
        //创建command对象	 
        private MySqlCommand cmd = null;
        //创建connection连接对象
        private MySqlConnection conn = null;

        public InterviewTestController()
        {
            //数据库连接字符串
            String connstr = "server=localhost;Database =interview;uid=root;pwd='apple1993';charset=utf8";
            //建立数据库连接
            conn = new MySqlConnection(connstr);
        }

        [HttpGet]
        public ActionResult<List<Page1>> Get([FromQuery]long userid = 0)
        {
            List<Page1> list = new List<Page1>();
            MySqlDataReader reader = null;
            try
            {
                conn.Open();    //打开数据库连接
                cmd = new MySqlCommand("SELECT * FROM interview.test_choice", conn);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Page1 choice = new Page1();
                    choice.Id = reader["id"].ToString();
                    choice.TYPE = reader["type"].ToString();
                    choice.TOPIC = reader["topic"].ToString();
                    choice.A = reader["a"].ToString();
                    choice.B = reader["b"].ToString();
                    choice.C = reader["c"].ToString();
                    choice.D = reader["d"].ToString();
                    choice.ANSWER = reader["answer"].ToString();
                    choice.NOTE = reader["note"].ToString();
                    list.Add(choice);
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
            }
            return this.Ok(new List<Page1>(list));
        }

        [HttpPost]
        public ActionResult<TestDone> POST([FromBody]TestDone TestDone)
        {
            List<TestDone> list = new List<TestDone>();
            try
            {
                DateTime myDate = DateTime.Now;
                string myDateString = myDate.ToString("yyyy-MM-dd HH:mm:ss");
                conn.Open();    //打开数据库连接
                MySqlCommand cmd;
                cmd = conn.CreateCommand();
                cmd.CommandText = $"INSERT INTO interviewee ( name , answer , test_time , timestamp ) VALUES (@name , @answer , @test_time , @timestamp )";
                cmd.Parameters.AddWithValue("@name",TestDone.NAME.ToString());
                cmd.Parameters.AddWithValue("@answer", TestDone.ANSWER.ToString());
                cmd.Parameters.AddWithValue("@test_time", TestDone.TESTTIME.ToString());
                cmd.Parameters.AddWithValue("@timestamp", myDateString);
                cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                //⑦关闭连接 
                conn.Close();
            }
            return this.Ok(new ActionResult<TestDone>(TestDone));
        }
    }
}
/*
  conn.Open();    //打开数据库连接
                MySqlCommand cmd = new MySqlCommand($"INSERT INTO interviewee ( name , answer , test_time , timestamp , note) " +
                    "VALUES (@name , @answer , @test_time , @timestamp , @note)", conn);
                cmd.ExecuteNonQuery();
     */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using interview_test.Models;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace interview_test.Controllers
{
    [Route("api/[controller]")]
    public class BackgroundController : Controller
    {
        //创建command对象	 
        private MySqlCommand cmd = null;
        //创建connection连接对象
        private MySqlConnection conn = null;

        public BackgroundController()
        {
            //数据库连接字符串
            String connstr = "server=localhost;Database =interview;uid=root;pwd='apple1993';charset=utf8";
            //建立数据库连接
            conn = new MySqlConnection(connstr);
        }

        [HttpGet]
        public ActionResult<List<TestDone>> Get()
        {
            List<TestDone> list = new List<TestDone>();
            MySqlDataReader reader = null;
            try
            {
                conn.Open();    //打开数据库连接
                cmd = new MySqlCommand("SELECT * FROM interview.interviewee", conn);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    TestDone answer = new TestDone();
                    answer.Id = reader["id"].ToString();
                    answer.NAME = reader["name"].ToString();
                    answer.ANSWER = reader["answer"].ToString();
                    answer.TESTTIME = reader["test_time"].ToString();
                    answer.TIMESTAMP = reader["timestamp"].ToString();
                    answer.STATUS = reader["status"].ToString();
                    answer.SCORE = reader["score"].ToString();
                    answer.NOTE = reader["note"].ToString();
                    list.Add(answer);
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                reader.Close();
                conn.Close();
            }
            return this.Ok(new List<TestDone>(list));
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<controller>
        [HttpPost]
        public ActionResult<TestDone> POST([FromQuery]long count,[FromBody]TestDone TestDone)
        {
            List<TestDone> list = new List<TestDone>();
            try
            {
                conn.Open();    //打开数据库连接
                MySqlCommand cmd;
                cmd = conn.CreateCommand();
                cmd.CommandText = $"UPDATE interviewee SET status = '1', score = @score, note = @note WHERE id = @count";
                cmd.Parameters.AddWithValue("@score", TestDone.SCORE.ToString());
                cmd.Parameters.AddWithValue("@note", TestDone.NOTE.ToString());
                cmd.Parameters.AddWithValue("@count", count.ToString());
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

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

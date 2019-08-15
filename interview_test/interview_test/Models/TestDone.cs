using System;
using System.ComponentModel.DataAnnotations.Schema;


namespace interview_test.Models
{
    public class TestDone 
    {

        [Column("id")]
        public string Id { get; set; }

        [Column("name")]
        public string NAME { get; set; }

        [Column("answer")]
        public string ANSWER { get; set; }

        [Column("test_time")]
        public string TESTTIME { get; set; }

        [Column("timestamp")]
        public string TIMESTAMP { get; set; }

        [Column("status")]
        public string STATUS { get; set; }

        [Column("score")]
        public string SCORE { get; set; }

        [Column("note")]
        public string NOTE { get; set; }

    }
}

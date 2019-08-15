using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace interview_test.Models
{
    public class Page1
    {
        [Column("id")]
        public string Id { get; set; }

        [Column("topic")]
        public string TOPIC { get; set; }

        [Column("type")]
        public string TYPE { get; set; }

        [Column("a")]
        public string A { get; set; }

        [Column("b")]
        public string B { get; set; }

        [Column("c")]
        public string C { get; set; }

        [Column("d")]
        public string D { get; set; }

        [Column("answer")]
        public string ANSWER { get; set; }

        [Column("note")]
        public string NOTE { get; set; }
    }
}

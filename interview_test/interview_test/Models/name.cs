using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Interview_C.Models
{
    public class CollectInstrument
    {
        [Column("id")]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("memo")]
        public string Memo { get; set; }
    }
}

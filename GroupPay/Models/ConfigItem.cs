using System;
using Core.Data;

namespace GroupPay.Models
{
    public class ConfigItem
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("display_name")]
        public string DisplayName { get; set; }

        [Column("value")]
        public string Value { get; set; }
    }
}

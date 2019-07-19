using System;
using Core.Data;

namespace GroupPay.Models
{
    public class ServiceInstance
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("service_id")]
        public int ServiceId { get; set; }

        [Column("cluster")]
        public string Cluster { get; set; }

        [Column("server")]
        public string Server { get; set; }

        [Column("endpoint")]
        public string Endpoint { get; set; }
    }
}

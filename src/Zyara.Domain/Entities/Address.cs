using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    internal class Address
    {
        public int Id { get; set; }
        public string Lable { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string StreetAndBuilding { get; set; }
        public decimal? Latitude { get; set; } // Optional GPS latitude for distance filtering
        public decimal? Longitude { get; set; } // Optional GPS longitude for distance filtering
    }
}

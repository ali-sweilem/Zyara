using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class Address
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public string Label { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string StreetAndBuilding { get; set; } = string.Empty;
        public double? Latitude { get; set; }   // optional GPS, for the distance filter
        public double? Longitude { get; set; }
    }
}

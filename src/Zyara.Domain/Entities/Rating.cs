using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class Rating
    {
        public int Id { get; set; }

        public int BookingId { get; set; }

        public int Stars { get; set; }

        public string? Comment { get; set; }
    }
}

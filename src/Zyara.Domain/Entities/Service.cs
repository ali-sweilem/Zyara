using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Domain.Entities
{
    public class Service
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public ServiceType Type { get; set; }
        public bool IsActive { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class Doctor:Provider
    {
        public string licenser { get; set; }
        public List<Prescription> prescriptions { get; set; }
    }
}

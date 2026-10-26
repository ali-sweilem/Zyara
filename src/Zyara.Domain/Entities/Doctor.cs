using System.Collections.Generic;

namespace Zyara.Domain.Entities
{
    public class Doctor : Provider
    {
        public string licenser { get; set; } = string.Empty;
        public List<Prescription> prescriptions { get; set; } = new();
    }
}


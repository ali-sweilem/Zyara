using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class Patient : User
    {
        public DateOnly DateOfBirth { get; set; }
    }
}
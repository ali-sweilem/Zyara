using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class Admin : User
    {
        public List<Provider> Providers { get; set; }
    }
}

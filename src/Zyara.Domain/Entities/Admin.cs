using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class Admin : User
    {
        // no attributes of its own; Provider.VerifiedByAdminId holds the relationship
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Enums
{
    public enum PaymentStatus
    {
        Pending,
        Authorized,
        Captured,
        Collected,
        Released,
        Refunded,
        PartiallyRefunded
    }
}
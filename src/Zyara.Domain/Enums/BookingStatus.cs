using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Enums
{
    public enum BookingStatus
    {
        PendingProviderApproval,
        Confirmed,
        ConfirmedCollectionPending,
        Started,
        Completed,
        Cancelled,
        Rejected,
        Expired
    }
}
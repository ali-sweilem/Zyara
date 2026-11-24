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
        Expired,
        NoShow          // confirmed, but not started 30 minutes after the window opened (set by the background job)
    }
}

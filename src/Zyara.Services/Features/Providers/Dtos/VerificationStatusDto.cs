using System;
using System.Collections.Generic;
using System.Text;
using Zyara.Domain.Enums;

namespace Zyara.Services.Features.Providers.Dtos
{
    public class VerificationStatusDto
    {
        public VerificationStatus  Status { get; set; }
        public String? RejectionReason { get; set; }
    }
}

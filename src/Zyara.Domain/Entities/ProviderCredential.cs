using System;
using System.Collections.Generic;
using System.Text;

namespace Zyara.Domain.Entities
{
    public class ProviderCredential
    {
        public int Id { get; set; }
        public string DocumentType { get; set; }
        public string FileUrl { get; set; }
        public int ProviderID { get; set; }
        public Provider Provider { get; set; }
    }
}

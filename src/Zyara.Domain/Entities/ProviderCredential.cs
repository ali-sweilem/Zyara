using System;

namespace Zyara.Domain.Entities
{
    public class ProviderCredential
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public int ProviderID { get; set; }
        public Provider? Provider { get; set; }
    }
}


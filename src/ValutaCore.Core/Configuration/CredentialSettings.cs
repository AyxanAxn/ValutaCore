namespace ValutaCore.Core.Configuration
{
    public class CredentialSettings
    {
        public const string SectionName = "UserCredentials";

        public List<CredentialProfile> Users { get; set; } = [];
    }
}
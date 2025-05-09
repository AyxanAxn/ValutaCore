namespace ValutaCore.Core.Configuration
{
    public class CredentialSettings
    {
        public const string SectionName = "UserCredentials";

        // Rename to match the JSON key:
        public List<CredentialProfile> Users { get; set; } = [];
    }
}
using CredentialManagement;

namespace QuantConnect.VisualStudioPlugin
{
    class CredentialsManager
    {
        private const string CREDENTIAL_TARGET = "QuantConnectPlugin";

        public Credentials? GetLastCredential()
        {
            var cm = new Credential { Target = CREDENTIAL_TARGET };
            if (!cm.Load())
            {
                return null;
            }

            return new Credentials(cm.Username, cm.Password);
        }

        public void SetCredentials(Credentials credentials)
        {
            var credential = new Credential
            {
                Target = CREDENTIAL_TARGET,
                Username = credentials.UserId,
                Password = credentials.AccessToken,
                PersistanceType = PersistanceType.LocalComputer
            };
            
            credential.Save();
        }
    }
}

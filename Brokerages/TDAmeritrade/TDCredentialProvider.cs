using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TDAmeritradeApi.Client;

namespace QuantConnect.Brokerages.TDAmeritrade
{
    public class TDCliCredentialProvider : ICredentials
    {
        public string GetPassword()
        {
            Console.WriteLine("Password: ");

            return Console.ReadLine();
        }

        public string GetSmsCode()
        {
            Console.WriteLine("Sms code: ");

            return Console.ReadLine();
        }

        public string GetUserName()
        {
            Console.WriteLine("Username: ");

            return Console.ReadLine();
        }
    }
}

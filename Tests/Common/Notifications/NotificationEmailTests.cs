using System;
using NUnit.Framework;
using QuantConnect.Notifications;

namespace QuantConnect.Tests.Common.Notifications
{
    [TestFixture]
    public class NotificationEmailTests
    {
        [Test]
        public void Constructor_SetsNullData_ToEmptyString()
        {
            // empty string used as default following NotificationManager.Email's default value for data
            var email = new NotificationEmail("e@d.com", "subject", "message", null);
            Assert.AreEqual(string.Empty, email.Data);
        }

        [Test]
        public void Constructor_SetsNullSubject_ToDefaultQuantConnectLeanEmailNotification()
        {
            // default subject chosen to provide context for the email
            var email = new NotificationEmail("e@d.com", null, "message", "data");
            Assert.AreEqual("QuantConnect LEAN Email Notification", email.Subject);
        }

        [Test]
        public void Constructor_SetsNullMessage_ToDefaultQuantConnectLeanEmailNotification()
        {
            // default message chosen to provide context for the email
            var email = new NotificationEmail("e@d.com", "subject", null, "data");
            Assert.AreEqual("QuantConnect LEAN Email Notification", email.Message);
        }

        [Test]
        [TestCase("js@contoso.中国", true)]
        [TestCase("j@proseware.com9", true)]
        [TestCase("js@proseware.com9", true)]
        [TestCase("j_9@[129.126.118.1]", true)]
        [TestCase("jones@ms1.proseware.com", true)]
        [TestCase("david.jones@proseware.com", true)]
        [TestCase("d.j@server1.proseware.com", true)]
        [TestCase("js#internal@proseware.com", true)]
        [TestCase("j.s@server1.proseware.com", true)]
        [TestCase("\"j\\\"s\\\"\"@proseware.com", true)]

        [TestCase("js*@proseware.com", false)]
        [TestCase("js@proseware..com", false)]
        [TestCase("j..s@proseware.com", false)]
        [TestCase("j.@server1.proseware.com", false)]
        public void Constructor_ThrowsArgumentException_WhenEmailAddressIsInvalid(string address, bool isValid)
        {
            // Test cases sourced via msdn:
            // https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format

            TestDelegate ctor = () => new NotificationEmail(
                address,
                string.Empty,
                string.Empty,
                string.Empty
            );

            if (isValid)
            {
                Assert.DoesNotThrow(ctor);
            }
            else
            {
                Assert.Throws<ArgumentException>(ctor);
            }
        }
    }
}
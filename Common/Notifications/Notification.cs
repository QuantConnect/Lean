/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
/**********************************************************
* USING NAMESPACES
**********************************************************/
namespace QuantConnect.Notifications
{
    /******************************************************** 
    * CLASS DEFINITIONS
    *********************************************************/
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    public abstract class Notification
    {
        /// <summary>
        /// Type of the underlying notification packet:
        /// </summary>
        public NotificationType Type;
    }


    /// <summary>
    /// Web Notification Class
    /// </summary>
    public class NotificationWeb : Notification
    {
        /// <summary>
        /// Send a notification message to this web address
        /// </summary>
        public string Address;

        /// <summary>
        /// Object data to send.
        /// </summary>
        public object Data;

        /// <summary>
        /// Constructor for sending a notification SMS to a specified phone number
        /// </summary>
        /// <param name="address"></param>
        /// <param name="data"></param>
        public NotificationWeb(string address, object data = null)
        {
            Address = address;
            Data = data;
            Type = NotificationType.Web;
        }
    }

    /// <summary>
    /// Sms Notification Class
    /// </summary>
    public class NotificationSms : Notification
    {
        /// <summary>
        /// Send a notification message to this phone number
        /// </summary>
        public string PhoneNumber;

        /// <summary>
        /// Message to send. Limited to 160 characters
        /// </summary>
        public string Message;

        /// <summary>
        /// Constructor for sending a notification SMS to a specified phone number
        /// </summary>
        /// <param name="number"></param>
        /// <param name="message"></param>
        public NotificationSms(string number, string message)
        {
            PhoneNumber = number;
            Message = message;
            Type = NotificationType.Sms;
        }
    }


    /// <summary>
    /// Email notification data.
    /// </summary>
    public class NotificationEmail : Notification
    {
        /// <summary>
        /// Send to address:
        /// </summary>
        public string Address;

        /// <summary>
        /// Email subject
        /// </summary>
        public string Subject;

        /// <summary>
        /// Message to send.
        /// </summary>
        public string Message;

        /// <summary>
        /// Email Data
        /// </summary>
        public string Data;

        /// <summary>
        /// Default constructor for sending an email notification
        /// </summary>
        /// <param name="address">Address to send to</param>
        /// <param name="subject">Subject of the email</param>
        /// <param name="message">Message body of the email</param>
        /// <param name="data">Data to attach to the email</param>
        public NotificationEmail(string address, string subject, string message, string data)
        {
            Type = NotificationType.Email;
            Message = message;
            Data = data;
            Subject = subject;
            Address = address;
        }
    }

    /// <summary>
    /// Type of the notification packet
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Send an email to your user account address.
        /// </summary>
        Email,

        /// <summary>
        /// Send a SMS to a mobile phone
        /// </summary>
        Sms,

        /// <summary>
        /// Web notification type for sending a request to web hook.
        /// </summary>
        Web
    }
}

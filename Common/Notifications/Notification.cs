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

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using QuantConnect.Util;

namespace QuantConnect.Notifications
{
    /// <summary>
    /// Local/desktop implementation of messaging system for Lean Engine.
    /// </summary>
    [JsonConverter(typeof(NotificationJsonConverter))]
    public abstract class Notification
    {
        /// <summary>
        /// Method for sending implementations of notification object types.
        /// </summary>
        /// <remarks>SMS, Email and Web are all handled by the QC Messaging Handler. To implement your own notification type implement it here.</remarks>
        public virtual void Send()
        {
            //
        }
    }

    /// <summary>
    /// Web Notification Class
    /// </summary>
    public class NotificationWeb : Notification
    {
        /// <summary>
        /// Optional email headers
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> Headers;

        /// <summary>
        /// Send a notification message to this web address
        /// </summary>
        public string Address;

        /// <summary>
        /// Object data to send.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public object Data;

        /// <summary>
        /// Constructor for sending a notification SMS to a specified phone number
        /// </summary>
        /// <param name="address">Address to send to</param>
        /// <param name="data">Data to send</param>
        /// <param name="headers">Optional headers to use</param>
        public NotificationWeb(string address, object data = null, Dictionary<string, string> headers = null)
        {
            Address = address;
            Data = data;
            Headers = headers;
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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
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
        }
    }

    /// <summary>
    /// Email notification data.
    /// </summary>
    public class NotificationEmail : Notification
    {
        /// <summary>
        /// Optional email headers
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Dictionary<string, string> Headers;

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
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Message;

        /// <summary>
        /// Email Data
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Data;

        /// <summary>
        /// Default constructor for sending an email notification
        /// </summary>
        /// <param name="address">Address to send to. Will throw <see cref="ArgumentException"/> if invalid
        /// <see cref="Validate.EmailAddress"/></param>
        /// <param name="subject">Subject of the email. Will set to <see cref="string.Empty"/> if null</param>
        /// <param name="message">Message body of the email. Will set to <see cref="string.Empty"/> if null</param>
        /// <param name="data">Data to attach to the email. Will set to <see cref="string.Empty"/> if null</param>
        /// <param name="headers">Optional email headers to use</param>
        public NotificationEmail(string address, string subject = "", string message = "", string data = "", Dictionary<string, string> headers = null)
        {
            if (!Validate.EmailAddress(address))
            {
                throw new ArgumentException(Messages.NotificationEmail.InvalidEmailAddress(address));
            }

            Address = address;
            Data = data ?? string.Empty;
            Message = message ?? string.Empty;
            Subject = subject ?? string.Empty;
            Headers = headers;
        }
    }

    /// <summary>
    /// Telegram notification data
    /// </summary>
    public class NotificationTelegram : Notification
    {
        /// <summary>
        /// Send a notification message to this user on Telegram
        /// Can be either a personal ID or Group ID.
        /// </summary>
        public string Id;

        /// <summary>
        /// Message to send. Limited to 4096 characters
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Message;

        /// <summary>
        /// Token to use
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Token;

        /// <summary>
        /// Constructor for sending a telegram notification to a specific User ID
        /// or group ID. Note: The bot must have an open chat with the user or be
        /// added to the group for messages to deliver.
        /// </summary>
        /// <param name="id">User Id or Group Id to send the message too</param>
        /// <param name="message">Message to send</param>
        /// <param name="token">Bot token to use, if null defaults to "telegram-token"
        /// in config on send</param>
        public NotificationTelegram(string id, string message, string token = null)
        {
            Id = id;
            Message = message;
            Token = token;
        }
    }

    /// <summary>
    /// FTP notification data
    /// </summary>
    public class NotificationFtp : Notification
    {
        private static readonly Regex HostnameProtocolRegex = new Regex(@"^[s]?ftp\:\/\/", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const int DefaultPort = 21;

        /// <summary>
        /// Whether to use SFTP or FTP.
        /// </summary>
        public bool Secure { get; }

        /// <summary>
        /// The FTP server hostname.
        /// </summary>
        public string Hostname { get; }

        /// <summary>
        /// The FTP server port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// The FTP server username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// The FTP server password.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Password { get; }

        /// <summary>
        /// The path to file on the FTP server.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// The contents of the file to send.
        /// </summary>
        public string FileContent { get; private set; }

        /// <summary>
        /// The public key to use for authentication (optional).
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? PublicKey { get; }

        /// <summary>
        /// The private key to use for authentication (optional).
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? PrivateKey { get; }

        /// <summary>
        /// The passphrase for the private key (optional).
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? PrivateKeyPassphrase { get; }

        private NotificationFtp(string hostname, string username, string filePath, byte[] fileContent, bool secure, int? port)
        {
            Hostname = NormalizeHostname(hostname);
            Port = port ?? DefaultPort;
            Username = username;
            FilePath = filePath;
            FileContent = Convert.ToBase64String(fileContent);
            Secure = secure;
        }

        /// <summary>
        /// Constructor for a notification to sent as a file to an FTP server using password authentication.
        /// </summary>
        /// <param name="hostname">FTP server hostname</param>
        /// <param name="username">The FTP server username</param>
        /// <param name="password">The FTP server password</param>
        /// <param name="filePath">The path to file on the FTP server</param>
        /// <param name="fileContent">The contents of the file</param>
        /// <param name="secure">Whether to use SFTP or FTP. Defaults to true</param>
        /// <param name="port">The FTP server port. Defaults to 21</param>
        public NotificationFtp(string hostname, string username, string password, string filePath, byte[] fileContent,
            bool secure = true, int? port = null)
            : this(hostname, username, filePath, fileContent, secure, port)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException(Messages.NotificationFtp.MissingPassword);
            }

            Password = password;
        }

        /// <summary>
        /// Constructor for a notification to sent as a file to an FTP server over SFTP using SSH keys.
        /// </summary>
        /// <param name="hostname">FTP server hostname</param>
        /// <param name="username">The FTP server username</param>
        /// <param name="publicKey">The public SSH key to use for authentication</param>
        /// <param name="privateKey">The private SSH key to use for authentication</param>
        /// <param name="filePath">The path to file on the FTP server</param>
        /// <param name="fileContent">The contents of the file</param>
        /// <param name="port">The FTP server port. Defaults to 21</param>
        /// <param name="privateKeyPassphrase">The optional passphrase to decrypt the private key</param>
        public NotificationFtp(string hostname, string username, string publicKey, string privateKey,
            string filePath, byte[] fileContent, int? port = null, string privateKeyPassphrase = null)
            : this(hostname, username, filePath, fileContent, true, port)
        {
            if (string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                throw new ArgumentException(Messages.NotificationFtp.MissingSSHKeys);
            }

            PublicKey = publicKey;
            PrivateKey = privateKey;
            PrivateKeyPassphrase = privateKeyPassphrase;
        }

        /// <summary>
        /// Constructor for a notification to sent as a file to an FTP server using password authentication.
        /// </summary>
        /// <param name="hostname">FTP server hostname</param>
        /// <param name="username">The FTP server username</param>
        /// <param name="password">The FTP server password</param>
        /// <param name="filePath">The path to file on the FTP server</param>
        /// <param name="fileContent">The contents of the file</param>
        /// <param name="secure">Whether to use SFTP or FTP. Defaults to true</param>
        /// <param name="port">The FTP server port. Defaults to 21</param>
        public NotificationFtp(string hostname, string username, string password, string filePath, string fileContent,
            bool secure = true, int? port = null)
            : this(hostname, username, password, filePath, Encoding.ASCII.GetBytes(fileContent), secure, port)
        {
        }

        /// <summary>
        /// Constructor for a notification to sent as a file to an FTP server over SFTP using SSH keys.
        /// </summary>
        /// <param name="hostname">FTP server hostname</param>
        /// <param name="username">The FTP server username</param>
        /// <param name="publicKey">The public SSH key to use for authentication</param>
        /// <param name="privateKey">The private SSH key to use for authentication</param>
        /// <param name="filePath">The path to file on the FTP server</param>
        /// <param name="fileContent">The contents of the file</param>
        /// <param name="port">The FTP server port. Defaults to 21</param>
        /// <param name="privateKeyPassphrase">The optional passphrase to decrypt the private key</param>
        public NotificationFtp(string hostname, string username, string publicKey, string privateKey,
            string filePath, string fileContent, int? port = null, string privateKeyPassphrase = null)
            : this(hostname, username, publicKey, privateKey, filePath, Encoding.ASCII.GetBytes(fileContent), port, privateKeyPassphrase)
        {
        }

        private static string NormalizeHostname(string hostname)
        {
            // Remove trailing slashes
            hostname = hostname.Trim().TrimEnd('/');
            // Remove protocol if present
            return HostnameProtocolRegex.Replace(hostname, "");
        }

        /// <summary>
        /// Factory method for Json deserialization: the file contents are already encoded
        /// </summary>
        internal static NotificationFtp FromEncodedData(string hostname, string username, string password, string filePath, string encodedFileContent,
            bool secure, int? port)
        {
            var notification = new NotificationFtp(hostname, username, password, filePath, Array.Empty<byte>(), secure, port);
            notification.FileContent = encodedFileContent;
            return notification;
        }

        /// <summary>
        /// Factory method for Json deserialization: the file contents are already encoded
        /// </summary>
        internal static NotificationFtp FromEncodedData(string hostname, string username, string publicKey, string privateKey,
            string filePath, string encodedFileContent, int? port, string privateKeyPassphrase)
        {
            var notification = new NotificationFtp(hostname, username, publicKey, privateKey, filePath, Array.Empty<byte>(), port, privateKeyPassphrase);
            notification.FileContent = encodedFileContent;
            return notification;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="Notification"/>
    /// </summary>
    public static class NotificationExtensions
    {
        /// <summary>
        /// Check if the notification can be sent (implements the <see cref="Notification.Send"/> method)
        /// </summary>
        /// <param name="notification">The notification</param>
        /// <returns>Whether the notification can be sent</returns>
        public static bool CanSend(this Notification notification)
        {
            var type = notification.GetType();
            return type != typeof(NotificationEmail) &&
                type != typeof(NotificationWeb) &&
                type != typeof(NotificationSms) &&
                type != typeof(NotificationTelegram) &&
                type != typeof(NotificationFtp);
        }
    }
}

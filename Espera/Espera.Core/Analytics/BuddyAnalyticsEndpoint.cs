﻿using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Buddy;

namespace Espera.Core.Analytics
{
    public class BuddyAnalyticsEndpoint : IAnalyticsEndpoint
    {
        private readonly BuddyClient client;
        private AuthenticatedUser storedUser;

        public BuddyAnalyticsEndpoint()
        {
            this.client = new BuddyClient("bbbbbc.mhbbbxjLrKNl", "83585740-AE7A-4F68-828D-5E6A8825A0EE", autoRecordDeviceInfo: false);
        }

        public async Task AuthenticateUserAsync(string analyticsToken)
        {
            this.storedUser = await this.client.LoginAsync(analyticsToken);
        }

        public async Task<string> CreateUserAsync()
        {
            string throwAwayToken = Guid.NewGuid().ToString();

            AuthenticatedUser user = await this.client.CreateUserAsync(throwAwayToken, throwAwayToken);

            this.storedUser = user;

            return user.Token;
        }

        public async Task RecordDeviceInformationAsync()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            await this.client.Device.RecordInformationAsync(Environment.OSVersion.VersionString, "Desktop", this.storedUser, version);
            await this.RecordLanguageAsync();
        }

        public Task RecordErrorAsync(string message, string logId, string stackTrace = null)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            return this.client.Device.RecordCrashAsync(message, Environment.OSVersion.VersionString, "Desktop", this.storedUser, stackTrace, version, metadata: logId);
        }

        public Task RecordMetaDataAsync(string key, string value)
        {
            return this.storedUser.Metadata.SetAsync(key, value);
        }

        public async Task<string> SendBlobAsync(string name, string mimeType, Stream data)
        {
            Blob blob = await this.storedUser.Blobs.AddAsync(name, mimeType, String.Empty, 0, 0, data);
            return blob.BlobID.ToString(CultureInfo.InvariantCulture);
        }

        public Task UpdateUserEmailAsync(string email)
        {
            AuthenticatedUser user = this.storedUser;

            return user.UpdateAsync(user.Email, String.Empty, user.Gender, user.Age, email, // email is the only field we change here
                user.Status, user.LocationFuzzing, user.CelebrityMode, user.ApplicationTag);
        }
    }
}

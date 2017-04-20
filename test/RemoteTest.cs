// Copyright (C) 2017 Dmitry Yakimenko (detunized@gmail.com).
// Licensed under the terms of the MIT license. See LICENCE for details.

using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using NUnit.Framework;

namespace TrueKey.Test
{
    [TestFixture]
    class RemoteTest
    {
        [Test]
        public void RegisetNewDevice_returns_device_info()
        {
            var client = SetupPostWithFixture("register-new-device-response");
            var result = Remote.RegisetNewDevice("truekey-sharp", client.Object);

            Assert.That(result.Token, Is.StringStarting("AQCmAwEA"));
            Assert.That(result.Id, Is.StringStarting("d871347b"));
        }

        [Test]
        public void AuthStep1_returns_transaction_id()
        {
            var client = SetupPostWithFixture("auth-step1-response");
            var result = Remote.AuthStep1(ClientInfo, client.Object);

            Assert.That(result, Is.EqualTo("6cdfcd43-065c-43a1-aa7a-017de98eefd0"));
        }

        [Test]
        public void AuthStep2_returns_two_factor_settings()
        {
            var client = SetupPostWithFixture("auth-step2-response");
            var result = Remote.AuthStep2(ClientInfo, "password", "transaction-id", client.Object);

            Assert.That(result.InitialStep, Is.EqualTo(TwoFactorAuth.Step.WaitForOob));
            Assert.That(result.TransactionId, Is.EqualTo("ae830c59-634b-437c-95b6-58158e85ffae"));
            Assert.That(result.Email, Is.EqualTo("username@example.com"));
            Assert.That(result.OAuthToken, Is.EqualTo(""));

            Assert.That(result.Devices.Length, Is.EqualTo(1));
            Assert.That(result.Devices[0].Name, Is.EqualTo("LGE Nexus 5"));
            Assert.That(result.Devices[0].Id, Is.StringStarting("MTU5NjAwMjI3MQP04dNsmSNQ2L"));
        }

        [Test]
        public void AuthCheck_returns_oauth_token()
        {
            var client = SetupPostWithFixture("auth-check-success-response");
            var result = Remote.AuthCheck(ClientInfo, "transaction-id", client.Object);

            Assert.That(result, Is.StringStarting("eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI"));
        }

        [Test]
        public void AuthCheck_throws_on_pending()
        {
            var client = SetupPostWithFixture("auth-check-pending-response");

            Assert.That(() => Remote.AuthCheck(ClientInfo, "transaction-id", client.Object),
                        Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void GetVault_returns_encrypted_accounts()
        {
            var client = SetupGetWithFixture("get-vault-response");
            var accounts = Remote.GetVault("oauth-token", client.Object);

            Assert.That(accounts.Length, Is.EqualTo(2));

            Assert.That(accounts[0].Id, Is.EqualTo(50934080));
            Assert.That(accounts[0].Name, Is.EqualTo("Google"));
            Assert.That(accounts[0].Username, Is.EqualTo("dude@gmail.com"));
            Assert.That(accounts[0].EncryptedPassword, Is.EqualTo("AAR24UbLgkHUhsSXB2mndMISE7U5qn+WA3znhgdXex0br6y5".Decode64()));
            Assert.That(accounts[0].Url, Is.EqualTo("https://accounts.google.com/ServiceLogin"));
            Assert.That(accounts[0].EncryptedNote, Is.EqualTo("AAS2l1XcabgdPTM3CuUZDbT5txJu1ou0gOQ=".Decode64()));

            Assert.That(accounts[1].Id, Is.EqualTo(60789074));
            Assert.That(accounts[1].Name, Is.EqualTo("facebook"));
            Assert.That(accounts[1].Username, Is.EqualTo("mark"));
            Assert.That(accounts[1].EncryptedPassword, Is.EqualTo("AAShzvG+qXE7bT8MhAbbXelu/huVjuUMDC8IsLw4Lw==".Decode64()));
            Assert.That(accounts[1].Url, Is.EqualTo("http://facebook.com"));
            Assert.That(accounts[1].EncryptedNote, Is.EqualTo("".Decode64()));
        }

        //
        // Data
        //

        private const string Username = "username@example.com";
        private const string DeviceName = "truekey-sharp";

        private const string ClientToken = "AQCmAwEAAh4AAAAAWMajHQAAGU9DUkEtMTpIT1RQLVNIQTI1Ni" +
                                           "0wOlFBMDgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                                           "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
                                           "AAAAAAAAAAAAAAAAAAAAAAAAAAIOiRfItpCTOkvq0ZfV2+GgvP" +
                                           "83aF9SrTBfOuabZfcQr9AAAAAAgAIBwWTZpUTIn493Us/Jwczr" +
                                           "K6O0+LH8FRidFaZkJ2AlTu";

        private const string DeviceId = "d871347bd0a3e7af61f60f511bc7de5e944c5c778705649d4aa8d" +
                                        "c77bcd21489412894";

        private static readonly Remote.DeviceInfo DeviceInfo = new Remote.DeviceInfo(
            token: ClientToken,
            id: DeviceId);

        private static readonly Crypto.OtpInfo OtpInfo = new Crypto.OtpInfo(
            version: 3,
            otpAlgorithm: 1,
            otpLength: 0,
            hashAlgorithm: 2,
            timeStep: 30,
            startTime: 0,
            suite: "OCRA-1:HOTP-SHA256-0:QA08".ToBytes(),
            hmacSeed: "6JF8i2kJM6S+rRl9Xb4aC8/zdoX1KtMF865ptl9xCv0=".Decode64(),
            iptmk: "HBZNmlRMifj3dSz8nBzOsro7T4sfwVGJ0VpmQnYCVO4=".Decode64());

        private static readonly Remote.ClientInfo ClientInfo = new Remote.ClientInfo(
            username: Username,
            name: DeviceName,
            deviceInfo: DeviceInfo,
            otpInfo: OtpInfo);

        //
        // Helpers
        //

        private static Mock<IHttpClient> SetupGet(string response)
        {
            var mock = new Mock<IHttpClient>();
            mock.Setup(x => x.Get(It.IsAny<string>(),
                                  It.IsAny<Dictionary<string, string>>()))
                .Returns(response);
            return mock;
        }

        private static Mock<IHttpClient> SetupGetWithFixture(string name)
        {
            return SetupGet(ReadFixture(name));
        }

        private static Mock<IHttpClient> SetupPost(string response)
        {
            var mock = new Mock<IHttpClient>();
            mock.Setup(x => x.Post(It.IsAny<string>(),
                                   It.IsAny<Dictionary<string, object>>()))
                .Returns(response);
            return mock;
        }

        private static Mock<IHttpClient> SetupPostWithFixture(string name)
        {
            return SetupPost(ReadFixture(name));
        }

        private static string ReadFixture(string name)
        {
            return File.ReadAllText(string.Format("Fixtures/{0}.json", name));
        }
    }
}

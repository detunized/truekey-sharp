// Copyright (C) 2017 Dmitry Yakimenko (detunized@gmail.com).
// Licensed under the terms of the MIT license. See LICENCE for details.

using System.Linq;

namespace TrueKey
{
    public class Vault
    {
        public readonly Account[] Accounts;

        public static Vault Open(string username, string password, Gui gui, ISecureStorage storage)
        {
            return Open(username, password, gui, storage, new HttpClient());
        }

        public static Vault Open(string username,
                                 string password,
                                 Gui gui,
                                 ISecureStorage storage,
                                 IHttpClient http)
        {
            // Step 1: Register a new device and get a token and an id back.
            var deviceInfo = Remote.RegisetNewDevice("truekey-sharp", http);

            // Step 2: Parse the token to decode OTP information.
            var otpInfo = Crypto.ParseClientToken(deviceInfo.Token);

            // Step 3: Validate the OTP info to make sure it's got only the
            //         things we support at the moment.
            Crypto.ValidateOtpInfo(otpInfo);

            // Bundle up everything in one place
            var clientInfo = new Remote.ClientInfo(username, "truekey-sharp", deviceInfo, otpInfo);

            // Step 4: Auth step 1 gives us a transaction id to pass along to the next step.
            var transactionId = Remote.AuthStep1(clientInfo, http);

            // Step 5: Auth step 2 gives us the instructions on what to do next. For a new client that
            //         would be some form of second factor auth. For a known client that would be a
            //         pair of OAuth tokens.
            var whatsNext = Remote.AuthStep2(clientInfo, password, transactionId, http);

            // Step 6: Auth FSM -- walk through all the auth steps until we're done.
            var oauthToken = TwoFactorAuth.Start(clientInfo, whatsNext, gui, http);

            // Step 7: Get the vault from the server.
            var encryptedVault = Remote.GetVault(oauthToken, http);

            // Step 8: Compute the master key.
            var masterKey = Crypto.DecryptMasterKey(password,
                                                    encryptedVault.MasterKeySalt,
                                                    encryptedVault.EncryptedMasterKey);

            // Step 9: Decrypt the accounts.
            var accounts = encryptedVault.EncryptedAccounts
                .Select(i => new Account(
                            i.Id,
                            i.Name,
                            i.Username,
                            Crypto.Decrypt(masterKey, i.EncryptedPassword).ToUtf8(),
                            i.Url,
                            Crypto.Decrypt(masterKey, i.EncryptedNote).ToUtf8()))
                .ToArray();

            return new Vault(accounts);
        }

        private Vault(Account[] accounts)
        {
            Accounts = accounts;
        }
    }
}

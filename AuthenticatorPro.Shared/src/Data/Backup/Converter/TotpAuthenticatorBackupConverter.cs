// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using SimpleBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Data.Backup.Converter
{
    public class TotpAuthenticatorBackupConverter : BackupConverter
    {
        public override BackupPasswordPolicy PasswordPolicy => BackupPasswordPolicy.Always;
        private const AuthenticatorType Type = AuthenticatorType.Totp;
        private const string Algorithm = "AES/CBC/PKCS7";

        public TotpAuthenticatorBackupConverter(IIconResolver iconResolver) : base(iconResolver) { }

        public override Task<Backup> ConvertAsync(byte[] data, string password = null)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password ?? throw new ArgumentNullException(nameof(password)));
            var key = SHA256.HashData(passwordBytes);

            var stringData = Encoding.UTF8.GetString(data);
            var actualBytes = Convert.FromBase64String(stringData);

            var keyParameter = new KeyParameter(key);
            var cipher = CipherUtilities.GetCipher(Algorithm);
            cipher.Init(false, keyParameter);

            var raw = cipher.DoFinal(actualBytes);
            var json = Encoding.UTF8.GetString(raw);

            // Deal with strange json
            json = json[2..];
            json = json[..(json.LastIndexOf(']') + 1)];
            json = json.Replace(@"\""", @"""");

            var sourceAccounts = JsonConvert.DeserializeObject<List<Account>>(json);
            var authenticators = sourceAccounts.Select(entry => entry.Convert(IconResolver)).ToList();

            return Task.FromResult(new Backup(authenticators));
        }

        private class Account
        {
            [JsonProperty(PropertyName = "issuer")]
            public string Issuer { get; set; }

            [JsonProperty(PropertyName = "name")] public string Name { get; set; }

            [JsonProperty(PropertyName = "key")] public string Key { get; set; }

            [JsonProperty(PropertyName = "digits")]
            public string Digits { get; set; }

            [JsonProperty(PropertyName = "period")]
            public string Period { get; set; }

            [JsonProperty(PropertyName = "base")] public int Base { get; set; }

            public Authenticator Convert(IIconResolver iconResolver)
            {
                string issuer;
                string username;

                if (Issuer == "Unknown")
                {
                    issuer = Name;
                    username = null;
                }
                else
                {
                    issuer = Issuer;
                    username = Name;
                }

                var period = Period == ""
                    ? Type.GetDefaultPeriod()
                    : Int32.Parse(Period);

                var digits = Digits == ""
                    ? Type.GetDefaultDigits()
                    : Int32.Parse(Digits);

                // TODO: figure out if this value ever changes
                if (Base != 16)
                {
                    throw new ArgumentOutOfRangeException(nameof(Base), "Cannot parse base other than 16");
                }

                var secretBytes = Base16.Decode(Key);
                var secret = Base32.Rfc4648.Encode(secretBytes);

                return new Authenticator
                {
                    Issuer = issuer,
                    Username = username,
                    Type = Type,
                    Period = period,
                    Digits = digits,
                    Algorithm = Authenticator.DefaultAlgorithm,
                    Secret = Authenticator.CleanSecret(secret, Type),
                    Icon = iconResolver.FindServiceKeyByName(issuer)
                };
            }
        }
    }
}
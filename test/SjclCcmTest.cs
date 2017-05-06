// Copyright (C) 2017 Dmitry Yakimenko (detunized@gmail.com).
// Licensed under the terms of the MIT license. See LICENCE for details.

using System.Collections.Generic;
using NUnit.Framework;

namespace TrueKey.Test
{
    [TestFixture]
    class SjclCcmTest
    {
        struct CcmTestCase
        {
            public CcmTestCase(string key, string plaintext, string ciphertext, string iv, string adata, int tagLength)
            {
                Key = key.DecodeHex();
                Plaintext = plaintext.DecodeHex();
                Ciphertext = ciphertext.DecodeHex();
                Iv = iv.DecodeHex();
                Adata = adata.DecodeHex();
                TagLength = tagLength;
            }

            public readonly byte[] Key;
            public readonly byte[] Plaintext;
            public readonly byte[] Ciphertext;
            public readonly byte[] Iv;
            public readonly byte[] Adata;
            public readonly int TagLength;
        }

        // Test data from https://tools.ietf.org/html/rfc3610
        private static readonly CcmTestCase[] Rfc3610TestCases =
        {
            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "08090a0b0c0d0e0f101112131415161718191a1b1c1d1e",
                ciphertext: "588c979a61c663d2f066d0c2c0f989806d5f6b61dac38417e8d12cfdf926e0",
                        iv: "00000003020100a0a1a2a3a4a5",
                     adata: "0001020304050607",
                 tagLength: 8),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "08090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f",
                ciphertext: "72c91a36e135f8cf291ca894085c87e3cc15c439c9e43a3ba091d56e10400916",
                        iv: "00000004030201a0a1a2a3a4a5",
                     adata: "0001020304050607",
                 tagLength: 8),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "08090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20",
                ciphertext: "51b1e5f44a197d1da46b0f8e2d282ae871e838bb64da8596574adaa76fbd9fb0c5",
                        iv: "00000005040302a0a1a2a3a4a5",
                     adata: "0001020304050607",
                 tagLength: 8),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "0c0d0e0f101112131415161718191a1b1c1d1e",
                ciphertext: "a28c6865939a9a79faaa5c4c2a9d4a91cdac8c96c861b9c9e61ef1",
                        iv: "00000006050403a0a1a2a3a4a5",
                     adata: "000102030405060708090a0b",
                 tagLength: 8),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "0c0d0e0f101112131415161718191a1b1c1d1e1f",
                ciphertext: "dcf1fb7b5d9e23fb9d4e131253658ad86ebdca3e51e83f077d9c2d93",
                        iv: "00000007060504a0a1a2a3a4a5",
                     adata: "000102030405060708090a0b",
                 tagLength: 8),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "0c0d0e0f101112131415161718191a1b1c1d1e1f20",
                ciphertext: "6fc1b011f006568b5171a42d953d469b2570a4bd87405a0443ac91cb94",
                        iv: "00000008070605a0a1a2a3a4a5",
                     adata: "000102030405060708090a0b",
                 tagLength: 8),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "08090a0b0c0d0e0f101112131415161718191a1b1c1d1e",
                ciphertext: "0135d1b2c95f41d5d1d4fec185d166b8094e999dfed96c048c56602c97acbb7490",
                        iv: "00000009080706a0a1a2a3a4a5",
                     adata: "0001020304050607",
                 tagLength: 10),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "08090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f",
                ciphertext: "7b75399ac0831dd2f0bbd75879a2fd8f6cae6b6cd9b7db24c17b4433f434963f34b4",
                        iv: "0000000a090807a0a1a2a3a4a5",
                     adata: "0001020304050607",
                 tagLength: 10),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "08090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20",
                ciphertext: "82531a60cc24945a4b8279181ab5c84df21ce7f9b73f42e197ea9c07e56b5eb17e5f4e",
                        iv: "0000000b0a0908a0a1a2a3a4a5",
                     adata: "0001020304050607",
                 tagLength: 10),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "0c0d0e0f101112131415161718191a1b1c1d1e",
                ciphertext: "07342594157785152b074098330abb141b947b566aa9406b4d999988dd",
                        iv: "0000000c0b0a09a0a1a2a3a4a5",
                     adata: "000102030405060708090a0b",
                 tagLength: 10),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "0c0d0e0f101112131415161718191a1b1c1d1e1f",
                ciphertext: "676bb20380b0e301e8ab79590a396da78b834934f53aa2e9107a8b6c022c",
                        iv: "0000000d0c0b0aa0a1a2a3a4a5",
                     adata: "000102030405060708090a0b",
                 tagLength: 10),

            new CcmTestCase(
                       key: "c0c1c2c3c4c5c6c7c8c9cacbcccdcecf",
                 plaintext: "0c0d0e0f101112131415161718191a1b1c1d1e1f20",
                ciphertext: "c0ffa0d6f05bdb67f24d43a4338d2aa4bed7b20e43cd1aa31662e7ad65d6db",
                        iv: "0000000e0d0c0ba0a1a2a3a4a5",
                     adata: "000102030405060708090a0b",
                 tagLength: 10),
        };

        [Test]
        public void Encrypt_returns_correct_value()
        {
            foreach (var i in Rfc3610TestCases)
            {
                var aes = new SjclAes(i.Key);
                var ciphertext = SjclCcm.Encrypt(aes, i.Plaintext, i.Iv, i.Adata, i.TagLength);
                Assert.That(ciphertext, Is.EqualTo(i.Ciphertext));
            }
        }

        [Test]
        public void Encrypt_throws_on_too_short_iv()
        {
            var aes = new SjclAes(new byte[16]);
            for (var i = 0; i < 7; ++i)
                Assert.That(() => SjclCcm.Encrypt(aes, new byte[1], new byte[i], new byte[0], 8),
                            Throws.TypeOf<CryptoException>()
                                .And.Message.EqualTo("IV must be at least 7 bytes long"));
        }

        [Test]
        public void Encrypt_throws_on_invalid_tag_length()
        {
            var testCases = new int[] {-1, 0, 1, 2, 3, 5, 7, 9, 11, 13, 15, 17, 18, 19, 20, 1024};

            var aes = new SjclAes(new byte[16]);
            foreach (var i in testCases)
                Assert.That(() => SjclCcm.Encrypt(aes, new byte[1], new byte[16], new byte[0], i),
                            Throws.TypeOf<CryptoException>()
                                .And.Message.EqualTo("Tag must be 4, 8, 10, 12, 14 or 16 bytes long"));
        }

        [Test]
        public void Decrypt_returns_correct_value()
        {
            foreach (var i in Rfc3610TestCases)
            {
                var aes = new SjclAes(i.Key);
                var plaintext = SjclCcm.Decrypt(aes, i.Ciphertext, i.Iv, i.Adata, i.TagLength);
                Assert.That(plaintext, Is.EqualTo(i.Plaintext));
            }
        }

        [Test]
        public void Decrypt_throws_on_mismatching_tag()
        {
            foreach (var i in Rfc3610TestCases)
            {
                var aes = new SjclAes(i.Key);

                // Change ciphertext
                var ciphertext = (byte[])i.Ciphertext.Clone();
                ++ciphertext[ciphertext.Length / 2];
                VerifyCmmMismatchThrown(aes, ciphertext, i.Iv, i.Adata, i.TagLength);

                // Change iv
                var iv = (byte[])i.Iv.Clone();
                ++iv[iv.Length / 2];
                VerifyCmmMismatchThrown(aes, i.Ciphertext, iv, i.Adata, i.TagLength);

                // Change adata
                var adata = (byte[])i.Adata.Clone();
                ++adata[adata.Length / 2];
                VerifyCmmMismatchThrown(aes, i.Ciphertext, i.Iv, adata, i.TagLength);
            }
        }

        [Test]
        public void ComputeLengthLength_returns_corrent_value()
        {
            var testCases = new Dictionary<int, int>
            {
                {      0x01, 2},
                {      0xff, 2},
                {    0x0100, 2},
                {    0xffff, 2},
                {  0x010000, 3},
                {  0xffffff, 3},
                {0x01000000, 4},
                {0x7fffffff, 4},
            };

            foreach (var i in testCases)
                Assert.That(SjclCcm.ComputeLengthLength(i.Key), Is.EqualTo(i.Value));
        }

        [Test]
        public void EncodeAdataLengt_returns_corrent_value()
        {
            var testCases = new Dictionary<int, string>
            {
                {    0x0001, "0001"},
                {    0x0010, "0010"},
                {    0xfefe, "fefe"},
                {    0xfeff, "fffe" + "0000feff"},
                {    0xffff, "fffe" + "0000ffff"},
                {0x7fffffff, "fffe" + "7fffffff"},
            };

            foreach (var i in testCases)
                Assert.That(SjclCcm.EncodeAdataLength(i.Key), Is.EqualTo(i.Value.DecodeHex()));
        }

        [Test]
        public void EncodeAdataLengt_throws_on_zero_length()
        {
            Assert.That(() => SjclCcm.EncodeAdataLength(0),
                        Throws.TypeOf<CryptoException>()
                            .And.Message.EqualTo("Adata length must be positive"));
        }

        [Test]
        public void EncodeAdataLengt_throws_on_negative_length()
        {
            Assert.That(() => SjclCcm.EncodeAdataLength(-1),
                        Throws.TypeOf<CryptoException>()
                            .And.Message.EqualTo("Adata length must be positive"));
        }

        //
        // Helpers
        //

        private static void VerifyCmmMismatchThrown(SjclAes aes, byte[] ciphertext, byte[] iv, byte[] adata, int tagLength)
        {
            Assert.That(() => SjclCcm.Decrypt(aes, ciphertext, iv, adata, tagLength),
                        Throws.TypeOf<CryptoException>()
                            .And.Message.EqualTo("CCM tag doesn't match"));
        }
    }
}

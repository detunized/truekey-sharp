// Copyright (C) 2017 Dmitry Yakimenko (detunized@gmail.com).
// Licensed under the terms of the MIT license. See LICENCE for details.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace TrueKey.Test
{
    [TestFixture]
    class ExtensionsTest
    {
        public const string TestString = "All your base are belong to us";
        public const string TestHex = "416c6c20796f757220626173652061" +
                                      "72652062656c6f6e6720746f207573";
        public static readonly byte[] TestBytes = {
            65, 108, 108, 32, 121, 111, 117, 114, 32, 98, 97, 115, 101, 32, 97,
            114, 101, 32, 98, 101, 108, 111, 110, 103, 32, 116, 111, 32, 117, 115
        };

        public static readonly Dictionary<string, byte[]> HexToBytes = new Dictionary<string, byte[]> {
            {"",
             new byte[] {}},

            {"00",
             new byte[] {0}},

            {"00ff",
             new byte[] {0, 255}},

            {"00010203040506070809",
             new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}},

            {"000102030405060708090a0b0c0d0e0f",
             new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15}},

            {"8af633933e96a3c3550c2734bd814195",
             new byte[] {0x8A, 0xF6, 0x33, 0x93, 0x3E, 0x96, 0xA3, 0xC3, 0x55, 0x0C, 0x27, 0x34, 0xBD, 0x81, 0x41, 0x95}}
        };

        //
        // uint
        //

        [Test]
        public void ChangeEndianness_swaps_bytes()
        {
            var tests = new Dictionary<uint, uint>
            {
                {0x00000000u, 0x00000000u},
                {0xFF000000u, 0x000000FFu},
                {0x000000FFu, 0xFF000000u},
                {0x00FF00FFu, 0xFF00FF00u},
                {0x12345678u, 0x78563412u},
                {0xEFBEADDEu, 0xDEADBEEFu},
            };

            foreach (var i in tests)
            {
                Assert.That(i.Key.ChangeEndianness(), Is.EqualTo(i.Value));
                Assert.That(i.Value.ChangeEndianness(), Is.EqualTo(i.Key));
            }
        }

        [Test]
        public void ChangeEndianness_applied_twice_doesn_change_value()
        {
            var tests = new []
            {
                0x00000000u,
                0x000000FFu,
                0xFF000000u,
                0xFF00FF00u,
                0x78563412u,
                0xDEADBEEFu,
            };

            foreach (var i in tests)
                Assert.That(i.ChangeEndianness().ChangeEndianness(), Is.EqualTo(i));
        }

        [Test]
        public void FromBigEndian()
        {
            var tests = new Dictionary<uint, byte[]>
            {
                {0x00000000u, new byte[] {0x00, 0x00, 0x00, 0x00}},
                {0x12345678u, new byte[] {0x12, 0x34, 0x56, 0x78}},
                {0xDEADBEEFu, new byte[] {0xde, 0xad, 0xbe, 0xef}},
            };

            foreach (var i in tests)
                Assert.That(BitConverter.ToUInt32(i.Value, 0).FromBigEndian(), Is.EqualTo(i.Key));
        }

        //
        // string
        //

        [Test]
        public void String_ToBytes_converts_string_to_utf8_bytes()
        {
            Assert.That("".ToBytes(), Is.EqualTo(new byte[] { }));
            Assert.That(TestString.ToBytes(), Is.EqualTo(TestBytes));
        }

        [Test]
        public void String_Decode64_decodes_base64()
        {
            Assert.That("".Decode64(), Is.EqualTo(new byte[] { }));
            Assert.That("YQ==".Decode64(), Is.EqualTo(new byte[] { 0x61 }));
            Assert.That("YWI=".Decode64(), Is.EqualTo(new byte[] { 0x61, 0x62 }));
            Assert.That("YWJj".Decode64(), Is.EqualTo(new byte[] { 0x61, 0x62, 0x63 }));
            Assert.That("YWJjZA==".Decode64(), Is.EqualTo(new byte[] { 0x61, 0x62, 0x63, 0x64 }));
        }

        //
        // byte[]
        //

        [Test]
        public void ByteArray_ToUtf8_returns_string()
        {
            Assert.That(new byte[] {}.ToUtf8(), Is.EqualTo(""));
            Assert.That(TestBytes.ToUtf8(), Is.EqualTo(TestString));
        }

        [Test]
        public void ByteArray_ToHex_returns_hex_string()
        {
            Assert.That(new byte[] { }.ToHex(), Is.EqualTo(""));
            Assert.That(TestBytes.ToHex(), Is.EqualTo(TestHex));
        }

        [Test]
        public void DecodeHex()
        {
            foreach (var i in HexToBytes)
            {
                Assert.That(i.Key.ToLower().DecodeHex(), Is.EqualTo(i.Value));
                Assert.That(i.Key.ToUpper().DecodeHex(), Is.EqualTo(i.Value));
            }
        }

        [Test]
        public void DecodeHex_throws_on_odd_length()
        {
            Assert.That(() => "0".DecodeHex(),
                        Throws.TypeOf<ArgumentException>()
                            .And.Message.EqualTo("Input length must be multple of 2"));
        }

        [Test]
        public void DecodeHex_throws_on_non_hex_characters()
        {
            Assert.That(() => "xz".DecodeHex(),
                        Throws.TypeOf<ArgumentException>()
                            .And.Message.EqualTo("Input contains invalid characters"));
        }

        [Test]
        public void ByteArray_ToBase64_returns_base64()
        {
            Assert.That(new byte[] { }.ToBase64(), Is.EqualTo(""));
            Assert.That(new byte[] { 0x61 }.ToBase64(), Is.EqualTo("YQ=="));
            Assert.That(new byte[] { 0x61, 0x62 }.ToBase64(), Is.EqualTo("YWI="));
            Assert.That(new byte[] { 0x61, 0x62, 0x63 }.ToBase64(), Is.EqualTo("YWJj"));
            Assert.That(new byte[] { 0x61, 0x62, 0x63, 0x64 }.ToBase64(), Is.EqualTo("YWJjZA=="));
        }

        //
        // DateTime
        //

        [Test]
        public void DateTime_UnixSeconds_returns_number_of_seconds_since_epoch()
        {
            Assert.That(new DateTime(2017, 4, 29, 9, 6, 29, DateTimeKind.Utc).UnixSeconds(),
                        Is.EqualTo(1493456789));
        }

        //
        // BinaryReader
        //

        [Test]
        public void BinaryReader_ReadUInt16BigEndian_reads_ushort()
        {
            using (var s = new MemoryStream(new byte[] {0xDE, 0xAD, 0xBE, 0xEF}))
            using (var r = new BinaryReader(s))
            {
                Assert.That(r.ReadUInt16BigEndian(), Is.EqualTo(0xDEAD));
                Assert.That(r.ReadUInt16BigEndian(), Is.EqualTo(0xBEEF));
            }
        }

        [Test]
        public void BinaryReader_ReadUInt32BigEndian_reads_uint()
        {
            using (var s = new MemoryStream(new byte[] {0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED, 0xF0, 0x0D}))
            using (var r = new BinaryReader(s))
            {
                Assert.That(r.ReadUInt32BigEndian(), Is.EqualTo(0xDEADBEEF));
                Assert.That(r.ReadUInt32BigEndian(), Is.EqualTo(0xFEEDF00D));
            }
        }

        //
        // JToken
        //

        [Test]
        public void JToken_chained_At_returns_token()
        {
            var j = JObject.Parse(@"{
                'k1': {'k2': {'k3': 'v3'}}
            }");

            var k1 = j["k1"];
            var k2 = j["k1"]["k2"];
            var k3 = j["k1"]["k2"]["k3"];

            Assert.That(j.At("k1"), Is.EqualTo(k1));
            Assert.That(j.At("k1").At("k2"), Is.EqualTo(k2));
            Assert.That(j.At("k1").At("k2").At("k3"), Is.EqualTo(k3));

            Assert.That(j.At("k1").At("k2/k3"), Is.EqualTo(k3));
            Assert.That(j.At("k1/k2").At("k3"), Is.EqualTo(k3));
        }

        [Test]
        public void JToken_At_is_case_insensitive()
        {
            var j = JObject.Parse(@"{
                'keyOne': {'keyTwo': {'keyThree': 'v3'}}
            }");

            var k1 = j["keyOne"];
            var k2 = j["keyOne"]["keyTwo"];
            var k3 = j["keyOne"]["keyTwo"]["keyThree"];

            Assert.That(j.At("KEYone"), Is.EqualTo(k1));
            Assert.That(j.At("keyONE").At("KEYtwo"), Is.EqualTo(k2));
            Assert.That(j.At("KEyonE").At("keyTWO").At("KEYthree"), Is.EqualTo(k3));

            Assert.That(j.At("keyone").At("KEYTWO/kEYtHREE"), Is.EqualTo(k3));
            Assert.That(j.At("KEYONE/keytwo").At("KeyThree"), Is.EqualTo(k3));
            Assert.That(j.At("keyone/KEYTWO/keyTHREE"), Is.EqualTo(k3));
        }

        [Test]
        public void JToken_At_throws_on_invalid_path()
        {
            var j = JObject.Parse(@"{
                'k1': 'v1',
                'k2': {'k22': 'v22'},
                'k3': {'k33': {'k333': 'v333'}}
            }");

            VerifyAtThrows(j, "i1");
            VerifyAtThrows(j, "k1/k11");
            VerifyAtThrows(j, "k2/i2");
            VerifyAtThrows(j, "k2/k22/i22");
            VerifyAtThrows(j, "k3/i3");
            VerifyAtThrows(j, "k3/k33/i33");
            VerifyAtThrows(j, "k3/k33/k333/i333");
        }

        [Test]
        public void JToken_At_throws_on_non_objects()
        {
            var j = JObject.Parse(@"{
                'k1': [],
                'k2': true,
                'k3': 10
            }");

            VerifyAtThrows(j, "k1/0");
            VerifyAtThrows(j, "k2/k22");
            VerifyAtThrows(j, "k3/k33/k333");
        }

        [Test]
        public void JToken_StringAt_returns_string()
        {
            var j = JObject.Parse(@"{
                'k1': 'v1',
                'k2': {'k22': 'v22'},
                'k3': {'k33': {'k333': 'v333'}}
            }");

            Assert.That(j.StringAt("k1"), Is.EqualTo("v1"));
            Assert.That(j.StringAt("k2/k22"), Is.EqualTo("v22"));
            Assert.That(j.StringAt("k3/k33/k333"), Is.EqualTo("v333"));
        }

        [Test]
        public void JToken_StringAt_throws_on_non_stings()
        {
            var j = JObject.Parse(@"{
                'k1': true,
                'k2': 10,
                'k3': 10.0,
                'k4': [],
                'k5': {},
            }");

            VerifyStringAtThrows(j, "k1");
            VerifyStringAtThrows(j, "k2");
            VerifyStringAtThrows(j, "k3");
            VerifyStringAtThrows(j, "k4");
            VerifyStringAtThrows(j, "k5");
        }

        [Test]
        public void JToken_StringAt_returns_default_value_on_non_strings()
        {
            var j = JObject.Parse(@"{
                'k1': true,
                'k2': 10,
                'k3': 10.0,
                'k4': [],
                'k5': {},
            }");

            VerifyStringAtReturnsDefault(j, "k1");
            VerifyStringAtReturnsDefault(j, "k2");
            VerifyStringAtReturnsDefault(j, "k3");
            VerifyStringAtReturnsDefault(j, "k4");
            VerifyStringAtReturnsDefault(j, "k5");
        }

        [Test]
        public void JToken_IntAt_returns_int()
        {
            var j = JObject.Parse(@"{
                'k1': 13,
                'k2': {'k22': 42},
                'k3': {'k33': {'k333': 1337}}
            }");

            Assert.That(j.IntAt("k1"), Is.EqualTo(13));
            Assert.That(j.IntAt("k2/k22"), Is.EqualTo(42));
            Assert.That(j.IntAt("k3/k33/k333"), Is.EqualTo(1337));
        }

        [Test]
        public void JToken_IntAt_throws_on_non_ints()
        {
            var j = JObject.Parse(@"{
                'k1': true,
                'k2': '10',
                'k3': 10.0,
                'k4': [],
                'k5': {},
            }");

            VerifyIntAtThrows(j, "k1");
            VerifyIntAtThrows(j, "k2");
            VerifyIntAtThrows(j, "k3");
            VerifyIntAtThrows(j, "k4");
            VerifyIntAtThrows(j, "k5");
        }

        [Test]
        public void JToken_IntAtOrNull_returns_default_value_on_non_ints()
        {
            var j = JObject.Parse(@"{
                'k1': true,
                'k2': '10',
                'k3': 10.0,
                'k4': [],
                'k5': {},
            }");

            VerifyIntAtReturnsDefault(j, "k1");
            VerifyIntAtReturnsDefault(j, "k2");
            VerifyIntAtReturnsDefault(j, "k3");
            VerifyIntAtReturnsDefault(j, "k4");
            VerifyIntAtReturnsDefault(j, "k5");
        }

        [Test]
        public void JToken_BoolAt_returns_bools()
        {
            var j = JObject.Parse(@"{
                'k1': true,
                'k2': {'k22': false},
                'k3': {'k33': {'k333': true}}
            }");

            Assert.That(j.BoolAt("k1"), Is.EqualTo(true));
            Assert.That(j.BoolAt("k2/k22"), Is.EqualTo(false));
            Assert.That(j.BoolAt("k3/k33/k333"), Is.EqualTo(true));
        }

        [Test]
        public void JToken_BoolAt_throws_on_non_bools()
        {
            var j = JObject.Parse(@"{
                'k1': 10,
                'k2': '10',
                'k3': 10.0,
                'k4': [],
                'k5': {},
            }");

            VerifyBoolAtThrows(j, "k1");
            VerifyBoolAtThrows(j, "k2");
            VerifyBoolAtThrows(j, "k3");
            VerifyBoolAtThrows(j, "k4");
            VerifyBoolAtThrows(j, "k5");
        }

        [Test]
        public void JToken_BoolAtOrNull_returns_null_on_non_bools()
        {
            var j = JObject.Parse(@"{
                'k1': 10,
                'k2': '10',
                'k3': 10.0,
                'k4': [],
                'k5': {},
            }");

            VerifyBoolAtReturnsDefault(j, "k1");
            VerifyBoolAtReturnsDefault(j, "k2");
            VerifyBoolAtReturnsDefault(j, "k3");
            VerifyBoolAtReturnsDefault(j, "k4");
            VerifyBoolAtReturnsDefault(j, "k5");
        }

        //
        // Helpers
        //

        private static void VerifyAtThrows(JToken token, string path)
        {
            VerifyAccessThrows(token, path, (t, p) => t.At(p));
        }

        private static void VerifyStringAtThrows(JToken token, string path)
        {
            VerifyAccessThrows(token, path, (t, p) => t.StringAt(p));
        }

        private static void VerifyIntAtThrows(JToken token, string path)
        {
            VerifyAccessThrows(token, path, (t, p) => t.IntAt(p));
        }

        private static void VerifyBoolAtThrows(JToken token, string path)
        {
            VerifyAccessThrows(token, path, (t, p) => t.BoolAt(p));
        }

        private static void VerifyAccessThrows(JToken token, string path, Action<JToken, string> access)
        {
            Assert.That(() => access(token, path), Throws.TypeOf<JTokenAccessException>());
        }

        private static void VerifyStringAtReturnsDefault(JToken token, string path)
        {
            Assert.That(token.StringAt(path, "default"), Is.EqualTo("default"));
        }

        private static void VerifyIntAtReturnsDefault(JToken token, string path)
        {
            Assert.That(token.IntAt(path, 1337), Is.EqualTo(1337));
        }

        private static void VerifyBoolAtReturnsDefault(JToken token, string path)
        {
            Assert.That(token.BoolAt(path, false), Is.EqualTo(false));
            Assert.That(token.BoolAt(path, true), Is.EqualTo(true));
        }
    }
}

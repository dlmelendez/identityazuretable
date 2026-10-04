// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.AzureTable.Helpers;
using ElCamino.AspNetCore.Identity.AzureTable.Tests.Fakes;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public class KeyHelperTests
    {
        private readonly DefaultKeyHelper _defaultKeyHelper = new();
        private readonly SHA256KeyHelper _sha256KeyHelper = new();
        private readonly HashTestKeyHelperFake _fakeKeyHelper = new();
        protected readonly ITestOutputHelper _output;

        public KeyHelperTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData("HashTestKeyHelperFake _fakeKeyHelper = new HashTestKeyHelperFake();")]
        [InlineData("thisIs Some Test Text123323 for Hashing")]
        [InlineData("{F1FFCC02-83E0-4347-8377-72D6007E3D93}")]
        public void SHA1FormatBackwardCompat(string textToHash)
        {
            _output.WriteLine($"plain text: {textToHash}");
            var sw = new Stopwatch();
            sw.Start();
            string expected = _fakeKeyHelper.ConvertKeyToHashBackwardCompatSHA1(textToHash);
            sw.Stop();
            _output.WriteLine($"expected {sw.Elapsed.TotalMilliseconds} ms: {expected}");

            sw.Reset();
            sw.Start();
            string returned = _defaultKeyHelper.ConvertKeyToHash(textToHash).ToString();
            sw.Stop();
            _output.WriteLine($"returned {sw.Elapsed.TotalMilliseconds} ms: {returned}");
            Assert.Equal(expected, returned, StringComparer.InvariantCulture);
        }

        [Theory]
        [InlineData("HashTestKeyHelperFake _fakeKeyHelper = new HashTestKeyHelperFake();")]
        [InlineData("thisIs Some Test Text123323 for Hashing")]
        [InlineData("{F1FFCC02-83E0-4347-8377-72D6007E3D93}")]
        public void SHA1FormatBackwardCompat_MemCheck(string textToHash)
        {
            _output.WriteLine($"plain text: {textToHash}");
            var sw = new Stopwatch();
            long mem = GC.GetTotalAllocatedBytes();
            sw.Start();
            for (int trial = 0; trial < 1000; trial++)
            {
                _ = _fakeKeyHelper.ConvertKeyToHashBackwardCompatSHA1(textToHash);
            }
            sw.Stop();
            mem = GC.GetTotalAllocatedBytes() - mem;
            _output.WriteLine($"expected {sw.Elapsed.TotalMilliseconds}ms, Alloc: {mem / 1024.0 / 1024:N2}mb");

            mem = GC.GetTotalAllocatedBytes();
            sw.Restart();
            for (int trial = 0; trial < 1000; trial++)
            {
                _ = _defaultKeyHelper.ConvertKeyToHash(textToHash);
            }
            sw.Stop();
            mem = GC.GetTotalAllocatedBytes() - mem;
            _output.WriteLine($"returned {sw.Elapsed.TotalMilliseconds}ms, Alloc: {mem / 1024.0 / 1024:N2}mb");

        }


        [Theory]
        [InlineData("HashTestKeyHelperFake _fakeKeyHelper = new HashTestKeyHelperFake();")]
        [InlineData("thisIs Some Test Text123323 for Hashing")]
        [InlineData("{F1FFCC02-83E0-4347-8377-72D6007E3D93}")]
        public void SHA256FormatBackwardCompat(string textToHash)
        {
            _output.WriteLine($"plain text: {textToHash}");
            var sw = new Stopwatch();
            sw.Start();
            string expected = _fakeKeyHelper.ConvertKeyToHashBackwardCompatSHA256(textToHash);
            sw.Stop();
            _output.WriteLine($"expected {sw.Elapsed.TotalMilliseconds} ms: {expected}");

            sw.Reset();
            sw.Start();
            string returned = _sha256KeyHelper.ConvertKeyToHash(textToHash).ToString();
            sw.Stop();
            _output.WriteLine($"returned {sw.Elapsed.TotalMilliseconds} ms: {returned}");
            Assert.Equal(expected, returned, StringComparer.InvariantCulture);
        }

        /// <summary>
        /// Keys are hashed from caller supplied text of any length: a key larger than the stack must hash, not overflow it.
        /// 340/341 and 511/512 chars straddle the stack buffer limit for the UTF8 and Unicode encoded keys.
        /// </summary>
        [Theory]
        [InlineData(340)]
        [InlineData(341)]
        [InlineData(511)]
        [InlineData(512)]
        [InlineData(4 * 1024 * 1024)]
        public void LargeKeyFormatBackwardCompat(int keyLength)
        {
            string textToHash = new string('a', keyLength);

            Assert.Equal(_fakeKeyHelper.ConvertKeyToHashBackwardCompatSHA1(textToHash), _defaultKeyHelper.ConvertKeyToHash(textToHash).ToString());
            Assert.Equal(_fakeKeyHelper.ConvertKeyToHashBackwardCompatSHA256(textToHash), _sha256KeyHelper.ConvertKeyToHash(textToHash).ToString());
        }

        /// <summary>
        /// The passkey row key is the prefix + the key helper's hash of the hex of the credential id bytes:
        /// its size does not depend on the size of the credential id.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(32)]
        [InlineData(1023)]
        public void GenerateRowKeyIdentityUserPasskey(int credentialIdLength)
        {
            byte[] credentialId = RandomNumberGenerator.GetBytes(credentialIdLength);
            string hex = Convert.ToHexString(credentialId);
            const string prefix = TableConstants.RowKeyConstants.PreFixIdentityUserPasskey;

            Assert.Equal(prefix + _fakeKeyHelper.ConvertKeyToHashBackwardCompatSHA1(hex), _defaultKeyHelper.GenerateRowKeyIdentityUserPasskey(credentialId).ToString());
            Assert.Equal(prefix + _fakeKeyHelper.ConvertKeyToHashBackwardCompatSHA256(hex), _sha256KeyHelper.GenerateRowKeyIdentityUserPasskey(credentialId).ToString());
        }

        [Theory]
        [InlineData("HashTestKeyHelperFake _fakeKeyHelper = new HashTestKeyHelperFake();")]
        [InlineData("thisIs Some Test Text123323 for Hashing")]
        [InlineData(null)]
        public void GenerateRowKeyUserId(string textToHash)
        {
            _output.WriteLine($"plain text: {textToHash}");
            var sw = new Stopwatch();
            sw.Start();
            var returned = _defaultKeyHelper.GenerateRowKeyUserId(textToHash);
            sw.Stop();
            _output.WriteLine($"returned {sw.Elapsed.TotalMilliseconds} ms: {returned}");
            Assert.StartsWith(TableConstants.RowKeyConstants.PreFixIdentityUserId, returned, StringComparison.OrdinalIgnoreCase);

            sw.Reset();
            sw.Start();
            try
            {
                returned = _sha256KeyHelper.GenerateRowKeyUserId(textToHash);
            }
            catch (ArgumentNullException)
            {
                returned = null;
            }
            sw.Stop();
            _output.WriteLine($"returned {sw.Elapsed.TotalMilliseconds} ms: {returned}");
            if (!returned.IsEmpty)
            {
                Assert.StartsWith(TableConstants.RowKeyConstants.PreFixIdentityUserId, returned, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

﻿// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

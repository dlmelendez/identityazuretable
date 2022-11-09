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
using NuGet.Frameworks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public class KeyHelperTests
    {
        private DefaultKeyHelper _defaultKeyHelper = new DefaultKeyHelper();
        private SHA256KeyHelper _sha256KeyHelper = new SHA256KeyHelper();
        private HashTestKeyHelperFake _fakeKeyHelper = new HashTestKeyHelperFake();
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
            string returned = _defaultKeyHelper.ConvertKeyToHash(textToHash);
            sw.Stop();
            _output.WriteLine($"returned {sw.Elapsed.TotalMilliseconds} ms: {returned}");
            Assert.Equal(expected, returned, StringComparer.InvariantCulture);
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
            string returned = _sha256KeyHelper.ConvertKeyToHash(textToHash);
            sw.Stop();
            _output.WriteLine($"returned {sw.Elapsed.TotalMilliseconds} ms: {returned}");
            Assert.Equal(expected, returned, StringComparer.InvariantCulture);
        }
    }
}

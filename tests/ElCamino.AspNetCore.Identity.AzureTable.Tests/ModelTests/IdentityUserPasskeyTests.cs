// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityUserPasskeyTests
    {
        /// <summary>
        /// Storage-type adaptations where IdentityUserPasskey intentionally differs
        /// from UserPasskeyInfo. Everything else must be a like-for-like match.
        /// </summary>
        private static readonly Dictionary<string, Type> ExpectedTypeAdaptations = new()
        {
            // Azure Table Storage has no string[] column type; packed with ';'
            [nameof(UserPasskeyInfo.Transports)] = typeof(string),
            // uint is not a supported table column type; stored as long
            [nameof(UserPasskeyInfo.SignCount)] = typeof(long),
        };

        [Fact(DisplayName = "IdentityUserPasskeyRepresentsAllUserPasskeyInfoProperties")]
        [Trait("IdentityCore.Azure.Model", "")]
        public void IdentityUserPasskeyRepresentsAllUserPasskeyInfoProperties()
        {
            var passkeyInfoProperties = typeof(UserPasskeyInfo)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name);

            var entityProperties = typeof(Model.IdentityUserPasskey)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name);

            Assert.NotEmpty(passkeyInfoProperties);

            foreach (var (name, infoProperty) in passkeyInfoProperties)
            {
                // Missing property fails the test: nothing from UserPasskeyInfo can be silently unrepresented
                Assert.True(entityProperties.TryGetValue(name, out var entityProperty),
                    $"UserPasskeyInfo.{name} ({infoProperty.PropertyType.Name}) is not represented on IdentityUserPasskey.");

                var expectedType = ExpectedTypeAdaptations.TryGetValue(name, out var adaptedType) ? adaptedType : infoProperty.PropertyType;

                // Like-for-like type check, except the documented storage adaptations
                Assert.True(entityProperty.PropertyType == expectedType,
                    $"IdentityUserPasskey.{name} type mismatch: expected {expectedType.Name}, found {entityProperty.PropertyType.Name}.");
            }
        }
    }
}

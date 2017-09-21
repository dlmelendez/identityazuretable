// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Resources;
using Microsoft.WindowsAzure;
#if net45
using System.Configuration;
using ElCamino.AspNet.Identity.AzureTable.Configuration;
using Microsoft.Azure;
using ElCamino.AspNet.Identity.AzureTable.Model;
#else
using ElCamino.AspNetCore.Identity.AzureTable.Model;
#endif
using Xunit;

namespace ElCamino.AspNet.Identity.AzureTable.Tests.ModelTests
{
    public class IdentityCloudContextTests
    {
#if net45
        [Fact(DisplayName = "IdentityCloudContextCtors")]
        [Trait("Identity.Azure.Model", "")]
        public void IdentityCloudContextCtors()
        {
            string strValidConnection = CloudConfigurationManager.GetSetting(
                ElCamino.AspNet.Identity.AzureTable.Constants.AppSettingsKeys.DefaultStorageConnectionStringKey);

            var currentConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var section = currentConfig.Sections[IdentityConfigurationSection.Name];
            if (section == null)
            {
                currentConfig.Sections.Add(IdentityConfigurationSection.Name,
                    new IdentityConfigurationSection()
                    {
                        TablePrefix = string.Empty,
                        StorageConnectionString = strValidConnection
                    });
                currentConfig.Save(ConfigurationSaveMode.Modified);
            }
            var ic = new IdentityCloudContext();
            Assert.NotNull(ic);

            //Pass in valid connection string
            var icc = new IdentityCloudContext(strValidConnection);
            icc.Dispose();

            ic = new IdentityCloudContext(new IdentityConfiguration() 
            { 
                TablePrefix = string.Empty, 
                StorageConnectionString = strValidConnection 
            });

            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>(
                new IdentityCloudContext(new IdentityConfiguration()
                    {
                        StorageConnectionString = strValidConnection,
                        TablePrefix = "My"
                    })))
            {
                var task = store.CreateTablesIfNotExists();
                task.Wait();
            }

            currentConfig.Sections.Remove(IdentityConfigurationSection.Name);
            currentConfig.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(IdentityConfigurationSection.Name);

            using (UserStore<IdentityUser> store = new UserStore<IdentityUser>(
                new IdentityCloudContext()))
            {
                var task = store.CreateTablesIfNotExists();
                task.Wait();
            }

            currentConfig.Sections.Add(IdentityConfigurationSection.Name,
                new IdentityConfigurationSection()
                {
                    TablePrefix = string.Empty,
                    StorageConnectionString = strValidConnection,
                    LocationMode = "PrimaryThenSecondary"
                });
            currentConfig.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(IdentityConfigurationSection.Name);

            string strInvalidConnectionStringKey = Guid.NewGuid().ToString();

            var testAppsettings = new IdentityCloudContext(ElCamino.AspNet.Identity.AzureTable.Constants.AppSettingsKeys.DefaultStorageConnectionStringKey);
            testAppsettings.Dispose();

            try
            {
                ic = new IdentityCloudContext(new IdentityConfiguration()
                {
                    TablePrefix = string.Empty,
                    StorageConnectionString = strValidConnection,
                    LocationMode = "InvalidLocationMode"
                });
            }
            catch (ArgumentException) { }

            try
            {
                ic = new IdentityCloudContext(strInvalidConnectionStringKey);
            }
            catch (System.FormatException) { }

            try
            {
                ic = new IdentityCloudContext(string.Empty);
            }
            catch (ArgumentException) { }

            //----------------------------------------------
            var iucc = new IdentityCloudContext();
            Assert.NotNull(iucc);

            try
            {
                iucc = new IdentityCloudContext(strInvalidConnectionStringKey);
            }
            catch (System.FormatException) { }

            try
            {
                iucc = new IdentityCloudContext(string.Empty);
            }
            catch (ArgumentException) { }

            //------------------------------------------

            var i2 = new IdentityCloudContext();
            Assert.NotNull(i2);

            try
            {
                i2 = new IdentityCloudContext(Guid.NewGuid().ToString());
            }
            catch (System.FormatException) { }
            try
            {
                i2 = new IdentityCloudContext(string.Empty);
            }
            catch (ArgumentException) { }
            try
            {
                var i3 = new IdentityCloudContext();
                i3.Dispose();
                var table = i3.RoleTable;
            }
            catch (ObjectDisposedException) { }

            try
            {
                var i4 = new IdentityCloudContext();
                i4.Dispose();
                var table = i4.UserTable;
            }
            catch (ObjectDisposedException) { }

            try
            {
                IdentityConfiguration iconfig = null;
                var i5 = new IdentityCloudContext(iconfig);
            }
            catch (ArgumentNullException) { }
        }
#endif
        }

}

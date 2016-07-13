// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
#if net45
using ElCamino.AspNet.Identity.AzureTable.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace ElCamino.AspNet.Identity.AzureTable.Configuration
{
    public class IdentityConfigurationSection : ConfigurationSection
    {
        public static string Name
        {
            get
            {
                return "elcaminoIdentityConfiguration";
            }
        }

        public static IdentityConfiguration GetCurrent()
        {           
            IdentityConfigurationSection section = ConfigurationManager.GetSection(Name) as IdentityConfigurationSection;
            //Add this code when appSettings configuration are phased out.
            //if (section == null)
            //    throw new ConfigurationErrorsException(string.Format("Configuration Section Not Found: {0}", Name));

            if (section != null)
            {
                return new IdentityConfiguration()
                {
                    StorageConnectionString = section.StorageConnectionString,
                    TablePrefix = section.TablePrefix,
                    LocationMode = section.LocationMode
                };
            }
            return null;
        }

        [ConfigurationProperty("tablePrefix", DefaultValue = "", IsRequired = false)]
        public string TablePrefix
        {
            get
            {
                return (string)this["tablePrefix"];
            }
            set
            {
                this["tablePrefix"] = value;
            }
        }

        [ConfigurationProperty("storageConnectionString", DefaultValue = "", IsRequired = false)]
        public string StorageConnectionString
        {
            get
            {
                return (string)this["storageConnectionString"];
            }
            set
            {
                this["storageConnectionString"] = value;
            }
        }

        [ConfigurationProperty("locationMode", IsRequired = false)]
        public string LocationMode
        {
            get
            {
                return (string)this["locationMode"];
            }
            set
            {
                this["locationMode"] = value;
            }
        }

    }
}
#endif
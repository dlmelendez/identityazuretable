// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

#if net45

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNet.Identity.AzureTable;
using ElCamino.AspNet.Identity.AzureTable.Model;
using Microsoft.AspNet.Identity;

namespace ElCamino.Web.Identity.AzureTable.Tests.Fixtures
{
    public partial class BaseFixture<TUser, TRole, TContext> : IDisposable
        where TUser : IdentityUser, new()
        where TRole : IdentityRole, new()
        where TContext : IdentityCloudContext, new()
        {

        #region IDisposable Support
        protected bool disposedValue = false; 

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //  dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

        public RoleStore<TRole> CreateRoleStore()
        {
            return new RoleStore<TRole>(new TContext());
        }

        public RoleStore<TRole> CreateRoleStore(TContext context)
        {
            return new RoleStore<TRole>(context);
        }

        public RoleManager<TRole> CreateRoleManager()
        {
            return CreateRoleManager(CreateRoleStore());
        }

        public RoleManager<TRole> CreateRoleManager(TContext context)
        {
            return CreateRoleManager(new RoleStore<TRole>(context));
        }

        public RoleManager<TRole> CreateRoleManager(RoleStore<TRole> store)
        {
            return new RoleManager<TRole>(store);
        }


        public UserStore<TUser> CreateUserStore()
        {
            return new UserStore<TUser>(new TContext());
        }

        public UserStore<TUser> CreateUserStore(TContext context)
        {
            return new UserStore<TUser>(context);
        }

        public UserManager<TUser> CreateUserManager()
        {
            return CreateUserManager(new UserStore<TUser>(new TContext()));
        }

        public UserManager<TUser> CreateUserManager(TContext context)
        {
            return CreateUserManager(new UserStore<TUser>(context));
        }

        public UserManager<TUser> CreateUserManager(UserStore<TUser> store)
        {
            return new UserManager<TUser>(store);
        }



    }
}
#endif

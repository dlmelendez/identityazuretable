// MIT License Copyright 2014 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElCamino.AspNet.Identity.AzureTable.Tests
{
    public static class  ExceptionHelper
    {
        public static void ValidateAggregateException<T>(this AggregateException agg) where T : Exception, new()
        {
            try
            {
                if (agg.InnerExceptions[0] is T)
                {
                    return;
                }
            }
            catch
            {
                throw agg;
            }

        }
    }
}

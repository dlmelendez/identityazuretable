﻿// MIT License Copyright 2020 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;

namespace ElCamino.AspNetCore.Identity.AzureTable.Tests
{
    public static class ExceptionHelper
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

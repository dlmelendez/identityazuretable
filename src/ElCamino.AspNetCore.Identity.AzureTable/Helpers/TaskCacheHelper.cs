using System.Threading.Tasks;

#if net45
namespace ElCamino.AspNet.Identity.AzureTable.Helpers

#else
namespace ElCamino.AspNetCore.Identity.AzureTable.Helpers
#endif
{
    internal static class TaskCacheHelper
    {

#if net451 || net45
        public static readonly Task CompletedTask = Task.FromResult(0);
#else
        public static readonly Task CompletedTask = Task.CompletedTask;
#endif
    }
}

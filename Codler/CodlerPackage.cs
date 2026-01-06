using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Codler
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideOptionPage(typeof(CodlerOptionsPage), "Codler", "User Methods", 0, 0, true)]
    [Guid("6E1E0E3A-3F8F-4A6A-9C8A-4E7E3D7C91A1")]
    public sealed class CodlerPackage : AsyncPackage
    {
        public static CodlerPackage Instance { get; private set; }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            Instance = this;
            await base.InitializeAsync(cancellationToken, progress);
        }
    }
}

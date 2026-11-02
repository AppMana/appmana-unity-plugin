using System.Reflection;
using System.Runtime.CompilerServices;
using AppManaPublic;

[assembly: InternalsVisibleTo("AppMana")]
[assembly: InternalsVisibleTo("AppManaPublic.Editor")]
[assembly: InternalsVisibleTo("AppManaPublic.Tests.Runtime")]
[assembly: InternalsVisibleTo("AppMana.Tests.Runtime")]
[assembly: AssemblyFileVersion(AssemblyInfo.version)]

namespace AppManaPublic
{
    internal static class AssemblyInfo
    {
        internal const string version = "2.4.6";
    }    
}
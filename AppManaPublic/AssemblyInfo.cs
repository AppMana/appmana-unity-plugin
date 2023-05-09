using System.Reflection;
using System.Runtime.CompilerServices;
using AppManaPublic;

[assembly: InternalsVisibleTo("AppMana")]
[assembly: AssemblyFileVersion(AssemblyInfo.version)]

namespace AppManaPublic
{
    internal static class AssemblyInfo
    {
        internal const string version = "2.3.0";
    }    
}
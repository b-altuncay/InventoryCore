// Required shim so C# 9 'init' properties compile on netstandard2.1 (Unity, older runtimes).
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

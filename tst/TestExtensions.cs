using System.Reflection;

public static class TestExtensions
{
    public static object? Protected(this object substitute, string memberName, params object[] args)
    {
        var method = substitute
            .GetType()
            .GetMethod(memberName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new Exception($"Could not find method {memberName} to substitute");

        if (!method.IsVirtual) {
            throw new Exception("Must be a virtual member");
        }

        return method.Invoke(substitute, args);
    }
}
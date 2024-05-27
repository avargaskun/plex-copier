using System.Reflection;

public static class ObjectExtensions
{
    public static void CopyTo<T>(this T sourceObject, T targetObject, bool skipNull = false) where T : class
    {
        foreach (PropertyInfo property in typeof(T).GetProperties().Where(p => p.CanWrite))
        {
            var sourceValue = property.GetValue(sourceObject, null);
            if (sourceValue == null && skipNull)
            {
                continue;
            }
            property.SetValue(targetObject, sourceValue, null);
        }
    }
}
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;

public static partial class BuildTools
{
    /// <summary>
    /// Get an array containing all types in baseType's
    /// assembly that are derived from baseType.
    /// </summary>
    public static Type[] GetDerivedFrom (Type baseType)
    {
        LinkedList<Type> derivedTypes = new LinkedList<Type>();
        Assembly assembly = Assembly.GetAssembly(baseType);
        Type[] allTypes = assembly.GetTypes();
        for (int t = 0; t < allTypes.Length; t++) if (allTypes[t].IsSubclassOf(baseType)) derivedTypes.AddLast(allTypes[t]);
        Type[] r = new Type[derivedTypes.Count];
        derivedTypes.CopyTo(r, 0);
        return r;
    }
}

#endif
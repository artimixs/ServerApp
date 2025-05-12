using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

public static class AssemblyLoader
{
    public static void EnsureAllReferencedAssembliesLoaded()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var toLoad = new Queue<Assembly>();

        foreach (var asm in loaded)
        {
            foreach (var reference in asm.GetReferencedAssemblies())
            {
                if (!loaded.Any(a =>
                {
                    try { return a.GetName().Name == reference.Name; }
                    catch { return false; }
                }))
                {
                    try
                    {
                        var loadedRef = Assembly.Load(reference);
                        toLoad.Enqueue(loadedRef);
                        loaded.Add(loadedRef);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Ошибка загрузки сборки] {reference.FullName}: {ex.Message}");
                    }
                }
            }
        }

        while (toLoad.Count > 0)
        {
            var asm = toLoad.Dequeue();
            foreach (var reference in asm.GetReferencedAssemblies())
            {
                if (!loaded.Any(a => a.GetName().Name == reference.Name))
                {
                    try
                    {
                        var loadedRef = Assembly.Load(reference);
                        toLoad.Enqueue(loadedRef);
                        loaded.Add(loadedRef);
                    }
                    catch { }
                }
            }
        }
    }
}
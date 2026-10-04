using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using Synaps.Tests;

internal static class Runner
{
    private static int Main()
    {
        var instance = new CoreBehaviorTests();
        var passed = 0;
        var failed = 0;
        foreach (var method in instance.GetType().GetMethods())
        {
            if (method.GetCustomAttributes(typeof(TestAttribute), false).Length == 0) continue;
            try
            {
                var task = method.Invoke(instance, null) as Task;
                if (task != null) task.GetAwaiter().GetResult();
                Console.WriteLine("PASS " + method.Name);
                passed++;
            }
            catch (Exception exception)
            {
                var reflectionError = exception as TargetInvocationException;
                Console.WriteLine("FAIL " + method.Name + ": " + (reflectionError == null ? exception : reflectionError.InnerException));
                failed++;
            }
        }
        Console.WriteLine(passed + " passed, " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
}

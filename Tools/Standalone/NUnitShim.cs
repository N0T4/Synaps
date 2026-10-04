// Used ONLY by Verify-Core.ps1, outside Assets. Unity uses the real NUnit framework.
using System;

namespace NUnit.Framework
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestAttribute : Attribute { }

    public static class Assert
    {
        public static void AreEqual(object expected, object actual)
        {
            if (!object.Equals(expected, actual)) throw new Exception("Expected: " + expected + "; actual: " + actual);
        }
        public static void IsTrue(bool value) { if (!value) throw new Exception("Expected true."); }
        public static void IsFalse(bool value) { if (value) throw new Exception("Expected false."); }
        public static void IsNull(object value) { if (value != null) throw new Exception("Expected null."); }
        public static T Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T exception) { return exception; }
            throw new Exception("Expected exception: " + typeof(T).Name);
        }
    }
}

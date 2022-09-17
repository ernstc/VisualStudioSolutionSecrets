using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.Tests.Helpers
{
    public static class TestPrivate
    {
        public static T? StaticMethod<T>(Type classType, string methodName, object[] callParams)
        {
            var methodList = classType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

            if (methodList is null || methodList.Length == 0)
                throw new EntryPointNotFoundException();

            var method = methodList.First(x => x.Name == methodName && !x.IsPublic && x.GetParameters().Length == callParams.Length);

            var output = (T?)method.Invoke(null, callParams);

            return output;
        }


        public static T? InstanceMethod<T>(object instance, string methodName, object[] callParams)
        {
            var classType = instance.GetType();
            var methodList = classType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            if (methodList is null || methodList.Length == 0)
                throw new EntryPointNotFoundException();

            var method = methodList.First(x => x.Name == methodName && !x.IsPublic && x.GetParameters().Length == callParams.Length);

            var output = (T?)method.Invoke(instance, callParams);

            return output;
        }
    }
}

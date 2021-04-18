using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace PBExtra {
    public static class MethodUtil {
        public static void ReplaceMethod(MethodBase source, MethodBase dest) {
            if (!MethodUtil.MethodSignaturesEqual(source, dest))
                throw new ArgumentException("The method signatures are not the same.", nameof(source));
            MethodUtil.ReplaceMethod(MethodUtil.GetMethodAddress(source), dest);
        }

        public static unsafe void ReplaceMethod(IntPtr srcAdr, MethodBase dest) {
            IntPtr methodAddress = MethodUtil.GetMethodAddress(dest);
            if (IntPtr.Size == 8)
                *(long*)methodAddress.ToPointer() = *(long*)srcAdr.ToPointer();
            else
                *(int*)methodAddress.ToPointer() = (int)*(uint*)srcAdr.ToPointer();
        }

        public static unsafe IntPtr GetMethodAddress(MethodBase method) {
            if (method is DynamicMethod)
                return GetDynamicMethodAddress(method);
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            return new IntPtr((void*)((IntPtr)method.MethodHandle.Value.ToPointer() + new IntPtr(2) * 4));
        }

        private static unsafe IntPtr GetDynamicMethodAddress(MethodBase method) {
            byte* pointer = (byte*)GetDynamicMethodRuntimeHandle(method).Value.ToPointer();
            return IntPtr.Size == 8 ? new IntPtr((void*)(pointer + (new IntPtr(6) * 8).ToInt64())) : new IntPtr((void*)(pointer + (new IntPtr(6) * 4).ToInt64()));
        }

        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(
          MethodBase method) {
            if (method is DynamicMethod) {
                FieldInfo field = typeof(DynamicMethod).GetField("m_method", BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                    return (RuntimeMethodHandle)field.GetValue(method);
            }
            return method.MethodHandle;
        }

        private static bool MethodSignaturesEqual(MethodBase x, MethodBase y) {
            if (x.CallingConvention != y.CallingConvention || GetMethodReturnType(x) != GetMethodReturnType(y))
                return false;
            ParameterInfo[] parameters1 = x.GetParameters();
            ParameterInfo[] parameters2 = y.GetParameters();
            if (parameters1.Length != parameters2.Length)
                return false;
            for (int index = 0; index < parameters1.Length; ++index) {
                if (parameters1[index].ParameterType != parameters2[index].ParameterType)
                    return false;
            }
            return true;
        }

        private static Type GetMethodReturnType(MethodBase method) {
            MethodInfo methodInfo = method as MethodInfo;
            return !(methodInfo == (MethodInfo)null) ? methodInfo.ReturnType : throw new ArgumentException("Unsupported MethodBase : " + method.GetType().Name, nameof(method));
        }
    }
}

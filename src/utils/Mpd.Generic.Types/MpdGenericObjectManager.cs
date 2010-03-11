using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.Types.IO;
using System.Reflection;

namespace Mpd.Generic.Types
{
    public class MpdGenericObjectManager
    {
        public static SafeMemFile CreateSafeMemFile(int nSize)
        {
            return new IO.Impl.SafeMemFileImpl(nSize);
        }

        public static SafeBufferedFile CreateSafeBufferedFile(string fullname, 
            System.IO.FileMode fileMode, System.IO.FileAccess fileAccess, System.IO.FileShare fileShare)
        {
            return new IO.Impl.SafeBufferedFileImpl(fullname, fileMode, fileAccess, fileShare);
        }

        public static Tag CreateTag(params object[] parameters)
        {
            return CreateObject(typeof(Impl.TagImpl), parameters) as Tag;
        }

        private static object CreateObject(Type t, params object[] parameters)
        {
            object obj = t.Assembly.CreateInstance(t.FullName,
                true,
                BindingFlags.CreateInstance,
                null,
                parameters,
                null,
                null);

            return obj;
        }
    }
}

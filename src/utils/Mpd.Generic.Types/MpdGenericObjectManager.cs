using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.Types.IO;
using System.Reflection;
using Mpd.Generic.Types.IO.Impl;

namespace Mpd.Generic.Types
{
    public class MpdGenericObjectManager
    {
        public static SafeFile OpenSafeFile(string fullpath,
            System.IO.FileMode fileMode,
            System.IO.FileAccess fileAccess,
            System.IO.FileShare fileShare)
        {
            return CreateObject(typeof(SafeFileImpl), fullpath, fileMode, fileAccess, fileShare) as SafeFile;
        }

        public static SafeMemFile CreateSafeMemFile(int size)
        {
            return CreateObject(typeof(SafeMemFileImpl), size) as SafeMemFile;
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

        public static object CreateObject(Type t, params object[] parameters)
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

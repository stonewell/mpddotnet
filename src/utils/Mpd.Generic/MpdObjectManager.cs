﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mpd.Generic.IO;
using System.Reflection;
using Mpd.Generic.IO.Impl;
using System.Security.Cryptography;

namespace Mpd.Generic
{
    public class MpdObjectManager
    {
        public static SafeFile OpenSafeFile(string fullpath,
            System.IO.FileMode fileMode,
            System.IO.FileAccess fileAccess,
            System.IO.FileShare fileShare)
        {
            return CreateObject(typeof(SafeFileImpl), fullpath, fileMode, fileAccess, fileShare) as SafeFile;
        }

        public static SafeMemFile CreateSafeMemFile(params object[] args)
        {
            return CreateObject(typeof(SafeMemFileImpl), args) as SafeMemFile;
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

        private static readonly RSACryptoServiceProvider rsa_ =
            new RSACryptoServiceProvider();

        public static RSAPKCS1SignatureFormatter CreateRSAPKCS1V15SHA1Signer()
        {
            RSAPKCS1SignatureFormatter formater =
                new RSAPKCS1SignatureFormatter(rsa_);

            formater.SetHashAlgorithm("SHA1");

            return formater;
        }
    }
}

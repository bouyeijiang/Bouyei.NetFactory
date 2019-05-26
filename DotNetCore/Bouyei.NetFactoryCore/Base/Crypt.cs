using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Bouyei.NetFactoryCore.Base
{
    public static class Crypt
    {
        public static string ToSha1Base64(this string value)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Convert.ToBase64String(bytes);
        }

        public static string ToMd5(this string value)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
            return Convert.ToBase64String(bytes);
        }
    }
}

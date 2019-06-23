using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Bouyei.NetFactoryCore.Base
{
    public static class Crypt
    {
        public static string ToSha1Base64(this string value,Encoding encoding)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] bytes = sha1.ComputeHash(encoding.GetBytes(value));
                return Convert.ToBase64String(bytes);
            }
        }

        public static string ToMd5(this string value,Encoding encoding)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(encoding.GetBytes(value));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}

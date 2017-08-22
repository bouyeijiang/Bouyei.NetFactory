using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bouyei.NetProviderFactory
{
    using Protocols;

    public class NetProtocolProvider : INetProtocolProvider
    {
        public static NetProtocolProvider CreateNetProtocolProvider()
        {
            return new NetProtocolProvider();
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public Package Decode(byte[] buffer, int offset, int size)
        {
            Package pkg = new Package();
            if (pkg.DeocdeFromBytes(buffer, offset, size)) return pkg;
            else return null;
        }

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="pkg"></param>
        /// <returns></returns>
        public byte[] Encode(Package pkg)
        {
            return pkg.EncodeToBytes();
        }
    }
}

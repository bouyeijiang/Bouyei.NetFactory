using System.Net;
using System;
using System.Net.Sockets;

namespace Bouyei.NetProviderFactory
{
    public class SocketToken:IDisposable,IComparable<SocketToken>
    {
        /// <summary>
        /// 会话编号
        /// </summary>
        public int TokenId { get; set; }
       
        /// <summary>
        /// 会话socket对象
        /// </summary>
        public Socket TokenSocket { get; set; }
        /// <summary>
        /// 会话的终结点
        /// </summary>
        public IPEndPoint TokenIpEndPoint { get; set; }

        private bool _isDisposed = false;

        //析构
        ~SocketToken()
        {
            Dispose(false);
        }

        /// <summary>
        /// 构造
        /// </summary>
        public SocketToken(int id)
        {
            this.TokenId = id;
        }
        
        public SocketToken(){}

        /// <summary>
        /// 关闭该连接对象，释放相关资源,非完全释放Socket对象
        /// </summary>
        public void Close()
        {
            if (TokenSocket != null)
            {
                if (TokenSocket.Connected)
                {
                    TokenSocket.Shutdown(SocketShutdown.Send);
                    TokenSocket.Close();
                }
            }
        }

        /// <summary>
        /// 关闭该连接对象并释放该对象资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 根据SocketId比较大小
        /// </summary>
        /// <param name="sToken"></param>
        /// <returns></returns>
        public int CompareTo(SocketToken sToken)
        {
            return this.TokenId.CompareTo(sToken.TokenId);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="isDisposing"></param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;

            if (isDisposing) {
                Close();
                _isDisposed = true;
                if (TokenSocket != null)
                {
                    TokenSocket.Dispose();
                    TokenSocket = null;
                }
            }
        }
    }
}
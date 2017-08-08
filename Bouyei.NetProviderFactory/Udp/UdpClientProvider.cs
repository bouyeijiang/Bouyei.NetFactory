using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Generic;

namespace Bouyei.NetProviderFactory.Udp
{
    public class UdpClientProvider:IDisposable
    {
        #region 定义变量
        private bool _isDisposed = false;
        //private SocketAsyncEventArgs sendArgs = null;
        //private SocketAsyncEventArgs recArgs = null;
        private SocketTokenManager<SocketAsyncEventArgs> sendPool = null;
        private SocketTokenManager<SocketAsyncEventArgs> receivePool = null;
        private Socket recSocket = null;
        private Socket sendSocket = null;
        
        #endregion

        #region 属性
        public int SendBufferPoolNumber { get { return sendPool.Count; } }

        /// <summary>
        /// 接收回调处理
        /// </summary>
        public OnReceiveHandler ReceiveCallbackHandler { get; set; }

        /// <summary>
        /// 发送回调处理
        /// </summary>
        public OnSentHandler SentCallbackHandler { get; set; }
        /// <summary>
        /// 接收缓冲区回调
        /// </summary>
        public OnReceiveOffsetHandler ReceiveOffsetHandler { get; set; }
        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;

            if (isDisposing)
            {
                DisposeSocketPool();
                recSocket.Dispose();
                sendSocket.Dispose();
                _isDisposed = true;
            }
        }

        private void DisposeSocketPool()
        {
            while (sendPool.Count > 0)
            {
                var item = sendPool.Get();
                if (item != null) item.Dispose();
            }

            while (receivePool.Count > 0)
            {
                var item = receivePool.Get();
                if (item != null) item.Dispose();
            }
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public UdpClientProvider()
        {
            recSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <summary>
        /// 初始化对象
        /// </summary>
        /// <param name="recBufferSize"></param>
        /// <param name="port"></param>
        public void Initialize(int maxNumberOfConnections, int port)
        {
            sendPool = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);
            receivePool = new SocketTokenManager<SocketAsyncEventArgs>(maxNumberOfConnections);

            IPEndPoint ips = new IPEndPoint(IPAddress.Any, port);
            recSocket.Bind(ips);
            //初始化发送接收对象池
            for (int i = 0; i < maxNumberOfConnections; ++i)
            {
                SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
                sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                sendArgs.UserToken = new SocketToken(i + maxNumberOfConnections);
                sendPool.Set(sendArgs);

                SocketAsyncEventArgs recArgs = new SocketAsyncEventArgs();
                recArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                recArgs.UserToken = new SocketToken(i);
                receivePool.Set(sendArgs);
            }
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remoteEP"></param>
        public void Send(byte[] data,int offset,int size, IPEndPoint remoteEP)
        {
            SocketAsyncEventArgs sendArgs = sendPool.Get();
            if (sendArgs == null)
                throw new Exception("发送缓冲池已用完,等待回收...");

            sendArgs.RemoteEndPoint = remoteEP;
            ((SocketToken)sendArgs.UserToken).TokenSocket = sendSocket;

            sendArgs.SetBuffer(data, offset, size);
            if (!sendSocket.SendToAsync(sendArgs))
            {
                ProcessSent(sendArgs);
            }
        }

        public void Send(byte[] buffer,int offset,int size,bool waitingSignal=true)
        {
            SocketAsyncEventArgs sendArgs = sendPool.Get();
            if (sendArgs == null)
            {
                while (waitingSignal)
                {
                    Thread.Sleep(1000);
                    sendArgs = sendPool.Get();
                    if (sendArgs != null) break;
                }
            }
            if (sendArgs == null)
                throw new Exception("发送缓冲池已用完,等待回收...");

            sendArgs.SetBuffer(buffer, offset, size);
            ((SocketToken)sendArgs.UserToken).TokenSocket = sendSocket;

            if (!sendSocket.SendToAsync(sendArgs))
            {
                ProcessSent(sendArgs);
            }
        }

        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="recAct"></param>
        /// <param name="recBufferSize"></param>
        /// <returns></returns>
        public int SendSync(byte[] buffer,Action<int,byte[]> recAct = null,int recBufferSize=4096)
        {
           int sent= sendSocket.Send(buffer);
            if (recAct != null && sent > 0)
            {
                byte[] recBuffer = new byte[recBufferSize];
                
                int cnt = sendSocket.Receive(recBuffer, recBuffer.Length, 0);
                  
                recAct(cnt,recBuffer);
            }
            return sent;
        }

        public void ReceiveSync(Action<int, byte[]> recAct, int recBufferSize = 4096)
        {
            int cnt = 0;
            byte[] buffer = new byte[recBufferSize];
            do
            {
                if (sendSocket.Connected == false) break;

                cnt = sendSocket.Receive(buffer, buffer.Length, 0);
                if (cnt > 0)
                {
                    recAct(cnt, buffer);
                }
            } while (cnt > 0);
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Send(byte[] data, string ip, int port)
        {
            var serverEP = new IPEndPoint(IPAddress.Parse(ip), port);

            SocketAsyncEventArgs sendArgs = sendPool.Get();
            if (sendArgs == null)
                throw new Exception("发送缓冲池已用完,等待回收...");

            sendArgs.RemoteEndPoint = serverEP;
            ((SocketToken)sendArgs.UserToken).TokenSocket = sendSocket;

            sendArgs.SetBuffer(data, 0, data.Length);
            if (!sendSocket.SendToAsync(sendArgs))
            {
                ProcessSent(sendArgs);
            }
        }

        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="remoteEP"></param>
        public void StartReceive(EndPoint remoteEP)
        {
            SocketAsyncEventArgs recArgs = receivePool.Get();
            recArgs.RemoteEndPoint = remoteEP;
            ((SocketToken)recArgs.UserToken).TokenSocket = recSocket;
          
            if (!recSocket.ReceiveFromAsync(recArgs))
            {
                ProcessReceive(recArgs);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                receivePool.Set(e);

                //缓冲区偏移量返回
                if (ReceiveOffsetHandler != null)
                    ReceiveOffsetHandler(e.UserToken as SocketToken, e.Buffer, e.Offset, e.BytesTransferred);

                //截取后返回
                if (ReceiveCallbackHandler != null)
                {
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        if (e.Offset == 0 && e.BytesTransferred == e.Buffer.Length)
                        {
                            ReceiveCallbackHandler(e.UserToken as SocketToken, e.Buffer);
                        }
                        else
                        {
                            byte[] realbytes = new byte[e.BytesTransferred];
                            Buffer.BlockCopy(e.Buffer, e.Offset, realbytes, 0, e.BytesTransferred);

                            ReceiveCallbackHandler(e.UserToken as SocketToken, realbytes);
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw ex;
            }
            finally
            {
                //继续下一个接收
                StartReceive((IPEndPoint)e.RemoteEndPoint);
            }
        }

        private void ProcessSent(SocketAsyncEventArgs e)
        {
            try
            {
                sendPool.Set(e);
                
                if (SentCallbackHandler != null)
                {
                    SentCallbackHandler(e.UserToken as SocketToken, e.BytesTransferred);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
                    ProcessSent(e);
                    break;
            }
        }
    }
}
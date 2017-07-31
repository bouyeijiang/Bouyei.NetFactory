using System;
using System.Net.Sockets;
using System.Net;
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
                var item = sendPool.Pop();
                if (item != null) item.Dispose();
            }

            while (receivePool.Count > 0)
            {
                var item = receivePool.Pop();
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
                sendPool.Push(sendArgs);

                SocketAsyncEventArgs recArgs = new SocketAsyncEventArgs();
                recArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                receivePool.Push(sendArgs);
            }
        }

        /// <summary>
        /// 异步发送数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="remoteEP"></param>
        public void Send(byte[] data, IPEndPoint remoteEP)
        {
            SocketAsyncEventArgs sendArgs = sendPool.Pop();
            sendArgs.RemoteEndPoint = remoteEP;
            sendArgs.SetBuffer(data, 0, data.Length);
            if (!sendSocket.SendToAsync(sendArgs))
            {
                ProcessSent(sendArgs);
            }
        }

        public void Send(byte[] buffer)
        {
            SocketAsyncEventArgs sendArgs = sendPool.Pop();
            sendArgs.SetBuffer(buffer, 0, buffer.Length);
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

        public void ReceiveSync(Action<int, byte[]> recAct = null, int recBufferSize = 4096)
        {
            int cnt = 0;
            byte[] buffer = new byte[recBufferSize];
            do
            {
                if (sendSocket.Connected == false) break;

                cnt = sendSocket.Receive(buffer, buffer.Length, 0);
                if (cnt > 0)
                {
                    recAct?.Invoke(cnt, buffer);
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

            SocketAsyncEventArgs sendArgs = sendPool.Pop();
            sendArgs.RemoteEndPoint = serverEP;
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
            SocketAsyncEventArgs recArgs = receivePool.Pop();
            recArgs.RemoteEndPoint = remoteEP;
            if (!recSocket.ReceiveFromAsync(recArgs))
            {
                ProcessReceive(recArgs);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {
                //缓冲区偏移量返回
                ReceiveOffsetHandler?.Invoke(new SocketToken()
                {
                    TokenSocket = e.ConnectSocket
                }, e.Buffer, e.Offset, e.BytesTransferred);

                //截取后返回
                if (ReceiveCallbackHandler != null)
                {
                    if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                    {
                        if (e.Offset == 0 && e.BytesTransferred == e.Buffer.Length)
                        {
                            ReceiveCallbackHandler(new SocketToken()
                            {
                                TokenSocket = e.ConnectSocket
                            }, e.Buffer);
                        }
                        else
                        {
                            byte[] realbytes = new byte[e.BytesTransferred];
                            Buffer.BlockCopy(e.Buffer, e.Offset, realbytes, 0, e.BytesTransferred);

                            ReceiveCallbackHandler(new SocketToken()
                            {
                                TokenSocket = e.ConnectSocket
                            }, realbytes);
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw ex;
            }
            finally
            {
                receivePool.Push(e);

                //继续下一个接收
                StartReceive((IPEndPoint)e.RemoteEndPoint);
            }
        }

        private void ProcessSent(SocketAsyncEventArgs e)
        {
            try
            {
                if (SentCallbackHandler != null)
                {
                    SentCallbackHandler(new SocketToken()
                    {
                        TokenSocket = e.ConnectSocket
                    }, e.BytesTransferred);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                sendPool.Push(e);
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
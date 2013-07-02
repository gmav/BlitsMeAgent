﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BlitsMe.Common.Security;
using BlitsMe.Communication.P2P.RUDP.Socket.API;
using log4net;

namespace BlitsMe.Communication.P2P.RUDP.Connector.API
{
    public abstract class TcpTransportConnection
    {

        private static readonly ILog Logger = LogManager.GetLogger(typeof(TcpTransportConnection));

        private readonly Thread _transportSocketReader;

        private readonly ITcpOverUdptSocket _socket;

        internal bool Closing { get; private set; }
        internal bool Closed { get; private set; }

        internal event EventHandler CloseConnection;

        private void OnCloseConnection()
        {
            EventHandler handler = CloseConnection;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        internal TcpTransportConnection(ITcpOverUdptSocket socket)
        {
            _socket = socket;
            _transportSocketReader = new Thread(TransportSocketReader) { IsBackground = true };
            _transportSocketReader.Name = "_transportSocketReader[" + _transportSocketReader.ManagedThreadId + "]";
            _transportSocketReader.Start();
        }

        protected abstract void _Close();

        public void Close()
        {
            if (!Closing && !Closed)
            {
                Closing = true;
                _Close();
#if(DEBUG)
                Logger.Debug("Closing tcp connection components");
#endif
#if(DEBUG)
                Logger.Debug("Closing Socket");
#endif
                _socket.Close();
                /* this will close with the surrounding connections
                // Only abort the thread if we are not it.
                if (_transportSocketReader.IsAlive && !_transportSocketReader.Equals(Thread.CurrentThread))
                {
#if(DEBUG)
                    Logger.Debug("Shutting of forward proxy thread.");
#endif
                    _transportSocketReader.Abort();
                }
                */
                Closing = false;
                Closed = true;
                OnCloseConnection();
            }
        }

        protected abstract bool ProcessTransportSocketRead(byte[] data, int length);

        private void TransportSocketReader()
        {
            try
            {
                while (!_socket.Closed && !_socket.Closing)
                {
                    byte[] tmpRead = new byte[8192];
                    int read;
                    try
                    {
                        read = _socket.Read(tmpRead, tmpRead.Length);
                    }
                    catch (ObjectDisposedException e)
                    {
                        break;
                    }
                    if (read > 0)
                    {
#if(DEBUG)
                        Logger.Debug("Read " + read + " bytes from transport socket, writing to upstream handler");
#endif
                        try
                        {
                            if (!ProcessTransportSocketRead(tmpRead, read))
                            {
#if DEBUG
                                Logger.Debug("Upstream handler failed to process the transport socket read data.");
#endif
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error(
                                "Processing a read from the transportManager failed, shutting down TcpConnection");
                            break;
                        }
                    }
                }
                Logger.Info("Connection has been closed");
            }
            catch (Exception ex)
            {
                Logger.Error("Connection has failed : " + ex.Message);
            }
            finally
            {
                try
                {
                    this.Close();
                }
                catch (Exception e)
                {
                    Logger.Error("Error while closing ProxyConnection : " + e.Message, e);
                }
            }
        }

        public bool SendDataToTransportSocket(byte[] data, int length)
        {
                if (!Closing && !Closed)
                {
                    if (!_socket.Closed)
                    {
                        _socket.Send(data, length, 30000);
                    }
                    else
                    {
#if(DEBUG)
                        Logger.Debug("Outgoing transport socket has been closed");
#endif
                        return false;
                    }
                }
                else
                {
                    Logger.Debug("Cannot write data to Transport socket, connection is closing or is closed");
                    return false;
                }
            return true;
        }
    }
}

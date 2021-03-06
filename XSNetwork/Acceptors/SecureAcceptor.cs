﻿using System.Net.Sockets;
using XSLibrary.Cryptography.ConnectionCryptos;
using XSLibrary.Network.Connections;
using XSLibrary.Utility;

namespace XSLibrary.Network.Acceptors
{
    public delegate void ConnectionCreatedHandler(object sender, TCPPacketConnection connection);

    public class SecureAcceptor
    {
        public event ConnectionCreatedHandler SecureConnectionEstablished;

        public int CryptoHandshakeTimeout { get; set; } = 5000;
        public CryptoType Crypto { get; set; } = CryptoType.NoCrypto;

        Logger m_logger;
        public Logger Logger
        {
            get { return m_logger; }
            set
            {
                m_logger = value;
                Acceptor.Logger = value;
            }
        }
        TCPAcceptor Acceptor { get; set; }

        public SecureAcceptor(TCPAcceptor acceptor)
        {
            m_logger = new NoLog();
            Acceptor = acceptor;
            Acceptor.ClientConnected += OnBaseAcceptorClientConnect;
        }

        public void Dispose()
        {
            Acceptor.Dispose();
        }

        public void Run()
        {
            Acceptor.Run();
        }

        public void Stop()
        {
            Acceptor.Stop();
        }

        private void OnBaseAcceptorClientConnect(object sender, Socket acceptedSocket)
        {
            TCPPacketConnection connection = new TCPPacketConnection(acceptedSocket);
            connection.Logger = Logger;

            if (!connection.InitializeCrypto(CryptoFactory.CreateCrypto(Crypto, false), CryptoHandshakeTimeout))
                return;

            SecureConnectionEstablished.Invoke(this, connection);
        }
    }
}

﻿using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using XSLibrary.ThreadSafety.Executors;
using System.Collections.Generic;
using XSLibrary.Utility;

namespace XSLibrary.Network.Acceptors
{
    public class GuardedAcceptor : TCPAcceptor
    {
        Dictionary<string, int> Filter { get; set; } = new Dictionary<string, int>();
        SafeExecutor m_lock;

        public int ReduceInterval { get; set; } = 5000;
        public int AllowedRequestCount { get; set; } = 10;

        public GuardedAcceptor(int port, int maxPendingConnection) : base(port, maxPendingConnection)
        {
            m_lock = new SingleThreadExecutor();
        }

        protected override void StartParallelLoops()
        {
            base.StartParallelLoops();

            DebugTools.ThreadpoolStarter("Reduce loop", ReduceLoop);
        }

        protected override void HandleAcceptedSocket(Socket acceptedSocket)
        {
            bool accept = false;

            m_lock.Execute(() =>
            {
                string key = (acceptedSocket.RemoteEndPoint as IPEndPoint).Address.ToString();

                if (!Filter.ContainsKey(key))
                    Filter.Add(key, 0);

                accept = Filter[key] < AllowedRequestCount;

                Filter[key] = Math.Min(Filter[key] + 1, AllowedRequestCount);
            });

            if (accept)
                base.HandleAcceptedSocket(acceptedSocket);
            else
            {
                Logger.Log(LogLevel.Warning, "Rejected connection from {0} on port {1}", acceptedSocket.RemoteEndPoint, Port);
                acceptedSocket.Dispose();
            }
        }

        private void ReduceLoop()
        {
            while(!Abort)
            {
                m_lock.Execute(() =>
                {
                    List<string> keys = new List<string>(Filter.Keys);
                    foreach (string key in keys)
                        Filter[key] = Math.Max(Filter[key] - 1, 0);
                });

                Thread.Sleep(ReduceInterval);
            }
        }
    }
}

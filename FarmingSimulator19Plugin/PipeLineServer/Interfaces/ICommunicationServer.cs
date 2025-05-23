﻿using System;

namespace FarmingSimulatorSDKClient.PipeLineServer.Interfaces
{
    internal interface ICommunicationServer : ICommunication
    {
        string ServerId { get; }

        event EventHandler<MessageReceivedEventArgs> MessageReceivedEvent;

        event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;

        event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
    }

    internal class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    internal class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }

    internal class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}

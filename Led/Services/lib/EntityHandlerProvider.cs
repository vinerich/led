﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Led.Services.lib.TCP;

namespace Led.Services.lib
{
    public class EntityHandlerProvider : TCP.TcpServiceProvider
    {
        private bool _MessageIdentified;
        private TcpMessages _IncomingMessage;
        private Int32 _DataLength;
        private Int32 _DataReceived;
        private byte[] _Data;

        public override TcpServer TcpServer { get; set; }

        public string ID { get; private set; }        

        public override object Clone()
        {
            return new EntityHandlerProvider(TcpServer);
        }

        public override void OnAcceptConnection(ConnectionState state)
        {
            _MessageIdentified = false;
            byte[] data = new byte[5];
            Buffer.BlockCopy(BitConverter.GetBytes((UInt16)TcpMessages.ID), 0, data, 0, 1);
            if (!state.Write(data, 0, 5))
                state.EndConnection();
        }        

        /*
         *  MESSAGE FORMAT:
         *  1 byte Message_ID   uint8
         *  4 byte Data_Length  int32
         *  * byte Data 
         * 
         */
        public override void OnReceiveData(ConnectionState state)
        {
            byte[] buffer = new byte[1024];
            while (state.AvailableData > 0)
            {
                int readBytes = state.Read(buffer, 0, 1024);
                if (readBytes > 0)
                {                    
                    if (!_MessageIdentified)
                    {
                        byte[] message = new byte[2];
                        message[0] = buffer[0];

                        _IncomingMessage = (TcpMessages)BitConverter.ToUInt16(message, 0);
                        _DataLength = BitConverter.ToInt32(buffer, 1);
                        _Data = new byte[_DataLength];

                        Int32 _dataToCopy = (Int32)(_DataLength - _DataReceived >= readBytes ? readBytes : _DataLength - _DataReceived);
                        Buffer.BlockCopy(buffer, 5, _Data, _DataReceived, _dataToCopy);
                        _DataReceived += _dataToCopy;

                        _MessageIdentified = true;
                    }
                    else
                    {
                        Int32 _dataToCopy = (Int32)(_DataLength - _DataReceived >= readBytes ? readBytes : _DataLength - _DataReceived);
                        Buffer.BlockCopy(buffer, 0, _Data, _DataReceived, _dataToCopy);
                        _DataReceived += _dataToCopy;
                    }

                    if (_DataReceived >= _DataLength)                    
                        _HandleData(state);               
                }
                else state.EndConnection();
                //If read fails then close connection
            }
        }

        public override void OnDropConnection(ConnectionState state)
        {
            App.Instance.MediatorService.BroadcastMessage(MediatorMessages.TcpServer_ClientsChanged, null, null);
        }

        public EntityHandlerProvider()
        {

        }

        public EntityHandlerProvider(TcpServer tcpServer)
        {
            TcpServer = tcpServer;
        }

        private void _HandleData(ConnectionState state)
        {
            switch (_IncomingMessage)
            {
                case TcpMessages.ID:
                    ID = Encoding.ASCII.GetString(_Data);
                    Console.WriteLine("Received ID from client: {0}", ID);
                    TcpServer.AddClientMapping(ID, state);
                    //Task.Run(() => App.Instance.MediatorService.BroadcastMessage(MediatorMessages.TcpServer_ClientsChanged, null, null));
                    App.Instance.MediatorService.BroadcastMessage(MediatorMessages.TcpServer_ClientsChanged, null, null);
                    break;
                case TcpMessages.Ready:
                    Console.WriteLine("Received Ready from client");
                    break;
            }

            _MessageIdentified = false;
            _DataLength = 0;
            _DataReceived = 0;
        }
    }
}

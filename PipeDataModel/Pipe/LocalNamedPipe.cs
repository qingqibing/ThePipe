﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using PipeDataModel.DataTree;
using System.Runtime.Serialization.Formatters.Binary;

namespace PipeDataModel.Pipe
{
    public class LocalNamedPipe : Pipe
    {
        #region-fields
        private string _name;
        private Action _callBack = null;
        #endregion

        #region-properties
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        #endregion

        #region-constructors
        public LocalNamedPipe(string name)
        {
            _name = name;
        }
        public LocalNamedPipe(string name, Action callBack) : this(name)
        {
            _callBack = callBack;
        }
        #endregion

        #region-base class implementation
        protected override void PushData(DataNode data)
        {
            var pipeServer = new NamedPipeServerStream(_name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            pipeServer.WaitForConnection();
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(pipeServer, data);
                pipeServer.WaitForPipeDrain();
            }
            catch(Exception e)
            {
                pipeServer.Close();
                pipeServer = null;
                throw e;                
            }
            pipeServer.Close();
            pipeServer = null;
        }

        protected override DataNode PullData()
        {
            var pipeClient = new NamedPipeClientStream(".", _name, PipeDirection.In, PipeOptions.None);
            pipeClient.Connect();
            BinaryFormatter bf = new BinaryFormatter();
            object received = bf.Deserialize(pipeClient);
            return (DataNode)received;
        }
        #endregion

        #region-methods
        public override void Update()
        {
            Thread updateThread = new Thread(() =>
            {
                base.Update();
                if(_callBack != null) { _callBack.Invoke(); }
            });
            updateThread.Start();
        }
        #endregion
    }
}

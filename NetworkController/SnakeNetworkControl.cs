using Model;
using NetworkUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetworkController
{
    /// <summary>
    /// Controller for our MVC archytype, communicates with both model and sends events to view
    /// </summary>
    public class SnakeNetworkControl
    {
        
        //Event handlers to send messages to the view, not a direct dependency to not allow a circular dependency

        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;

        public delegate void ConnectedHandler();
        public event ConnectedHandler? Connected;

        public delegate void UpdateScreenHandler(World wrld);
        public event UpdateScreenHandler? UpdateScreen;


        private bool establishedConnection = false;

        //create a world from the model
        Model.World wrld = new Model.World();


        /// <summary>
        /// Socketstate which is of the connection with the server
        /// </summary>
        SocketState? serversSocket = null;

        /// <summary>
        /// Connects client to network via network controller library
        /// </summary>
        /// <param name="IP"></param>
        public void Connect( string IP)
        {
            Networking.ConnectToServer(OnConnect, IP, 11000);
            
        }

       

        /// <summary>
        /// Protocol when we first connect to server
        /// </summary>
        /// <param name="s"></param>
        private void OnConnect( SocketState s)
        {
            //if error occurs call that event
            if(s.ErrorOccurred)
            {
                //tell the view
                Error?.Invoke("Error while attempting to connect to server");
                return;
            }

            //set serversSocketState
            serversSocket = s;

            //set on network action to receive messages when they come in
            s.OnNetworkAction = ReceiveMessage;

            //calls the networking library
            Networking.GetData(s);

            //if connect occurs, call that event which begins network protocol by calling name
            Connected?.Invoke();
            

        }

        /// <summary>
        /// Sends the string parameter message to the server
        /// </summary>
        /// <param name="message"></param>
        public void SendToServer(string message)
        {
            Networking.Send(serversSocket!.TheSocket, message);
        }

        /// <summary>
        /// Recieves the message sent by the server, processes data and calls events to the view, updates model
        /// </summary>
        /// <param name="s"></param>
        private void ReceiveMessage(SocketState s)
        {
            //if error occurs, send event
            if(s.ErrorOccurred)
            {
                Error?.Invoke("Lost Connection to the server");
                return;
            }

            //if this is the first connect make sure we get, ID, walls, snakes before we send
            SplitMessages(s);

            //updates screen
            UpdateScreen?.Invoke(wrld);

            //Restart the event loop
            Networking.GetData(s);
        }

        /// <summary>
        /// Splits messages on '\n', creates a list of messages, processes them, 
        /// then invokes event in the view to update screen
        /// </summary>
        /// <param name="state"></param>
        private void SplitMessages(SocketState state)
        {
            //split messages into different parts, seperated by \n
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // Loop until we have processed all messages.
            // We may have received more than one.

            List<string> newMessages = new List<string>();

            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                // build a list of messages to send to the view
                newMessages.Add(p);

                // Then remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);
            }

            //processes the message to update the model
            ProcessMessages(newMessages);


        }

        /// <summary>
        /// processes the messages received from the server
        /// </summary>
        /// <param name="newMessages"></param>
        private void ProcessMessages(IEnumerable<string> newMessages)
        {
            //if we haven't established a connection, the ID, walls, and players will come in
            if (!establishedConnection)
            {
                //retrieve the player ID and worldsize from the first messages
                string[] arrayMessages = newMessages.ToArray();

                //first message is user ID, second message is worldsize
                wrld.setUserID(arrayMessages[0]);
                wrld.setWorldSize(arrayMessages[1]);

                //set the connections as established
                establishedConnection = true;

                return;
            }

            //send the new infromation to the model
            wrld.parseJSon(newMessages);


        }

        /// <summary>
        /// Closes the connection with the server
        /// </summary>
        public void Close()
        {
            serversSocket?.TheSocket.Close();
        }

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="message"></param>
        public void MessageEntered(string message)
        {
            if (serversSocket is not null)
                Networking.Send(serversSocket.TheSocket, message + "\n");
        }

    }
}

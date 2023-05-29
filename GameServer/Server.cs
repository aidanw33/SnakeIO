using Model;
using NetworkUtil;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using SnakeGame;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System;

namespace GameServer
{

    /// <summary>
    /// Server Class represents the server and holds the executable to run the game server,
    /// handles networking, updating and handling server situations
    /// </summary>
    public class Server
    {
        //fields for the server
        private static GameSettings? _settings;

        public static World? wrld;

        private static Stopwatch stopWatch = new Stopwatch();

        private static long inbetweenTime = 0;

        private static int start = 0;

        private static int totalF = 0;

        private static int fps = 0;

        public static LinkedList<SocketState> connections = new LinkedList<SocketState>();

        private static XmlReader? xmlReader;


        /// <summary>
        /// Main method which begins our server, imports settings
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
           //get the settings from the XML file
            GameSettings _settings = getGameSettings();

            //create a world with the described settings from the XML file
            createWrld(_settings);

            //use the networking library to start the server
            Networking.StartServer(ClientConnect, 11000);

            //create a stopwatch and start it to keep track of frame duration
            stopWatch = new Stopwatch();
            stopWatch.Start();

            //follow the behavior of the sample server provided
            Console.WriteLine("Server is now running, accepting clients.");

            //Update the world continually, the server stays running until closed
            while (true)
            {
                Update(_settings.MSPerFrame);
            }

        }

        private static void ClientConnect(SocketState s)
        {
            //if an error occured do nothing, do not engage
            if (!s.ErrorOccurred)
            {
                //set the socket to be ready to recieve player name as described in the server/client handshake
                s.OnNetworkAction = ClientHandshake;

                //begin the event loop for data
                Networking.GetData(s);

                //Mimic behavoir of the sample server, announce connection
                Console.WriteLine("Accepted new client");
            }
        }

        /// <summary>
        /// As described by the server/client handshake/network protocol recieve the playername
        /// </summary>
        /// <param name="state"></param>
        private static void ClientHandshake(SocketState state)
        {
            //once again if something has gone wrong to do finish method
            if (!state.ErrorOccurred)
            {
                ProcessClientHandshake(state);
           

                //add the socketstate to the list of connections, lock as multiple threads may want access to this
                //list concurrenlty
                lock (connections)
                {
                    connections.AddLast(state);
                }

                //continue network loop, but know the callback is DataComeFromClient
                Networking.GetData(state);
            }
        }

        /// <summary>
        /// Processes the data sent from the client after the connection has been established
        /// </summary>
        /// <param name="state"></param>
        private static void PostHandshakeData(SocketState state)
        {
            //if something goes wrong with the connection do nothing
            if (!state.ErrorOccurred)
            { 
                 //get the ID for the client
                 int uid = (int)state.ID;
               
                 //split messages into different parts, seperated by \n
                 string clientData = state.GetData();

                //Helper method to process the clients data
                 ProcessClientData(clientData, state);    
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msPerFrame"></param>
        private static void Update(int msPerFrame)
        {

           
                //waiting for the frame time specified by XML
                while (stopWatch.ElapsedMilliseconds < msPerFrame)
                {
                }

                //record how long it's been since the last frame
                inbetweenTime += stopWatch.ElapsedMilliseconds;

                //restart the stopWatch timer for the next frame
                stopWatch.Restart();

                //for every second calculate the fps the server is isServerRunning at
                if (inbetweenTime >= 1000)
                {

                    //calculte the fps
                    fps = totalF - start;

                    //write to the server
                    Console.WriteLine("fps: " + fps);

                    //reset inbetweenTime
                    inbetweenTime = 0;

                    //increment the start inbetweenTime
                    start = totalF;

            }


            //increment total amount of frames
            totalF++;
            String jsonWorld;
            lock (wrld!)
            {
                //update the wrld
                jsonWorld = wrld.Update();
            }

       
            //code to handle removing clients, and sending them data
            lock(connections)
            {
                List<SocketState> willRemove = new List<SocketState>();
                foreach(SocketState ss in connections)
                {
                    //if we disconnect
                    if(!ss.TheSocket.Connected)
                    {
                        //remove from connections list
                      //  connections.Remove(ss);
                        willRemove.Add(ss);

                        //remove from snake dictionary
                        wrld!.snakes.TryGetValue((int)ss.ID, out Snake? sn);

                        sn!.disconnected = true;
                        if (sn!.Alive)
                        {
                            sn!.died = true;
                            sn!.Alive = false;
                        }
                    }
                    else
                    {
                        //if we don't disconnect send the data
                        Networking.Send(ss.TheSocket, jsonWorld);
                    }


                }
                //remove after enumerating to prevent errors
                foreach(SocketState ss in willRemove)
                {
                    //remove socket
                    connections.Remove(ss);

                    //mimic behavior of sample server
                    Console.WriteLine("Client " + ss.ID + " disconnected");

                }
            }
        }

        /// <summary>
        /// Returns the gamesettings found in the files
        /// </summary>
        /// <returns></returns>
        private static GameSettings getGameSettings()
        {
            //get the file path of the 'settings.xml' file
            string settingsString = "settings.xml";
            FileInfo f = new FileInfo(settingsString);
            settingsString = f.FullName;


            //create a serializer object to read the file
            DataContractSerializer serializer = new DataContractSerializer(typeof(GameSettings));

            //create the xml reader for settingsString
            xmlReader = XmlReader.Create(settingsString);

            //make _settings a GameSettings
            _settings = serializer!.ReadObject(xmlReader!) as GameSettings;
            
            //return our parsed GameSettings from the original file
            return _settings!;
        }

        /// <summary>
        /// create a world with the game settings loaded in as defined by the xml settings file
        /// </summary>
        /// <param name="_settings"></param>
        private static void createWrld(GameSettings _settings)
        {
            wrld = new World();
            wrld.setWrldFromXML(_settings.UniverseSize, _settings.Walls, _settings.RespawnRate, _settings.FramesPerShot);
        }

        /// <summary>
        /// Add a new snake for the client in a random position
        /// </summary>
        private static Snake addNewClientsSnake(string clientName, int clientID)
        {
            //lock the wrld as we are changing our dictionary in wrld to add a snake
            lock (wrld!)
            {
               return wrld.AddSnakeInRandomSpot(clientName, clientID);
            }
        }

        /// <summary>
        /// Handles the details of the client/server handshake
        /// </summary>
        /// <param name="state"></param>
        private static void ProcessClientHandshake(SocketState state)
        {
            //get the socket from the SS
            Socket socket = state.TheSocket;

            //get clients name
            string dataFromClient = state.GetData().Trim();
            string clientName = dataFromClient;

            //add the clients new snake into the world
            Snake s = addNewClientsSnake(clientName, (int)state.ID);

            //send the data to the client via the specifications
            Networking.Send(socket, s.ID + "\n" + wrld!.worldSize + "\n");

            //send the walls to the client
            SendWalls(state);

            //change the callback to no longer be the handshake procedure
            state.OnNetworkAction = PostHandshakeData;

            //Mimic behavoir of the sample server
            Console.WriteLine("Player(" + state.ID + ") " + clientName + " joined.");

        }

        /// <summary>
        /// Sends the walls to the client
        /// </summary>
        /// <param name="state"></param>
        private static void SendWalls(SocketState state)
        {
            //create a long string of all the walls
            string allWalls = "";
            foreach (Wall w in wrld!.walls.Values)
            {
                //add a '/n' after each wall
                allWalls += (JsonConvert.SerializeObject(w) + "\n");
            }

            //send all the walls
            Networking.Send(state.TheSocket, allWalls);
        }

        /// <summary>
        /// Processes the clients data, moves the snakes if they requested to be moved
        /// </summary>
        /// <param name="clientData"></param>
        private static void ProcessClientData(string clientData, SocketState ss)
        {
            //parse the clientdata into an array
            string[] clientDataArray = clientData.Split('\n'); ;

            foreach(string s in clientDataArray)
            {
                //check to remove empty messages
                if (s.Length == 0)
                    continue;

                //lock the wrld as processcommand changes it
                lock(wrld!)
                {
                    Snake sn;
                    if(wrld!.snakes.TryGetValue((int)ss.ID, out sn!))
                    {
                        wrld!.ReadUserInput(sn, s);
                    }
                }
                ss.RemoveData(0, s.Length + 1);
            }
           
            //restart the loop
            Networking.GetData(ss);
        }
    }
}
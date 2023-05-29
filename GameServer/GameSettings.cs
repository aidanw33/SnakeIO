using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Model;
using SnakeGame;


namespace GameServer
{
    /// <summary>
    /// The gamesettings outlined by the XML settings file
    /// </summary>
    [DataContract(Name = "GameSettings", Namespace = "")]
    public class GameSettings
    {

        //fields

        [DataMember]
        public int MSPerFrame;

        [DataMember]
        public int RespawnRate;

        [DataMember]
        public int FramesPerShot;

        [DataMember]
        public int UniverseSize;

        [DataMember]
        public List<Wall> Walls;


        public GameSettings()
        {
            UniverseSize = 0;
            MSPerFrame = 0;
            RespawnRate = 0;
            Walls = new List<Wall>();
        }


    }
}

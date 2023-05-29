using Newtonsoft.Json;
using SnakeGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{

    /// <summary>
    /// Class which represents a power up
    /// </summary>
    [JsonObject( MemberSerialization.OptIn )]
    public class Power
    {

        private static int IdIncremment;
        /// <summary>
        /// Id property
        /// </summary>
        [JsonProperty(PropertyName = "power")]
        public int ID { get; private set; }

        /// <summary>
        /// location
        /// </summary>
        [JsonProperty(PropertyName = "loc")]
        public Vector2D? location;

        /// <summary>
        /// died property
        /// </summary>
        [JsonProperty(PropertyName = "died")]
        public bool died;

        /// <summary>
        /// Constructor to create a powerup
        /// </summary>
        /// <param name="location"></param>
        public Power(Vector2D? location)
        {
            this.ID = IdIncremment;
            IdIncremment++;
            this.location = location;
            this.died = false;
        }





    }
}

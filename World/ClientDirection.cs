using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    /// <summary>
    /// Class to represent the movement of client's wishes
    /// </summary>
    public class ClientDirection
    {
        //fields
        public string moving;

        /// <summary>
        /// constructor to create the class
        /// </summary>
        /// <param name="direction"></param>
        public ClientDirection(string direction)
        {
            this.moving = direction;
        }

    }
}

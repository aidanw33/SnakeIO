using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;
using SnakeGame;
using System.Runtime.Serialization;
using System.Xml.Linq;

namespace Model;

/// <summary>
/// class representing a wall
/// </summary>
[DataContract(Name = "Wall", Namespace = "")]
[JsonObject(MemberSerialization.OptIn)]
public class Wall
{

    //fields
    [DataMember(Name = "ID")]
    [JsonProperty(PropertyName = "wall")]
    public int ID { get; private set; }

    [DataMember(Name = "p1")]
    [JsonProperty(PropertyName = "p1")]
    public Vector2D? P1 { get; private set; }

    [DataMember(Name = "p2")]
    [JsonProperty(PropertyName = "p2")]
    public Vector2D? P2 { get; private set; }

    /// <summary>
    /// Checks to see if a position and a wall collide
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool Collides(Vector2D position, int buffer)
    {
        //get X and Y positions
        double xPos = position.X;
        double yPos = position.Y;

        //if wall is horizantal make sure we aren't in range of the wall
        if(!isVertical())
        {
            //check to see if p1.x > or < p2.x
            if(P1!.X > P2!.X)
            {
                if ((xPos >= P2!.X - buffer && xPos <= P1!.X + buffer) && (yPos < P1!.Y + 25 + buffer && yPos > P1!.Y - 25 - buffer))
                    return true;
            }
            else
            {
                if ((xPos >= P1!.X - buffer && xPos <= P2!.X + buffer) && (yPos < P1!.Y + 25 + buffer && yPos > P1!.Y - 25 - buffer))
                    return true;
            }
        }
        else
        {
            //check to see if p1.y > or < p2.y
            if (P1!.Y > P2!.Y)
            {
                if ((yPos >= P2!.Y - buffer && yPos <= P1!.Y + buffer) && (xPos < P1!.X + 25 + buffer && xPos > P1!.X - 25 - buffer))
                    return true;
            }
            else
            {
                if ((yPos >= P1!.Y - buffer && yPos <= P2!.Y + buffer) && (xPos < P1!.X + 25 + buffer && xPos > P1!.X - 25 - buffer))
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Checks for equality among the x points, which signifies a vertical wall
    /// </summary>
    /// <returns></returns>
    private bool isVertical()
    {
        if(P1!.X == P2!.X)
        {
            return true;
        }
        return false;
    }
}



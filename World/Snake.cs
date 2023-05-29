using Newtonsoft.Json;
using SnakeGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Model
{
    /// <summary>
    /// This class represents a snake
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Snake
    {

        //tracks the amount of frames since the death of this snake
        private int framesSinceDeath;

        //tracks the growth of the snake after a powerup is consumed
        public int sizeToGrow;

        //the speed of the snake
        public int pace = 3;

        //all prooperties of the snake defined by the server, sent a json representation to the client
        [JsonProperty(PropertyName = "snake")]
        public int ID { get; private set; }

        [JsonProperty(PropertyName = "body")]
        public LinkedList<Vector2D>? body { get;  set; }

        [JsonProperty(PropertyName = "dir")]
        public Vector2D? direction { get;  set; }

        [JsonProperty(PropertyName = "name")]
        public string name { get; private set; } = "";


        [JsonProperty(PropertyName = "score")]
        public int score { get;  set; }

        [JsonProperty(PropertyName = "died")]
        public bool died { get; set; }

        [JsonProperty(PropertyName = "alive")]
        public bool Alive { get; set; } = true;


        [JsonProperty(PropertyName = "dc")]
        public bool disconnected { get;  set; }

        [JsonProperty(PropertyName = "join")]
        public bool joined { get;  set; }

        /// <summary>
        /// The head of the snake
        /// </summary>
        public Vector2D SnakeHead
        {
            get
            {
                return body!.Last();
            }
            private set
            {
                body!.RemoveLast();
                body!.AddLast(value);
            }
        }

        /// <summary>
        /// The tail of the snake
        /// </summary>
        public Vector2D Tail
        {
            get
            {
                return body!.First();
            }
            private set
            {
                body!.RemoveFirst();
                body!.AddFirst(value);
            }
        }


        /// <summary>
        /// Constructor for a basic snake, in it's preutilized state
        /// </summary>
        public Snake()
        {
            score = 0;
            died = false;
            disconnected = false;
            ID = 0;
            body = new LinkedList<Vector2D>();
            name = "";
            direction = new Vector2D();
            joined = false;
        }


        /// <summary>
        /// Sets the snake with the given parameters
        /// </summary>
        /// <param name="head"></param>
        /// <param name="tail"></param>
        /// <param name="ID"></param>
        /// <param name="name"></param>
        public void setSnake(Vector2D head, Vector2D tail, int ID, string name)
        {
            this.body!.AddFirst(tail);
            this.body!.AddLast(head);
            this.direction = head - tail;
            direction.Clamp();
            this.ID = ID;
            this.name = name;
            joined = true;
        }

        /// <summary>
        /// Method which determines if it is time for this snake to respawn
        /// </summary>
        /// <param name="respawnDelay"></param>
        /// <returns></returns>
        public bool TimeToRespawn(int respawnDelay)
        {
            //If the frames per death excedes the respawnDelay amount
            //return true signifing it is time to respawn
            if(framesSinceDeath > respawnDelay)
            {
                //reset frames since death
                framesSinceDeath = 0;

                //return 
                return true;
            }
            //if it's not time to respawn keep going and increment
            framesSinceDeath++;

            //return false
            return false;
        }

  

        public void WrapAroundTheWorld(int worldSize)
        {
            //get head of the snake
            Vector2D head = new Vector2D(body!.Last!.Value);

            //move the head, then move the tail
            if(head.X > worldSize/2 || head.X < -worldSize / 2)
            {
                if (head.X > 0)
                    head.X = -worldSize / 2;
                else
                    head.X = worldSize / 2;
            }
            if (head.Y > worldSize / 2 || head.Y < -worldSize / 2)
            {
                if (head.Y > 0)
                    head.Y = -worldSize / 2;
                else
                    head.Y = worldSize / 2;
            }

            if(Tail.X > worldSize/2 || Tail.X < -worldSize / 2)
            {
                if (Tail.X > 0)
                    Tail.X = -worldSize / 2;
                else
                    Tail.X = worldSize / 2;
            }
            if (Tail.Y > worldSize / 2 || Tail.Y < -worldSize / 2 )
            {
                if (Tail.Y > 0)
                    Tail.Y = -worldSize / 2;
                else
                    Tail.Y = worldSize / 2;
            }

            //if the tail equals the next segment, remove the next segment
            if (body.First!.Value.Equals(body.First!.Next!.Value))
            {
                body.RemoveFirst();
            }

            //if the new head equals the actuall head
            if (!head.Equals(body.Last.Value))
            {
                //add the new head as a body segment
                body.AddLast(head);
                body.AddLast(new Vector2D(head));
            }
        }


        /// <summary>
        /// Updates the snakes position
        /// </summary>
        /// <param name="wrldSize"></param>
        public void Update(int wrldSize)
        {
            //begin process by updating head first
            Vector2D headSegment = SnakeHead - body!.Last!.Previous!.Value;

            //get a 2D vector from the first head segment
            Vector2D headSeg = new Vector2D(headSegment);

            //normalize it
            headSeg.Normalize();

            //if head segement isn't currently in the correct direction
            if (!headSeg.Equals(direction))
            {
                //check the world parameter and lenght of snake
                if(worldParameterCheck((int)headSegment.Length(), wrldSize))
                {
                    //add the new head
                    body.AddLast(new Vector2D(SnakeHead));
                }
            }

            //increment the snakes movement, dir + speed
            SnakeHead += direction! * pace;

            //move the snake
            moveTheSnake();
          
            //check for the snake wrapping around the world to the other side
            WrapAroundTheWorld(wrldSize);

            //incrememnt size to grow if necessary
            if(sizeToGrow > 0)
                sizeToGrow--;
        }
       

        /// <summary>
        /// Checks to see if the snake hits the wall
        /// </summary>
        /// <param name="w"></param>
        /// <returns></returns>
        public bool HitsWall(Wall w)
        {
            return ProximityCheck(body!.Last!.Value, w.P1!, w.P2!, 30.0);
        }


        /// <summary>
        /// Changes the direction of the snake
        /// </summary>
        /// <param name="d"></param>
        /// <param name="w"></param>
        public void Turn(ClientDirection d, World w)
        {
            Vector2D oldDir = direction!;
            //if we are facing the opposite of the orignal direction, do not change the direction
            if (!DirectionToMove(d).IsOppositeCardinalDirection(direction!))
            {
                //otherwise change the directin
                direction = DirectionToMove(d);
            }
            //check for collision with this snake
            if (CheckForCollision(this, true))
            {
                direction = oldDir;
            }

        }



        /// <summary>
        /// Checks for collision of a snake
        /// </summary>
        /// <param name="snake"></param>
        /// <param name="selfTurn"></param>
        /// <returns></returns>
        public bool CheckForCollision(Snake snake, bool selfTurn)
        {
              //get a list of all the segments of the snake
            IEnumerable<(Vector2D, Vector2D)> segments;
            segments = GetSegmentList(snake);


            int increment = 0;
            foreach ( var segment in segments)
            {
                if(ProximityCheck(body!.Last!.Value, segment.Item1, segment.Item2, 10.0))
                {
                    if ((!selfTurn || increment++ >= (body!.Count - 4)))
                    { 
                        return true;
                    }
                }
               
            }
            return false;
        }

        /// <summary>
        /// Returns the list of the snake body
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(Vector2D v1, Vector2D v2)> getSnakeBody()
        {
            //get the list of the bodies 
            LinkedListNode<Vector2D> pos = body!.First!;

            //go until null
            if (pos != null)
            {
                //loop until null
                while (pos.Next != null)
                {
                    //yield increments, and adds
                    yield return (pos.Value, pos.Next!.Value);

                    //move the marker
                    pos = pos.Next;
                }


            }
        }

        /// <summary>
        /// Check for intersection among objects
        /// </summary>
        /// <param name="objLocation"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        private bool ProximityCheck(Vector2D objLocation, Vector2D P1, Vector2D P2, double buffer)
        {
            //get X and Y positions
            double xPos = objLocation.X;
            double yPos = objLocation.Y;

            //check to see if the defined p1, p2 is vertical or horizantel
            bool isVertical;
            if (P1!.X == P2!.X)
            {
                isVertical = true;
            }
            else
            {
                isVertical = false;
            }

            //Code below checks if we are in the buffer of the defined variables given by the paramteres
            //if wall is horizantal make sure we aren't in range of the wall
            if (!isVertical)
            {
                
                if (P1!.X > P2!.X)
                {
                    if ((xPos >= P2!.X - buffer && xPos <= P1!.X + buffer) && (yPos < P1!.Y + buffer && yPos > P1!.Y - buffer))
                        return true;
                }
                else
                {
                    if ((xPos >= P1!.X - buffer && xPos <= P2!.X + buffer) && (yPos < P1!.Y + buffer && yPos > P1!.Y - buffer))
                        return true;
                }
            }
            else
            {
                //check to see if p1.y > or < p2.y
                if (P1!.Y > P2!.Y)
                {
                    if ((yPos >= P2!.Y - buffer && yPos <= P1!.Y + buffer) && (xPos < P1!.X + buffer && xPos > P1!.X - buffer))
                        return true;
                }
                else
                {
                    if ((yPos >= P1!.Y - buffer && yPos <= P2!.Y + buffer) && (xPos < P1!.X + buffer && xPos > P1!.X - buffer))
                        return true;
                }
            }
            return false;

        }
        
        /// <summary>
        /// Checks if the length of an object is appropriate for the size of the wrld
        /// </summary>
        /// <param name="length"></param>
        /// <param name="wrldsize"></param>
        /// <returns></returns>
        private bool worldParameterCheck(int length, int wrldsize)
        {

            if (length <= wrldsize)
                if (length > 0)
                    return true;
                else
                    return false;
            else
                return false;
            
        }

        /// <summary>
        /// updates the movement of the snake
        /// </summary>
        private  void moveTheSnake()
        {
            //if we aren't growing
            if (!(sizeToGrow > 0))
            {
                //we stay moving the same direction
                Vector2D tailDir = body!.First!.Next!.Value - body!.First.Value;
                tailDir.Normalize();

                //add the speed value with the tailDir to the tial
                body.First.Value += tailDir * pace;
            }

            //if equal remove tail
            if (body!.First!.Value.Equals(body.First!.Next!.Value))
            {
                body.RemoveFirst();
            }
        }


        /// <summary>
        /// Returns the 2dVector representation of the direction we must move
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private Vector2D DirectionToMove(ClientDirection dir)
        {
            //get the string direction
            string strDir = dir.moving;

            //return the vector equal to the direction
            if(strDir.Equals("up"))
                return new Vector2D(0, -1);
            if (strDir.Equals("left"))
                return new Vector2D(-1, 0);
            if (strDir.Equals("right"))
                return new Vector2D(1, 0);
            if (strDir.Equals("down"))
                return new Vector2D(0, 1);
            return new Vector2D(direction!);
        }

        /// <summary>
        /// get the segment list of the snake to be used for collision detection
        /// based on whether or not the snake is checking for collision on this snake
        /// </summary>
        /// <param name="snake"></param>
        /// <returns></returns>
        private IEnumerable<(Vector2D, Vector2D)> GetSegmentList(Snake snake)
        {

            IEnumerable<(Vector2D, Vector2D)> snakeBody;

            //check if we are checking for collision among ourselves
            if (ID == snake.ID)
            {
                //if snake is this snake, omit final two body parts
                snakeBody = snake.getSnakeBody().SkipLast(2);
            }
            else
            {
                //just send the whole snake
                snakeBody = snake.getSnakeBody();
            }
            return snakeBody;
        }
      

    }
}

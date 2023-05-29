using System.IO.IsolatedStorage;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SnakeGame;
using static System.Formats.Asn1.AsnWriter;

namespace Model
{

    /// <summary>
    /// Represents the model for the snake client
    /// </summary>
    public class World
    {
        //holds world size and user ID
        public int worldSize;
        public int userID;

        //fields specifically for the server
        int framerpershoot;
        int respawnrate;
        int powerUpSpawn = 50;
        int maxPowerups = 20;
        int framesSincePowerUp = 0;


        //dictionaries for walls, snakes, and powers
        public Dictionary<int, Wall> walls = new Dictionary<int, Wall>();
        public Dictionary<int, Snake> snakes  = new Dictionary<int, Snake>();
        public Dictionary<int, Power> powers = new Dictionary<int, Power>();


        /// <summary>
        /// sets the world size
        /// </summary>
        /// <param name="serverString"></param>
        public void setWorldSize(string serverString)
        {
            this.worldSize = (int)BigInteger.Parse(serverString.Substring(0, serverString.Length - 1));
        }

        /// <summary  >
        /// Method sets userID
        /// </summary>
        /// <param name="serverString"></param>
        public void setUserID(string serverString)
        {
            this.userID = (int)BigInteger.Parse(serverString.Substring(0, serverString.Length - 1));

        }

        /// <summary>
        /// parses the list of json objects sent by the controller, updates the models in view.
        /// </summary>
        /// <param name="jsonObjects"></param>
        public void parseJSon(IEnumerable<string> jsonObjects)
        {
            //locked to prevent race condition
            lock (this)
            {
                //for each jsonobject we need to parse to find what it represents
                foreach (string s in jsonObjects)
                {

                    if (s.Contains("wall"))
                    {
                        //if wall go to handlwall
                        Wall? w = JsonConvert.DeserializeObject<Wall>(s);
                        handleWall(w!);
                    }

                   //if power handle power
                    if (s.Contains("power"))
                    {
                        
                        Power? p = JsonConvert.DeserializeObject<Power>(s);
                        handlePower(p!);
                        
                    }

                    //if snake handle snake
                    if (s.Contains("snake"))
                    {
                        //if snake go to handle snake
                        Snake? sn = JsonConvert.DeserializeObject<Snake>(s);
                        handleSnake(sn!);
                    }


                }
            }
        }

        /// <summary>
        /// Public method to add the settings which are described as in the XML file
        /// </summary>
        /// <param name="worldSize"></param>
        /// <param name="walls"></param>
        /// <param name="respawnrate"></param>
        /// <param name="framespershoot"></param>
        public void setWrldFromXML(int worldSize, IEnumerable<Wall> walls, int respawnrate, int framespershoot)
        {
            //trivially set the parameters to the this
            this.worldSize = worldSize;
            foreach(Wall w in walls)
            {
                this.walls.Add(w.ID, w);
            }
            this.respawnrate = respawnrate;
            this.framerpershoot = framespershoot;
        }

        /// <summary>
        /// adds a random snake going in a random direction in a safe position
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ID"></param>
        /// <returns></returns>
        public Snake AddSnakeInRandomSpot(string name, int ID)
        {
            Vector2D newLocation = NoCollisionLoc(120);

            //add the snake at the given position
            Snake snake = new Snake();
            snake.joined = true;
            snake.setSnake(OneTwentyInADirection(newLocation), newLocation, ID, name);
            snakes.Add(ID, snake);

            return snake;
            
        }

        /// <summary>
        /// Add a random powerup to the map in a safe position
        /// </summary>
        private void AddRandomPowerUp()
        {

            Vector2D newLocation = NoCollisionLoc(16);

            //add the powerup at the given position
            Power p = new Power(newLocation);
            powers.Add(p.ID, p);
        }

        /// <summary>
        /// Takes the clients move data and implements it into the world
        /// </summary>
        /// <param name="s"></param>
        /// <param name="cmd"></param>
        public void ReadUserInput(Snake s, string userInput)
        {
           //if snake is not alive return
            if (!s.Alive)
            {
                return;
            }
            ClientDirection? direction = new ClientDirection("");
            try
            {
                 direction = JsonConvert.DeserializeObject<ClientDirection>(userInput);
            }
            catch(Exception)
            { }

            s.Turn(direction!, this);

        }

        /// <summary>
        /// Updates the world, adds powerups, updates snakes
        /// </summary>
        public string Update()
        {
            
            //update the snakes
            foreach(Snake s in snakes.Values)
            {
                //if the snake is alive update the snake
                if (s.Alive)
                {
                    s.Update(worldSize);

                    //if the snake collides with a snake it dies
                    if (SnakeDeathCollision(s))
                    {
                        s.died = true;
                        s.Alive = false;
                    }
                }
                //if snake is dead check for respawn
                else
                {
                    if (s.TimeToRespawn(respawnrate))
                    {
                        Vector2D newLocation = NoCollisionLoc(16);
                        Vector2D head = OneTwentyInADirection(newLocation);

                        s.body!.Clear();
                        s.body!.AddFirst(newLocation);
                        s.body!.AddLast(head);
                        s.direction = head - newLocation;
                        s.direction.Clamp();
                        s.Alive = true;
                        s.score = 0;

                    }
                }
            }
            
            //update powerups
            if ((maxPowerups > powers.Count) && (framesSincePowerUp > powerUpSpawn))
            {
                AddRandomPowerUp();

                //update trackers for the powerups
                framesSincePowerUp = 0;

                //create random obejct
                Random r = new Random();
                powerUpSpawn = r.Next(200);
            }
            else
            {
                framesSincePowerUp++;
            }

            //return the string of the current state of the world, all the powerups, snakes, etc.
            return StringStateAndRemove();
        }

        /// <summary>
        /// first does the collision checking for powerups, if it collides the snake grows and doesn't die,
        /// then does the checking for other snakes, if it collides it dies
        /// then does the checking for walls, if it collides it dies
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public bool SnakeDeathCollision(Snake s)
        {
            //check for collision with powerups
            foreach (Power p in powers.Values)
            {
                //for every power up check is the snake is within 10 units
                if (WithinDistance(p.location!, s.body!.Last!.Value, 10.0))
                {
                    //if so the power up dies and the snake grows
                    p.died = true;

                    //increase the score of snake
                    s.score++;

                    //increase the amount to grow of the snake
                    s.sizeToGrow += 12;
                }
            }

            //check for collisions with other snakes
            foreach(Snake sna in snakes.Values)
            {
                //if snake is not alive don't consider it
                if(!sna.Alive)
                {
                    continue;
                }
                //if the snake collides return true and leave the method
                if (s.CheckForCollision(sna, false))
                {
                    return true;
                }
            }

            //check for collisions with walls
            foreach (Wall w in walls.Values)
            {
                //walls can not be alive or dead so just remove snake if collision
                if (s.HitsWall(w))
                {
                    return true;
                }
            }

            //if no collision occurs, return false
            return false;
        }

        /// <summary>
        /// Checks for a position which isn't spawning on top of a wall, lets 
        /// user provide a buffer to establish how far away from the wall one wishes to spawn
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private Vector2D NoCollisionLoc(int buffer)
        {
            //for generating random value
            Random r = new Random();
            Vector2D newLocation = new Vector2D();
            //flag for logic
            bool flag = true;
            while (flag)
            {
                //flag for logic
                flag = false;
                //create a newLocation scaled to world size
                newLocation = new Vector2D( (r.NextDouble() * 2 - 1) * (worldSize / 2), (r.NextDouble() * 2 - 1) * (worldSize / 2));
                
                flag = CollideWithWall(newLocation);
            }
            return newLocation;

        }

        /// <summary>
        /// handles a json representation of the snake class coming in
        /// </summary>
        /// <param name="snake"></param>
        private void handleSnake(Snake snake)
        {
            if ((!snake.died && !snake.Alive) || snake.disconnected) 
            {
                snakes.Remove(snake.ID);
               
                return;

            }

            //if the snake already exists, replace it
            if (snakes.ContainsKey(snake.ID))
            {
                snakes.Remove(snake.ID);
            }
            
                snakes.Add(snake.ID, snake);

        }

        /// <summary>
        /// Handles a json representation of snake
        /// </summary>
        /// <param name="power"></param>
        private void handlePower(Power power)
        {
            //if died remove
            if (power.died)
            {
                powers.Remove(power.ID);
                return;
            }

            //if the snake already exists, replace it
            if (powers.ContainsKey(power.ID))
            {
                powers.Remove(power.ID);
            }
                powers.Add(power.ID, power);
        }

        /// <summary>
        /// Handles the Json of a wall
        /// </summary>
        /// <param name="wall"></param>
        private void handleWall(Wall wall)
        {
            //if the snake already exists, replace it
            if (walls.ContainsKey(wall.ID))
            {
                walls.Remove(wall.ID);
            }
            //add wall if it's not already there
            walls.Add(wall.ID, wall);
        }

        private Vector2D OneTwentyInADirection(Vector2D tail)
        {
            Random r = new Random();
            double d = r.NextDouble();
            if (d > .75)
            {
                return tail + new Vector2D(120, 0);
            }
            else if (d > .5)
            {
                return tail + new Vector2D(-120, 0);
            }
            else if (d > .25)
            {
                return tail + new Vector2D(0, 120);
            }
            else
                return tail + new Vector2D(0, -120); 


        }


      

        /// <summary>
        /// Checks to see if two vector2d are within the defined distance of each other,
        /// used for detecting collisions
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="obj2"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static bool WithinDistance(Vector2D obj, Vector2D obj2, double distance)
        {
            if ((obj - obj2).Length() <= distance)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Create a Json string which holds the state of the world for clients, then remove
        /// any powerup which is dead or any snake which has disconnected, put bools such as died
        /// and joined back to their holding state
        /// </summary>
        /// <returns></returns>
        private  string StringStateAndRemove()
        {
            //create a long string to send to the client of all the snakes, and powerups in the wrld
            String jsonString = "";
            foreach (Snake s in snakes.Values)
            {
                jsonString += (JsonConvert.SerializeObject(s) + "\n");

                if (s.disconnected)
                    snakes.Remove(s.ID);
                s.died = false;
                s.joined = false;
            }
            foreach (Power p in powers.Values)
            {
                jsonString += (JsonConvert.SerializeObject(p) + "\n");

                if (p.died)
                    powers.Remove(p.ID);
            }

            //return the state of the world as a string
            return jsonString;
        }

        /// <summary>
        /// Checks to see if a given location collides with a wall
        /// </summary>
        /// <param name="newLocation"></param>
        /// <returns></returns>
        private bool CollideWithWall(Vector2D newLocation)
        {
            //check powerup for collisions against walls
            foreach (Wall w in walls.Values)
            {
                //if we collide with a wall loop again until we find a position in which a wall doesn't exist
                if (w.Collides(newLocation, 16))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
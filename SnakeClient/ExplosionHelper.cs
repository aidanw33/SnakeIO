using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace SnakeGame
{
    /// <summary>
    /// Class represents an explosion
    /// </summary>
    public class ExplosionHelper
    {
        //fields
        private int framesRun;
        private double initialX;
        private double initialY;
        private int ID;

        /// <summary>
        /// constructor for the explosion class
        /// </summary>
        /// <param name="initialX"></param>
        /// <param name="initialY"></param>
        /// <param name="ID"></param>
        public ExplosionHelper(double initialX, double initialY, int ID)
        {
            this.initialX = initialX;
            this.initialY = initialY;
            this.ID = ID;
            framesRun = 0;
        }

        /// <summary>
        /// returns the amount of frames run
        /// </summary>
        /// <returns></returns>
        public int framesRunSoFar()
        {
            return framesRun;
        }

        /// <summary>
        /// returns the ID
        /// </summary>
        /// <returns></returns>
        public int getID()
        {
            return ID;
        }

        public double getX()
        {
            return initialX; 
        }

        /// <summary>
        /// returns the Y location
        /// </summary>
        /// <returns></returns>
        public double getY()
        {
            return initialY;
        }

        /// <summary>
        /// returns the X location
        /// </summary>
        public void increment()
        {
         
            framesRun++;
        }




    }
}

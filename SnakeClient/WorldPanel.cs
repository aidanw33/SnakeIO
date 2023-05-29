using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using Color = Microsoft.Maui.Graphics.Color;
using System.Reflection;
using Microsoft.Maui;
using System.Net;
using Font = Microsoft.Maui.Graphics.Font;
using SizeF = Microsoft.Maui.Graphics.SizeF;
using Windows.UI.ViewManagement;
using Model;
using Windows.UI.Input.Inking;
using Microsoft.UI.Composition.Interactions;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace SnakeGame;
public class WorldPanel : IDrawable
{
    //images
    private IImage wall;
    private IImage background;
    private IImage powerUp;

    //viewsize
    private int viewSize = 900;

    //colors
    Color[] clrs = { Colors.Red, Colors.Blue, Colors.Yellow, Colors.Purple, Colors.Orange, Colors.Green, Colors.Black, Colors.Teal };

    //explosion dictionary
    Dictionary<int, ExplosionHelper> explosions = new Dictionary<int, ExplosionHelper>();

    //coordinates for player
    double playerX = 0;
    double playerY = 0;

    private bool initializedForDrawing = false;

    //model wrld
    private World wrld;

    public delegate void ObjectDrawer(object o, ICanvas canvas);


#if MACCATALYST
    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        return PlatformImage.FromStream(assembly.GetManifestResourceStream($"{path}.{name}"));
    }
#else
    private IImage loadImage( string name )
    {
        //loads images
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeGame.Resources.Images";
        var service = new W2DImageLoadingService();
        return service.FromStream( assembly.GetManifestResourceStream( $"{path}.{name}" ) );
    }
#endif

    public WorldPanel()
    {
    }

    //sets the world to the given world in parameter
    public void SetWorld(World wrld)
    {
        this.wrld = wrld;
    }

    //makes sure to load all images
    private void InitializeDrawing()
    {
        wall = loadImage( "WallSprite.png" );
        background = loadImage( "Background.png" );
        powerUp = loadImage("Mushroom.png");

        initializedForDrawing = true;
    }

    /// <summary>
    /// draws all the walls, snakes, powerups, explosions, backgrounds onto the panel
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        //make sure we have a model to reference
        if (wrld is not null)
        {
            //make sure to load images
            if (!initializedForDrawing)
                InitializeDrawing();

            // undo previous transformations from last frame
            canvas.ResetState();

            //translate to follow player
            Snake userSnake;
            if(wrld.snakes.TryGetValue(wrld.userID, out userSnake))
            {
                    playerX = userSnake.body.Last.Value.X;
                    playerY = userSnake.body.Last.Value.Y;
            }
            canvas.Translate((float)-playerX + (viewSize / 2), (float)-playerY + (viewSize / 2));


            //draw background
            canvas.DrawImage(background, -wrld.worldSize / 2, -wrld.worldSize / 2, wrld.worldSize, wrld.worldSize);

            //lock to prevent race condition
            lock (wrld)
            {
               //every wall we must draw
                foreach (Wall wall in wrld.walls.Values)
                {
                    if (wall.P1.X == wall.P2.X)
                    {
                        //find the length of the wall
                        int difference = (int)(wall.P2.Y - wall.P1.Y);

                        //divide it by 50
                        int loopSize = Math.Abs(difference / 50);

                        //draw each individual wall
                        for (int x = 0; x <= loopSize; x++)
                        {
                            DrawObjectWithTransform(canvas, wall, wall.P1.X, wall.P1.Y + ((difference / loopSize) * x), 0, WallDrawer);
                        }
                    }
                    else
                    {
                        //find the length of wall
                        int difference = (int)(wall.P2.X - wall.P1.X);

                        //divide it by 50
                        int loopSize = Math.Abs(difference / 50);

                        //draw each individual wall
                        for (int x = 0; x <= loopSize; x++)
                        {
                            DrawObjectWithTransform(canvas, wall, wall.P1.X + ((difference / loopSize) * x), wall.P1.Y, 0, WallDrawer);
                        }
                    }
                }
                //draw each powerup in designated location
                foreach (Power p in wrld.powers.Values)
                {
                       DrawObjectWithTransform(canvas, p, p.location.X, p.location.Y, 0, PowerDrawer);
                }

                //parameters used by the foreach loop
                double length = 0;
                double direction = -1;
                foreach (Snake s in wrld.snakes.Values)
                {
                   
                    //change the color for each snake
                    canvas.StrokeColor = clrs[s.ID % 8];

                    //if snake is dead draw the explosion
                    if (s.died)
                    {
                        //create a new explosion
                        ExplosionHelper eH = new ExplosionHelper(s.body.Last.Value.X, s.body.Last.Value.Y, s.ID);
                        //don't add an explosion if the snake is already exploding
                        if (!explosions.ContainsKey(s.ID))
                        {
                            explosions.Add(s.ID, eH);
                        }
                    }
                  
                    //draw the snake body
                    LinkedList<Vector2D> body = s.body;
                    //get the body vectors in an array
                    Vector2D[] array = body.ToArray();
                    for (int x = 0; x < array.Length - 1; x++)
                    {
                        //get the length and direction
                        if (array[x].X == array[x + 1].X)
                        {
                            length = array[x].Y - array[x + 1].Y;
                            direction = 180;
                        }
                        else
                        {
                            length = array[x].X - array[x + 1].X;
                            direction = 90; 
                        }
                        //draw the snake and the label with score/name
                        DrawObjectWithTransform(canvas, (int)length, array[x].X, array[x].Y, direction, SnakeDrawer);
                        DrawObjectWithTransform(canvas, s, body.Last.Value.X, body.Last.Value.Y, 0, LabelDrawer);
                    }
                }
                //get all explosions
                IEnumerable<ExplosionHelper> exp = explosions.Values;
                foreach (ExplosionHelper e in exp)
                {
                    //draw each explosion going farther away from epicenter as more frames run
                    DrawObjectWithTransform(canvas, 0, e.getX() + (2 * e.framesRunSoFar()), e.getY() + (2 * e.framesRunSoFar()), 0, ExplosionDrawer);
                    DrawObjectWithTransform(canvas, 0, e.getX() - (2 * e.framesRunSoFar()), e.getY() + (2 * e.framesRunSoFar()), 0, ExplosionDrawer);
                    DrawObjectWithTransform(canvas, 0, e.getX() + (2 * e.framesRunSoFar()), e.getY() - (2 * e.framesRunSoFar()), 0, ExplosionDrawer);
                    DrawObjectWithTransform(canvas, 0, e.getX() - (2 * e.framesRunSoFar()), e.getY() - (2 * e.framesRunSoFar()), 0, ExplosionDrawer);
                    //increment the frames
                    e.increment();

                    //if we run 30 frames delete the explosion
                    if (e.framesRunSoFar() > 30)
                    {
                        explosions.Remove(e.getID());

                    }
                }

            }
        }




    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void WallDrawer(object o, ICanvas canvas)
    {
        Wall w = o as Wall;
        

        canvas.DrawImage(wall, -25, -25, 50, 50);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void PowerDrawer(object o, ICanvas canvas)
    {
        Power p = o as Power;


        canvas.DrawImage(powerUp, -8, -8, 16, 16);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void SnakeDrawer(object o, ICanvas canvas)
    {
        //get snake length
        int snakeSegmentLength = (int) o ;

        canvas.StrokeSize = 10; //set stroke size
        LineCap lc = LineCap.Round;
        canvas.StrokeLineCap = lc;
        //draw the line
        canvas.DrawLine( 0, 0, 0, snakeSegmentLength);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void LabelDrawer(object o, ICanvas canvas)
    {
        Snake s = o as Snake;

        canvas.Font = new Font("Times New Roman");
        canvas.FontColor = new Color(100);
        canvas.DrawString(s.name + ": " + s.score,0,0,0);
    }

    /// <summary>
    /// A method that can be used as an ObjectDrawer delegate
    /// </summary>
    /// <param name="o">The powerup to draw</param>
    /// <param name="canvas"></param>
    private void ExplosionDrawer(object o, ICanvas canvas)
    {
        canvas.DrawEllipse(0, 0, 6, 6);
    }
}

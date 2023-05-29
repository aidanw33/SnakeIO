using NetworkController;

namespace SnakeGame;

public partial class MainPage : ContentPage
{
    //initialize a snakeNetworkControl
    SnakeNetworkControl snakeNetworkControl = new SnakeNetworkControl();

    public MainPage()
    {
        InitializeComponent();

        graphicsView.Invalidate();

        //create an event when Error is recieved
        snakeNetworkControl.Error += ShowError;
        snakeNetworkControl.Connected += JustConnected;
        snakeNetworkControl.UpdateScreen += UpdateView;



    }

    /// <summary>
    /// Every time the keyboard is tapped, we have to focus the keyboard
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTapped(object sender, EventArgs args)
    {
        keyboardHack.Focus();
    }

    /// <summary>
    /// When keyboard is clicked, we send the appropriate json of the keyclicked to let 
    /// the server know we are moving
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnTextChanged(object sender, TextChangedEventArgs args)
    {
        Entry entry = (Entry)sender;
        String text = entry.Text.ToLower();
        if (text == "w")
        {
            snakeNetworkControl.SendToServer("{\"moving\":\"up\"}" + "\n");
        }
        else if (text == "a")
        {
            snakeNetworkControl.SendToServer("{\"moving\":\"left\"}" + "\n");
        }
        else if (text == "s")
        {
            snakeNetworkControl.SendToServer("{\"moving\":\"down\"}" + "\n");

        }
        else if (text == "d")
        {
            snakeNetworkControl.SendToServer("{\"moving\":\"right\"}" + "\n");

        }
        entry.Text = "";
    }

    /// <summary>
    /// Shows error is a network error has occured
    /// </summary>
    private void NetworkErrorHandler()
    {
        DisplayAlert("Error", "Disconnected from server", "OK");
    }


    /// <summary>
    /// Event handler for the connect button
    /// We will put the connection attempt interface here in the view.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private void ConnectClick(object sender, EventArgs args)
    {
        if (serverText.Text == "")
        {
            DisplayAlert("Error", "Please enter a server address", "OK");
            return;
        }
        if (nameText.Text == "")
        {
            DisplayAlert("Error", "Please enter a name", "OK");
            return;
        }
        if (nameText.Text.Length > 16)
        {
            DisplayAlert("Error", "Name must be less than 16 characters", "OK");
            return;
        }
        //send the server text
        snakeNetworkControl.Connect(serverText.Text);

        keyboardHack.Focus();
    }

    /// <summary>
    /// Follow network protocol, after JustConnected send name, then '\n'
    /// </summary>
    private void JustConnected()
    {
        //after connection via network protocol we send name followed by '/n'
        snakeNetworkControl.SendToServer(nameText.Text + "\n");

    }

    /// <summary>
    /// Use this method as an event handler for when the controller has updated the world
    /// </summary>
    public void OnFrame()
    {
        Dispatcher.Dispatch(() => graphicsView.Invalidate());
    }

    /// <summary>
    /// Notifies user of the controls
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ControlsButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("Controls",
                     "W:\t\t Move up\n" +
                     "A:\t\t Move left\n" +
                     "S:\t\t Move down\n" +
                     "D:\t\t Move right\n",
                     "OK");
    }

    /// <summary>
    /// Notifies user about the program they are running
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void AboutButton_Clicked(object sender, EventArgs e)
    {
        DisplayAlert("About",
      "SnakeGame solution\nArtwork by Jolie Uk and Alex Smith\nGame design by Daniel Kopta and Travis Martin\n" +
      "Implementation by Aidan Wilde and Jaden Rosoff\n" +
        "CS 3500 Fall 2022, University of Utah", "OK");
    }

    /// <summary>
    /// focuses contentpage
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentPage_Focused(object sender, FocusEventArgs e)
    {
        if (!connectButton.IsEnabled)
            keyboardHack.Focus();
    }


    /// <summary>
    /// Handler for controllers error event
    /// </summary>
    /// <param name="error"></param>
    private void ShowError(string error)
    {
        Dispatcher.Dispatch(() => DisplayAlert("Error", error, "OK"));

        //might need to re-enable controls for buttons
    }

    /// <summary>
    /// Updates the view when event is called
    /// </summary>
    /// <param name="wrld"></param>
    private void UpdateView(Model.World wrld)
    {
        worldPanel.SetWorld(wrld);
        Dispatcher.Dispatch(() => graphicsView.Invalidate());

    }

}

  
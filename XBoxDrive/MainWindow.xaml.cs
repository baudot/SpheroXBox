using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Sphero.Communicator;
using Microsoft.Xna.Framework.Input;


namespace SpheroFlick
{
    public class LogListener : TraceListener
    {
        MainWindow mainWindow;

        public LogListener(MainWindow window)
        {
            mainWindow = window;
        }

        public override void Write(string message)
        {
            // mainWindow.Dispatcher.BeginInvoke((Action)(() => { mainWindow.LogMessage(message); }));
        }

        public override void WriteLine(string message)
        {
            mainWindow.Dispatcher.BeginInvoke((Action)(() => { mainWindow.LogMessage(message); }));
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /*
         * The next three variables link the four possible spheros to the possible controllers:
         * At any moment "commander" is pointed to the sphero we're currently controlling,
         * and "controllerState" is linked to the XBox controller we're taking input from.
         * These are associated by an index number. i.e. spheros[0] should be associated with 
         * the first controller, etc.
         */
        private static int ticksBetweenSwitch = 600;
        private static int maxRoundCount = 3;

        SpheroCommander commander;
        GamePadState controllerState = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
        List<SpheroCommander> spheros = new List<SpheroCommander>(4);
        bool gameOn = false;
        int tickCount = 0;
        int roundCount = 0;
        int index = 0;
        int indexOfIt = 0;

        public MainWindow()
        {
            InitializeComponent();
            PopulateDropDownMenus();
            PopulateListOfSpheros();
            Start_XNA_Loop();
        }

        Thread XnaInputThread;

        void XnaInputHandler() 
        {
            while (true)
            {
                Thread.Sleep(10);
                if (gameOn)
                {
                    tickCount++;
                    if (tickCount > ticksBetweenSwitch)
                    {
                        tickCount = 0;
                        indexOfIt = 1 - indexOfIt; // Hack. Alternates between 0 and 1.
                        if (indexOfIt == 0) { roundCount++; } // If we're back to the first Sphero being it, then we've started a new round.
                        if (roundCount >= maxRoundCount) { StopPlaying(null, null); }
                    }
                }
                for (int i = 0; i < 4; i++)
                {
                    index = i;
                    switch (i)
                    {
                        case 0:
                            controllerState = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
                            if (controllerState.IsConnected)
                            {
                                //spheroOneConnectionStatusLabel.Content = "Connected";
                                if (spheros[i].IsConnected()) {
                                    commander = spheros[i];
                                    SendSpheroCommands();
                                }
                            }
                            break;
                        case 1:
                            controllerState = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.Two);
                            if (controllerState.IsConnected) {
                                //spheroTwoConnectionStatusLabel.Content = "Connected";
                                if (spheros[i].IsConnected())
                                {
                                    commander = spheros[i];
                                    SendSpheroCommands();
                                }
                            }
                            break;
                        case 2:
                            controllerState = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.Three);
                            if (controllerState.IsConnected) {
                                //spheroThreeConnectionStatusLabel.Content = "Connected";
                                if (spheros[i].IsConnected())
                                {
                                    commander = spheros[i];
                                    SendSpheroCommands();
                                }
                            }
                            break;
                        case 3:
                            controllerState = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.Four);
                            if (controllerState.IsConnected) {
                                //spheroFourConnectionStatusLabel.Content = "Connected";
                                if (spheros[i].IsConnected())
                                {
                                    commander = spheros[i];
                                    SendSpheroCommands();
                                }
                            }
                            break;
                        default:
                            controllerState = Microsoft.Xna.Framework.Input.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
                            if (controllerState.IsConnected) {
                                Debug.WriteLine("Error case: While polling all controllers");
                            }
                            break;
                    }
                }
            }
        }

        private void SendSpheroCommands()
        {
            if (gameOn) {
                if (tickCount == 0) { commander.RevertToBaseColor(); }
                if (index == indexOfIt) { commander.Dim(breathIntensity(tickCount)); }
            }

            var length = controllerState.ThumbSticks.Left.Length();
            var rightLength = controllerState.ThumbSticks.Right.Length();
            byte rightMagnitude = (byte)Math.Min((Math.Sqrt(rightLength) * 255), 255);

            float XRightStick = controllerState.ThumbSticks.Right.X;
            float YRightStick = controllerState.ThumbSticks.Right.Y;

            if (controllerState.Buttons.LeftShoulder == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                // brake
                if (!commander.destabilized)
                {
                    commander.destabilized = true;
                    SendBrakeCommand();
                }
            }
            else
            {
                if (commander.destabilized)
                {
                    commander.destabilized = false;
                    commander.Stabilize(true);
                }
            }

            ushort rightStickHeading = (ushort)(((Math.Atan2(YRightStick, -XRightStick) * 180 / Math.PI) + 720 - 90) % 360);
            if ((XRightStick != 0 || YRightStick != 0) && (controllerState.Buttons.RightShoulder == Microsoft.Xna.Framework.Input.ButtonState.Pressed))
            // Comment this out and uncomment the block below to: 
            // Hijacks this from calibration while we test spinning in place (which, because that wasn't confusing enough, calls sphero's calibrate command.)
            {
                if (!commander.correctingHeading)
                {
                    commander.headingCorrection -= rightStickHeading;
                    commander.lastRightStickHeading = rightStickHeading;
                    commander.correctingHeading = true;
                }
                else  // If we've already gotten a new heading, fine tune.
                {
                    commander.headingCorrection += (ushort)(((commander.lastRightStickHeading - rightStickHeading) + 360) % 360);
                    commander.lastRightStickHeading = rightStickHeading;
                }
            }
            else
            {
                commander.correctingHeading = false;
            }

            // Reset the speed based on the left & right triggers.
            byte maxSpeed;
            if (controllerState.Triggers.Right > .5) {maxSpeed = 255;}
            else if (controllerState.Triggers.Left > .5) {maxSpeed = 60;}
            else{maxSpeed = 120;}

            byte magnitude = (byte)Math.Min((Math.Sqrt(length) * maxSpeed), maxSpeed);

            float Xstick = controllerState.ThumbSticks.Left.X;
            float Ystick = controllerState.ThumbSticks.Left.Y;

            if (!gameOn)
            {
                if (controllerState.Buttons.A == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                {
                    SetBaseColor(0);
                    commander.RevertToBaseColor();
                }
                else if (controllerState.Buttons.B == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                {
                    SetBaseColor(1);
                    commander.RevertToBaseColor();
                }
                else if (controllerState.Buttons.X == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                {
                    SetBaseColor(2);
                    commander.RevertToBaseColor();
                }
                else if (controllerState.Buttons.Y == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
                {
                    SetBaseColor(3);
                    commander.RevertToBaseColor();
                }
            }

            if (Xstick == 0 && Ystick == 0)
            {
                SendRollCommand(0, commander.lastHeading);
            }
            else //if (Xstick != 0 || Ystick != 0)
            {
                ushort heading = (ushort)(((Math.Atan2(Ystick, -Xstick) * 180 / Math.PI) + 720 - 90 + commander.headingCorrection) % 360);

                if (commander.wasAtRest)
                {
                    SendRollCommand(0, heading);
                    //Thread.Sleep(100);
                }

                SendRollCommand(magnitude, heading);
            }
        }

        public void SendRollCommand(byte Magnitued, UInt16 Heading)
        {
            commander.lastHeading = Heading;

            if (Magnitued < 10){Magnitued = 0;}
            commander.wasAtRest = Magnitued == 0;

            try{commander.spheroComm.write(PacketFactory.new_RollPacket(Magnitued, Heading, true));}
            catch (SpheroCommunicationException ex){DebugNote(ex.Message);}
        }

        public void SendRightSpinCommand() {
            try {commander.spheroComm.write(PacketFactory.new_SetRawMotorValuesPacket(0x02, 0x80, 0x01, 0x80));}
            catch (SpheroCommunicationException ex){DebugNote(ex.Message);}
        }

        public void SendLeftSpinCommand() {
            try {commander.spheroComm.write(PacketFactory.new_SetRawMotorValuesPacket(0x01, 0x80, 0x02, 0x80));}
            catch (SpheroCommunicationException ex){DebugNote(ex.Message);}
        }

        public void SendBrakeCommand()
        {
            try {commander.spheroComm.write(PacketFactory.new_SetRawMotorValuesPacket(0x00, 0x00, 0x00, 0x00));}
            catch (SpheroCommunicationException ex){DebugNote(ex.Message);}
        }

        public void SendBoostCommand()  // Doesn't work right - Gets near immediately overridden by the normal movement commands that follow
        {
            try{commander.spheroComm.write(PacketFactory.new_SetBoostWithTimePacket(200, 0));}
            catch (SpheroCommunicationException ex) {DebugNote(ex.Message);}
        }

        // No longer favored: Should use ColorShift in most cases.
        public void SendRGBCommand(byte r, byte g, byte b)
        {
            try{commander.spheroComm.write(PacketFactory.new_SetRGBLEDPacket(r, g, b, true));}
            catch (SpheroCommunicationException ex){DebugNote(ex.Message);}
        }

        // ColorShift changes a sphero's color relative to a BASE color known to that sphero.
        // When each sphero is just shifting off a base color, we can still tell them apart on the track!
        // 
        // Averages the sphero's base color with the color presented.
        public void ColorShift(byte r, byte g, byte b)
        {
            commander.ShiftColor(r, g, b);
        }

        private void Window_ManipulationStarting_1(object sender, ManipulationStartingEventArgs e)
        {
            e.ManipulationContainer = this;
            e.Handled = true;
        }

        internal void LogMessage(string message)
        {
            //var prev1 = Log.Items.Count > 0 ? Log.Items[0] : null;
            //var prev2 = Log.Items.Count > 1 ? Log.Items[1] : null;
            //var prev3 = Log.Items.Count > 2 ? Log.Items[2] : null;
            //var prev4 = Log.Items.Count > 3 ? Log.Items[3] : null;
            //var prev5 = Log.Items.Count > 4 ? Log.Items[4] : null;
            //var prev6 = Log.Items.Count > 5 ? Log.Items[5] : null;
            //var prev7 = Log.Items.Count > 6 ? Log.Items[6] : null;
            //var prev8 = Log.Items.Count > 7 ? Log.Items[7] : null;
            //var prev9 = Log.Items.Count > 8 ? Log.Items[8] : null;
            //var prev10 = Log.Items.Count > 9 ? Log.Items[9] : null;

            /*
            Log.Items.Clear();
            Log.Items.Add(message);
            Log.Items.Add(prev1);
            Log.Items.Add(prev2);
            Log.Items.Add(prev3);
            Log.Items.Add(prev4);
            Log.Items.Add(prev5);
            Log.Items.Add(prev6);
            Log.Items.Add(prev7);
            Log.Items.Add(prev8);
            Log.Items.Add(prev9);
            Log.Items.Add(prev10);
             */
        }

        double totalX;
        double totalY;

        private void Window_ManipulationDelta_1(object sender, ManipulationDeltaEventArgs e)
        {
            Rectangle rectToMove = e.OriginalSource as Rectangle;
            Matrix rectsMatrix = ((MatrixTransform)rectToMove.RenderTransform).Matrix;

            rectsMatrix.Translate(e.DeltaManipulation.Translation.X, e.DeltaManipulation.Translation.Y);

            rectToMove.RenderTransform = new MatrixTransform(rectsMatrix);

            totalX += e.DeltaManipulation.Translation.X;
            totalY += e.DeltaManipulation.Translation.Y;

            e.Handled = true;
        }

        private void Window_ManipulationInertiaStarting_1(object sender, ManipulationInertiaStartingEventArgs e)
        {
            LogMessage("Total X: " + totalX);
            LogMessage("Total Y: " + totalY);

            commander.ChangeDirection((short)totalX, (short)totalY);

            totalX = 0;
            totalY = 0;

            //((MatrixTransform)FlickPad.RenderTransform).Matrix = origin;
        }

        private void ClearLog_Click_1(object sender, RoutedEventArgs e)
        {
            //Log.Items.Clear();
        }

        private void Exit_Click_1(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        object subscriberKey = new Object();

        private void ConnectSpheroToPort(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("SpheroName: " + SpheroNames.SelectedIndex + "   PortName: " + PortNames.SelectedValue);

            int index = SpheroNames.SelectedIndex;
            string portName = (string)PortNames.SelectedValue;
            var listener = new LogListener(this);

            spheros[index].spheroTrace.Listeners.Add(listener);
            spheros[index].Initialize(portName);

            spheros[index].spheroComm.subscribeToAllAsyncPackets(subscriberKey);

            commander = spheros[index];

            SpheroMessageLoop = new Thread(SpheroMessageLoopHandler);
            SpheroMessageLoop.Start();

            spheros[index].GetConfigurationBlock();

            spheros[index].SetCollisionDetection();

            SetBaseColor(index);
        }


        public void VibrationTimerElapsed(object state)
        {
            GamePad.SetVibration(Microsoft.Xna.Framework.PlayerIndex.One, 0.0f, 0.0f);
        }

        private void SpheroMessageLoopHandler()
        {
            Debug.WriteLine("Starting SpheroMessageLoopHandler.");
            while (true)
            {
                if (subscriberKey != null)
                {
                    var packets = commander.spheroComm.waitForAsyncPackets(subscriberKey);
                    foreach (var packet in packets)
                    {
                        if (packet.PacketType == UnsafePacket.PacketTypes.ASYNC_PACKET)
                        {
                            if (packet.ID_CODE == 0x07 ) { 
                                Debug.WriteLine("Collision detection packet detected");
                                GamePad.SetVibration(Microsoft.Xna.Framework.PlayerIndex.One, 1.0f, 1.0f);
                                Timer vibrationTimer = new Timer(new TimerCallback(VibrationTimerElapsed), null, 250, System.Threading.Timeout.Infinite);
                            }                    
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Would have logged a message, but subscriberKey was null.");
                }
            }
        }



        private Thread SpheroMessageLoop;

        private void Start_XNA_Loop()
        {
            XnaInputThread = new Thread(XnaInputHandler);
            XnaInputThread.Start();
        }

        private bool SpheroIsReadyForCommands(int index)
        {
            return spheros[index].IsConnected();
        }

        private void PopulateDropDownMenus()
        {
            var portnames = System.IO.Ports.SerialPort.GetPortNames();
            foreach (var portname in portnames) {PortNames.Items.Add(portname);}
            SpheroNames.Items.Add("One");
            SpheroNames.Items.Add("Two");
            SpheroNames.Items.Add("Three");
            SpheroNames.Items.Add("Four");
        }

        private void PopulateListOfSpheros() {
            for (int i = 0; i < 4; i++) { 
                spheros.Add(new SpheroCommander());
                spheros[i].spheroTrace.Switch.Level = SourceLevels.All;
            }
        }

        private void DebugNote(string error)
        {
            Debug.WriteLine(error);
        }

        private void SetBaseColor(int index)
        {
            switch (index)
            {
                case 0:
                    commander.baseRed = 0;
                    commander.baseGreen = 255;
                    commander.baseBlue = 0;
                    break;
                case 1:
                    commander.baseRed = 255;
                    commander.baseGreen = 0;
                    commander.baseBlue = 0;
                    break;
                case 2:
                    commander.baseRed = 0;
                    commander.baseGreen = 0;
                    commander.baseBlue = 255;
                    break;
                case 3:
                    commander.baseRed = 255;
                    commander.baseGreen = 255;
                    commander.baseBlue = 0;
                    break;
            }
        }

        private double breathIntensity(int tickCount)
        {
            
            double intensity = Math.PI * tickCount / 30.0f;
            intensity = Math.Abs(Math.Sin(intensity));
            return intensity;
        }

        private void StartPlaying(object sender, RoutedEventArgs e)
        {
            gameOn = true;
        }

        private void StopPlaying(object sender, RoutedEventArgs e)
        {
            gameOn = false;
            tickCount = 0;
            roundCount = 0;
            indexOfIt = 0;
            for (int i = 0; i < 4; i++) { if (spheros[i].IsConnected()) { spheros[i].RevertToBaseColor(); } } // Reset all connected spheros to their base color.
        }
    }
}

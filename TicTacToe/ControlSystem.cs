using Crestron.SimplSharp;                          	// For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro;                       	// For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;        	// For Threading
using Crestron.SimplSharpPro.DeviceSupport;         	// For Generic Device Support
using Crestron.SimplSharpPro.Diagnostics;		    	// For System Monitor Access
using Crestron.SimplSharpPro.UI;
using Independentsoft.Exchange.Autodiscover;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe
{
    public class ControlSystem : CrestronControlSystem
    {
        public FileStream fs;
        public XpanelForSmartGraphics myXpanel;
        public Game game;

        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;
                myXpanel = new XpanelForSmartGraphics(0x03, this);
                game = new Game(3, 3);
                game.panel = myXpanel;



                myXpanel.SigChange += MyXpanel_SigChange;
                myXpanel.OnlineStatusChange += MyXpanel_OnlineStatusChange; ;



                if (myXpanel.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                {
                    ErrorLog.Error("Error in registration of panel = {0}", myXpanel.RegistrationFailureReason);

                }
                else
                {

                }

                //Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(_ControllerEthernetEventHandler);
            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in the constructor: {0}", e.Message);
            }
        }

        public void MyXpanel_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            switch (args.DeviceOnLine)
            {
                case true:
                    ErrorLog.Notice("Xpanel online!");
                    break;

                case false:
                    ErrorLog.Error("Xpanel offline!");
                    break;

                default:
                    break;

            }
        }

        public void MyXpanel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            if (currentDevice == myXpanel)
            {
                switch (args.Sig.Type)
                {
                    case eSigType.NA:
                        break;
                    case eSigType.Bool:
                        {

                            // Button 1: Start new game

                            if (args.Sig.Number == 1 && args.Sig.BoolValue)
                            {
                                game.NewGame();



                            }
                            // Button 2: Reset all - Start new game and reset win totals

                            else if (args.Sig.Number == 2 && args.Sig.BoolValue)
                            {
                                game.ResetAll();

                            }



                            // Buttons 11-19: Gameboard space selection

                            else if (args.Sig.Number >= 11 && args.Sig.Number <= 19 && args.Sig.BoolValue)
                            {
                                int spaceNum = (int)args.Sig.Number - 11;

                                game.PlayerMove(spaceNum);

                            }
                            // Buttons 31-33: Difficulty selection 

                            else if (args.Sig.Number >= 31 && args.Sig.Number <= 33 && args.Sig.BoolValue)
                            {
                                game.SetDifficulty((int)args.Sig.Number - 30);

                            }


                            break;

                        }
                    case eSigType.UShort:
                        break;
                    case eSigType.String:
                        break;
                }
            }
        }


        public override void InitializeSystem()
        {
            try
            {
                game.SetDifficulty(3);
                game.ResetAll();

                string fileDirectory = Directory.GetApplicationRootDirectory();
                string fullPath = Path.Combine(fileDirectory, "tictactoe.txt");
                FileStream fs = new FileStream($"{fullPath}", FileMode.Create);
                using (StreamWriter sw = new StreamWriter(fs))
                    sw.WriteLine("Game Log File Created!");

            }
            catch (Exception e)
            {
                ErrorLog.Error("Error in InitializeSystem: {0}", e.Message);
            }
        }


        void _ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {//Determine the event type Link Up or Link Down
                case (eEthernetEventType.LinkDown):
                    //Next need to determine which adapter the event is for. 
                    //LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                        //
                    }
                    break;
                case (eEthernetEventType.LinkUp):
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {

                    }
                    break;
            }
        }

        // Utility function for sending exception messages to a string input on the
        // touchpanel for easy access
        public void PrintException(Exception e, uint join)
        {
            myXpanel.StringInput[join].StringValue = e.Message;
        }


        void _ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case (eProgramStatusEventType.Paused):
                    //The program has been paused.  Pause all user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Resumed):
                    //The program has been resumed. Resume all the user threads/timers as needed.
                    break;
                case (eProgramStatusEventType.Stopping):
                    //The program has been stopped.
                    //Close all threads. 
                    //Shutdown all Client/Servers in the system.
                    //General cleanup.
                    //Unsubscribe to all System Monitor events
                    break;
            }

        }



        void _ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case (eSystemEventType.DiskInserted):
                    //Removable media was detected on the system
                    break;
                case (eSystemEventType.DiskRemoved):
                    //Removable media was detached from the system
                    break;
                case (eSystemEventType.Rebooting):
                    //The system is rebooting. 
                    //Very limited time to preform clean up and save any settings to disk.
                    break;
            }

        }
    }
}
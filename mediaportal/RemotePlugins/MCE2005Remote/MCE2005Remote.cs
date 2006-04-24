#region Copyright (C) 2005-2006 Team MediaPortal

/* 
 *	Copyright (C) 2005-2006 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;
using MediaPortal.GUI.Library;
using MediaPortal.Hardware;

namespace MediaPortal.InputDevices
{
  /// <summary>
  /// MCE 2005 remote plugin
  /// </summary>
  public class MCE2005Remote
  {
    bool controlEnabled = false;  // MCE Remote enabled
    bool logVerbose = false;      // Verbose logging
    InputHandler inputHandler;    // Input Mapper


    /// <summary>
    /// Constructor
    /// </summary>
    public MCE2005Remote()
    {
    }


    /// <summary>
    /// Initialize MCE Remote
    /// </summary>
    /// <param name="hwnd">Window Handle</param>
    public void Init(IntPtr hwnd)
    {
      Init();
    }


    /// <summary>
    /// Initialize MCE Remote
    /// </summary>
    void Init()
    {
      using (MediaPortal.Profile.Settings xmlreader = new MediaPortal.Profile.Settings("MediaPortal.xml"))
      {
        controlEnabled = xmlreader.GetValueAsBool("remote", "MCE", true);
        logVerbose = xmlreader.GetValueAsBool("remote", "MCEVerboseLog", false);
      }
      if (!controlEnabled)
        return;

      if (logVerbose) Log.Write("MCE: Initializing MCE remote");

      try
      {
        Remote.LogVerbose = logVerbose;
        // Register Device
        Remote.Click += new RemoteEventHandler(OnRemoteClick);
        Remote.DeviceRemoval += new DeviceEventHandler(OnDeviceRemoval);
      }
      catch (Exception ex)
      {
        controlEnabled = false;
        Log.Write("MCE: {0} - support disabled until MP restart", ex.InnerException.Message);
        return;
      }

      // Kill ehtray.exe since that program catches the MCE remote keys and would start MCE 2005
      Process[] myProcesses;
      myProcesses = Process.GetProcesses();
      foreach (Process myProcess in myProcesses)
        if (myProcess.ProcessName.ToLower().Equals("ehtray"))
          try
          {
            Log.Write("MCE: Stopping Microsoft ehtray");
            myProcess.Kill();
          }
          catch (Exception)
          {
            Log.Write("MCE: Cannot stop Microsoft ehtray");
            DeInit();
            return;
          }

      Log.Write("MCE: MCE remote enabled");

      try
      {
        inputHandler = new InputHandler("Microsoft MCE");
      }
      catch (System.IO.FileNotFoundException)
      {
        Log.Write("MCE: can't find default mapping file - reinstall MediaPortal");
        DeInit();
        return;
      }
      catch (System.Xml.XmlException)
      {
        Log.Write("MCE: error in default mapping file - reinstall MediaPortal");
        DeInit();
        return;
      }
      catch (System.ApplicationException)
      {
        Log.Write("MCE: version mismatch in default mapping file - reinstall MediaPortal");
        DeInit();
        return;
      }
    }


    /// <summary>
    /// Remove all device handling
    /// </summary>
    public void DeInit()
    {
      if (controlEnabled)
      {
        if (logVerbose) Log.Write("MCE: Stopping MCE remote");
        Remote.Click -= new RemoteEventHandler(OnRemoteClick);
        inputHandler = null;
        controlEnabled = false;
      }
    }

    void OnDeviceRemoval(object sender, EventArgs e)
    {
      Remote.DeviceRemoval -= new DeviceEventHandler(OnDeviceRemoval);
      Remote.DeviceArrival += new DeviceEventHandler(OnDeviceArrival);
      Log.Write("MCE: MCE receiver has been unplugged");
      DeInit();
    }

    void OnDeviceArrival(object sender, EventArgs e)
    {
      Remote.DeviceArrival -= new DeviceEventHandler(OnDeviceArrival);
      Log.Write("MCE: MCE receiver detected");
      Init();
    }

    /// <summary>
    /// Let everybody know that this HID message may not be handled by anyone else
    /// </summary>
    /// <param name="msg">System.Windows.Forms.Message</param>
    /// <returns>Command handled</returns>
    public bool WndProc(Message msg)
    {
      return (controlEnabled && msg.Msg == 0x0319);
    }


    /// <summary>
    /// Evaluate button press from remote
    /// </summary>
    /// <param name="button">Remote Button</param>
    void OnRemoteClick(object sender, RemoteEventArgs e)
    //RemoteButton button)
    {
      RemoteButton button = e.Button;
      if (logVerbose) Log.Write("MCE: Incoming button command: {0}", button);

      // Set LastHidRequest, otherwise the HID handler (if enabled) would react on some remote buttons (double execution of command)
      switch (button)
      {
        case RemoteButton.Record:
          InputDevices.LastHidRequest = AppCommands.MediaRecord;
          break;
        case RemoteButton.Stop:
          InputDevices.LastHidRequest = AppCommands.MediaStop;
          break;
        case RemoteButton.Pause:
          InputDevices.LastHidRequest = AppCommands.MediaPause;
          break;
        case RemoteButton.Rewind:
          InputDevices.LastHidRequest = AppCommands.MediaRewind;
          break;
        case RemoteButton.Play:
          InputDevices.LastHidRequest = AppCommands.MediaPlay;
          break;
        case RemoteButton.Forward:
          InputDevices.LastHidRequest = AppCommands.MediaFastForward;
          break;
        case RemoteButton.Replay:
          InputDevices.LastHidRequest = AppCommands.MediaPreviousTrack;
          break;
        case RemoteButton.Skip:
          InputDevices.LastHidRequest = AppCommands.MediaNextTrack;
          break;
        case RemoteButton.Back:
          InputDevices.LastHidRequest = AppCommands.BrowserBackward;
          break;
        case RemoteButton.VolumeUp:
          InputDevices.LastHidRequest = AppCommands.VolumeUp;
          break;
        case RemoteButton.VolumeDown:
          InputDevices.LastHidRequest = AppCommands.VolumeDown;
          break;
        case RemoteButton.ChannelUp:
          InputDevices.LastHidRequest = AppCommands.MediaChannelUp;
          break;
        case RemoteButton.ChannelDown:
          InputDevices.LastHidRequest = AppCommands.MediaChannelDown;
          break;
        case RemoteButton.Mute:
          InputDevices.LastHidRequest = AppCommands.VolumeMute;
          break;
      }

      // Get & execute Mapping
      try
      {
        if (inputHandler.MapAction((int)button))
        {
          if (logVerbose) Log.Write("MCE: Command \"{0}\" mapped", button);
        }
        else if (logVerbose) Log.Write("MCE: Command \"{0}\" not mapped", button);
      }
      catch (System.ApplicationException ex)
      {
        if (ex.Message == "No button mapping found")
        {
          if (logVerbose) Log.Write("MCE: No button mapping found for button \"{0}\"", button);
        }
        else
          Log.Write("MCE: Button \"{0}\" threw exception: {1}", button, ex);
      }
      catch (Exception ex)
      {
        Log.Write("MCE: Button \"{0}\" threw exception: {1}", button, ex);
      }
    }

  }
}

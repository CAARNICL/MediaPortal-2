#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Windows.Forms;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Runtime;
using MediaPortal.UI;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Workflow;
#if !DEBUG
using MediaPortal.UI.Services.Logging;
using System.IO;
#endif
using MediaPortal.UI.Shares;
using CommandLine;
using MediaPortal.Core;
using MediaPortal.Core.PathManager;
using MediaPortal.Core.Services.Runtime;
using MediaPortal.Core.Logging;

[assembly: CLSCompliant(true)]

namespace MediaPortal
{
  internal static class ApplicationLauncher
  {
    /// <summary>
    /// The main entry point for the MP 2 client application.
    /// </summary>
    [STAThread]
    private static void Main(params string[] args)
    {
      System.Threading.Thread.CurrentThread.Name = "Main Thread";
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      // Parse Command Line options
      CommandLineOptions mpArgs = new CommandLineOptions();
      ICommandLineParser parser = new CommandLineParser(new CommandLineParserSettings(Console.Error));
      if (!parser.ParseArguments(args, mpArgs, Console.Out))
          Environment.Exit(1);

#if !DEBUG
      string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Team MediaPortal\MP2-Client\Log");
#endif

      using (new ServiceScope(true)) // Create the servicescope
      {
        SystemStateService systemStateService = new SystemStateService();
        ServiceScope.Add<ISystemStateService>(systemStateService);
        systemStateService.SwitchSystemState(SystemState.Initializing, false);

        try
        {
          ILogger logger = null;
          try
          {

            ApplicationCore.RegisterCoreServices(mpArgs.LogLevel, mpArgs.LogMethods, mpArgs.FlushLog);

            logger = ServiceScope.Get<ILogger>();

            IPathManager pathManager = ServiceScope.Get<IPathManager>();

            // Check if user wants to override the default Application Data location.
            if (!string.IsNullOrEmpty(mpArgs.DataDirectory))
              pathManager.SetPath("DATA", mpArgs.DataDirectory);

#if !DEBUG
            logPath = pathManager.GetPath("<LOG>");
#endif

            UiExtension.RegisterUiServices();
          }
          catch (Exception e)
          {
            if (logger != null)
              logger.Critical("Error starting application", e);
            systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
            ServiceScope.IsShuttingDown = true;

            UiExtension.DisposeUiServices();
            ApplicationCore.DisposeCoreServices();

            throw;
          }

          // Start the core
          logger.Debug("ApplicationLauncher: Starting application");

          try
          {
            IPluginManager pluginManager = ServiceScope.Get<IPluginManager>();
            pluginManager.Initialize();
            pluginManager.Startup(false);
            ApplicationCore.StartCoreServices();

            ISkinEngine skinEngine = ServiceScope.Get<ISkinEngine>();
            IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
            IMediaAccessor mediaAccessor = ServiceScope.Get<IMediaAccessor>();
            ILocalSharesManagement localSharesManagement = ServiceScope.Get<ILocalSharesManagement>();

            // We have to handle some dependencies here in the start order:
            // 1) After all plugins are loaded, the SkinEngine can initialize (=load all skin resources)
            // 2) After the skin resources are loaded, the workflow manager can initialize (=load its states and actions)
            // 3) After the workflow states and actions are loaded, the startup screen can be shown
            // 4) After the skinengine triggers the first workflow state/startup screen, the default shortcuts can be registered
            mediaAccessor.Initialize(); // Independent from other services
            localSharesManagement.Initialize(); // After media accessor was initialized
            skinEngine.Initialize(); // 1)
            workflowManager.Initialize(); // 2)
            skinEngine.Startup(); // 3)
            UiExtension.Startup();

            ApplicationCore.RegisterDefaultMediaItemAspectTypes(); // To be done after UI services are running

            systemStateService.SwitchSystemState(SystemState.Running, true);

            Application.Run();

            systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
            ServiceScope.IsShuttingDown = true; // Block ServiceScope from trying to load new services in shutdown phase

            // 1) Stop UI extensions (Releases all active players, must be done before shutting down SE)
            // 2) Shutdown SkinEngine (Closes all screens, uninstalls background manager, stops render thread)
            // 3) Shutdown WorkflowManager (Disposes all models)
            // 4) Shutdown PluginManager (Shuts down all plugins)
            // 5) Remove all services
            UiExtension.StopAll();
            skinEngine.Shutdown();
            workflowManager.Shutdown();
            pluginManager.Shutdown();
            mediaAccessor.Shutdown();
            localSharesManagement.Shutdown();
            ApplicationCore.StopCoreServices();
          }
          catch (Exception e)
          {
            logger.Critical("Error executing application", e);
            systemStateService.SwitchSystemState(SystemState.ShuttingDown, true);
            ServiceScope.IsShuttingDown = true;
          }
          finally
          {
            UiExtension.DisposeUiServices();
            ApplicationCore.DisposeCoreServices();

            systemStateService.SwitchSystemState(SystemState.Ending, false);
          }
        }
        catch (Exception ex)
        {
#if !DEBUG
          UiCrashLogger crash = new UiCrashLogger(logPath);
          crash.CreateLog(ex);
#endif
          systemStateService.SwitchSystemState(SystemState.Ending, false);
          Application.Exit();
        }
      }
    }
  }
}
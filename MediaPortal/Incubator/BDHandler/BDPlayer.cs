#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BDInfo;
using DirectShowLib;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.BDHandler.Settings;
using MediaPortal.UI.Players.Video.Tools;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UI.Players.Video
{
  /// <summary>
  /// BDPlayer implements a BluRay player based on the raw files. Currently there is no menu support available.
  /// </summary>
  public class BDPlayer : VideoPlayer, IDVDPlayer
  {
    #region Consts and delegates
       
    public const double MINIMAL_FULL_FEATURE_LENGTH = 3000;
    
    /// <summary>
    /// Delegate for starting a BDInfo thread.
    /// </summary>
    /// <param name="path">Path to scan</param>
    /// <returns>BDInfo</returns>
    delegate BDInfoExt ScanProcess(string path);

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a BDPlayer player object.
    /// </summary>
    public BDPlayer()
    {
      PlayerTitle = "BDPlayer"; // for logging
    }

    #endregion

    #region VideoPlayer overrides 

    protected override void CreateGraphBuilder()
    {
      base.CreateGraphBuilder();
      // configure EVR
      _streamCount = 2; // Allow Video and Subtitle
    }
    
    /// <summary>
    /// Adds the file source filter to the graph.
    /// </summary>
    protected override void AddFileSource()
    {
      string strFile = _resourceAccessor.LocalFileSystemPath;
      
      // Render the file
      strFile = Path.Combine(strFile.ToLower(), @"BDMV\index.bdmv");

      // only continue with playback if a feature was selected or the extension was m2ts.
      if (DoFeatureSelection(ref strFile))
      {
        // find the SourceFilter
        CodecInfo sourceFilter = ServiceRegistration.Get<ISettingsManager>().Load<BDPlayerSettings>().BDSourceFilter;

        // load the SourceFilter         
        if (TryAdd(sourceFilter))
        {
          IFileSourceFilter fileSourceFilter = FilterGraphTools.FindFilterByInterface<IFileSourceFilter>(_graphBuilder);
          // load the file
          int hr = fileSourceFilter.Load(strFile, null);
          DsError.ThrowExceptionForHR(hr);
        }
        else
        {
          BDPlayerBuilder.LogError("Unable to load DirectShowFilter: {0}", sourceFilter.Name);
          throw new Exception("Unable to load DirectShowFilter");
        }
      }
    }

    protected override void OnBeforeGraphRunning()
    {
      base.OnBeforeGraphRunning();

      IBaseFilter fileSourceFilter = FilterGraphTools.FindFilterByInterface<IFileSourceFilter>(_graphBuilder) as IBaseFilter;

      // first all automatically rendered pins
      FilterGraphTools.RenderOutputPins(_graphBuilder, fileSourceFilter);

      // MSDN: "During the connection process, the Filter Graph Manager ignores pins on intermediate filters if the pin name begins with a tilde (~)."
      // then connect the skipped "~" output pins
      FilterGraphTools.RenderAllManualConnectPins(_graphBuilder);

      AnalyseStreams();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Analyzes the current graph and extracts information about chapter markers and subtitle streams.
    /// </summary>
    /// <returns></returns>
    public bool AnalyseStreams()
    {
      BDPlayerBuilder.LogDebug("Analyzing streams to filter duplicates...");
      try
      {
        IAMExtendedSeeking pEs = FilterGraphTools.FindFilterByInterface<IAMExtendedSeeking>(_graphBuilder);
        if (pEs != null)
        {
          int markerCount;
          if (pEs.get_MarkerCount(out markerCount) == 0 && markerCount > 0)
          {
            _chapterTimestamps = new double[markerCount];
            _chapterNames = new string[markerCount];
            for (int i = 1; i <= markerCount; i++)
            {
              double markerTime;
              pEs.GetMarkerTime(i, out markerTime);
              _chapterTimestamps[i - 1] = markerTime;
              _chapterNames[i - 1] = GetChapterName(i);
            }
          }
        }
      }
      catch { }
      return true;
    }

    #endregion

    /// <summary>
    /// Scans a bluray folder and returns a BDInfo object
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static BDInfoExt ScanWorker(string path)
    {
      BDPlayerBuilder.LogInfo("Scanning bluray structure: {0}", path);
      BDInfoExt bluray = new BDInfoExt(path.ToUpper(), false); // We do not need all information here, only the playlists
      bluray.ScanPlaylists();
      return bluray;
    }

    /// <summary>
    /// Returns wether a choice was made and changes the file path
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>True if playback should continue, False if user cancelled.</returns>
    private bool DoFeatureSelection(ref string filePath)
    {
      try
      {
        ScanProcess scanner = ScanWorker;
        IAsyncResult result = scanner.BeginInvoke(filePath, null, scanner);

        while (result.IsCompleted == false)
          Thread.Sleep(100);

        BDInfoExt bluray = scanner.EndInvoke(result);
        List<TSPlaylistFile> allPlayLists = bluray.PlaylistFiles.Values.Where(p => p.IsValid).OrderByDescending(p => p.TotalLength).Distinct().ToList();

        // Feature selection logic 
        TSPlaylistFile listToPlay;
        if (allPlayLists.Count == 0)
        {
          BDPlayerBuilder.LogInfo("No playlists found, bypassing dialog.", allPlayLists.Count);
          return true;
        }
        if (allPlayLists.Count == 1)
        {
          // if we have only one playlist to show just move on
          BDPlayerBuilder.LogInfo("Found one valid playlist, bypassing dialog.", filePath);
          listToPlay = allPlayLists[0];
        }
        else
        {
          // Show selection dialog
          BDPlayerBuilder.LogInfo("Found {0} playlists, showing selection dialog.", allPlayLists.Count);

          // first make an educated guess about what the real features are (more than one chapter, no loops and longer than one hour)
          // todo: make a better filter on the playlists containing the real features
          List<TSPlaylistFile> playLists = allPlayLists.Where(p => (p.Chapters.Count > 1 || p.TotalLength >= MINIMAL_FULL_FEATURE_LENGTH) && !p.HasLoops).ToList();

          // if the filter yields zero results just list all playlists 
          if (playLists.Count == 0)
            playLists = allPlayLists;

          listToPlay = playLists[0];
        }

        GetChapters(listToPlay);

        // Combine the chosen file path (playlist)
        filePath = Path.Combine(bluray.DirectoryPLAYLIST.FullName, listToPlay.Name);

        return true;
      }
      catch (Exception e)
      {
        BDPlayerBuilder.LogError("Exception while reading bluray structure {0} {1}", e.Message, e.StackTrace);
        return true;
      }
    }

    private void GetChapters(TSPlaylistFile playlistFile)
    {
      if (playlistFile == null || playlistFile.Chapters == null)
        return;

      _chapterTimestamps = playlistFile.Chapters.ToArray();
      _chapterNames = new string[_chapterTimestamps.Length];
      for (int c = 0; c < _chapterNames.Length; c++)
        _chapterNames[c] = GetChapterName(c + 1);
    }

    #region ITitlePlayer Member

    private readonly string[] _emptyStringArray = new string[0];

    public override string[] Titles
    {
      get { return _emptyStringArray; }
    }

    public override void SetTitle(string title)
    { }

    public override string CurrentTitle
    {
      get { return null; }
    }

    #endregion

    #region IDVDPlayer Member

    public bool IsHandlingUserInput
    {
      get { return false; }
    }

    public void ShowDvdMenu()
    { }

    public void OnMouseMove(float x, float y)
    { }

    public void OnMouseClick(float x, float y)
    { }

    public void OnKeyPress(Control.InputManager.Key key)
    { }

    #endregion
  }
}
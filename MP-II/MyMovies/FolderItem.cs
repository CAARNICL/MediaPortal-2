﻿#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Collections;
using MediaPortal.Core.MediaManager;

namespace MyMovies
{
  public class FolderItem : ListItem
  {
    //private readonly IRootContainer _root;
    private readonly IRootContainer _folder;
    private readonly IMediaItem _mediaItem;

    public FolderItem() {}

    //public FolderItem(IRootContainer root)
    //{
    //  _root = root;
    //}

    public FolderItem(IRootContainer folder)
    {
      _folder = folder;
    }

    public FolderItem(IMediaItem mediaItem)
    {
      _mediaItem = mediaItem;
    }

    public IRootContainer MediaContainer
    {
      get { return _folder; }
    }

    public IRootContainer Root
    {
      get
      {
        if (_folder==null)
          return null;
        return _folder.Root;
      }
    }

    public IMediaItem MediaItem
    {
      get { return _mediaItem; }
    }
  }
}
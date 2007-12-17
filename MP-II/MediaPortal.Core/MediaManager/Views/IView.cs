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

using System.Collections.Generic;

namespace MediaPortal.Core.MediaManager.Views
{
  public interface IView 
  {
    /// <summary>
    /// Gets or sets the view type.
    /// </summary>
    /// <value>The view type.</value>
    string Type { get;set;}

    /// <summary>
    /// returns a list of all databases uses in this view.
    /// </summary>
    /// <value>The databases.</value>
    List<string> Databases { get; set; }

    /// <summary>
    /// Gets or sets the subviews.
    /// </summary>
    /// <value>The subviews.</value>
    List<IView> SubViews { get; set; }


    /// <summary>
    /// Gets or sets the query.
    /// </summary>
    /// <value>The query.</value>
    IQuery Query { get; set; }

    string MappingTable { get;set;}

    bool IsLastSubView { get;set;}

  }
}
#region Copyright (C) 2007-2008 Team MediaPortal

/*
 *  Copyright (C) 2007-2008 Team MediaPortal
 *  http://www.team-mediaportal.com
 *
 *  This file is part of MediaPortal II
 *
 *  MediaPortal II is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  MediaPortal II is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
 * 
 */

#endregion

using System.Collections.Generic;
using MediaPortal.Configuration;

using FormControl = System.Windows.Forms.Control;


namespace MediaPortal.Manager
{
  public class SectionDetails
  {

    #region Variables

    private ConfigBase _section;
    private IList<ConfigBase> _settings;
    private FormControl _control;
    private bool _rightToLeft;
    private int _width;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the specifications for the section.
    /// </summary>
    public ConfigBase Section
    {
      get { return _section; }
      set { _section = value; }
    }

    /// <summary>
    /// Gets or sets all settings inside the section.
    /// </summary>
    public IList<ConfigBase> Settings
    {
      get { return _settings; }
      set { _settings = value; }
    }

    /// <summary>
    /// Gets or sets all controls inside the section.
    /// </summary>
    public FormControl Control
    {
      get { return _control; }
      set { _control = value; }
    }

    /// <summary>
    /// Gets or sets if controls are built from right to left.
    /// </summary>
    public bool RightToLeft
    {
      get { return _rightToLeft; }
      set { _rightToLeft = value; }
    }

    /// <summary>
    /// Gets or sets the width the controls are built for.
    /// </summary>
    public int Width
    {
      get { return _width; }
      set { _width = value; }
    }

    #endregion

    public SectionDetails()
    {
      _section = new ConfigBase();
      _settings = new List<ConfigBase>();
      _control = new FormControl();
      _rightToLeft = false;
      _width = -1;
    }

  }
}

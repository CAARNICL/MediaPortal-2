#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

namespace MediaPortal.UI.SkinEngine.DirectX.RenderPipelines
{
  /// <summary>
  /// <see cref="IRenderPipeline"/> defines the basic steps to render a scene. Classes that implement this interface can control how the
  /// GUI will be rendered. This way either a single pass 2D rendering is possible, or multi-pass 3D rendering (i.e. Side-By-Side, Top-And-Bottom).
  /// </summary>
  public interface IRenderPipeline
  {
    /// <summary>
    /// Begins the rendering pass. All intialization of resources must be done in this step (i.e. allocation of custom render targets, clearing the device...)
    /// </summary>
    void BeginRenderPass();

    /// <summary>
    /// Performs the rendering. Depending on the desired output (2D or 3D) this method can run one or two render passes of the same scene.
    /// </summary>
    void Render();

    /// <summary>
    /// Ends the rendering pass. All de-initialization of resources must be done in this step (i.e. restoring render targets).
    /// </summary>
    void EndRenderPass();
  }
}
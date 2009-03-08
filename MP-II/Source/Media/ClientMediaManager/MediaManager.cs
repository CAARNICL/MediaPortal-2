#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Settings;
using MediaPortal.Media.ClientMediaManager.Views;

namespace MediaPortal.Media.ClientMediaManager
{
  /// <summary>
  /// The client's media manager class. It holds all media providers and metadata extractors and
  /// provides the concept of "views".
  /// </summary>
  public class MediaManager : MediaManagerBase, IImporter, ISharesManagement
  {
    #region Protected fields

    protected ViewCollectionView _rootView;

    protected LocalSharesManagement _localLocalSharesManagement;

    #endregion

    #region Ctor & initialization

    public MediaManager()
    {
      _localLocalSharesManagement = new LocalSharesManagement();

      ServiceScope.Get<ILogger>().Debug("MediaManager: Registering global SharesManagement service");
      ServiceScope.Add<ISharesManagement>(this);
    }

    public override void Initialize()
    {
      base.Initialize();
      ServiceScope.Get<ILogger>().Info("MediaManager: Startup");
      _localLocalSharesManagement.LoadSharesFromSettings();
      LoadViews();
    }

    #endregion

    #region Protected methods

    protected void InitializeDefaultViews()
    {
      ISharesManagement sharesManagement = ServiceScope.Get<ISharesManagement>();
      // Create root view
      // Hint: Localization resource for [Media.RootViewName] will be provided by the Media model
      ViewCollectionView vcv = new ViewCollectionView("[Media.RootViewName]", null);
      _rootView = vcv;

      // Create a local view for each share
      ICollection<ShareDescriptor> shares = sharesManagement.GetSharesBySystem(SystemName.GetLocalSystemName()).Values;
      foreach (ShareDescriptor share in shares)
      {
        ICollection<Guid> mediaItemAspectIds = new HashSet<Guid>();
        foreach (Guid metadataExtractorId in share.MetadataExtractorIds)
        {
          MetadataExtractorMetadata metadata = LocalMetadataExtractors[metadataExtractorId].Metadata;
          foreach (MediaItemAspectMetadata aspectMetadata in metadata.ExtractedAspectTypes)
            mediaItemAspectIds.Add(aspectMetadata.AspectId);
        }
        mediaItemAspectIds.Add(ProviderResourceAspect.ASPECT_ID);
        mediaItemAspectIds.Add(MediaAspect.ASPECT_ID);
        LocalShareView lsvm = new LocalShareView(share.ShareId, share.Name, string.Empty, _rootView, mediaItemAspectIds);
        vcv.AddSubView(lsvm);
      }
      // TODO: Create default database views
    }

    #endregion

    #region View access & management

    protected void LoadViews()
    {
      ViewsSettings settings = ServiceScope.Get<ISettingsManager>().Load<ViewsSettings>();
      _rootView = settings.RootView;
      if (_rootView == null)
      {
        // The views are still uninitialized - use defaults
        InitializeDefaultViews();
        SaveViews();
      }
      else
        _rootView.Loaded(null);
    }

    protected void SaveViews()
    {
      ViewsSettings settings = new ViewsSettings();
      settings.RootView = _rootView;
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

    /// <summary>
    /// Checks the provided <paramref name="view"/> recursively for <see cref="ViewCollectionView"/>s
    /// containing <see cref="LocalShareView"/>s based on the deleted share with the specified
    /// <paramref name="shareId"/>.
    /// </summary>
    /// <param name="view">View to check recursively.</param>
    /// <param name="shareId">Id of the deleted share.</param>
    protected void PropagateShareRemoval(View view, Guid shareId)
    {
      if (view == null || view.SubViews == null)
        return;
      ICollection<View> invalidViews = new List<View>();
      foreach (View subView in view.SubViews)
      {
        if (subView.IsBasedOnShare(shareId))
          invalidViews.Add(subView);
        else
          PropagateShareRemoval(subView, shareId);
      }
      ViewCollectionView vcv = view as ViewCollectionView;
      if (vcv != null && invalidViews.Count > 0)
      {
        vcv.Invalidate();
        foreach (View invalidView in invalidViews)
          vcv.RemoveSubView(invalidView);
        SaveViews();
      }
    }

    /// <summary>
    /// Returns the root view. The root view is the entrance point into the navigation hierarchy of media items.
    /// </summary>
    public View RootView
    {
      get { return _rootView; }
    }

    #endregion

    #region IImporter implementation

    public void ForceImport(Guid? shareId, string path)
    {
      // TODO
      throw new System.NotImplementedException();
    }

    #endregion

    #region ISharesManagement implementation

    public ShareDescriptor RegisterShare(SystemName nativeSystem, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds)
    {
      // TODO: When connected, assign result from the call of the method at the MP server's
      // ISharesManagement interface
      ShareDescriptor result = null;
      if (nativeSystem.IsLocalSystem())
        result = _localLocalSharesManagement.RegisterShare(nativeSystem, providerId, path,
            shareName, mediaCategories, metadataExtractorIds);
      return result;
    }

    public void RemoveShare(Guid shareId)
    {
      // TODO: When connected, also call the method at the MP server's ISharesManagement interface
      _localLocalSharesManagement.RemoveShare(shareId);
      PropagateShareRemoval(_rootView, shareId);
    }

    public ShareDescriptor UpdateShare(Guid shareId, SystemName nativeSystem, Guid providerId, string path,
        string shareName, IEnumerable<string> mediaCategories, IEnumerable<Guid> metadataExtractorIds,
        bool relocateMediaItems)
    {
      ShareDescriptor sd = _localLocalSharesManagement.UpdateShare(shareId, nativeSystem, providerId, path,
          shareName, mediaCategories, metadataExtractorIds, relocateMediaItems);
      // TODO: When connected, also call the method at the MP server's ISharesManagement interface
      return sd;
    }

    public IDictionary<Guid, ShareDescriptor> GetShares()
    {
      // TODO: When connected, call the method at the MP server's ISharesManagement interface instead of
      // calling it on the local shares management
      return _localLocalSharesManagement.GetShares();
    }

    public ShareDescriptor GetShare(Guid shareId)
    {
      ShareDescriptor result = _localLocalSharesManagement.GetShare(shareId);
      // TODO: When connected and result == null, call method at the MP server's ISharesManagement interface
      return result;
    }

    public IDictionary<Guid, ShareDescriptor> GetSharesBySystem(SystemName systemName)
    {
      if (systemName.IsLocalSystem())
        return _localLocalSharesManagement.GetSharesBySystem(systemName);
      else
        // TODO: When connected, call the method at the MP server's ISharesManagement interface and return
        // its results
        return new Dictionary<Guid, ShareDescriptor>();
    }

    public ICollection<SystemName> GetManagedClients()
    {
      // TODO: When connected, call the method at the MP server's ISharesManagement interface
      return _localLocalSharesManagement.GetManagedClients();
    }

    public IDictionary<Guid, MetadataExtractorMetadata> GetMetadataExtractorsBySystem(SystemName systemName)
    {
      if (systemName.IsLocalSystem())
        return _localLocalSharesManagement.GetMetadataExtractorsBySystem(SystemName.GetLocalSystemName());
      else
        // TODO: When connected, call the method at the MP server's ISharesManagement interface
        return new Dictionary<Guid, MetadataExtractorMetadata>();
    }

    #endregion
  }
}

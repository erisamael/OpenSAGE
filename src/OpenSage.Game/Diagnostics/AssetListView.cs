﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using OpenSage.Diagnostics.AssetViews;
using OpenSage.Diagnostics.Util;

namespace OpenSage.Diagnostics
{
    internal sealed class AssetListView : DiagnosticView
    {
        private readonly List<string> _audioFilenames;

        private readonly List<AssetListItem> _items;

        private readonly byte[] _searchTextBuffer;
        private string _searchText;

        private AssetListItem _currentItem;
        private AssetView _currentAssetView;

        public override string DisplayName { get; } = "Asset List";

        public override Vector2 DefaultSize { get; } = new Vector2(700, 400);

        public AssetListView(DiagnosticViewContext context)
            : base(context)
        {
            // TODO: This actually needs to use assets that have already been loaded.
            // And update when assets are loaded or unloaded.

            // TODO: Remove this.
            _audioFilenames = new List<string>();
            foreach (var entry in context.Game.ContentManager.FileSystem.Files)
            {
                switch (Path.GetExtension(entry.FilePath).ToLowerInvariant())
                {
                    case ".mp3":
                    case ".wav":
                        _audioFilenames.Add(entry.FilePath);
                        break;
                }
            }

            _items = new List<AssetListItem>();

            _searchTextBuffer = new byte[32];

            UpdateSearch(null);
        }

        private sealed class AssetListItem
        {
            public readonly string Name;
            public readonly Func<AssetView> CreateAssetView;

            public AssetListItem(string name, Func<AssetView> createAssetView)
            {
                Name = name;
                CreateAssetView = createAssetView;
            }
        }

        protected override void DrawOverride(ref bool isGameViewFocused)
        {
            ImGui.BeginChild("asset list sidebar", new Vector2(350, 0), true, 0);

            ImGui.PushItemWidth(-1);
            ImGuiUtility.InputText("##search", _searchTextBuffer, out var searchText);
            UpdateSearch(searchText);
            ImGui.PopItemWidth();

            ImGui.BeginChild("files list", Vector2.Zero, true);

            foreach (var item in _items)
            {
                if (ImGui.Selectable(item.Name, item == _currentItem))
                {
                    _currentItem = item;

                    RemoveAndDispose(ref _currentAssetView);

                    _currentAssetView = AddDisposable(item.CreateAssetView());
                }
                ImGuiUtility.DisplayTooltipOnHover(item.Name);
            }

            ImGui.EndChild();
            ImGui.EndChild();

            ImGui.SameLine();

            if (_currentItem != null)
            {
                ImGui.BeginChild("asset view");
                _currentAssetView.Draw();
                ImGui.EndChild();
            }
            else
            {
                ImGui.Text("Select a previewable asset.");
            }
        }

        private void UpdateSearch(string searchText)
        {
            searchText = ImGuiUtility.TrimToNullByte(searchText);

            if (searchText == _searchText)
            {
                return;
            }

            _searchText = searchText;

            _items.Clear();

            var isEmptySearch = string.IsNullOrWhiteSpace(_searchText);

            void AddItem(string assetName, Func<AssetView> createAssetView)
            {
                if (isEmptySearch || assetName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _items.Add(new AssetListItem(assetName, createAssetView));
                }
            }

            foreach (var asset in Game.ContentManager.CachedObjects)
            {
                var assetName = AssetView.GetAssetName(asset);
                if (assetName == null)
                {
                    continue;
                }

                AddItem(assetName, () => AssetView.CreateAssetView(Context, asset));
            }

            // TODO: Remove these, once audio assets are handled the same as other assets.
            foreach (var objectDefinition in Context.Game.ContentManager.IniDataContext.Objects)
            {
                AddItem($"GameObject:{objectDefinition.Name}", () => new GameObjectView(Context, objectDefinition));
            }
            foreach (var audioFilename in _audioFilenames)
            {
                AddItem($"Audio:{audioFilename}", () => new SoundView(Context, audioFilename));
            }
            foreach (var particleSystemDefinition in Context.Game.ContentManager.IniDataContext.ParticleSystems)
            {
                AddItem($"ParticleSystem:{particleSystemDefinition.Name}", () => new ParticleSystemView(Context, particleSystemDefinition.ToFXParticleSystemTemplate()));
            }
            foreach (var particleSystemTemplate in Context.Game.ContentManager.IniDataContext.FXParticleSystems)
            {
                AddItem($"FXParticleSystem:{particleSystemTemplate.Name}", () => new ParticleSystemView(Context, particleSystemTemplate));
            }
        }
    }
}

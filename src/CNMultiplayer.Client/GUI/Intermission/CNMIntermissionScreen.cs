using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace CNMultiplayer.Client.GUI.Intermission
{
    // copy from Full Invasion 3
    // IntermissionScreen用于解决投票界面显示的地图数目过少的问题
    [GameStateScreen(typeof(LobbyGameStateCustomGameClient))]
    [GameStateScreen(typeof(LobbyGameStateCommunityClient))]
    internal class CNMIntermissionScreen : ScreenBase, IGameStateListener, IGauntletChatLogHandlerScreen
    {
        public GauntletLayer Layer { get; private set; }

        public CNMIntermissionScreen(LobbyGameStateCustomGameClient gameState)
        {
            this.Construct();
        }

        public CNMIntermissionScreen(LobbyGameStateCommunityClient gameState)
        {
            this.Construct();
        }

        private void Construct()
        {
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
            this._customGameClientCategory = spriteData.SpriteCategories["ui_mpintermission"];
            this._customGameClientCategory.Load(resourceContext, uiresourceDepot);
            this._dataSource = new CNMIntermissionVM();
            this.Layer = new GauntletLayer(100, "GauntletLayer", false);
            this.Layer.IsFocusLayer = true;
            base.AddLayer(this.Layer);
            this.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
            this.Layer.LoadMovie("CNMIntermission", this._dataSource);
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            this._dataSource.Tick();
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            this._customGameClientCategory.Unload();
            this.Layer.InputRestrictions.ResetInputRestrictions();
            this.Layer = null;
            this._dataSource.OnFinalize();
            this._dataSource = null;
        }

        void IGameStateListener.OnActivate()
        {
            this.Layer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            ScreenManager.TrySetFocus(this.Layer);
            LoadingWindow.EnableGlobalLoadingWindow();
        }

        void IGameStateListener.OnDeactivate()
        {
        }

        void IGameStateListener.OnInitialize()
        {
        }

        void IGameStateListener.OnFinalize()
        {
        }

        void IGauntletChatLogHandlerScreen.TryUpdateChatLogLayerParameters(ref bool isTeamChatAvailable, ref bool inputEnabled, ref InputContext inputContext)
        {
            if (this.Layer != null)
            {
                isTeamChatAvailable = false;
                inputEnabled = true;
                inputContext = this.Layer.Input;
            }
        }

        private CNMIntermissionVM _dataSource;

        private SpriteCategory _customGameClientCategory;
    }
}

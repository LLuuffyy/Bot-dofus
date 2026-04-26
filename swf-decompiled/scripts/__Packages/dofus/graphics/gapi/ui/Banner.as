class dofus.graphics.gapi.ui.Banner extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _bIsMutant;
   var _bIsOnFocus;
   var _btnFights;
   var _btnFriends;
   var _btnGiveUp;
   var _btnGuild;
   var _btnHelp;
   var _btnInventory;
   var _btnMap;
   var _btnMount;
   var _btnNextTurn;
   var _btnOgrinas;
   var _btnPvP;
   var _btnQuests;
   var _btnSpells;
   var _btnStatsJob;
   var _btnTemporis;
   var _cChat;
   var _ccChrono;
   var _circleXtra;
   var _hHeart;
   var _lastKeyIsShortcut;
   var _lblFinalCountDown;
   var _mcBgTxtConsole;
   var _mcCircleXtraMask;
   var _mcCircleXtraMaskBig;
   var _mcCircleXtraPlacer;
   var _mcRightPanel;
   var _mcRightPanelPlacer;
   var _mcXtra;
   var _mcbMovableBar;
   var _msShortcuts;
   var _nAutoCompleteTimeout;
   var _oData;
   var _parent;
   var _pvAP;
   var _pvMP;
   var _sCurrentCircleXtra;
   var _txtConsole;
   var addToQueue;
   var attachMovie;
   var gapi;
   var getNextHighestDepth;
   var parent;
   var toggleDisplay;
   static var CHECK_MOUSE_POSITION_REFRESH_RATE = 250;
   static var CLASS_NAME = "Banner";
   var _nFightsCount = 0;
   var _bChatAutoFocus = true;
   var _sChatPrefix = "";
   var _bHeartMovedTop = false;
   var _bUseFlashChat = true;
   function Banner()
   {
      super();
   }
   function get currentOverItem()
   {
      return this._msShortcuts.currentOverItem;
   }
   function get useFlashChat()
   {
      return this._bUseFlashChat;
   }
   function get circleXtra()
   {
      return this._circleXtra;
   }
   function get chatPrefix()
   {
      return this._sChatPrefix;
   }
   function set data(oData)
   {
      this._oData = oData;
   }
   function get fightsCount()
   {
      return this._nFightsCount;
   }
   function set fightsCount(nFightsCount)
   {
      this._nFightsCount = nFightsCount;
      this.updateEye();
   }
   function get chatAutoFocus()
   {
      return this._bChatAutoFocus;
   }
   function set chatAutoFocus(bChatAutoFocus)
   {
      this._bChatAutoFocus = bChatAutoFocus;
   }
   function get txtConsole()
   {
      return this._txtConsole.text;
   }
   function set txtConsole(sText)
   {
      this._txtConsole.text = sText;
   }
   function get chat()
   {
      return this._cChat;
   }
   function get shortcuts()
   {
      return this._msShortcuts;
   }
   function get illustration()
   {
      return this._mcXtra;
   }
   function get illustrationType()
   {
      return this._sCurrentCircleXtra;
   }
   function updateEye()
   {
      if(this._btnFights.icon == "")
      {
         this._btnFights.icon = "Eye2";
      }
      var _loc2_ = this._nFightsCount != 0 && !this.api.datacenter.Game.isFight;
      this._btnFights._visible = _loc2_;
   }
   function setSelectable(bSelectable)
   {
      this._cChat.selectable = bSelectable;
   }
   function setChatAutoScroll(bAutoScroll)
   {
      this._cChat.isAutoScrollingEnabled = bAutoScroll;
   }
   function insertChat(sText)
   {
      if(this._bUseFlashChat)
      {
         this._txtConsole.text += sText;
      }
      else
      {
         this.api.electron.retroChatInsertPromptText(sText);
      }
   }
   function showNextTurnButton(bShow)
   {
      this._btnNextTurn._visible = bShow;
   }
   function showGiveUpButton(bShow)
   {
      if(bShow)
      {
         this._circleXtra.setXtraFightMask(true);
      }
      this._btnGiveUp._visible = bShow;
   }
   function moveHeart(bTop)
   {
      if(bTop)
      {
         if(!this._bHeartMovedTop)
         {
            this._hHeart._y -= 30;
         }
      }
      else if(this._bHeartMovedTop)
      {
         this._hHeart._y += 30;
      }
      this._bHeartMovedTop = bTop;
   }
   function showPoints(bShow)
   {
      this._pvAP._visible = bShow;
      this._pvMP._visible = bShow;
      this._cChat.showSitDown(!bShow);
      if(bShow)
      {
         this._oData.data.addEventListener("lpChanged",this);
         this._oData.data.addEventListener("apChanged",this);
         this._oData.data.addEventListener("mpChanged",this);
         this.apChanged({value:Math.max(0,this._oData.data.AP)});
         this.mpChanged({value:Math.max(0,this._oData.data.MP)});
      }
   }
   function startTimer(nDuration)
   {
      this.moveHeart(false);
      this._circleXtra.setXtraFightMask(true);
      this._ccChrono.startTimer(nDuration);
   }
   function redrawChrono()
   {
      this._ccChrono.redraw();
   }
   function stopTimer()
   {
      this._ccChrono.stopTimer();
   }
   function setChatText(sText)
   {
      this._cChat.setText(sText);
   }
   function showRightPanel(sPanelName, oParams, bSpecForce, bMouseSpriteRollover)
   {
      if(this.api.datacenter.Game.isSpectator && this._mcRightPanel.bMouseSpriteRollover == true)
      {
         return undefined;
      }
      if(this._mcRightPanel.className == sPanelName && !(this.api.datacenter.Game.isSpectator && bSpecForce == true))
      {
         return undefined;
      }
      var _loc6_;
      var _loc7_;
      if(!(this.api.datacenter.Game.isSpectator && bSpecForce != true))
      {
         _loc6_ = this.chat.fightSpectatorReplacementPanel;
         if(_loc6_ != undefined)
         {
            _loc6_.update(oParams.data);
         }
         else if(this.api.kernel.OptionsManager.getOption("SpriteInfos"))
         {
            if(this.chat.replacementPanelsManager.currentReplacementPanel == dofus.graphics.gapi.ui.chat.ChatReplacementPanelsManager.SHORTCUTS)
            {
               this.chat.shortcutsReplacementPanel.showMiniMap(false);
               this.chat.shortcutsReplacementPanel.updateSprite(oParams.data);
            }
            else
            {
               this.chat.useTemporaryReplacementPanel(dofus.graphics.gapi.ui.chat.ChatReplacementPanelsManager.FULL_WIDTH_FIGHTER_EFFECTS,[oParams.data]);
            }
         }
         if(this._mcRightPanel.className == sPanelName)
         {
            this._mcRightPanel.update(oParams.data);
         }
         else
         {
            if(this._mcRightPanel != undefined)
            {
               this.hideRightPanel(true);
            }
            oParams._x = this._mcRightPanelPlacer._x;
            oParams._y = this._mcRightPanelPlacer._y;
            _loc7_ = this.attachMovie(sPanelName,"_mcRightPanel",this.getNextHighestDepth(),oParams);
            _loc7_.swapDepths(this._mcRightPanelPlacer);
            _loc7_.parent = this;
            _loc7_.onRollOver = function()
            {
               this.parent.hideRightPanel(true);
            };
         }
         this._mcRightPanel.bMouseSpriteRollover = bMouseSpriteRollover;
      }
   }
   function hideRightPanel(bSpecForce, bMouseSpriteRollout)
   {
      if(bMouseSpriteRollout)
      {
         this._mcRightPanel.bMouseSpriteRollover = false;
      }
      if(this._mcRightPanel != undefined && !(this.api.datacenter.Game.isSpectator && bSpecForce != true))
      {
         this._mcRightPanel.swapDepths(this._mcRightPanelPlacer);
         this._mcRightPanel.removeMovieClip();
      }
   }
   function updateSmileysEmotes()
   {
      this._cChat.updateSmileysEmotes();
   }
   function showSmileysEmotesPanel(bShow)
   {
      if(bShow == undefined)
      {
         bShow = true;
      }
      this._cChat.hideSmileys(!bShow);
      this._cChat._btnSmileys.selected = bShow;
   }
   function updateLocalPlayer()
   {
      this._circleXtra.updateArtwork(false);
      this._bIsMutant = this._oData.isMutant && !this.api.datacenter.Player.isAuthorized;
      this._msShortcuts.meleeVisible = !this._oData.isMutant && this._msShortcuts.currentTab == dofus.graphics.gapi.controls.MouseShortcuts.TAB_SPELLS;
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.Banner.CLASS_NAME);
      this._circleXtra = new dofus.graphics.gapi.ui.banner.BannerCircleXtra(this.api,this);
   }
   function createChildren()
   {
      this._btnFights._visible = false;
      this.addToQueue({object:this,method:this.hideEpisodicContent});
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.showPoints(false);
      this.showNextTurnButton(false);
      this.showGiveUpButton(false);
      this._mcRightPanelPlacer._visible = false;
      this._mcCircleXtraPlacer._visible = false;
      this.api.ui.unloadUIComponent("FightOptionButtons");
      this.api.kernel.KeyManager.addShortcutsListener("onShortcut",this);
      this.api.kernel.KeyManager.addKeysListener("onKeys",this);
      this.api.kernel.SpeakingItemsManager.triggerEvent(dofus.managers.SpeakingItemsManager.SPEAK_TRIGGER_ON_CONNECT);
      this.api.network.Game.nLastMapIdReceived = -1;
      this._txtConsole.onSetFocus = function()
      {
         this._parent.onSetFocus();
      };
      this._txtConsole.onKillFocus = function()
      {
         this._parent.onKillFocus();
      };
      this._txtConsole.maxChars = dofus.Constants.MAX_MESSAGE_LENGTH + dofus.Constants.MAX_MESSAGE_LENGTH_MARGIN;
      ank.battlefield.Battlefield.useCacheAsBitmapOnStaticAnim = this.api.lang.getConfigText("USE_CACHEASBITMAP_ON_STATICANIM");
      var _loc2_ = this.api.datacenter.Basics.forceFlashChat || (!this.api.kernel.OptionsManager.getOption("EnableWidescreenPanels") || !this.api.electron.isShowingWidescreenPanel);
      this.addToQueue({object:this,method:this.configureUseFlashChat,params:[_loc2_]});
   }
   function linkMovableContainer()
   {
      var _loc2_ = this._mcbMovableBar.containers;
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < _loc2_.length)
      {
         _loc4_ = _loc2_[_loc3_];
         this._msShortcuts.setContainer(_loc3_ + 15,_loc4_);
         _loc4_.addEventListener("click",this._msShortcuts);
         _loc4_.addEventListener("dblClick",this._msShortcuts);
         _loc4_.addEventListener("over",this._msShortcuts);
         _loc4_.addEventListener("out",this._msShortcuts);
         _loc4_.addEventListener("drag",this._msShortcuts);
         _loc4_.addEventListener("drop",this._msShortcuts);
         _loc4_.addEventListener("onContentLoaded",this._msShortcuts);
         _loc4_.params = {position:_loc3_ + 15};
         _loc3_ += 1;
      }
   }
   function addListeners()
   {
      this._btnPvP.addEventListener("click",this);
      this._btnMount.addEventListener("click",this);
      this._btnGuild.addEventListener("click",this);
      this._btnStatsJob.addEventListener("click",this);
      this._btnSpells.addEventListener("click",this);
      this._btnInventory.addEventListener("click",this);
      this._btnQuests.addEventListener("click",this);
      this._btnMap.addEventListener("click",this);
      this._btnFriends.addEventListener("click",this);
      this._btnFights.addEventListener("click",this);
      this._btnHelp.addEventListener("click",this);
      this._cChat._btnHelpForPanel.addEventListener("click",this);
      this._btnTemporis.addEventListener("click",this);
      this._btnPvP.addEventListener("over",this);
      this._btnMount.addEventListener("over",this);
      this._btnGuild.addEventListener("over",this);
      this._btnStatsJob.addEventListener("over",this);
      this._btnSpells.addEventListener("over",this);
      this._btnInventory.addEventListener("over",this);
      this._btnQuests.addEventListener("over",this);
      this._btnMap.addEventListener("over",this);
      this._btnFriends.addEventListener("over",this);
      this._btnFights.addEventListener("over",this);
      this._btnHelp.addEventListener("over",this);
      this._cChat._btnHelpForPanel.addEventListener("over",this);
      this._btnTemporis.addEventListener("over",this);
      this._btnPvP.addEventListener("out",this);
      this._btnMount.addEventListener("out",this);
      this._btnGuild.addEventListener("out",this);
      this._btnStatsJob.addEventListener("out",this);
      this._btnSpells.addEventListener("out",this);
      this._btnInventory.addEventListener("out",this);
      this._btnQuests.addEventListener("out",this);
      this._btnMap.addEventListener("out",this);
      this._btnFriends.addEventListener("out",this);
      this._btnFights.addEventListener("out",this);
      this._btnHelp.addEventListener("out",this);
      this._cChat._btnHelpForPanel.addEventListener("out",this);
      this._btnTemporis.addEventListener("out",this);
      this._btnStatsJob.tabIndex = 0;
      this._btnSpells.tabIndex = 1;
      this._btnInventory.tabIndex = 2;
      this._btnQuests.tabIndex = 3;
      this._btnMap.tabIndex = 4;
      this._btnFriends.tabIndex = 5;
      this._btnGuild.tabIndex = 6;
      this._btnOgrinas.addEventListener("click",this);
      this._btnOgrinas.addEventListener("over",this);
      this._btnOgrinas.addEventListener("out",this);
      this._ccChrono.addEventListener("finalCountDown",this);
      this._ccChrono.addEventListener("beforeFinalCountDown",this);
      this._ccChrono.addEventListener("tictac",this);
      this._ccChrono.addEventListener("finish",this);
      this._cChat.addEventListener("filterChanged",this);
      this._cChat.addEventListener("selectSmiley",this);
      this._cChat.addEventListener("selectEmote",this);
      this._cChat.addEventListener("href",this);
      this._oData.addEventListener("lpChanged",this);
      this._oData.addEventListener("lpMaxChanged",this);
      this._btnNextTurn.addEventListener("click",this);
      this._btnNextTurn.addEventListener("over",this);
      this._btnNextTurn.addEventListener("out",this);
      this._btnGiveUp.addEventListener("click",this);
      this._btnGiveUp.addEventListener("over",this);
      this._btnGiveUp.addEventListener("out",this);
      this._oData.SpellsManager.addEventListener("spellLaunched",this);
      this._oData.SpellsManager.addEventListener("nextTurn",this);
      this._pvAP.addEventListener("over",this);
      this._pvAP.addEventListener("out",this);
      this._pvMP.addEventListener("over",this);
      this._pvMP.addEventListener("out",this);
      this._oData.Spells.addEventListener("modelChanged",this);
      this._oData.Inventory.addEventListener("modelChanged",this);
      this.api.datacenter.Player.addEventListener("huntMatchmakingStatusChanged",this);
      this._hHeart.onRollOver = function()
      {
         this._parent.over({target:this});
      };
      this._hHeart.onRollOut = function()
      {
         this._parent.out({target:this});
      };
      this._hHeart.onRelease = function()
      {
         this.toggleDisplay();
      };
      var banner = this;
      this._mcCircleXtraMaskBig.onRelease = function()
      {
         var _loc2_ = {};
         _loc2_.target = this;
         banner.click(_loc2_);
      };
   }
   function initData()
   {
      var _loc2_ = this.api.kernel.OptionsManager.getOption("BannerIllustrationMode");
      switch(_loc2_)
      {
         case "artwork":
            this._circleXtra.showCircleXtra("artwork",true,{bMask:true});
            break;
         case "clock":
            this._circleXtra.showCircleXtra("clock",true,{bMask:true});
            break;
         case "compass":
            this._circleXtra.showCircleXtra("compass",true,{bMask:true});
            break;
         case "helper":
            this._circleXtra.showCircleXtra("helper",true,{bMask:true});
            break;
         case "map":
            this._circleXtra.showCircleXtra("map",true,{bMask:true});
      }
      this.drawBar();
      this.lpMaxChanged({value:this._oData.LPmax});
      this.lpChanged({value:this._oData.LP});
      this.api.kernel.ChatManager.refresh();
      dofus.graphics.gapi.ui.banner.BannerGauge.setGaugeMode(this,this.api.kernel.OptionsManager.getOption("BannerGaugeMode"));
      this.updatePvPButtonState();
      if(this.api.kernel.OptionsManager.getOption("MovableBar"))
      {
         this.displayMovableBar(this.api.kernel.OptionsManager.getOption("MovableBar") && (!this.api.kernel.OptionsManager.getOption("HideSpellBar") || this.api.datacenter.Game.isFight));
      }
      this.setChatPrefix("",false);
   }
   function setChatFocus()
   {
      if(this._bUseFlashChat)
      {
         Selection.setFocus(this._txtConsole);
      }
      else
      {
         this.api.electron.focusWidescreenPanelIfPossible();
      }
   }
   function isChatFocus()
   {
      return this._bUseFlashChat && eval(Selection.getFocus())._name == "_txtConsole";
   }
   function placeCursorAtTheEnd()
   {
      if(!this._bUseFlashChat)
      {
         return undefined;
      }
      Selection.setFocus(this._txtConsole);
      Selection.setSelection(this._txtConsole.text.length,dofus.Constants.MAX_MESSAGE_LENGTH + dofus.Constants.MAX_MESSAGE_LENGTH_MARGIN);
   }
   function setChatFocusWithLastKey()
   {
      if(!this._bChatAutoFocus)
      {
         return undefined;
      }
      if(Selection.getFocus() != undefined)
      {
         return undefined;
      }
      this.setChatFocus();
      this.placeCursorAtTheEnd();
   }
   function setChatPrefix(sPrefix, bFocusTxtCommand)
   {
      if(bFocusTxtCommand == undefined)
      {
         bFocusTxtCommand = true;
      }
      if(sPrefix == "")
      {
         sPrefix = "/s";
      }
      this._sChatPrefix = sPrefix;
      if(sPrefix != "/s")
      {
         this._btnHelp.label = sPrefix;
         this._btnHelp.icon = "";
      }
      else
      {
         this._btnHelp.label = "";
         this._btnHelp.icon = "UI_BannerChatCommandAll";
      }
      this.api.electron.retroChatSetPrefix(sPrefix);
      if(bFocusTxtCommand)
      {
         this.addToQueue({object:this,method:this.placeCursorAtTheEnd});
      }
   }
   function getChatCommand()
   {
      var _loc2_ = this._txtConsole.text;
      if(_loc2_.charAt(0) == "/")
      {
         return _loc2_;
      }
      if(this._sChatPrefix != "")
      {
         return this._sChatPrefix + " " + _loc2_;
      }
      return _loc2_;
   }
   function hideEpisodicContent()
   {
      this._btnPvP._visible = this.api.datacenter.Basics.aks_current_regional_version >= 16;
      this._btnMount._visible = this.api.datacenter.Basics.aks_current_regional_version >= 20;
      if(this.api.datacenter.Basics.aks_current_server.isTemporis())
      {
         this._btnMount._visible = false;
         this._btnTemporis._visible = true;
      }
      else
      {
         this._btnTemporis._visible = false;
      }
      this._btnGuild._visible = !this.api.config.isStreaming;
      var _loc2_ = this._btnStatsJob._x;
      var _loc3_ = this._btnPvP._x;
      var _loc4_ = [];
      _loc4_.push(this._btnStatsJob);
      _loc4_.push(this._btnSpells);
      _loc4_.push(this._btnInventory);
      _loc4_.push(this._btnQuests);
      _loc4_.push(this._btnMap);
      _loc4_.push(this._btnFriends);
      if(this._btnGuild._visible)
      {
         _loc4_.push(this._btnGuild);
      }
      if(this._btnMount._visible)
      {
         _loc4_.push(this._btnMount);
      }
      if(this._btnPvP._visible)
      {
         _loc4_.push(this._btnPvP);
      }
      if(this._btnTemporis._visible)
      {
         _loc4_.push(this._btnTemporis);
      }
      var _loc5_ = (_loc3_ - _loc2_) / (_loc4_.length - 2);
      var _loc6_ = 0;
      while(_loc6_ < _loc4_.length)
      {
         _loc4_[_loc6_]._x = _loc6_ * _loc5_ + _loc2_;
         _loc6_ += 1;
      }
   }
   function displayChatHelp()
   {
      this.api.kernel.Console.process("/help");
      this._cChat.open(false);
   }
   function xpChanged()
   {
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
   }
   function energyChanged()
   {
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
   }
   function currentWeightChanged()
   {
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
   }
   function mountChanged()
   {
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
   }
   function currentJobChanged()
   {
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
   }
   function tempotonsChanged()
   {
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
   }
   function displayMovableBar(bShow)
   {
      if(bShow == undefined)
      {
         bShow = this._mcbMovableBar == undefined;
      }
      var _loc3_;
      var _loc4_;
      var _loc5_;
      if(bShow)
      {
         if(this._mcbMovableBar._name != undefined)
         {
            return undefined;
         }
         this._mcbMovableBar = dofus.graphics.gapi.ui.MovableContainerBar(this.api.ui.loadUIComponent("MovableContainerBar","MovableBar",[],{bAlwaysOnTop:true}));
         this._mcbMovableBar.addEventListener("drawBar",this);
         this._mcbMovableBar.addEventListener("drop",this);
         this._mcbMovableBar.addEventListener("dblClick",this);
         _loc3_ = {left:0,top:0,right:this.gapi.screenWidth,bottom:this.gapi.screenHeight};
         _loc4_ = this.api.kernel.OptionsManager.getOption("MovableBarSize");
         _loc5_ = this.api.kernel.OptionsManager.getOption("MovableBarCoord");
         _loc5_ = !_loc5_ ? {x:0,y:(this.gapi.screenHeight - this._mcbMovableBar._height) / 2} : _loc5_;
         this.addToQueue({object:this._mcbMovableBar,method:this._mcbMovableBar.setOptions,params:[16,20,_loc3_,_loc4_,_loc5_]});
      }
      else
      {
         this.api.ui.unloadUIComponent("MovableBar");
      }
   }
   function setMovableBarSize(nSize)
   {
      this._mcbMovableBar.size = nSize;
   }
   function chatInputHasText()
   {
      return this._txtConsole.text != undefined && this._txtConsole.text != "";
   }
   function onKeys(sKey)
   {
      if(this._lastKeyIsShortcut)
      {
         this._lastKeyIsShortcut = false;
         return undefined;
      }
      this.setChatFocusWithLastKey();
   }
   function onShortcut(sShortcut)
   {
      var _loc3_ = true;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      switch(sShortcut)
      {
         case "CTRL_STATE_CHANGED_ON":
            if(this._bIsOnFocus && !(this.api.config.isLinux || this.api.config.isMac))
            {
               fscommand("trapallkeys","false");
            }
            break;
         case "CTRL_STATE_CHANGED_OFF":
            if(this._bIsOnFocus && !(this.api.config.isLinux || this.api.config.isMac))
            {
               fscommand("trapallkeys","true");
            }
            break;
         case "ESCAPE":
            if(this.isChatFocus())
            {
               Selection.setFocus(null);
               _loc3_ = false;
            }
            break;
         case "ACCEPT_CURRENT_DIALOG":
            if(this.isChatFocus())
            {
               if(this._txtConsole.text.length == 0)
               {
                  if(this.api.electron.isShowingWidescreenPanel)
                  {
                     _loc3_ = false;
                     this.api.electron.focusWidescreenPanelIfPossible();
                  }
                  break;
               }
               this.api.kernel.Console.process(this.getChatCommand(),this.api.datacenter.Basics.chatParams);
               this.api.datacenter.Basics.chatParams = {};
               if(this._txtConsole.text != undefined)
               {
                  this._txtConsole.text = "";
                  this.api.electron.retroChatSetPromptText("");
               }
               _loc3_ = false;
               break;
            }
            if(this._bChatAutoFocus)
            {
               _loc4_ = dofus.graphics.gapi.ui.Debug(this.gapi.getUIComponent("Debug"));
               if(Selection.getFocus() != undefined && !(_loc4_ != undefined && (_loc4_.isFocused() && !_loc4_.commandInputHasText())))
               {
                  break;
               }
               _loc3_ = false;
               this.setChatFocus();
            }
            break;
         case "TEAM_MESSAGE":
            if(this.isChatFocus())
            {
               if(this._txtConsole.text.length != 0)
               {
                  _loc5_ = this.getChatCommand();
                  if(_loc5_.charAt(0) == "/")
                  {
                     _loc5_ = _loc5_.substr(_loc5_.indexOf(" ") + 1);
                  }
                  this.api.kernel.Console.process("/t " + _loc5_,this.api.datacenter.Basics.chatParams);
                  this.api.datacenter.Basics.chatParams = {};
                  if(this._txtConsole.text != undefined)
                  {
                     this._txtConsole.text = "";
                     this.api.electron.retroChatSetPromptText("");
                  }
                  _loc3_ = false;
               }
               break;
            }
            if(Selection.getFocus() == undefined && this._bChatAutoFocus)
            {
               _loc3_ = false;
               this.setChatFocus();
            }
            break;
         case "GUILD_MESSAGE":
            if(this.isChatFocus())
            {
               if(this._txtConsole.text.length != 0)
               {
                  _loc6_ = this.getChatCommand();
                  if(_loc6_.charAt(0) == "/")
                  {
                     _loc6_ = _loc6_.substr(_loc6_.indexOf(" ") + 1);
                  }
                  this.api.kernel.Console.process("/g " + _loc6_,this.api.datacenter.Basics.chatParams);
                  this.api.datacenter.Basics.chatParams = {};
                  if(this._txtConsole.text != undefined)
                  {
                     this._txtConsole.text = "";
                     this.api.electron.retroChatSetPromptText("");
                  }
                  _loc3_ = false;
               }
               break;
            }
            if(Selection.getFocus() == undefined && this._bChatAutoFocus)
            {
               _loc3_ = false;
               this.setChatFocus();
            }
            break;
         case "WHISPER_HISTORY_UP":
            if(this.isChatFocus())
            {
               this.doChatWhisperHistoryUp();
               _loc3_ = false;
            }
            break;
         case "WHISPER_HISTORY_DOWN":
            if(this.isChatFocus())
            {
               this.doChatWhisperHistoryDown();
               _loc3_ = false;
            }
            break;
         case "HISTORY_UP":
            if(this.isChatFocus())
            {
               this.doChatHistoryUp();
               _loc3_ = false;
            }
            break;
         case "HISTORY_DOWN":
            if(this.isChatFocus())
            {
               this.doChatHistoryDown();
               _loc3_ = false;
            }
            break;
         case "AUTOCOMPLETE":
            if(this.isChatFocus())
            {
               this.askShowAutoCompleteResult();
               _loc3_ = false;
            }
            break;
         case "NEXTTURN":
            if(!this.isChatFocus() && this.api.datacenter.Game.isFight)
            {
               if(!this.api.datacenter.Game.isRunning)
               {
                  _loc7_ = dofus.graphics.gapi.ui.ChallengeMenu(this.gapi.getUIComponent("ChallengeMenu"));
                  if(_loc7_ != undefined)
                  {
                     _loc7_.sendReadyState();
                  }
               }
               else
               {
                  this.api.network.Game.prepareTurnEnd();
               }
               _loc3_ = false;
            }
            break;
         case "MAXI":
            if(this._bUseFlashChat)
            {
               this._cChat.open(false);
               _loc3_ = false;
            }
            break;
         case "MINI":
            if(this._bUseFlashChat)
            {
               this._cChat.open(true);
               _loc3_ = false;
            }
            break;
         case "CHARAC":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnStatsJob});
               _loc3_ = false;
            }
            break;
         case "SPELLS":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnSpells});
               _loc3_ = false;
            }
            break;
         case "INVENTORY":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnInventory});
               _loc3_ = false;
            }
            break;
         case "QUESTS":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnQuests});
               _loc3_ = false;
            }
            break;
         case "MAP":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnMap});
               _loc3_ = false;
            }
            break;
         case "FRIENDS":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnFriends});
               _loc3_ = false;
            }
            break;
         case "GUILD":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnGuild});
               _loc3_ = false;
            }
            break;
         case "GUILD_TAX_COLLECTOR":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts") && this.api.datacenter.Player.guildInfos.isValid)
            {
               this.click({target:this._btnGuild,currentTab:"TaxCollectors"});
               _loc3_ = false;
               break;
            }
            this.api.kernel.showMessage(undefined,this.api.lang.getText("NOT_ENOUGHT_MEMBERS_IN_GUILD"),"ERROR_CHAT");
            break;
         case "CONQUEST_AND_HUNT":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.click({target:this._btnPvP});
               _loc3_ = false;
            }
            break;
         case "MOUNT":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts") && !this.api.datacenter.Basics.aks_current_server.isTemporis())
            {
               this.click({target:this._btnMount});
               _loc3_ = false;
            }
            break;
         case "MOUNT_INVENTORY":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts"))
            {
               this.api.network.Exchange.request(15);
               _loc3_ = false;
            }
            break;
         case "TEMPORIS":
            if(this.api.kernel.OptionsManager.getOption("BannerShortcuts") && this.api.datacenter.Basics.aks_current_server.isTemporis())
            {
               this.click({target:this._btnTemporis});
               _loc3_ = false;
            }
      }
      this._lastKeyIsShortcut = _loc3_;
      return _loc3_;
   }
   function askShowAutoCompleteResult()
   {
      Selection.setFocus(null);
      if(this._nAutoCompleteTimeout != undefined)
      {
         _global.clearTimeout(this._nAutoCompleteTimeout);
      }
      var _loc3_ = _global.setTimeout(this,"doAutoComplete",100);
      this._nAutoCompleteTimeout = _loc3_;
   }
   function doChatWhisperHistoryUp()
   {
      this._txtConsole.text = this.api.kernel.Console.getWhisperHistoryUp();
      this.addToQueue({object:this,method:this.placeCursorAtTheEnd});
      this.api.electron.retroChatSetPromptText(this._txtConsole.text);
   }
   function doChatWhisperHistoryDown()
   {
      this._txtConsole.text = this.api.kernel.Console.getWhisperHistoryDown();
      this.addToQueue({object:this,method:this.placeCursorAtTheEnd});
      this.api.electron.retroChatSetPromptText(this._txtConsole.text);
   }
   function doChatHistoryUp()
   {
      var _loc2_ = this.api.kernel.Console.getHistoryUp();
      if(_loc2_ != undefined)
      {
         this.api.datacenter.Basics.chatParams = _loc2_.params;
         this._txtConsole.text = _loc2_.value;
      }
      this.addToQueue({object:this,method:this.placeCursorAtTheEnd});
      this.api.electron.retroChatSetPromptText(this._txtConsole.text);
   }
   function doChatHistoryDown()
   {
      var _loc2_ = this.api.kernel.Console.getHistoryDown();
      if(_loc2_ != undefined)
      {
         this.api.datacenter.Basics.chatParams = _loc2_.params;
         this._txtConsole.text = _loc2_.value;
      }
      else
      {
         this._txtConsole.text = "";
      }
      this.addToQueue({object:this,method:this.placeCursorAtTheEnd});
      this.api.electron.retroChatSetPromptText(this._txtConsole.text);
   }
   function doAutoComplete()
   {
      var _loc2_ = [];
      var _loc3_ = this.api.datacenter.Sprites.getItems();
      for(var _loc4_ in _loc3_)
      {
         if(_loc3_[_loc4_] instanceof dofus.datacenter.Character)
         {
            _loc2_.push(_loc3_[_loc4_].name);
         }
      }
      var _loc5_ = this.api.kernel.Console.autoCompletion(_loc2_,this._txtConsole.text);
      if(!_loc5_.isFull)
      {
         if(_loc5_.list == undefined || _loc5_.list.length == 0)
         {
            this.api.sounds.events.onError();
         }
         else
         {
            this.api.ui.showTooltip(_loc5_.list.sort().join(", "),0,520);
         }
      }
      this._txtConsole.text = _loc5_.result + (!_loc5_.isFull ? "" : " ");
      this.api.electron.retroChatSetPromptText(this._txtConsole.text);
      this.placeCursorAtTheEnd();
   }
   function click(oEvent)
   {
      this.api.kernel.GameManager.signalFightActivity();
      var _loc4_;
      var _loc5_;
      switch(oEvent.target._name)
      {
         case "_btnPvP":
            this.api.sounds.events.onBannerRoundButtonClick();
            if(this.api.datacenter.Player.data.alignment.index == 0)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("NEED_ALIGNMENT"),"ERROR_CHAT");
            }
            else
            {
               this.showSmileysEmotesPanel(false);
               this.gapi.loadUIAutoHideComponent("Conquest","Conquest",{currentTab:"Stats"});
            }
            return;
         case "_btnMount":
            this.api.sounds.events.onBannerRoundButtonClick();
            if(this._bIsMutant)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_U_ARE_MUTANT"),"ERROR_CHAT");
               return undefined;
            }
            if(this._oData.mount != undefined)
            {
               this.showSmileysEmotesPanel(false);
               if(this.gapi.getUIComponent("MountAncestorsViewer") != undefined)
               {
                  this.gapi.unloadUIComponent("MountAncestorsViewer");
                  this.gapi.unloadUIComponent("Mount");
               }
               else
               {
                  this.gapi.loadUIAutoHideComponent("Mount","Mount");
               }
            }
            else
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("UI_ONLY_FOR_MOUNT"),"ERROR_CHAT");
            }
            return;
            break;
         case "_btnGuild":
            this.api.sounds.events.onBannerRoundButtonClick();
            if(this._bIsMutant)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_U_ARE_MUTANT"),"ERROR_CHAT");
               return undefined;
            }
            if(this._oData.guildInfos != undefined)
            {
               _loc4_ = oEvent.currentTab == undefined ? "Members" : oEvent.currentTab;
               this.showSmileysEmotesPanel(false);
               this.gapi.loadUIAutoHideComponent("Guild","Guild",{currentTab:_loc4_});
            }
            else
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("UI_ONLY_FOR_GUILD"),"ERROR_CHAT");
            }
            return;
            break;
         case "_btnStatsJob":
            this.api.sounds.events.onBannerRoundButtonClick();
            if(this._bIsMutant)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_U_ARE_MUTANT"),"ERROR_CHAT");
               return undefined;
            }
            this.showSmileysEmotesPanel(false);
            this.gapi.loadUIAutoHideComponent("StatsJob","StatsJob");
            return;
            break;
         case "_btnSpells":
            this.api.sounds.events.onBannerRoundButtonClick();
            if(this._bIsMutant)
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("CANT_U_ARE_MUTANT"),"ERROR_CHAT");
               return undefined;
            }
            this.showSmileysEmotesPanel(false);
            this.gapi.loadUIAutoHideComponent("SpellsCollection","SpellsCollection");
            return;
            break;
         case "_btnTemporis":
            this.api.sounds.events.onBannerRoundButtonClick();
            if(!this.api.datacenter.Basics.aks_current_server.isTemporis())
            {
               this.api.kernel.showMessage(undefined,this.api.lang.getText("ERROR_226"),"ERROR_CHAT");
               return undefined;
            }
            this.showSmileysEmotesPanel(false);
            this.gapi.loadUIAutoHideComponent("Temporis","Temporis");
            return;
            break;
         case "_btnInventory":
            this.api.sounds.events.onBannerRoundButtonClick();
            this.showSmileysEmotesPanel(false);
            this.gapi.loadUIAutoHideComponent("Inventory","Inventory");
            return;
         case "_btnQuests":
            this.api.sounds.events.onBannerRoundButtonClick();
            this.showSmileysEmotesPanel(false);
            this.gapi.loadUIAutoHideComponent("Quests","Quests");
            return;
         case "_btnMap":
            this.api.sounds.events.onBannerRoundButtonClick();
            this.showSmileysEmotesPanel(false);
            this.gapi.loadUIAutoHideComponent("MapExplorer","MapExplorer",undefined,{nHideSprites:1});
            return;
         case "_btnFriends":
            this.api.sounds.events.onBannerRoundButtonClick();
            this.showSmileysEmotesPanel(false);
            this.gapi.loadUIAutoHideComponent("Friends","Friends");
            return;
         case "_btnFights":
            if(!this.api.datacenter.Game.isFight)
            {
               this.gapi.loadUIComponent("FightsInfos","FightsInfos",null,{bAlwaysOnTop:true});
            }
            return;
         case "_btnOgrinas":
            this.api.sounds.events.onBannerRoundButtonClick();
            this.showSmileysEmotesPanel(false);
            this.api.network.send("!CSO");
            return;
         case "_btnHelp":
            this.openChatPrefixMenu();
            return;
         case "_btnHelpForPanel":
            this.openChatPrefixMenu();
            return;
         case "_btnNextTurn":
            if(this.api.datacenter.Game.isFight)
            {
               this.api.network.Game.prepareTurnEnd();
            }
            return;
         case "_btnGiveUp":
            if(this.api.datacenter.Game.isFight)
            {
               if(this.api.datacenter.Game.isSpectator)
               {
                  this.api.network.Game.leave();
               }
               else
               {
                  this.api.kernel.GameManager.giveUpGame();
               }
            }
            return;
         case "_mcXtra":
         case "_mcCircleXtraMaskBig":
            if(!this.api.datacenter.Player.isAuthorized || this.api.datacenter.Player.isAuthorized && Key.isDown(Key.SHIFT))
            {
               if(this._sCurrentCircleXtra == "helper" && dofus.managers.TipsManager.getInstance().hasNewTips())
               {
                  dofus.managers.TipsManager.getInstance().displayNextTips();
               }
               else
               {
                  _loc5_ = this.api.ui.createPopupMenu();
                  _loc5_.addItem(this.api.lang.getText("SHOW") + " >>",dofus.graphics.gapi.ui.banner.BannerGauge,dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeModeSelectMenu,[this]);
                  if(this._sCurrentCircleXtra == "helper")
                  {
                     _loc5_.addStaticItem(this.api.lang.getText("HELP_ME"));
                     _loc5_.addItem(this.api.lang.getText("KB_TITLE"),this.api.ui,this.api.ui.loadUIComponent,["KnownledgeBase","KnownledgeBase"],true);
                     _loc5_.addStaticItem(this.api.lang.getText("OTHER_DISPLAY_OPTIONS"));
                  }
                  _loc5_.addItem(this.api.lang.getText("BANNER_ARTWORK"),this._circleXtra,this._circleXtra.showCircleXtra,["artwork",true,{bMask:true,bUpdateGauge:true}],this._sCurrentCircleXtra != "artwork");
                  _loc5_.addItem(this.api.lang.getText("BANNER_CLOCK"),this._circleXtra,this._circleXtra.showCircleXtra,["clock",true,{bMask:true,bUpdateGauge:true}],this._sCurrentCircleXtra != "clock");
                  _loc5_.addItem(this.api.lang.getText("BANNER_COMPASS"),this._circleXtra,this._circleXtra.showCircleXtra,["compass",true,{bUpdateGauge:true}],this._sCurrentCircleXtra != "compass");
                  _loc5_.addItem(this.api.lang.getText("BANNER_HELPER"),this._circleXtra,this._circleXtra.showCircleXtra,["helper",true,{bUpdateGauge:true}],this._sCurrentCircleXtra != "helper");
                  _loc5_.addItem(this.api.lang.getText("BANNER_MAP"),this._circleXtra,this._circleXtra.showCircleXtra,["map",true,{bMask:true,bUpdateGauge:true}],this._sCurrentCircleXtra != "map");
                  _loc5_.show(_root._xmouse,_root._ymouse,true);
               }
               break;
            }
            this.api.kernel.GameManager.showPlayerPopupMenu(undefined,{sPlayerName:this.api.datacenter.Player.Name,sPlayerID:this.api.datacenter.Player.ID});
      }
      return undefined;
   }
   function openChatPrefixMenu(oCustomPopupPosition)
   {
      var _loc3_ = this.api.lang.getConfigText("CHAT_FILTERS");
      var _loc4_ = this.api.ui.createPopupMenu();
      _loc4_.addStaticItem(this.api.lang.getText("CHAT_PREFIX"));
      _loc4_.addItem(this.api.lang.getText("DEFAUT") + " (/s)",this,this.setChatPrefix,["/s"]);
      _loc4_.addItem(this.api.lang.getText("TEAM") + " (/t)",this,this.setChatPrefix,["/t"],this.api.datacenter.Game.isFight);
      _loc4_.addItem(this.api.lang.getText("PARTY") + " (/p)",this,this.setChatPrefix,["/p"],this.api.ui.getUIComponent("Party") != undefined);
      _loc4_.addItem(this.api.lang.getText("GUILD") + " (/g)",this,this.setChatPrefix,["/g"],this.api.datacenter.Player.guildInfos != undefined);
      if(_loc3_[4])
      {
         _loc4_.addItem(this.api.lang.getText("ALIGNMENT") + " (/a)",this,this.setChatPrefix,["/a"],this.api.datacenter.Player.alignment.index != 0);
      }
      if(_loc3_[5])
      {
         _loc4_.addItem(this.api.lang.getText("RECRUITMENT") + " (/r)",this,this.setChatPrefix,["/r"]);
      }
      if(_loc3_[6])
      {
         _loc4_.addItem(this.api.lang.getText("TRADE") + " (/b)",this,this.setChatPrefix,["/b"]);
      }
      if(_loc3_[7])
      {
         _loc4_.addItem("Hystoria" + " (/i)",this,this.setChatPrefix,["/i"]);
      }
      if(this.api.datacenter.Player.isAuthorized)
      {
         _loc4_.addItem(this.api.lang.getText("PRIVATE_CHANNEL") + " (/q)",this,this.setChatPrefix,["/q"]);
      }
      _loc4_.addItem(this.api.lang.getText("HELP"),this,this.displayChatHelp,[]);
      if(this.api.electron.enabled)
      {
         _loc4_.addItem(this.api.lang.getText("OPEN_EXTERNAL_CHAT"),dofus.Electron,dofus.Electron.retroChatOpen,[]);
      }
      if(oCustomPopupPosition != undefined)
      {
         _loc4_.show(oCustomPopupPosition.x,oCustomPopupPosition.y,true);
      }
      else
      {
         _loc4_.show(this._btnHelp._x,this._btnHelp._y,true);
      }
   }
   function dblClick(oEvent)
   {
      if(oEvent.target == this._mcbMovableBar)
      {
         this._mcbMovableBar.size = this._mcbMovableBar.size != 0 ? 0 : this.api.kernel.OptionsManager.getOption("MovableBarSize");
         return undefined;
      }
   }
   function createSpellActionPopupMenu(oSpell)
   {
      var _loc4_ = this.api.ui.createPopupMenu();
      _loc4_.addItem("Retirer ce raccourci",this.api.network.Spells,this.api.network.Spells.spellRemove,[oSpell.position],!oSpell.isUndeletable);
      _loc4_.show(_root._xmouse,_root._ymouse,true);
   }
   function createInventoryShortcutItemActionPopupMenu(oInventoryShortcut)
   {
      var _loc4_ = this.api.ui.createPopupMenu();
      _loc4_.addItem("Retirer ce raccourci",this.api.network.InventoryShortcuts,this.api.network.InventoryShortcuts.sendInventoryShortcutRemove,[oInventoryShortcut.position]);
      _loc4_.show(_root._xmouse,_root._ymouse,true);
   }
   function beforeFinalCountDown(oEvent)
   {
      this.api.kernel.TipsManager.showNewTip(dofus.managers.TipsManager.TIP_FINAL_COUNTDOWN);
   }
   function finalCountDown(oEvent)
   {
      this._mcXtra._visible = false;
      this._lblFinalCountDown.text = oEvent.value;
   }
   function tictac(oEvent)
   {
      this.api.sounds.events.onBannerTimer();
   }
   function finish(oEvent)
   {
      this._mcXtra._visible = true;
      if(this._lblFinalCountDown.text != undefined)
      {
         this._lblFinalCountDown.text = "";
      }
   }
   function complete(oEvent)
   {
      var _loc3_ = this.api.kernel.OptionsManager.getOption("BannerIllustrationMode");
      if(oEvent.target.contentPath.indexOf("artworks") != -1 && _loc3_ == "helper")
      {
         this._circleXtra.showCircleXtra("helper",true,{bMask:true});
      }
      else
      {
         this.api.colors.addSprite(this._mcXtra.content,this._oData);
      }
   }
   function over(oEvent)
   {
      if(!this.gapi.isCursorHidden())
      {
         return undefined;
      }
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      switch(oEvent.target._name)
      {
         case "_btnHelp":
         case "_btnHelpForPanel":
            this.gapi.showTooltip(this.api.lang.getText("CHAT_MENU"),oEvent.target,-20,{bXLimit:false,bYLimit:false});
            return;
         case "_btnGiveUp":
            if(this.api.datacenter.Game.isSpectator)
            {
               _loc3_ = this.api.lang.getText("GIVE_UP_SPECTATOR");
            }
            else if(this.api.datacenter.Game.fightType == dofus.managers.GameManager.FIGHT_TYPE_CHALLENGE || !this.api.datacenter.Basics.aks_current_server.isHardcore())
            {
               _loc3_ = this.api.lang.getText("GIVE_UP");
            }
            else
            {
               _loc3_ = this.api.lang.getText("SUICIDE");
            }
            this.gapi.showTooltip(_loc3_,oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnPvP":
            _loc4_ = this.api.lang.getText("CONQUEST_WORD") + " " + this.api.lang.getText("AND") + " " + this.api.lang.getText("HUNT");
            _loc5_ = -20;
            _loc6_ = this.api.datacenter.Player.huntMatchmakingStatus;
            if(_loc6_ != undefined && _loc6_.currentStatus == "WAITING_FOR_START_CONFIRMATION")
            {
               _loc4_ += "\n\n" + this.api.lang.getText("HUNT_LOOKING_FOR_TARGET_HURRY_UP_ALIGN_" + this.api.datacenter.Player.alignment.index);
               _loc5_ -= 35;
            }
            this.gapi.showTooltip(_loc4_,oEvent.target,_loc5_,{bXLimit:true,bYLimit:false});
            return;
         case "_btnMount":
            this.gapi.showTooltip(this.api.lang.getText("MY_MOUNT"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnTemporis":
            this.gapi.showTooltip("Temporis",oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnGuild":
            this.gapi.showTooltip(this.api.lang.getText("YOUR_GUILD"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnStatsJob":
            _loc7_ = this.api.datacenter.Player.XPhigh > this.api.datacenter.Player.XPlow;
            !_loc7_ ? this.gapi.showTooltip(this.api.lang.getText("YOUR_STATS_JOB"),oEvent.target,-20,{bXLimit:true,bYLimit:false}) : this.gapi.showTooltip(this.api.lang.getText("YOUR_STATS_JOB") + "\n\n" + this.api.lang.getText("NEXT_LEVEL") + " " + this.api.kernel.Console.getCurrentPercent() + "\n" + this.api.lang.getText("REQUIRED") + " " + new ank.utils.ExtendedString(this.api.datacenter.Player.XPhigh - this.api.datacenter.Player.XP).addMiddleChar(" ",3) + " " + this.api.lang.getText("WORD_XP"),oEvent.target,-54,{bXLimit:true,bYLimit:false});
            return;
         case "_btnSpells":
            this.gapi.showTooltip(this.api.lang.getText("YOUR_SPELLS"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnQuests":
            this.gapi.showTooltip(this.api.lang.getText("YOUR_QUESTS"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnInventory":
            this.gapi.showTooltip(this.api.lang.getText("YOUR_INVENTORY") + "\n\n" + this.api.datacenter.Player.getWeightText(),oEvent.target,-43,{bXLimit:true,bYLimit:false});
            return;
         case "_btnMap":
            this.gapi.showTooltip(this.api.lang.getText("YOUR_BOOK"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnFriends":
            this.gapi.showTooltip(this.api.lang.getText("YOUR_FRIENDS"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnFights":
            if(this._nFightsCount != 0)
            {
               this.gapi.showTooltip(ank.utils.PatternDecoder.combine(this.api.lang.getText("FIGHTS_ON_MAP",[this._nFightsCount]),"m",this._nFightsCount < 2),oEvent.target,-20,{bYLimit:false});
            }
            return;
         case "_btnOgrinas":
            _loc7_ = this.api.lang.getText("SHOP");
            if(!_loc7_ || _loc7_ == "")
            {
               _loc7_ = "Boutique";
            }
            this.gapi.showTooltip(_loc7_,oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_btnNextTurn":
            this.gapi.showTooltip(this.api.lang.getText("NEXT_TURN"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_pvAP":
            this.gapi.showTooltip(this.api.lang.getText("ACTIONPOINTS"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_pvMP":
            this.gapi.showTooltip(this.api.lang.getText("MOVEPOINTS"),oEvent.target,-20,{bXLimit:true,bYLimit:false});
            return;
         case "_mcXtra":
            switch(this._sCurrentCircleXtra)
            {
               case "compass":
                  _loc8_ = oEvent.target.targetCoords;
                  if(_loc8_ == undefined)
                  {
                     this.gapi.showTooltip(this.api.lang.getText("BANNER_SET_FLAG"),oEvent.target,0,{bXLimit:true,bYLimit:false});
                     break;
                  }
                  this.gapi.showTooltip(_loc8_[0] + ", " + _loc8_[1],oEvent.target,0,{bXLimit:true,bYLimit:false});
                  break;
               case "clock":
                  this.gapi.showTooltip(this.api.kernel.NightManager.time + "\n" + this.api.kernel.NightManager.getCurrentDateString(),oEvent.target,0,{bXLimit:true,bYLimit:false});
            }
            if(!this.api.datacenter.Game.isFight)
            {
               this._circleXtra.setXtraMask(this._mcCircleXtraMaskBig);
               this.moveHeart(true);
               dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
            }
            return;
         case "_hHeart":
            this.gapi.showTooltip(this.api.lang.getText("HELP_LIFE"),oEvent.target,-20);
      }
      return undefined;
   }
   function out(oEvent)
   {
      var _loc3_ = null;
      if((_loc3_ = oEvent.target._name) === "_mcXtra")
      {
         this.moveHeart(false);
         if(!this.api.datacenter.Game.isFight && this.api.kernel.OptionsManager.getOption("BannerGaugeMode") == "none")
         {
            this._circleXtra.setXtraMask(this._mcCircleXtraMaskBig);
         }
         else
         {
            this._circleXtra.setXtraMask(this._mcCircleXtraMask);
         }
         dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(this);
      }
      this.gapi.hideTooltip();
   }
   function drop(oEvent)
   {
      var _loc3_ = null;
      if((_loc3_ = oEvent.target) === this._mcbMovableBar)
      {
         this.api.kernel.OptionsManager.setOption("MovableBarCoord",{x:this._mcbMovableBar._x,y:this._mcbMovableBar._y,v:this._mcbMovableBar._bVertical});
      }
   }
   function filterChanged(oEvent)
   {
      this.api.network.Chat.subscribeChannels(oEvent.filter,oEvent.selected);
   }
   function lpChanged(oEvent)
   {
      this._hHeart.value = oEvent.value;
   }
   function lpMaxChanged(oEvent)
   {
      this._hHeart.max = oEvent.value;
   }
   function apChanged(oEvent)
   {
      this._pvAP.value = oEvent.value;
      if(this.api.datacenter.Game.isFight)
      {
      }
      this._msShortcuts.setSpellStateOnAllContainers();
   }
   function mpChanged(oEvent)
   {
      this._pvMP.value = Math.max(0,oEvent.value);
   }
   function selectSmiley(oEvent)
   {
      this.api.network.Chat.useSmiley(oEvent.index);
   }
   function selectEmote(oEvent)
   {
      this.api.network.Emotes.useEmote(oEvent.index);
   }
   function spellLaunched(oEvent)
   {
      this._msShortcuts.setSpellStateOnContainer(oEvent.spell.position);
   }
   function nextTurn(oEvent)
   {
      this._msShortcuts.setSpellStateOnAllContainers();
   }
   function updatePvPButtonState()
   {
      var _loc2_ = this.api.datacenter.Player.huntMatchmakingStatus;
      if(_loc2_ == undefined)
      {
         return undefined;
      }
      var _loc3_ = _loc2_.currentStatus;
      switch(_loc3_)
      {
         case "PLAYER_LEFT_MATCHMAKING":
         case "HUNT_STARTED":
         case "WAITING_FOR_START_CONFIRMATION_TIMEOUT":
            this._btnPvP.backgroundDown = "ButtonBannerRoundDown";
            this._btnPvP.backgroundUp = "ButtonBannerRoundUp";
            return;
         case "WAITING_FOR_TARGET":
            this._btnPvP.backgroundDown = "ButtonBannerRoundInSearchDown";
            this._btnPvP.backgroundUp = "ButtonBannerRoundInSearchUp";
            return;
         case "WAITING_FOR_START_CONFIRMATION":
            this._btnPvP.backgroundDown = "ButtonBannerRoundWaitResponseDown";
            this._btnPvP.backgroundUp = !dofus.Constants.TRIPLEFRAMERATE ? "ButtonBannerRoundWaitResponseUp" : "ButtonBannerRoundWaitResponseUp_TripleFramerate";
      }
      return undefined;
   }
   function huntMatchmakingStatusChanged(oEvent)
   {
      this.updatePvPButtonState();
   }
   function href(oEvent, oCustomPopupPosition)
   {
      var _loc5_ = oEvent.params.split(",");
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      switch(_loc5_[0])
      {
         case "showEndPanel":
            this.addToQueue({object:this.api.kernel.GameManager,method:this.api.kernel.GameManager.showEndPanel,params:[_loc5_[1]]});
            break;
         case "HunterAcceptPvPHunt":
            this.addToQueue({object:this.api.network.Game,method:this.api.network.Game.hunterAcceptPvPHunt});
            break;
         case "OpenGuildTaxCollectors":
            _loc6_ = this.gapi.getUIComponent("Guild");
            if(_loc6_ == null)
            {
               this.addToQueue({object:this.gapi,method:this.gapi.loadUIAutoHideComponent,params:["Guild","Guild",{currentTab:"TaxCollectors"},{bStayIfPresent:true}]});
            }
            else
            {
               this.addToQueue({object:_loc6_,method:_loc6_.setCurrentTab,params:["TaxCollectors"]});
            }
            break;
         case "OpenPayZoneDetails":
            this.addToQueue({object:this.gapi,method:this.gapi.loadUIComponent,params:["PayZoneDialog2","PayZoneDialog2",{name:"El Pemy",gfx:"9059",dialogID:dofus.graphics.gapi.ui.PayZoneDialog.PAYZONE_DETAILS},{bForceLoad:true}]});
            break;
         case "ShowPlayerPopupMenu":
            this.addToQueue({object:this.api.kernel.GameManager,method:this.api.kernel.GameManager.showPlayerPopupMenu,params:[undefined,{sPlayerID:_global.unescape(_loc5_[1]),sPlayerName:_global.unescape(_loc5_[2]),bShowJoinFriend:this.api.datacenter.Player.isAuthorized,oCustomPopupPosition:oCustomPopupPosition}]});
            break;
         case "ShowMessagePopupMenu":
            this.addToQueue({object:this.api.kernel.GameManager,method:this.api.kernel.GameManager.showMessagePopupMenu,params:[_global.unescape(_loc5_[1]),_global.unescape(_loc5_[2]),_global.unescape(_loc5_[3]),oCustomPopupPosition]});
            break;
         case "ShowItemViewer":
            _loc7_ = this.api.kernel.ChatManager.getItemFromBuffer(Number(_loc5_[1]));
            if(_loc7_ == undefined)
            {
               this.addToQueue({object:this.api.kernel,method:this.api.kernel.showMessage,params:[this.api.lang.getText("ERROR_WORD"),this.api.lang.getText("ERROR_ITEM_CANT_BE_DISPLAYED"),"ERROR_BOX"]});
            }
            else
            {
               this.addToQueue({object:this.api.ui,method:this.api.ui.loadUIComponent,params:["ItemViewer","ItemViewer",{item:_loc7_},{bAlwaysOnTop:true}]});
            }
            break;
         case "updateCompass":
            this.api.kernel.GameManager.updateCompass(Number(_loc5_[1]),Number(_loc5_[2]));
            break;
         case "ShowLinkWarning":
            this.addToQueue({object:this.api.ui,method:this.api.ui.loadUIComponent,params:["AskLinkWarning","AskLinkWarning",{text:this.api.lang.getText(_loc5_[1])}]});
            break;
         case "highlightSprite":
            if(this.api.datacenter.Game.isRunning)
            {
               _loc8_ = 1;
               while(_loc8_ < _loc5_.length)
               {
                  _loc9_ = _loc5_[_loc8_];
                  _loc10_ = this.api.datacenter.Sprites.getItemAt(_loc9_);
                  if(_loc10_ != undefined)
                  {
                     if(!(!_loc10_.isVisible || _loc10_.isClear))
                     {
                        _loc11_ = _loc10_.cellNum;
                        if(_loc11_ != undefined)
                        {
                           this.addToQueue({object:this.api.gfx.mapHandler,method:this.api.gfx.mapHandler.flagCellNonBlocking,params:[_loc11_,_loc9_]});
                        }
                     }
                  }
                  _loc8_ += 1;
               }
            }
            break;
         case "acceptTeleport":
            this.api.network.Temporis.episodeThree.acceptTeleport();
         default:
            return;
      }
   }
   function configureUseFlashChat(bUse)
   {
      this._bUseFlashChat = bUse;
      this._cChat.useReplacementPanel(bUse ? dofus.graphics.gapi.ui.chat.ChatReplacementPanelsManager.NO_REPLACEMENT_PANEL : this.api.kernel.OptionsManager.getOption("chatReplacementPanel"));
      this._txtConsole._visible = bUse;
      this._mcBgTxtConsole._visible = bUse;
      this._cChat._btnOpenClose._visible = bUse;
      this._cChat._btnHelpForPanel._visible = !bUse;
      this._btnHelp._visible = bUse;
      if(!bUse)
      {
         this._cChat.open(true);
      }
   }
   function drawBar(oEvent)
   {
      this.linkMovableContainer();
      this._msShortcuts.updateCurrentTabInformations();
      this.updateEye();
   }
   function onSetFocus()
   {
      this.api.kernel.KeyManager.addShortcutsListener("onShortcut",this);
      if(this.api.config.isLinux || this.api.config.isMac)
      {
         fscommand("trapallkeys","false");
      }
      else
      {
         this._bIsOnFocus = true;
      }
   }
   function onKillFocus()
   {
      if(this.api.config.isLinux || this.api.config.isMac)
      {
         fscommand("trapallkeys","true");
      }
      else
      {
         this._bIsOnFocus = false;
      }
   }
   function updateOgrinasVisibility()
   {
      this._btnOgrinas._visible = !this.api.datacenter.Game.isFight;
   }
   function statesChanged()
   {
      this._msShortcuts.updateSpells();
   }
}

class dofus.aks.Game extends dofus.aks.Handler
{
   var aks;
   var api;
   var extendIn;
   static var TYPE_SOLO = 1;
   static var TYPE_FIGHT = 2;
   var bSubareaHasWhiteFloor = false;
   var _bIsBusy = false;
   var nLastMapIdReceived = -1;
   function Game(oAKS, oAPI)
   {
      super.initialize(oAKS,oAPI);
      this.extendIn = new dofus.aks.extend.GameIn(oAKS,oAPI);
   }
   function get isBusy()
   {
      return this._bIsBusy;
   }
   function set isBusy(bIsBusy)
   {
      this._bIsBusy = bIsBusy;
   }
   function create()
   {
      this.aks.send("GC" + dofus.aks.Game.TYPE_SOLO);
   }
   function leave(sSpriteID)
   {
      this.aks.send("GQ" + (sSpriteID != undefined ? sSpriteID : ""));
   }
   function setPlayerPosition(nCellNum)
   {
      this.aks.send("Gp" + nCellNum,true);
   }
   function swapPlacement(nTargetId)
   {
      this.aks.send("Gw" + nTargetId,true);
   }
   function ready(bReady)
   {
      this.aks.send("GR" + (!bReady ? "0" : "1"));
   }
   function getMapData(nMapID)
   {
      if(this.api.lang.getConfigText("ENABLE_CLIENT_MAP_REQUEST"))
      {
         this.aks.send("GD" + (nMapID == undefined ? "" : String(nMapID)));
      }
   }
   function getExtraInformations()
   {
      var _loc2_ = "G";
      if(!this.aks.bMachineStateSent)
      {
         if(this.api.electron.getSystemInformation("virtual"))
         {
            _loc2_ += "i";
         }
         else
         {
            _loc2_ += "І";
         }
         this.aks.bMachineStateSent = true;
      }
      else
      {
         _loc2_ += "I";
      }
      this.aks.send(_loc2_);
   }
   function turnEnd()
   {
      if(this.api.datacenter.Player.isCurrentPlayer)
      {
         this.aks.send("Gt",false);
      }
   }
   function turnOk(sSpriteID)
   {
      this.aks.send("GT" + (sSpriteID == undefined ? "" : sSpriteID),false);
   }
   function turnOk2(sSpriteID)
   {
      this.aks.send("GT" + (sSpriteID == undefined ? "" : sSpriteID),false);
   }
   function askDisablePVPMode()
   {
      this.aks.send("GP*",false);
   }
   function enabledPVPMode(bEnabled)
   {
      this.aks.send("GP" + (!bEnabled ? "-" : "+"),false);
   }
   function freeMySoul()
   {
      this.aks.send("GF",false);
   }
   function setFlag(nCellID)
   {
      this.aks.send("Gf" + nCellID,false);
   }
   function showFightChallengeTarget(challengeId)
   {
      this.aks.send("Gdi" + challengeId,false);
   }
   function onCreate(bSuccess, sExtraData)
   {
      if(!bSuccess)
      {
         ank.utils.Logger.err("[onCreate] Impossible de créer la partie");
         return undefined;
      }
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = Number(_loc4_[0]);
      if(_loc5_ != 1)
      {
         ank.utils.Logger.err("[onCreate] Type incorrect");
         return undefined;
      }
      this.api.datacenter.Game = new dofus.datacenter.Game();
      this.api.datacenter.Game.state = _loc5_;
      var _loc6_ = dofus.graphics.gapi.ui.Banner(this.api.ui.getUIComponent("Banner"));
      dofus.graphics.gapi.ui.banner.BannerGauge.showGaugeMode(_loc6_);
      _loc6_.chat.removeTemporaryReplacementPanel();
      var _loc7_ = _loc6_.chat.shortcutsReplacementPanel;
      if(_loc7_ != undefined)
      {
         _loc7_.showMiniMap(true);
         _loc7_.updateSprite(undefined);
      }
      this.api.datacenter.Player.data.initAP(false);
      this.api.datacenter.Player.data.initMP(false);
      this.api.datacenter.Player.SpellsManager.clear();
      this.api.datacenter.Player.data.CharacteristicsManager.initialize();
      this.api.datacenter.Player.data.EffectsManager.initialize();
      this.api.datacenter.Player.clearSummon();
      this.api.gfx.cleanMap(1);
      this.onCreateSolo();
   }
   function onJoin(sExtraData)
   {
      this.api.datacenter.Player.guildInfos.defendedTaxCollectorID = undefined;
      if(this.api.gfx.spriteHandler.isPlayerSpritesHidden)
      {
         this.api.gfx.spriteHandler.hideSprites(false);
      }
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = Number(_loc4_[0]);
      var _loc6_ = _loc4_[1] != "0" ? true : false;
      var _loc7_ = _loc4_[2] != "0" ? true : false;
      var _loc8_ = _loc4_[3] != "0" ? true : false;
      var _loc9_ = Number(_loc4_[4]);
      var _loc10_ = Number(_loc4_[5]);
      this.api.datacenter.Game = new dofus.datacenter.Game();
      this.api.datacenter.Game.state = _loc5_;
      this.api.datacenter.Game.fightType = _loc10_;
      var _loc11_ = dofus.graphics.gapi.ui.Banner(this.api.ui.getUIComponent("Banner"));
      _loc11_.redrawChrono();
      _loc11_.updateEye();
      this.api.datacenter.Game.isSpectator = _loc8_;
      if(!_loc8_)
      {
         this.api.datacenter.Player.data.initAP(false);
         this.api.datacenter.Player.data.initMP(false);
         this.api.datacenter.Player.SpellsManager.clear();
      }
      this.api.gfx.cleanMap(1);
      if(this.api.datacenter.Game.isTacticMode)
      {
         this.api.gfx.activateTacticMode(this.api,true);
      }
      if(_loc7_)
      {
         this.api.ui.loadUIComponent("ChallengeMenu","ChallengeMenu",{labelReady:this.api.lang.getText("READY"),labelCancel:this.api.lang.getText("CANCEL_SMALL"),cancelButton:_loc6_,ready:false},{bStayIfPresent:true});
      }
      if(!_global.isNaN(_loc9_))
      {
         _loc11_.startTimer(_loc9_ / 1000);
      }
      this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_OBJECT_NONE);
      this.api.ui.unloadLastUIAutoHideComponent();
      this.api.ui.unloadUIComponent("FightsInfos");
      switch(this.api.datacenter.Map.subarea)
      {
         case 320:
         case 321:
            this.bSubareaHasWhiteFloor = true;
            break;
         default:
            this.bSubareaHasWhiteFloor = false;
      }
      this.api.ui.unloadUIComponent("GameResult");
      this.api.ui.unloadUIComponent("GameResultLight");
   }
   function onPositionStart(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0];
      var _loc5_ = _loc3_[1];
      var _loc6_ = Number(_loc3_[2]);
      this.api.datacenter.Basics.aks_current_team = _loc6_;
      this.api.datacenter.Basics.aks_team1_starts = [];
      this.api.datacenter.Basics.aks_team2_starts = [];
      this.api.kernel.StreamingDisplayManager.onFightStart();
      this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_NONE);
      this.api.datacenter.Game.setInteractionType("place");
      if(_loc6_ == undefined)
      {
         ank.utils.Logger.err("[onPositionStart] Impossible de trouver l\'équipe du joueur local !");
      }
      this.api.gfx.mapHandler.showFightCells(_loc4_,_loc5_);
      var _loc7_ = 0;
      var _loc8_;
      while(_loc7_ < _loc4_.length)
      {
         _loc8_ = ank.utils.Compressor.decode64(_loc4_.charAt(_loc7_)) << 6;
         _loc8_ += ank.utils.Compressor.decode64(_loc4_.charAt(_loc7_ + 1));
         this.api.datacenter.Basics.aks_team1_starts.push(_loc8_);
         if(_loc6_ == 0)
         {
            this.api.gfx.setInteractionOnCell(_loc8_,ank.battlefield.Constants.INTERACTION_CELL_RELEASE);
         }
         _loc7_ += 2;
      }
      var _loc9_ = 0;
      var _loc10_;
      while(_loc9_ < _loc5_.length)
      {
         _loc10_ = ank.utils.Compressor.decode64(_loc5_.charAt(_loc9_)) << 6;
         _loc10_ += ank.utils.Compressor.decode64(_loc5_.charAt(_loc9_ + 1));
         this.api.datacenter.Basics.aks_team2_starts.push(_loc10_);
         if(_loc6_ == 1)
         {
            this.api.gfx.setInteractionOnCell(_loc10_,ank.battlefield.Constants.INTERACTION_CELL_RELEASE);
         }
         _loc9_ += 2;
      }
      if(this.api.ui.getUIComponent("FightOptionButtons") == undefined)
      {
         this.api.ui.loadUIComponent("FightOptionButtons","FightOptionButtons");
      }
      this.api.kernel.TipsManager.showNewTip(dofus.managers.TipsManager.TIP_FIGHT_PLACEMENT);
   }
   function onPlayersCoordinates(sExtraData)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      if(sExtraData != "e")
      {
         _loc3_ = sExtraData.split("|");
         _loc4_ = 0;
         while(_loc4_ < _loc3_.length)
         {
            _loc5_ = _loc3_[_loc4_].split(";");
            _loc6_ = _loc5_[0];
            _loc7_ = Number(_loc5_[1]);
            this.api.gfx.setSpritePosition(_loc6_,_loc7_);
            _loc4_ += 1;
         }
      }
      else
      {
         this.api.sounds.events.onError();
      }
   }
   function onReady(sExtraData)
   {
      var _loc3_ = sExtraData.charAt(0) == "1";
      var _loc4_ = sExtraData.substr(1);
      if(_loc3_)
      {
         this.api.gfx.addSpriteExtraClip(_loc4_,dofus.Constants.READY_FILE,undefined,true);
      }
      else
      {
         this.api.gfx.removeSpriteExtraClip(_loc4_,true);
      }
   }
   function onStartToPlay()
   {
      this.api.ui.getUIComponent("Banner").stopTimer();
      this.aks.GameActions.onActionsFinish(this.api.datacenter.Player.ID);
      this.api.sounds.events.onGameStart(this.api.datacenter.Map.musics);
      this.api.kernel.StreamingDisplayManager.onFightStartEnd();
      var _loc2_ = this.api.ui.getUIComponent("Banner");
      _loc2_.showGiveUpButton(true);
      if(this.api.ui.getUIComponent("FightOptionButtons") == undefined)
      {
         this.api.ui.loadUIComponent("FightOptionButtons","FightOptionButtons");
      }
      var _loc3_;
      if(!this.api.datacenter.Game.isSpectator)
      {
         _loc3_ = this.api.datacenter.Player.data;
         _loc3_.initAP();
         _loc3_.initMP();
         _loc2_.showPoints(true);
         _loc2_.showNextTurnButton(true);
         this.api.ui.loadUIComponent("CenterText","CenterText",{text:this.api.lang.getText("GAME_LAUNCH"),background:true,timer:2000},{bForceLoad:true});
         this.api.ui.getUIComponent("FightOptionButtons").onGameRunning();
         _loc2_.shortcuts.setCurrentTab("Spells");
      }
      this.api.ui.loadUIComponent("Timeline","Timeline");
      this.api.ui.unloadUIComponent("ChallengeMenu");
      this.api.gfx.unSelect(true);
      this.api.gfx.mapHandler.showingFightCells = false;
      if(!this.api.gfx.gridHandler.bGridVisible)
      {
         this.api.gfx.drawGrid();
      }
      this.api.datacenter.Game.setInteractionType("move");
      this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_NONE);
      this.api.kernel.GameManager.signalFightActivity();
      this.api.datacenter.Game.isRunning = true;
      var _loc4_ = this.api.datacenter.Sprites.getItems();
      for(var _loc5_ in _loc4_)
      {
         this.api.gfx.addSpriteExtraClip(_loc5_,dofus.Constants.CIRCLE_FILE,dofus.Constants.TEAMS_COLOR[_loc4_[_loc5_].Team]);
      }
      if(this.api.datacenter.Game.isTacticMode)
      {
         this.api.gfx.activateTacticMode(this.api,true);
      }
   }
   function onTurnStart(sExtraData)
   {
      var _loc3_ = sExtraData.split("|")[0];
      if(this.api.datacenter.Game.controlledInvocationID != undefined)
      {
         if(String(_loc3_) != String(this.api.datacenter.Game.controlledInvocationID))
         {
            this.api.datacenter.Game.controlledInvocationID = undefined;
            this.api.datacenter.Game.controlledInvocationCell = undefined;
            this.api.datacenter.Game.controlledInvocationPA = undefined;
            this.api.datacenter.Game.controlledInvocationPM = undefined;
         }
      }
      var _loc4_;
      if(this.api.datacenter.Game.isFirstTurn)
      {
         this.api.datacenter.Game.isFirstTurn = false;
         _loc4_ = this.api.gfx.spriteHandler.getSprites().getItems();
         for(var _loc5_ in _loc4_)
         {
            this.api.gfx.removeSpriteExtraClip(_loc5_,true);
         }
      }
      var _loc6_ = sExtraData.split("|");
      var _loc7_ = _loc6_[0];
      var _loc8_ = Number(_loc6_[1]) / 1000;
      var _loc9_ = Number(_loc6_[2]);
      var _loc10_ = _loc6_.length > 3 && _loc6_[3] == "1";
      this.api.datacenter.Game.currentTableTurn = _loc9_;
      var _loc11_ = this.api.datacenter.Sprites.getItemAt(_loc7_);
      _loc11_.GameActionsManager.clear();
      this.api.gfx.unSelect(true);
      this.api.datacenter.Game.currentPlayerID = _loc7_;
      this.api.kernel.GameManager.cleanPlayer(this.api.datacenter.Game.lastPlayerID);
      this.api.ui.getUIComponent("Timeline").nextTurn(_loc7_);
      if(this.api.datacenter.Player.isCurrentPlayer)
      {
         this.api.electron.makeNotification(this.api.lang.getText("PLAYER_TURN",[this.api.datacenter.Player.Name]));
         if(!this.api.datacenter.Game.passiveTurn && this.api.kernel.OptionsManager.getOption("StartTurnSound"))
         {
            this.api.sounds.events.onTurnStart();
         }
         if(this.api.kernel.GameManager.autoSkip && this.api.datacenter.Game.isFight)
         {
            this.api.network.Game.turnEnd();
         }
         this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_RELEASE_OVER_OUT);
         if(!_loc10_)
         {
            this.api.datacenter.Player.SpellsManager.nextTurn();
         }
         this.api.ui.getUIComponent("Banner").startTimer(_loc8_);
         this.api.kernel.GameManager.startInactivityDetector();
         if(this.api.gfx.rollOverMcSprite == undefined)
         {
            dofus.DofusCore.getInstance().forceMouseOver();
         }
         this.api.gfx.mapHandler.resetEmptyCells();
      }
      else
      {
         this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_NONE);
         this.api.ui.getUIComponent("Timeline").startChrono(_loc8_);
         if(this.api.datacenter.Game.isSpectator && this.api.kernel.OptionsManager.getOption("SpriteInfos"))
         {
            this.api.ui.getUIComponent("Banner").showRightPanel("BannerSpriteInfos",{data:_loc11_},true);
         }
      }
      var _loc12_;
      if(!this.api.datacenter.Game.passiveTurn && this.api.kernel.OptionsManager.getOption("StringCourse"))
      {
         _loc12_ = [];
         _loc12_[1] = _loc11_.color1;
         _loc12_[2] = _loc11_.color2;
         _loc12_[3] = _loc11_.color3;
         this.api.ui.loadUIComponent("StringCourse","StringCourse",{gfx:_loc11_.artworkFile,name:_loc11_.name,level:this.api.lang.getText("LEVEL_SMALL") + " " + _loc11_.Level,colors:_loc12_,gfxID:_loc11_.gfxID,bFilters:_loc11_ instanceof dofus.datacenter.Monster},{bForceLoad:true});
      }
      var _loc13_;
      var _loc14_;
      if(this.api.electron.isWindowFocused && (_loc11_ instanceof dofus.datacenter.Character && !this.api.datacenter.Game.passiveTurn))
      {
         _loc13_ = new ank.battlefield.datacenter.VisualEffect();
         _loc13_.file = dofus.Constants.HIGHLIGHT_FILE;
         _loc13_.bInFrontOfSprite = false;
         _loc14_ = _loc11_.cellNum;
         this.api.gfx.spriteLaunchVisualEffect(_loc7_,_loc13_,_loc14_,10);
      }
      this.api.kernel.GameManager.cleanUpGameArea(true);
      ank.utils.Timer.setTimer(this.api.network.Ping,"GameDecoDetect",this.api.network,this.api.network.quickPing,_loc8_ * 1000);
      this.api.kernel.TipsManager.showNewTip(dofus.managers.TipsManager.TIP_FIGHT_START);
   }
   function onTurnFinish(sExtraData)
   {
      var _loc3_ = sExtraData;
      var _loc4_ = this.api.datacenter.Sprites.getItemAt(_loc3_);
      if(this.api.datacenter.Player.isCurrentPlayer)
      {
         this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_NONE);
         this.api.kernel.GameManager.stopInactivityDetector();
         this.api.kernel.GameManager.onTurnEnd();
      }
      this.api.datacenter.Game.lastPlayerID = this.api.datacenter.Game.currentPlayerID;
      this.api.datacenter.Game.currentPlayerID = undefined;
      this.api.ui.getUIComponent("Banner").stopTimer();
      this.api.ui.getUIComponent("Timeline").stopChrono();
      this.api.kernel.GameManager.cleanUpGameArea(true);
   }
   function onTurnlist(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      this.api.datacenter.Game.turnSequence = _loc3_;
      this.api.ui.getUIComponent("Timeline").update();
   }
   function onTurnMiddle(sExtraData)
   {
      if(!this.api.datacenter.Game.isRunning)
      {
         ank.utils.Logger.err("[innerOnTurnMiddle] on est pas en combat");
         return undefined;
      }
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = {};
      var _loc6_ = 0;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      while(_loc6_ < _loc4_.length)
      {
         _loc7_ = _loc4_[_loc6_].split(";");
         if(_loc7_.length != 0)
         {
            _loc8_ = _loc7_[0];
            _loc9_ = _loc7_[1] != "1" ? false : true;
            _loc10_ = Number(_loc7_[2]);
            _loc11_ = Number(_loc7_[3]);
            _loc12_ = Number(_loc7_[4]);
            _loc13_ = Number(_loc7_[5]);
            _loc14_ = Number(_loc7_[6]);
            _loc15_ = Number(_loc7_[7]);
            _loc5_[_loc8_] = true;
            _loc16_ = this.api.datacenter.Sprites.getItemAt(_loc8_);
            if(_loc16_ != undefined)
            {
               _loc17_ = _loc16_.sequencer;
               if(_loc9_)
               {
                  if(!_loc17_.isPlaying())
                  {
                     _loc16_.mc.clear();
                     this.api.gfx.removeSpriteOverHeadLayer(_loc8_,"text");
                  }
               }
               else
               {
                  _loc16_.LP = _loc10_;
                  _loc16_.LPmax = _loc15_;
                  _loc16_.AP = _loc11_;
                  _loc16_.MP = _loc12_;
                  if(!_loc17_.isPlaying())
                  {
                     if(!_global.isNaN(_loc13_) && !_loc16_.hasCarriedParent())
                     {
                        this.api.gfx.setSpritePosition(_loc8_,_loc13_);
                     }
                     if(_loc16_.hasCarriedChild())
                     {
                        _loc16_.carriedChild.updateCarriedPosition();
                     }
                  }
               }
            }
            else
            {
               ank.utils.Logger.err("[onTurnMiddle] le sprite n\'existe pas");
            }
         }
         _loc6_ += 1;
      }
      var _loc18_ = this.api.datacenter.Sprites.getItems();
      for(var _loc19_ in _loc18_)
      {
         if(!_loc5_[_loc19_])
         {
            _loc18_[_loc19_].mc.clear();
            this.api.datacenter.Sprites.removeItemAt(_loc19_);
         }
      }
      this.api.ui.getUIComponent("Timeline").timelineControl.updateCharacters();
   }
   function prepareTurnEnd()
   {
      if(!this.api.datacenter.Game.isRunning || (!this.api.datacenter.Game.isFight || !this.api.datacenter.Player.isCurrentPlayer))
      {
         return undefined;
      }
      var _loc2_ = this.api.datacenter.Player.data.sequencer;
      if(_loc2_.containsAction(this,this.turnEnd))
      {
         return undefined;
      }
      _loc2_.addAction(24,false,this,this.turnEnd,[]);
      _loc2_.execute();
   }
   function onTurnReady(sExtraData)
   {
      var _loc3_ = sExtraData;
      var _loc4_ = this.api.datacenter.Sprites.getItemAt(_loc3_);
      var _loc5_;
      if(_loc4_ != undefined)
      {
         _loc5_ = _loc4_.sequencer;
         _loc5_.addAction(25,false,this,this.turnOk);
         _loc5_.execute();
      }
      else
      {
         ank.utils.Logger.err("[onTurnReday] le sprite " + _loc3_ + " n\'existe pas");
         this.turnOk2();
      }
   }
   function onMapData(sExtraData)
   {
      var _loc4_ = sExtraData.split("|");
      var _loc5_ = _loc4_[0];
      var _loc6_ = _loc4_[1];
      var _loc7_ = _loc4_[2];
      if(Number(_loc5_) == this.api.datacenter.Map.id)
      {
         this.api.gfx.onMapLoaded();
         return undefined;
      }
      this.api.gfx.showContainer(false);
      this.nLastMapIdReceived = _global.parseInt(_loc5_,10);
      this.api.kernel.MapsServersManager.loadMap(_loc5_,_loc6_,_loc7_);
   }
   function onMapLoaded()
   {
      this.api.gfx.showContainer(true);
      this.api.gfx.dispatchEvent({type:"mapDataLoaded"});
      this.api.kernel.GameManager.applyCreatureMode();
      if(dofus.Constants.SAVING_THE_WORLD)
      {
         dofus.SaveTheWorld.getInstance().nextAction();
      }
      if(this.api.datacenter.Game.isRunning && this.api.datacenter.Game.isTacticMode)
      {
         this.api.gfx.activateTacticMode(this.api,true);
      }
   }
   function onCreateSolo()
   {
      this.api.datacenter.Player.InteractionsManager.setState(false);
      this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_OBJECT_RELEASE_OVER_OUT);
      this.api.ui.removeCursor();
      this.api.ui.getUIComponent("Banner").shortcuts.setCurrentTab("Items");
      this.api.datacenter.Basics.gfx_isSpritesHidden = false;
      this.api.gfx.spriteHandler.unmaskAllSprites();
      var _loc2_;
      if(this.api.ui.getUIComponent("Banner") == undefined)
      {
         this.api.kernel.OptionsManager.applyAllOptions();
         this.api.ui.loadUIComponent("Banner","Banner",{data:this.api.datacenter.Player},{bAlwaysOnTop:true});
         this.api.ui.setScreenSize(742,432);
      }
      else
      {
         _loc2_ = this.api.ui.getUIComponent("Banner");
         _loc2_.showPoints(false);
         _loc2_.showNextTurnButton(false);
         _loc2_.showGiveUpButton(false);
         this.api.ui.unloadUIComponent("FightOptionButtons");
         this.api.ui.unloadUIComponent("ChallengeMenu");
      }
      this.api.gfx.cleanMap(2);
   }
   function onPVP(sExtraData, bEnabled)
   {
      var _loc4_;
      if(!bEnabled)
      {
         _loc4_ = Number(sExtraData);
         this.api.kernel.showMessage(undefined,this.api.lang.getText("ASK_DISABLE_PVP",[_loc4_]),"CAUTION_YESNO",{name:"DisabledPVP",listener:this});
      }
      else
      {
         this.api.kernel.showMessage(undefined,this.api.lang.getText("ASK_ENABLED_PVP"),"CAUTION_YESNO",{name:"EnabledPVP",listener:this});
      }
   }
   function onHuntInfos(sExtraData)
   {
      var _loc3_ = sExtraData.substring(1);
      var _loc4_ = _loc3_.split("|");
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      switch(sExtraData.charAt(0))
      {
         case "I":
            if(_loc3_ == undefined || _loc3_.length == 0)
            {
               this.api.datacenter.Basics.pvpHuntedSpriteID = undefined;
            }
            else
            {
               this.api.datacenter.Basics.pvpHuntedSpriteID = _loc3_;
            }
            return;
         case "S":
            _loc5_ = _loc4_[0];
            _loc6_ = _loc4_[1];
            _loc7_ = _loc5_ != _loc6_;
            _loc8_ = true;
            switch(_loc6_)
            {
               case "WAITING_FOR_TARGET":
                  if(_loc5_ == "WAITING_FOR_START_CONFIRMATION")
                  {
                     this.api.kernel.showMessage(undefined,this.api.lang.getText("HUNT_NOT_AVAILABLE_ANYMORE"),"HUNT_CHAT");
                     break;
                  }
                  if(_loc5_ == "NOT_IN_MATCHMAKING")
                  {
                     this.api.kernel.showMessage(undefined,this.api.lang.getText("HUNT_LOOKING_FOR_TARGET_ALIGN_" + this.api.datacenter.Player.alignment.index),"HUNT_CHAT");
                  }
                  break;
               case "WAITING_FOR_START_CONFIRMATION_TIMEOUT":
                  _loc8_ = false;
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("HUNT_REQUEST_TIMEOUT"),"HUNT_CHAT");
                  break;
               case "PLAYER_LEFT_MATCHMAKING":
                  _loc8_ = false;
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("HUNTER_HAS_LEFT_MATCHMAKING"),"HUNT_CHAT");
                  break;
               case "HUNT_STARTED":
                  _loc8_ = false;
                  this.api.kernel.showMessage(undefined,this.api.lang.getText("HUNT_STARTED"),"HUNT_CHAT");
                  break;
               case "WAITING_FOR_START_CONFIRMATION":
                  _loc9_ = this.api.lang.getText("HUNT_FOUND_PART_1_ALIGN_" + this.api.datacenter.Player.alignment.index);
                  _loc10_ = Number(_loc4_[2]);
                  if(_loc10_ == 1)
                  {
                     _loc9_ += this.api.lang.getText("HUNT_FOUND_PART_2_AFTER_WIN");
                  }
                  else if(_loc10_ == 2)
                  {
                     _loc9_ += this.api.lang.getText("HUNT_FOUND_PART_2_AFTER_DEFEAT");
                  }
                  _loc9_ += ". ";
                  _loc9_ += this.api.lang.getText("HUNT_FOUND_PART_3");
                  this.api.kernel.showMessage(undefined,_loc9_,"HUNT_CHAT",undefined,"START_CONFIRMATION");
            }
            this.api.datacenter.Player.huntMatchmakingStatus = new dofus.datacenter.HuntMatchmakingStatus(_loc8_,_loc6_);
      }
      return undefined;
   }
   function hunterAcceptPvPHunt()
   {
      this.aks.send("GhA");
   }
   function toggleHunterMatchmakingRegister()
   {
      if(this.api.datacenter.Player.isHuntMatchmakingActive())
      {
         this.api.network.Game.hunterMatchmakingUnregister();
      }
      else
      {
         this.api.network.Game.hunterMatchmakingRegister();
      }
   }
   function hunterMatchmakingRegister()
   {
      this.aks.send("Ghr");
   }
   function hunterMatchmakingUnregister()
   {
      this.aks.send("Ghu");
   }
   function onFlag(sExtraData)
   {
      var _loc3_ = sExtraData.split("|");
      var _loc4_ = _loc3_[0];
      var _loc5_ = Number(_loc3_[1]);
      var _loc6_ = this.api.datacenter.Sprites.getItemAt(_loc4_);
      var _loc7_ = new ank.battlefield.datacenter.VisualEffect();
      _loc7_.file = dofus.Constants.CLIPS_PATH + "flag.swf";
      _loc7_.bInFrontOfSprite = true;
      _loc7_.bTryToBypassContainerColor = true;
      this.api.kernel.showMessage(undefined,this.api.lang.getText("PLAYER_SET_FLAG",[_loc6_.name,_loc5_]),"INFO_CHAT");
      this.api.gfx.spriteLaunchVisualEffect(_loc4_,_loc7_,_loc5_,11,undefined,undefined,undefined,true);
   }
   function onFightChallenge(sExtraData)
   {
      var _loc4_ = sExtraData.split(";");
      if(!this.api.ui.getUIComponent("FightChallenge"))
      {
         this.api.ui.loadUIComponent("FightChallenge","FightChallenge");
      }
      var _loc5_ = new dofus.datacenter.FightChallengeData(_global.parseInt(_loc4_[0]),_loc4_[1] == "1",_global.parseInt(_loc4_[2]),_global.parseInt(_loc4_[3]),_global.parseInt(_loc4_[4]),_global.parseInt(_loc4_[5]),_global.parseInt(_loc4_[6]));
      dofus.graphics.gapi.ui.FightChallenge(dofus.graphics.gapi.ui.FightChallenge(this.api.ui.getUIComponent("FightChallenge"))).addChallenge(_loc5_);
   }
   function onFightChallengeUpdate(sExtraData, success)
   {
      var _loc5_ = _global.parseInt(sExtraData);
      dofus.graphics.gapi.ui.FightChallenge(dofus.graphics.gapi.ui.FightChallenge(this.api.ui.getUIComponent("FightChallenge"))).updateChallenge(_loc5_,success);
      var _loc6_ = !success ? this.api.lang.getText("FIGHT_CHALLENGE_FAILED") : this.api.lang.getText("FIGHT_CHALLENGE_DONE");
      _loc6_ += " : " + this.api.lang.getFightChallenge(_loc5_).n;
      this.api.kernel.showMessage(undefined,_loc6_,"INFO_CHAT");
   }
   function cancel(oEvent)
   {
      var _loc2_ = oEvent.target._name;
   }
   function yes(oEvent)
   {
      switch(oEvent.target._name)
      {
         case "AskYesNoEnabledPVP":
            this.api.network.Game.enabledPVPMode(true);
            return;
         case "AskYesNoDisabledPVP":
            this.api.network.Game.enabledPVPMode(false);
      }
      return undefined;
   }
   function no(oEvent)
   {
      var _loc2_ = oEvent.target._name;
   }
}

class dofus.graphics.battlefield.DofusBattlefield extends ank.battlefield.Battlefield
{
   var _oAPI;
   var _pathNumClips;
   var _rollOverMcObject;
   var _rollOverMcSprite;
   var createEmptyMovieClip;
   var dispatchEvent;
   var getNextHighestDepth;
   var globalToLocal;
   var mapHandler;
   function DofusBattlefield()
   {
      super();
   }
   function get api()
   {
      return this._oAPI;
   }
   function get rollOverMcSprite()
   {
      return this._rollOverMcSprite;
   }
   function get rollOverMcObject()
   {
      return this._rollOverMcObject;
   }
   function set rollOverMcObject(rollOverMcObject)
   {
      this._rollOverMcObject = rollOverMcObject;
   }
   function initialize(oDatacenter, sGroundFile, sObjectFile, sAccessoriesPath, oAPI)
   {
      super.initialize(oDatacenter,sGroundFile,sObjectFile,sAccessoriesPath,oAPI);
      mx.events.EventDispatcher.initialize(this);
      this._oAPI = oAPI;
   }
   function addSpritePoints(sID, sValue, nTypePoint)
   {
      if(this.api.kernel.OptionsManager.getOption("PointsOverHead") && this.api.electron.isWindowFocused)
      {
         super.addSpritePoints(sID,sValue,nTypePoint);
      }
   }
   function onInitError()
   {
      _root.onCriticalError(this.api.lang.getText("CRITICAL_ERROR_LOADING_BATTLEFIELD"));
   }
   function onMapLoaded()
   {
      this._rollOverMcObject = undefined;
      this._rollOverMcSprite = undefined;
      var _loc3_ = this.api.datacenter.Map;
      this.api.ui.unloadUIComponent("CenterText");
      this.api.ui.unloadUIComponent("CenterTextMap");
      this.api.ui.unloadUIComponent("FightsInfos");
      this.setInteraction(ank.battlefield.Constants.INTERACTION_NONE);
      this.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_RELEASE);
      this.setInteraction(ank.battlefield.Constants.INTERACTION_SPRITE_RELEASE_OVER_OUT);
      if(this.api.datacenter.Game.isFight)
      {
         this.setInteraction(ank.battlefield.Constants.INTERACTION_OBJECT_NONE);
      }
      else
      {
         this.setInteraction(ank.battlefield.Constants.INTERACTION_OBJECT_RELEASE_OVER_OUT);
      }
      this.api.datacenter.Game.setInteractionType("move");
      this.api.network.Game.getExtraInformations();
      this.api.ui.unloadLastUIAutoHideComponent();
      this.api.ui.removePopupMenu();
      this.api.ui.getUIComponent("MapInfos").update();
      var _loc4_ = _loc3_.subarea;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      if(_loc4_ != this.api.datacenter.Basics.gfx_lastSubarea)
      {
         _loc5_ = this.api.datacenter.Subareas.getItemAt(_loc4_);
         _loc6_ = new String();
         _loc7_ = new String();
         _loc8_ = this.api.lang.getMapAreaText(_loc3_.area).n;
         if(_loc5_ == undefined)
         {
            _loc7_ = this.api.lang.getMapSubAreaName(_loc4_);
            if(_loc8_ != _loc7_)
            {
               _loc6_ = _loc8_ + "\n(" + _loc7_ + ")";
            }
            else
            {
               _loc6_ = _loc8_;
            }
         }
         else
         {
            _loc7_ = _loc5_.name;
            _loc6_ = _loc5_.name + " (" + _loc5_.alignment.name + ")";
            if(_loc8_ != _loc7_)
            {
               _loc6_ = _loc8_ + "\n(" + _loc7_ + ")\n" + _loc5_.alignment.name;
            }
            else
            {
               _loc6_ = _loc8_ + "\n" + _loc5_.alignment.name;
            }
         }
         if(dofus.Constants.INVADER_AREA && (!_loc3_.isDungeon && !_global.isNaN(this.api.datacenter.Temporis.currentAreaInvadeLevel)))
         {
            _loc6_ += " - " + this.api.lang.getText("TR3_ACTUAL_INVADE_TIME",[this.api.datacenter.Temporis.currentAreaInvadeTimer]) + " (" + this.api.lang.getText("LEVEL") + " " + this.api.datacenter.Temporis.currentAreaInvadeLevel + ")";
         }
         if(!this.api.kernel.TutorialManager.isTutorialMode)
         {
            this.api.ui.loadUIComponent("CenterText","CenterText",{text:_loc6_,background:false,timer:2000},{bForceLoad:true});
         }
         this.api.datacenter.Basics.gfx_lastSubarea = _loc4_;
      }
      if(this.api.kernel.OptionsManager.getOption("Grid") == true || this.api.datacenter.Game.isRunning)
      {
         this.api.gfx.drawGrid();
      }
      else
      {
         this.api.gfx.removeGrid();
      }
      if(this.showingCellIds)
      {
         this.updateCellIds();
      }
      this.api.ui.getUIComponent("Banner").circleXtra.setCircleXtraParams({currentCoords:[_loc3_.x,_loc3_.y]});
      if(!this.api.datacenter.Game.isRunning)
      {
         if(Number(_loc3_.ambianceID) > 0)
         {
            this.api.sounds.playEnvironment(_loc3_.ambianceID);
         }
         if(Number(_loc3_.musicID) > 0)
         {
            this.api.sounds.playMusic(_loc3_.musicID,true);
         }
      }
      var _loc9_ = Array(this.api.lang.getMapText(_loc3_.id).p);
      var _loc10_ = 0;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      while(_loc9_.length > _loc10_)
      {
         _loc11_ = _loc9_[_loc10_][0];
         _loc12_ = _loc9_[_loc10_][1];
         _loc13_ = _loc9_[_loc10_][2];
         if(!dofus.utils.criterions.CriterionManager.fillingCriterions(_loc13_))
         {
            _loc14_ = this.api.gfx.mapHandler.getCellData(_loc12_);
            _loc15_ = 0;
            while(_loc15_ < _loc11_.length)
            {
               if(_loc14_.layerObject1Num == _loc11_[_loc15_])
               {
                  _loc14_.mcObject1._visible = false;
               }
               if(_loc14_.layerObject2Num == _loc11_[_loc15_])
               {
                  _loc14_.mcObject2._visible = false;
               }
               _loc15_ += 1;
            }
         }
         _loc10_ += 1;
      }
      this.dispatchEvent({type:"mapLoaded",currentMap:_loc3_});
   }
   function onCellRelease(mcCell)
   {
      if(this.api.kernel.TutorialManager.isTutorialMode)
      {
         this.api.kernel.TutorialManager.onWaitingCase({code:"CELL_RELEASE",params:[mcCell.num]});
         return false;
      }
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      switch(this.api.datacenter.Game.interactionType)
      {
         case 1:
            _loc3_ = this.api.datacenter.Player.data;
            _loc4_ = false;
            _loc5_ = this.api.datacenter.Player.canMoveInAllDirections;
            if(this.api.datacenter.Player.InteractionsManager.calculatePath(this.mapHandler,mcCell.num,true,this.api.datacenter.Game.isFight,false,_loc5_))
            {
               if(this.api.datacenter.Game.isFight)
               {
                  _loc4_ = true;
               }
               else
               {
                  _loc4_ = this.api.datacenter.Basics.interactionsManager_path[this.api.datacenter.Basics.interactionsManager_path.length - 1].num == mcCell.num;
               }
            }
            if(!this.api.datacenter.Game.isFight && !_loc4_)
            {
               if(this.api.datacenter.Player.InteractionsManager.calculatePath(this.mapHandler,mcCell.num,true,this.api.datacenter.Game.isFight,true,_loc5_))
               {
                  _loc4_ = true;
               }
            }
            if(_loc4_)
            {
               if(getTimer() - this.api.datacenter.Basics.gfx_lastActionTime < dofus.Constants.CLICK_MIN_DELAY && (_loc3_ == undefined || !_loc3_.isAdminSonicSpeed))
               {
                  ank.utils.Logger.err("T trop rapide du clic");
                  return null;
               }
               this.api.datacenter.Basics.gfx_lastActionTime = getTimer();
               if(this.api.datacenter.Basics.interactionsManager_path.length != 0)
               {
                  _loc6_ = ank.battlefield.utils.Compressor.compressPath(this.api.datacenter.Basics.interactionsManager_path);
                  if(_loc6_ != undefined)
                  {
                     if(this.api.datacenter.Game.isFight && this.api.datacenter.Game.isRunning)
                     {
                        _loc7_ = _loc3_.sequencer;
                        _loc7_.addAction(122,false,_loc3_.GameActionsManager,_loc3_.GameActionsManager.transmittingMove,[1,[_loc6_]]);
                        _loc7_.execute();
                     }
                     else
                     {
                        _loc3_.GameActionsManager.transmittingMove(1,[_loc6_]);
                     }
                     delete this.api.datacenter.Basics.interactionsManager_path;
                     this.clearPathIndexNumbers();
                  }
               }
               return true;
            }
            return false;
            break;
         case 2:
            if(this.api.datacenter.Player.currentUseObject != null && this.api.datacenter.Basics.gfx_canLaunch)
            {
               _loc8_ = this.api.datacenter.Player.data;
               _loc9_ = _loc8_.sequencer;
               _loc9_.addAction(123,false,_loc8_.GameActionsManager,_loc8_.GameActionsManager.transmittingOther,[300,[this.api.datacenter.Player.currentUseObject.ID,mcCell.num]]);
               _loc9_.execute();
               this.api.datacenter.Player.currentUseObject = null;
            }
            else if(this.api.datacenter.Basics.spellManager_errorMsg != undefined)
            {
               this.api.kernel.showMessage(undefined,this.api.datacenter.Basics.spellManager_errorMsg,"ERROR_CHAT");
               delete this.api.datacenter.Basics.spellManager_errorMsg;
            }
            this.api.gfx.clearSpellPreview();
            this.api.kernel.GameManager.lastSpellLaunch = getTimer();
            this.api.datacenter.Game.setInteractionType("move");
            return undefined;
         case 3:
            if(this.api.datacenter.Player.currentUseObject != null && this.api.datacenter.Basics.gfx_canLaunch)
            {
               _loc10_ = this.api.datacenter.Player.data;
               _loc11_ = _loc10_.sequencer;
               _loc11_.addAction(124,false,_loc10_.GameActionsManager,_loc10_.GameActionsManager.transmittingOther,[303,[mcCell.num]]);
               _loc11_.execute();
               this.api.datacenter.Player.currentUseObject = null;
            }
            this.api.gfx.clearSpellPreview();
            this.api.kernel.GameManager.lastSpellLaunch = getTimer();
            this.api.datacenter.Game.setInteractionType("move");
            return undefined;
         case 4:
            _loc12_ = this.mapHandler.getCellData(mcCell.num).spriteOnID;
            if(_loc12_ == undefined)
            {
               this.api.network.Game.setPlayerPosition(mcCell.num);
               return undefined;
            }
            return undefined;
            break;
         case 5:
            if(this.api.datacenter.Player.currentUseObject != null && this.api.datacenter.Basics.gfx_canLaunch)
            {
               this.api.network.Items.use(this.api.datacenter.Player.currentUseObject.ID,this.mapHandler.getCellData(mcCell.num).spriteOnID,mcCell.num);
            }
            this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_RELEASE);
            this.api.gfx.clearPointer();
            this.unSelect(true);
            this.api.datacenter.Player.reset();
            this.api.ui.removeCursor();
            this.api.datacenter.Game.setInteractionType("move");
            return undefined;
         case 6:
            if(this.api.datacenter.Game.isFight)
            {
               if(mcCell.num != undefined)
               {
                  this.api.network.Game.setFlag(mcCell.num);
               }
               this.api.gfx.clearPointer();
               this.api.gfx.unSelectAllButOne("startPosition");
               this.api.ui.removeCursor();
               if(this.api.datacenter.Game.isRunning && this.api.datacenter.Game.currentPlayerID == this.api.datacenter.Player.ID)
               {
                  this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_RELEASE_OVER_OUT);
                  this.api.datacenter.Game.setInteractionType("move");
                  break;
               }
               this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_RELEASE);
               this.api.datacenter.Game.setInteractionType("place");
            }
      }
      return undefined;
   }
   function onCellRollOver(mcCell)
   {
      if(this.api.kernel.TutorialManager.isTutorialMode)
      {
         this.api.kernel.TutorialManager.onWaitingCase({code:"CELL_OVER",params:[mcCell.num]});
         return undefined;
      }
      if(this.api.datacenter.Game.isRunning && (!this.api.datacenter.Player.isCurrentPlayer && this.api.datacenter.Game.interactionType != 6))
      {
         return undefined;
      }
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      switch(this.api.datacenter.Game.interactionType)
      {
         case 1:
            _loc3_ = this.api.datacenter.Player;
            _loc4_ = _loc3_.data;
            _loc5_ = this.mapHandler.getCellData(mcCell.num).spriteOnID;
            _loc6_ = this.api.datacenter.Sprites.getItemAt(_loc5_);
            if(_loc6_ != undefined)
            {
               this.showSpriteInfos(_loc6_);
            }
            _loc14_ = this.api.datacenter.Game.isControllingInvocation ? this.api.datacenter.Game.controlledInvocationCell : _loc4_.cellNum;
            _loc15_ = this.api.datacenter.Game.isControllingInvocation ? this.api.datacenter.Game.controlledInvocationPM : _loc4_.MP;
            if(ank.battlefield.utils.Pathfinding.checkRange(this.mapHandler,_loc14_,mcCell.num,false,0,_loc15_,0))
            {
               this.api.datacenter.Player.InteractionsManager.setState(this.api.datacenter.Game.isFight);
               this.api.datacenter.Player.InteractionsManager.calculatePath(this.mapHandler,mcCell.num,false,this.api.datacenter.Game.isFight);
               _loc13_ = this.api.datacenter.Basics.interactionsManager_path;
               if(_loc13_ && _loc13_.length)
               {
                  this.drawPathIndexNumbers(_loc13_);
                  return undefined;
               }
               this.clearPathIndexNumbers();
               return undefined;
            }
            delete this.api.datacenter.Basics.interactionsManager_path;
            this.clearPathIndexNumbers();
            return undefined;
            break;
         case 2:
         case 3:
            _loc7_ = this.api.datacenter.Player;
            _loc8_ = _loc7_.data;
            _loc9_ = this.api.datacenter.Game.isControllingInvocation ? this.api.datacenter.Game.controlledInvocationCell : _loc8_.cellNum;
            _loc10_ = _loc7_.currentUseObject;
            _loc11_ = _loc7_.SpellsManager;
            _loc12_ = _loc10_.rangeModerator;
            this.api.gfx.mapHandler.resetEmptyCells();
            this.api.datacenter.Basics.gfx_canLaunch = _loc11_.checkCanLaunchSpellOnCell(this.mapHandler,_loc10_,this.mapHandler.getCellData(mcCell.num),_loc12_,false);
            if(this.api.datacenter.Basics.gfx_canLaunch)
            {
               this.api.ui.setCursorForbidden(false);
               this.drawPointer(mcCell.num);
               return undefined;
            }
            this.api.ui.setCursorForbidden(true,dofus.Constants.FORBIDDEN_FILE);
            return undefined;
            break;
         case 5:
         case 6:
            this.api.datacenter.Basics.gfx_canLaunch = true;
            this.api.ui.setCursorForbidden(false);
            this.drawPointer(mcCell.num);
      }
      return undefined;
   }
   function onCellRollOut(mcCell)
   {
      if(this.api.kernel.TutorialManager.isTutorialMode)
      {
         this.api.kernel.TutorialManager.onWaitingCase({code:"CELL_OUT",params:[mcCell.num]});
         return undefined;
      }
      if(this.api.datacenter.Game.isRunning && (!this.api.datacenter.Player.isCurrentPlayer && this.api.datacenter.Game.interactionType != 6))
      {
         return undefined;
      }
      switch(this.api.datacenter.Game.interactionType)
      {
         case 1:
            this.hideSpriteInfos();
            this.unSelect(true);
            break;
         case 2:
         case 3:
            this.api.ui.setCursorForbidden(true,dofus.Constants.FORBIDDEN_FILE);
            this.hidePointer();
            this.api.datacenter.Basics.gfx_canLaunch = false;
            this.hideSpriteInfos();
            break;
         case 5:
         case 6:
            this.api.ui.setCursorForbidden(true,dofus.Constants.FORBIDDEN_FILE);
            this.api.datacenter.Basics.gfx_canLaunch = false;
            this.hidePointer();
         default:
            return undefined;
      }
      this.clearPathIndexNumbers();
   }
   function onSpriteRelease(mcSprite, bRightClick)
   {
      if(bRightClick == undefined)
      {
         bRightClick = false;
      }
      var _loc5_ = mcSprite.data;
      var _loc6_ = _loc5_.id;
      if(this.api.kernel.TutorialManager.isTutorialMode)
      {
         this.api.kernel.TutorialManager.onWaitingCase({code:"SPRITE_RELEASE",params:[_loc5_.id]});
         return undefined;
      }
      if(_loc5_.hasParent)
      {
         this.onSpriteRelease(_loc5_.linkedParent.mc);
         return undefined;
      }
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
      var _loc18_;
      var _loc19_;
      var _loc20_;
      var _loc21_;
      var _loc22_;
      var _loc23_;
      var _loc24_;
      var _loc25_;
      var _loc26_;
      var _loc27_;
      var _loc28_;
      var _loc29_;
      var _loc30_;
      var _loc31_;
      var _loc32_;
      var _loc33_;
      var _loc34_;
      var _loc35_;
      var _loc36_;
      var _loc37_;
      var _loc38_;
      var _loc39_;
      var _loc40_;
      var _loc41_;
      var _loc42_;
      var _loc43_;
      var _loc44_;
      var _loc45_;
      var _loc46_;
      var _loc47_;
      var _loc48_;
      var _loc49_;
      var _loc50_;
      var _loc51_;
      var _loc52_;
      var _loc53_;
      var _loc54_;
      if((_loc7_ = this.api.datacenter.Game.interactionType) !== 5)
      {
         if(_loc5_ instanceof dofus.datacenter.Mutant && !_loc5_.showIsPlayer)
         {
            if(!this.api.datacenter.Game.isRunning)
            {
               if(this.api.datacenter.Player.isMutant)
               {
                  return undefined;
               }
            }
            _loc8_ = this.mapHandler.getCellData(_loc5_.cellNum).mc;
            this.onCellRelease(_loc8_);
         }
         else if(_loc5_ instanceof dofus.datacenter.Character || _loc5_ instanceof dofus.datacenter.Mutant && _loc5_.showIsPlayer)
         {
            if(this.api.datacenter.Game.isFight && (this.api.datacenter.Game.isRunning && !(this.api.datacenter.Player.isAuthorized && (this.api.datacenter.Game.interactionType == dofus.datacenter.Game.INTERACTION_TYPE_MOVE && this.api.datacenter.Player.currentUseObject == null))))
            {
               _loc9_ = this.mapHandler.getCellData(_loc5_.cellNum).mc;
               this.onCellRelease(_loc9_);
               return undefined;
            }
            if(Key.isDown(Key.CONTROL))
            {
               _loc10_ = this.mapHandler.getCellData(_loc5_.cellNum).allSpritesOn;
               this.api.kernel.GameManager.showCellPlayersPopupMenu(_loc10_);
            }
            else
            {
               this.api.kernel.GameManager.showPlayerPopupMenu(_loc5_);
            }
         }
         else if(_loc5_ instanceof dofus.datacenter.NonPlayableCharacter)
         {
            if(this.api.datacenter.Player.cantSpeakNPC)
            {
               return undefined;
            }
            _loc11_ = _loc5_.actions;
            if(_loc11_ != undefined && _loc11_.length != 0)
            {
               _loc12_ = this.api.ui.createPopupMenu();
               if(Key.isDown(Key.SHIFT) || bRightClick)
               {
                  _loc13_ = [6,3,1,2,4,5,7,8];
                  _loc14_ = 0;
                  while(_loc14_ < _loc13_.length)
                  {
                     _loc15_ = _loc11_.findFirstItem("actionId",_loc13_[_loc14_]).item;
                     if(_loc15_ != undefined)
                     {
                        _loc16_ = _loc15_.action;
                        _loc17_ = _loc16_.method;
                        _loc18_ = _loc16_.object;
                        _loc19_ = _loc16_.params;
                        _loc17_.apply(_loc18_,_loc19_);
                        break;
                     }
                     _loc14_ += 1;
                  }
               }
               else
               {
                  _loc20_ = _loc11_.length;
                  while(_loc20_-- > 0)
                  {
                     _loc21_ = _loc11_[_loc20_];
                     _loc22_ = _loc21_.actionId;
                     _loc23_ = _loc21_.action;
                     _loc24_ = _loc23_.method;
                     _loc25_ = _loc23_.object;
                     _loc26_ = _loc23_.params;
                     _loc12_.addItem(_loc21_.name,_loc25_,_loc24_,_loc26_);
                  }
                  _loc12_.show(_root._xmouse,_root._ymouse);
               }
            }
         }
         else if(_loc5_ instanceof dofus.datacenter.Team)
         {
            _loc27_ = this.api.datacenter.Player.data.alignment.index;
            _loc28_ = _loc5_.alignment.index;
            _loc29_ = _loc5_.enemyTeam.alignment.index;
            _loc30_ = _loc5_.challenge.fightType;
            _loc31_ = false;
            switch(_loc30_)
            {
               case 0:
                  switch(_loc5_.type)
                  {
                     case 0:
                     case 2:
                        _loc31_ = this.api.datacenter.Player.canChallenge && (!this.api.datacenter.Player.isMutant || this.api.datacenter.Player.canAttackDungeonMonstersWhenMutant);
                  }
                  break;
               case 1:
               case 2:
                  switch(_loc5_.type)
                  {
                     case 0:
                     case 1:
                        if(_loc27_ == _loc28_)
                        {
                           _loc31_ = !this.api.datacenter.Player.isMutant;
                           break;
                        }
                        _loc31_ = this.api.lang.getAlignmentCanJoin(_loc27_,_loc28_) && (this.api.lang.getAlignmentCanAttack(_loc27_,_loc29_) && !this.api.datacenter.Player.isMutant);
                  }
                  break;
               case 3:
                  switch(_loc5_.type)
                  {
                     case 0:
                        _loc31_ = !this.api.datacenter.Player.isMutant || this.api.datacenter.Player.canAttackDungeonMonstersWhenMutant;
                        break;
                     case 1:
                        _loc31_ = false;
                  }
                  break;
               case 4:
                  switch(_loc5_.type)
                  {
                     case 0:
                        _loc31_ = !this.api.datacenter.Player.isMutant || this.api.datacenter.Player.canAttackDungeonMonstersWhenMutant;
                        break;
                     case 1:
                        _loc31_ = false;
                  }
                  break;
               case 5:
                  switch(_loc5_.type)
                  {
                     case 0:
                        _loc31_ = !this.api.datacenter.Player.isMutant && !this.api.datacenter.Player.cantInteractWithTaxCollector;
                        break;
                     case 3:
                        _loc31_ = false;
                  }
                  break;
               case 6:
                  switch(_loc5_.type)
                  {
                     case 0:
                        _loc31_ = !this.api.datacenter.Player.isMutant || this.api.datacenter.Player.canAttackDungeonMonstersWhenMutant;
                        break;
                     case 2:
                        _loc31_ = this.api.datacenter.Player.isMutant && !this.api.datacenter.Player.canAttackDungeonMonstersWhenMutant == true;
                  }
            }
            if(_loc31_)
            {
               _loc32_ = true;
               _loc33_ = this.api.ui.createPopupMenu();
               _loc34_ = this.api.lang.getMapMaxTeam(this.api.datacenter.Map.id);
               _loc35_ = this.api.lang.getMapMaxChallenge(this.api.datacenter.Map.id);
               if(_loc5_.challenge.count >= _loc35_)
               {
                  _loc33_.addItem(this.api.lang.getText("CHALENGE_FULL"));
               }
               else if(_loc5_.count >= _loc34_)
               {
                  _loc33_.addItem(this.api.lang.getText("TEAM_FULL"));
               }
               else if(Key.isDown(Key.SHIFT) || bRightClick)
               {
                  _loc32_ = false;
                  this.api.network.GameActions.joinChallenge(_loc5_.challenge.id,_loc5_.id);
                  this.api.ui.hideTooltip();
               }
               else
               {
                  _loc33_.addItem(this.api.lang.getText("JOIN_SMALL"),this.api.network.GameActions,this.api.network.GameActions.joinChallenge,[_loc5_.challenge.id,_loc5_.id]);
               }
               if(_loc32_)
               {
                  _loc33_.show(_root._xmouse,_root._ymouse);
               }
            }
         }
         else if(_loc5_ instanceof dofus.datacenter.ParkMount)
         {
            if(_loc5_.ownerName == this.api.datacenter.Player.Name || this.api.datacenter.Map.firstMountPark.guildName == this.api.datacenter.Player.guildInfos.name && this.api.datacenter.Player.guildInfos.playerRights.canManageOtherMount)
            {
               if(Key.isDown(Key.SHIFT) || bRightClick)
               {
                  this.api.network.Mount.parkMountData(_loc5_.id);
               }
               else
               {
                  _loc36_ = this.api.ui.createPopupMenu();
                  _loc36_.addStaticItem(this.api.lang.getText("MOUNT_OF",[_loc5_.ownerName]));
                  _loc36_.addItem(this.api.lang.getText("VIEW_MOUNT_DETAILS"),this.api.network.Mount,this.api.network.Mount.parkMountData,[_loc5_.id]);
                  _loc36_.show(_root._xmouse,_root._ymouse);
               }
            }
         }
         else if(_loc5_ instanceof dofus.datacenter.Creature)
         {
            _loc37_ = this.mapHandler.getCellData(_loc5_.cellNum).mc;
            this.onCellRelease(_loc37_);
         }
         else if(_loc5_ instanceof dofus.datacenter.MonsterGroup || _loc5_ instanceof dofus.datacenter.Monster)
         {
            if(_loc5_ instanceof dofus.datacenter.Monster && this.api.kernel.GameManager.isInMyTeam(_loc5_))
            {
               this.api.kernel.GameManager.showMonsterPopupMenu(_loc5_);
            }
            if(!this.api.datacenter.Player.isMutant || (this.api.datacenter.Player.canAttackDungeonMonstersWhenMutant || this.api.datacenter.Player.canAttackMonstersAnywhereWhenMutant))
            {
               _loc38_ = this.mapHandler.getCellData(_loc5_.cellNum);
               _loc39_ = _loc38_.mc;
               if(!Key.isDown(Key.SHIFT) && (!bRightClick && (!this.api.datacenter.Game.isFight && _loc5_ instanceof dofus.datacenter.MonsterGroup)))
               {
                  _loc40_ = _loc38_.isTrigger;
                  if(!_loc40_ && this.api.kernel.OptionsManager.getOption("ViewAllMonsterInGroup") == true)
                  {
                     _loc41_ = this.api.ui.createPopupMenu();
                     _loc41_.addItem(this.api.lang.getText("ATTACK"),this,this.onCellRelease,[_loc39_]);
                     _loc41_.show();
                  }
                  else
                  {
                     this.onCellRelease(_loc39_);
                  }
               }
               else
               {
                  this.onCellRelease(_loc39_);
               }
            }
         }
         else if(_loc5_ instanceof dofus.datacenter.OfflineCharacter)
         {
            if(!this.api.datacenter.Player.isMutant || this.api.datacenter.Player.canAttackDungeonMonstersWhenMutant)
            {
               if(!this.api.datacenter.Player.canExchange)
               {
                  return undefined;
               }
               if(Key.isDown(Key.SHIFT) || bRightClick)
               {
                  this.api.kernel.GameManager.startExchange(4,_loc5_.id,_loc5_.cellNum);
               }
               else
               {
                  _loc42_ = _loc5_.name;
                  if(this.api.datacenter.Player.isAuthorized)
                  {
                     _loc43_ = this.api.kernel.AdminManager.getAdminPopupMenu(_loc42_,false);
                  }
                  else
                  {
                     _loc43_ = this.api.ui.createPopupMenu();
                  }
                  _loc43_.addStaticItem(this.api.lang.getText("SHOP") + " " + this.api.lang.getText("OF") + " " + _loc5_.name);
                  _loc43_.addItem(this.api.lang.getText("BUY"),this.api.kernel.GameManager,this.api.kernel.GameManager.startExchange,[4,_loc5_.id,_loc5_.cellNum]);
                  if(_loc5_.characterID != undefined && _loc5_.name != undefined)
                  {
                     _loc43_.addItem(this.api.lang.getText("REPORT_PLAYER"),this.api.kernel.GameManager,this.api.kernel.GameManager.reportPlayer,[_loc5_.characterID,_loc5_.name,true]);
                  }
                  _loc44_ = 2;
                  if(this.api.datacenter.Map.isMyHome)
                  {
                     _loc43_.addItem(this.api.lang.getText("KICKOFF"),this.api.network.Basics,this.api.network.Basics.kick,[_loc5_.cellNum]);
                     _loc44_ += 1;
                  }
                  if(this.api.datacenter.Player.isAuthorized)
                  {
                     _loc45_ = 0;
                     while(_loc45_ < _loc44_)
                     {
                        _loc43_.items.unshift(_loc43_.items.pop());
                        _loc45_ += 1;
                     }
                  }
                  _loc43_.show(_root._xmouse,_root._ymouse,true);
               }
            }
         }
         else if(_loc5_ instanceof dofus.datacenter.TaxCollector)
         {
            if(!this.api.datacenter.Player.isMutant)
            {
               if(this.api.datacenter.Player.cantInteractWithTaxCollector)
               {
                  return undefined;
               }
               if(this.api.datacenter.Game.isFight)
               {
                  _loc46_ = this.mapHandler.getCellData(_loc5_.cellNum).mc;
                  this.onCellRelease(_loc46_);
               }
               else if(Key.isDown(Key.SHIFT) || bRightClick)
               {
                  this.api.network.Dialog.create(_loc6_);
               }
               else
               {
                  _loc47_ = this.api.datacenter.Player.guildInfos.playerRights;
                  _loc48_ = _loc5_.guildName == this.api.datacenter.Player.guildInfos.name;
                  _loc49_ = _loc47_.canCollect || _loc5_.isMine && _loc47_.canCollectOwnTaxCollector;
                  _loc50_ = this.api.ui.createPopupMenu();
                  _loc50_.addItem(this.api.lang.getText("SPEAK"),this.api.network.Dialog,this.api.network.Dialog.create,[_loc6_]);
                  _loc50_.addItem(this.api.lang.getText("COLLECT_TAX"),this.api.kernel.GameManager,this.api.kernel.GameManager.startExchange,[8,_loc6_],_loc48_ && _loc49_);
                  _loc50_.addItem(this.api.lang.getText("ATTACK"),this.api.network.GameActions,this.api.network.GameActions.attackTaxCollector,[[_loc6_]],!_loc48_);
                  _loc50_.show(_root._xmouse,_root._ymouse);
               }
            }
         }
         else if(_loc5_ instanceof dofus.datacenter.PrismSprite)
         {
            if(!this.api.datacenter.Player.isMutant)
            {
               if(this.api.datacenter.Game.isFight)
               {
                  _loc51_ = this.mapHandler.getCellData(_loc5_.cellNum).mc;
                  this.onCellRelease(_loc51_);
               }
               else
               {
                  _loc52_ = this.api.datacenter.Player.alignment.index == 0;
                  _loc53_ = this.api.datacenter.Player.alignment.compareTo(_loc5_.alignment) == 0;
                  if((Key.isDown(Key.SHIFT) || bRightClick) && _loc53_)
                  {
                     this.api.network.GameActions.usePrism([_loc6_]);
                  }
                  else
                  {
                     _loc54_ = this.api.ui.createPopupMenu();
                     _loc54_.addItem(this.api.lang.getText("USE_WORD"),this.api.network.GameActions,this.api.network.GameActions.usePrism,[[_loc6_]],_loc53_);
                     _loc54_.addItem(this.api.lang.getText("ATTACK"),this.api.network.GameActions,this.api.network.GameActions.attackPrism,[[_loc6_]],!_loc53_ && !_loc52_);
                     _loc54_.show(_root._xmouse,_root._ymouse);
                  }
               }
            }
         }
      }
      else
      {
         if(this.api.datacenter.Player.currentUseObject != null && this.api.datacenter.Basics.gfx_canLaunch)
         {
            this.api.network.Items.use(this.api.datacenter.Player.currentUseObject.ID,_loc5_.id,_loc5_.cellNum);
         }
         this.api.gfx.setInteraction(ank.battlefield.Constants.INTERACTION_CELL_RELEASE);
         this.api.gfx.clearPointer();
         this.unSelect(true);
         this.api.datacenter.Player.reset();
         this.api.ui.removeCursor();
         this.api.datacenter.Game.setInteractionType("move");
      }
   }
   function onSpriteRollOver(mcSprite, bFakeEvent)
   {
      if(!bFakeEvent)
      {
         this._rollOverMcSprite = mcSprite;
      }
      if(_root._xscale != 100)
      {
         return undefined;
      }
      var _loc5_ = mcSprite.data;
      var _loc6_ = dofus.Constants.OVERHEAD_TEXT_OTHER;
      if(!_loc5_.isVisible)
      {
         this.showSpriteInfos(_loc5_);
         return undefined;
      }
      if(_loc5_.isClear)
      {
         return undefined;
      }
      if(_loc5_.hasParent)
      {
         this.onSpriteRollOver(_loc5_.linkedParent.mc,bFakeEvent);
         return undefined;
      }
      var _loc7_;
      if(this.api.datacenter.Game.isRunning || this.api.datacenter.Game.interactionType == 5)
      {
         _loc7_ = this.mapHandler.getCellData(_loc5_.cellNum).mc;
         if(_loc5_.isVisible)
         {
            this.onCellRollOver(_loc7_);
         }
      }
      var _loc8_ = _loc5_.name;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      if(_loc5_ instanceof dofus.datacenter.Mutant && _loc5_.showIsPlayer)
      {
         if(this.api.datacenter.Game.isRunning)
         {
            if(this.api.kernel.OptionsManager.getOption("ViewHPAsBar"))
            {
               _loc8_ = "";
               this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.HealthBarOverHead,[_loc5_,100]);
            }
            else
            {
               _loc8_ = _loc5_.playerName + " (" + _loc5_.LP + ")";
            }
            this.showSpriteInfos(_loc5_);
         }
         else
         {
            _loc8_ = _loc5_.playerName + " [" + _loc5_.monsterName + " (" + _loc5_.Level + ")]";
         }
      }
      else if(_loc5_ instanceof dofus.datacenter.Mutant || (_loc5_ instanceof dofus.datacenter.Creature || _loc5_ instanceof dofus.datacenter.Monster))
      {
         _loc6_ = dofus.Constants.NPC_ALIGNMENT_COLOR[_loc5_.alignment.index];
         if(this.api.datacenter.Game.isRunning)
         {
            if(this.api.kernel.OptionsManager.getOption("ViewHPAsBar"))
            {
               _loc8_ = "";
               this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.HealthBarOverHead,[_loc5_,100]);
            }
            else
            {
               _loc8_ += " (" + _loc5_.LP + ")";
            }
            this.showSpriteInfos(_loc5_);
         }
         else
         {
            _loc8_ += " (" + _loc5_.Level + ")";
         }
      }
      else if(_loc5_ instanceof dofus.datacenter.Character)
      {
         _loc6_ = dofus.Constants.OVERHEAD_TEXT_CHARACTER;
         _loc9_ = dofus.Constants.DEMON_ANGEL_FILE;
         if(_loc5_.alignment.fallenAngelDemon)
         {
            _loc9_ = dofus.Constants.FALLEN_DEMON_ANGEL_FILE;
         }
         _loc10_ = !_loc5_.haveFakeAlignement ? _loc5_.alignment.index : _loc5_.fakeAlignment.index;
         if(_loc5_.rank.value > 0)
         {
            if(_loc10_ == 1)
            {
               _loc11_ = _loc5_.rank.value;
            }
            else if(_loc10_ == 2)
            {
               _loc11_ = 10 + _loc5_.rank.value;
            }
            else if(_loc10_ == 3)
            {
               _loc11_ = 20 + _loc5_.rank.value;
            }
         }
         if(this.api.datacenter.Game.isRunning)
         {
            this.addSpriteOverHeadItem(_loc5_.id,"effects",dofus.graphics.battlefield.EffectsOverHead,[_loc5_]);
            if(this.api.kernel.OptionsManager.getOption("ViewHPAsBar"))
            {
               _loc8_ = "";
               this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.HealthBarOverHead,[_loc5_,100,_loc9_,_loc11_]);
            }
            else
            {
               _loc8_ += " (" + _loc5_.LP + ")";
            }
            this.showSpriteInfos(_loc5_);
         }
         else if(this.api.datacenter.Game.isFight)
         {
            _loc8_ += " (" + _loc5_.Level + ")";
         }
         _loc12_ = _loc5_.title;
         if(_loc5_.guildName != undefined && _loc5_.guildName.length != 0)
         {
            _loc8_ = "";
            this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.GuildOverHead,[_loc5_.guildName,_loc5_.name,_loc5_.emblem,_loc9_,_loc11_,_loc5_.pvpGain,_loc12_,"clips/ornamentos.swf",_loc5_.ornamento],undefined,true);
         }
      }
      else if(_loc5_ instanceof dofus.datacenter.TaxCollector)
      {
         if(this.api.datacenter.Game.isRunning)
         {
            if(this.api.kernel.OptionsManager.getOption("ViewHPAsBar"))
            {
               _loc8_ = "";
               this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.HealthBarOverHead,[_loc5_,100]);
            }
            else
            {
               _loc8_ += " (" + _loc5_.LP + ")";
            }
            this.showSpriteInfos(_loc5_);
         }
         else if(this.api.datacenter.Game.isFight)
         {
            _loc8_ += " (" + _loc5_.Level + ")";
         }
         else
         {
            _loc8_ = "";
            this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.GuildOverHead,[_loc5_.guildName,_loc5_.name,_loc5_.emblem]);
         }
      }
      else if(_loc5_ instanceof dofus.datacenter.PrismSprite)
      {
         _loc9_ = dofus.Constants.DEMON_ANGEL_FILE;
         if(_loc5_.alignment.value > 0)
         {
            if(_loc5_.alignment.index == 1)
            {
               _loc11_ = _loc5_.alignment.value;
            }
            else if(_loc5_.alignment.index == 2)
            {
               _loc11_ = 10 + _loc5_.alignment.value;
            }
            else if(_loc5_.alignment.index == 3)
            {
               _loc11_ = 20 + _loc5_.alignment.value;
            }
         }
         _loc6_ = dofus.Constants.NPC_ALIGNMENT_COLOR[_loc5_.alignment.index];
         this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.TextOverHead,[_loc8_,_loc9_,_loc6_,_loc11_]);
      }
      else if(_loc5_ instanceof dofus.datacenter.ParkMount)
      {
         _loc6_ = dofus.Constants.OVERHEAD_TEXT_CHARACTER;
         _loc8_ = this.api.lang.getText("MOUNT_PARK_OVERHEAD",[_loc5_.modelName,_loc5_.level,_loc5_.ownerName]);
         this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.TextOverHead,[_loc8_,_loc9_,_loc6_,_loc11_]);
      }
      else if(_loc5_ instanceof dofus.datacenter.OfflineCharacter)
      {
         _loc6_ = dofus.Constants.OVERHEAD_TEXT_CHARACTER;
         _loc8_ = "";
         this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.OfflineOverHead,[_loc5_]);
      }
      else if(_loc5_ instanceof dofus.datacenter.NonPlayableCharacter)
      {
         _loc13_ = this.api.datacenter.Map;
         _loc14_ = this.api.datacenter.Subareas.getItemAt(_loc13_.subarea);
         if(_loc14_ != undefined)
         {
            _loc6_ = dofus.Constants.NPC_ALIGNMENT_COLOR[_loc14_.alignment.index];
         }
      }
      else if(_loc5_ instanceof dofus.datacenter.MonsterGroup || _loc5_ instanceof dofus.datacenter.Team)
      {
         if(_loc5_.alignment.index != -1)
         {
            _loc6_ = dofus.Constants.NPC_ALIGNMENT_COLOR[_loc5_.alignment.index];
         }
         _loc15_ = _loc5_.challenge.fightType;
         if(_loc5_.isVisible && (_loc5_ instanceof dofus.datacenter.MonsterGroup || _loc5_.type == 1 && (_loc15_ == 2 || (_loc15_ == 3 || _loc15_ == 4))))
         {
            if(_loc8_ != "")
            {
               _loc16_ = dofus.Constants.OVERHEAD_TEXT_TITLE;
               this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.TextWithTitleOverHead,[_loc8_,_loc9_,_loc6_,_loc11_,this.api.lang.getText("LEVEL") + " " + _loc5_.totalLevel,_loc16_,_loc5_.bonusValue]);
            }
            this.selectSprite(_loc5_.id,true);
            return undefined;
         }
      }
      if(_loc8_ != "")
      {
         this.addSpriteOverHeadItem(_loc5_.id,"text",dofus.graphics.battlefield.TextOverHead,[_loc8_,_loc9_,_loc6_,_loc11_,_loc5_,_loc12_,"clips/others/css.swf",_loc5_.ornamento]);
      }
      this.selectSprite(_loc5_.id,true);
      this.clearPathIndexNumbers();
   }
   function onSpriteRollOut(mcSprite, bFakeEvent)
   {
      if(!bFakeEvent)
      {
         this._rollOverMcSprite = undefined;
      }
      var _loc4_ = mcSprite.data;
      if(this.api.gfx.spriteHandler.isShowingMonstersTooltip && _loc4_ instanceof dofus.datacenter.MonsterGroup)
      {
         return undefined;
      }
      if(_loc4_.hasParent)
      {
         this.onSpriteRollOut(_loc4_.linkedParent.mc);
         return undefined;
      }
      var _loc5_;
      if(this.api.datacenter.Game.isRunning || this.api.datacenter.Game.interactionType == 5)
      {
         this.hideSpriteInfos();
         _loc5_ = this.mapHandler.getCellData(_loc4_.cellNum).mc;
         this.onCellRollOut(_loc5_);
      }
      this.removeSpriteOverHeadLayer(_loc4_.id,"text");
      this.removeSpriteOverHeadLayer(_loc4_.id,"effects");
      this.selectSprite(_loc4_.id,false);
   }
   function onObjectRelease(mcObject, bRightClick)
   {
      if(bRightClick == undefined)
      {
         bRightClick = false;
      }
      this.api.ui.hideTooltip();
      var _loc6_ = mcObject.cellData;
      var _loc7_ = _loc6_.mc;
      var _loc8_ = _loc6_.layerObject2Num;
      if(this.api.kernel.TutorialManager.isTutorialMode)
      {
         this.api.kernel.TutorialManager.onWaitingCase({code:"OBJECT_RELEASE",params:[_loc6_.num,_loc8_]});
         return undefined;
      }
      var _loc9_ = _loc6_.layerObjectExternalData;
      var _loc10_;
      if(_loc9_ != undefined)
      {
         if(_loc9_.rideItemDurability != undefined)
         {
            if(this.api.datacenter.Map.firstMountPark.isMine(this.api))
            {
               _loc10_ = this.api.ui.createPopupMenu();
               _loc10_.addStaticItem(_loc9_.name);
               _loc10_.addItem(this.api.lang.getText("REMOVE"),this.api.network.Mount,this.api.network.Mount.removeObjectInPark,[_loc7_.num]);
               _loc10_.show(_root._xmouse,_root._ymouse);
               return undefined;
            }
         }
      }
      var _loc11_;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      var _loc18_;
      var _loc19_;
      var _loc20_;
      var _loc21_;
      var _loc22_;
      var _loc23_;
      var _loc24_;
      var _loc25_;
      var _loc26_;
      var _loc27_;
      var _loc28_;
      var _loc29_;
      var _loc30_;
      var _loc31_;
      var _loc32_;
      var _loc33_;
      var _loc34_;
      var _loc35_;
      var _loc36_;
      var _loc37_;
      var _loc38_;
      var _loc39_;
      var _loc40_;
      var _loc41_;
      if(!_global.isNaN(_loc8_) && (this.api.datacenter.Player.canUseInteractiveObjects && this.api.datacenter.Game.interactionType != 5))
      {
         _loc11_ = this.api.lang.getInteractiveObjectDataByGfxText(_loc8_);
         _loc12_ = _loc11_.n;
         _loc13_ = _loc11_.sk;
         _loc14_ = _loc11_.t;
         switch(_loc14_)
         {
            case 1:
            case 2:
            case 3:
            case 4:
            case 7:
            case 10:
            case 12:
            case 14:
            case 15:
               _loc15_ = _loc14_ == 1;
               if(_loc15_)
               {
                  _loc16_ = this.api.mouseClicksMemorizer.getMouseClickForGather(2);
                  if(_loc16_ != undefined)
                  {
                     _loc17_ = getTimer() - _loc16_.time;
                     _loc18_ = _loc17_ < dofus.Constants.CLICK_MIN_DELAY;
                     if(_loc18_)
                     {
                        _loc19_ = mcObject.hitTest(_loc16_.nX,_loc16_.nY,true);
                        if(_loc19_)
                        {
                           this.api.kernel.showMessage(undefined,this.api.lang.getText("SRV_MSG_0"),"ERROR_CHAT");
                           return undefined;
                        }
                     }
                  }
                  this.api.mouseClicksMemorizer.resetForGather();
               }
               _loc20_ = this.api.datacenter.Player.currentJobID != undefined;
               if(_loc20_)
               {
                  _loc21_ = this.api.datacenter.Player.Jobs.findFirstItem("id",this.api.datacenter.Player.currentJobID).item.skills;
               }
               else
               {
                  _loc21_ = new ank.utils.ExtendedArray();
               }
               _loc22_ = true;
               _loc23_ = this.api.ui.createPopupMenu();
               _loc23_.addStaticItem(_loc12_);
               for(var _loc42_ in _loc13_)
               {
                  _loc24_ = _loc13_[_loc42_];
                  _loc25_ = new dofus.datacenter.Skill(_loc24_);
                  _loc26_ = _loc21_.findFirstItem("id",_loc24_).index != -1;
                  _loc27_ = this.api.datacenter.Player.Level <= dofus.Constants.NOVICE_LEVEL;
                  _loc28_ = _loc25_.getState(_loc26_,false,false,false,false,_loc27_);
                  if(_loc28_ != "X")
                  {
                     _loc29_ = _loc28_ == "V";
                     if(_loc29_ && ((Key.isDown(Key.SHIFT) || bRightClick) && (_loc24_ != 44 && _loc14_ != 1)))
                     {
                        this.api.kernel.GameManager.useRessource(_loc7_,_loc7_.num,_loc24_);
                        _loc22_ = false;
                        return undefined;
                     }
                     if(_root._xscale != 100 && _loc14_ == 1)
                     {
                        return undefined;
                     }
                     _loc23_.addItem(_loc25_.description,this.api.kernel.GameManager,this.api.kernel.GameManager.useRessource,[_loc7_,_loc7_.num,_loc24_],_loc29_);
                  }
               }
               if(_loc22_)
               {
                  _loc23_.isGatherPopupMenu = _loc15_;
                  if(_loc23_.isGatherPopupMenu && _loc14_ == 1)
                  {
                     _loc23_.gatherCellNum = _loc7_.num;
                  }
                  _loc23_.show(_root._xmouse,_root._ymouse);
                  return undefined;
               }
               return undefined;
               break;
            case 5:
               _loc30_ = this.api.lang.getHousesDoorText(this.api.datacenter.Map.id,_loc7_.num);
               this.api.kernel.HouseManager.openHouseMenu(_loc12_,_loc30_,_loc13_,_loc7_);
               return undefined;
            case 6:
               _loc31_ = _loc7_.num;
               _loc32_ = this.api.datacenter.Storages.getItemAt(_loc31_);
               _loc33_ = _loc32_.isLocked;
               _loc34_ = this.api.datacenter.Map.isMyHome;
               _loc35_ = true;
               _loc36_ = this.api.ui.createPopupMenu();
               _loc36_.addStaticItem(_loc12_);
               for(_loc42_ in _loc13_)
               {
                  _loc37_ = _loc13_[_loc42_];
                  _loc38_ = new dofus.datacenter.Skill(_loc37_);
                  _loc39_ = _loc38_.getState(true,_loc34_,true,_loc33_);
                  if(_loc39_ != "X")
                  {
                     _loc40_ = _loc39_ == "V";
                     if(_loc40_ && ((Key.isDown(Key.SHIFT) || bRightClick) && (_loc37_ == 104 || _loc37_ == 153)))
                     {
                        this.api.kernel.GameManager.useRessource(_loc7_,_loc7_.num,_loc37_);
                        _loc35_ = false;
                        return undefined;
                     }
                     _loc36_.addItem(_loc38_.description,this.api.kernel.GameManager,this.api.kernel.GameManager.useRessource,[_loc7_,_loc7_.num,_loc37_],_loc40_);
                  }
               }
               if(_loc35_)
               {
                  _loc36_.show(_root._xmouse,_root._ymouse);
                  return undefined;
               }
               return undefined;
               break;
            case 13:
               _loc41_ = this.api.datacenter.Map.firstMountPark;
               this.api.kernel.MountParkManager.openMountParkMenu(_loc12_,_loc13_,_loc7_,_loc41_);
               return undefined;
            default:
               this.onCellRelease(_loc7_);
               return undefined;
         }
      }
      else
      {
         this.onCellRelease(_loc7_);
      }
   }
   function onObjectRollOver(mcObject)
   {
      this._rollOverMcObject = mcObject;
      if(_root._xscale != 100)
      {
         return undefined;
      }
      var _loc4_ = mcObject.cellData;
      var _loc5_ = _loc4_.mc;
      var _loc6_ = _loc4_.layerObject2Num;
      if(this.api.datacenter.Game.interactionType == 5)
      {
         _loc5_ = mcObject.cellData.mc;
         this.onCellRollOver(_loc5_);
      }
      mcObject.select(true);
      var _loc7_ = _loc4_.layerObjectExternalData;
      var _loc8_;
      var _loc9_;
      if(_loc7_ != undefined)
      {
         _loc8_ = _loc7_.name;
         if(_loc7_.rideItemDurability != undefined)
         {
            if(this.api.datacenter.Map.firstMountPark.isMine(this.api))
            {
               _loc8_ += "\n" + this.api.lang.getText("DURABILITY") + " : " + _loc7_.rideItemDurability + "/" + _loc7_.rideItemDurabilityMax;
            }
         }
         _loc9_ = new dofus.datacenter.Character("itemOnCell",ank.battlefield.mc.Sprite,"",_loc5_.num,0,0);
         this.api.datacenter.Sprites.addItemAt("itemOnCell",_loc9_);
         this.api.gfx.addSprite("itemOnCell");
         this.addSpriteOverHeadItem("itemOnCell","text",dofus.graphics.battlefield.TextOverHead,[_loc8_,"",dofus.Constants.OVERHEAD_TEXT_CHARACTER]);
      }
      var _loc10_ = this.api.lang.getInteractiveObjectDataByGfxText(_loc6_);
      var _loc11_ = _loc10_.n;
      var _loc12_ = _loc10_.sk;
      var _loc13_ = _loc10_.t;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      var _loc17_;
      var _loc18_;
      var _loc19_;
      switch(_loc13_)
      {
         case 5:
            _loc14_ = this.api.lang.getHousesDoorText(this.api.datacenter.Map.id,_loc5_.num);
            _loc15_ = this.api.kernel.HouseManager.getHouseInstances(_loc14_);
            _loc16_ = new dofus.datacenter.Character("porte",ank.battlefield.mc.Sprite,"",_loc5_.num,0,0);
            this.api.datacenter.Sprites.addItemAt("porte",_loc16_);
            this.api.gfx.addSprite("porte");
            this.addSpriteOverHeadItem("porte","text",dofus.graphics.battlefield.PropertyOverHead,[_loc15_,"HouseIcon"]);
            return undefined;
         case 13:
            _loc17_ = this.api.datacenter.Map.firstMountPark;
            _loc18_ = new dofus.datacenter.Character("enclos",ank.battlefield.mc.Sprite,"",_loc5_.num,0,0);
            this.api.datacenter.Sprites.addItemAt("enclos",_loc18_);
            this.api.gfx.addSprite("enclos");
            _loc19_ = this.api.datacenter.Map.mountParks;
            this.addSpriteOverHeadItem("enclos","text",dofus.graphics.battlefield.MountParkOverHead,[_loc19_,"FarmIcon"]);
      }
      return undefined;
   }
   function onObjectRollOut(mcObject)
   {
      this._rollOverMcObject = undefined;
      this.api.ui.hideTooltip();
      var _loc3_;
      if(this.api.datacenter.Game.interactionType == 5)
      {
         _loc3_ = mcObject.cellData.mc;
         this.onCellRollOut(_loc3_);
      }
      mcObject.select(false);
      this.removeSpriteOverHeadLayer("enclos","text");
      this.removeSprite("enclos",false);
      this.removeSpriteOverHeadLayer("porte","text");
      this.removeSprite("porte",false);
      this.removeSpriteOverHeadLayer("itemOnCell","text");
      this.removeSprite("itemOnCell",false);
   }
   function showSpriteInfos(oSprite)
   {
      if(!this.api.kernel.OptionsManager.getOption("SpriteInfos"))
      {
         return undefined;
      }
      if(this.api.kernel.OptionsManager.getOption("SpriteMove") && (oSprite.isVisible && this.api.ui.isCursorHidden()))
      {
         this.api.gfx.drawZone(oSprite.cellNum,0,oSprite.MP,"move",dofus.Constants.CELL_MOVE_RANGE_COLOR,"C".charCodeAt(0));
      }
      this.api.ui.getUIComponent("Banner").showRightPanel("BannerSpriteInfos",{data:oSprite},true,true);
   }
   function hideSpriteInfos()
   {
      this.api.ui.getUIComponent("Banner").hideRightPanel(false,true);
      this.api.gfx.clearZoneLayer("move");
   }
   function clearPathIndexNumbers()
   {
      if(!this._pathNumClips)
      {
         return undefined;
      }
      var _loc2_ = 0;
      var _loc3_;
      while(_loc2_ < this._pathNumClips.length)
      {
         _loc3_ = this._pathNumClips[_loc2_];
         if(_loc3_)
         {
            _loc3_.removeMovieClip();
         }
         _loc2_ += 1;
      }
      this._pathNumClips = null;
      return undefined;
   }
   function clearPathIndexNumbers()
   {
      if(!this._pathNumClips)
      {
         return undefined;
      }
      var _loc2_ = 0;
      var _loc3_;
      while(_loc2_ < this._pathNumClips.length)
      {
         _loc3_ = this._pathNumClips[_loc2_];
         if(_loc3_)
         {
            _loc3_.removeMovieClip();
         }
         _loc2_ += 1;
      }
      this._pathNumClips = null;
      return undefined;
   }
   function drawPathIndexNumbers(path)
   {
      this.clearPathIndexNumbers();
      if(!path || !path.length)
      {
         return undefined;
      }
      this._pathNumClips = [];
      var _loc3_ = 0;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      var _loc12_;
      while(_loc3_ < path.length)
      {
         _loc4_ = path[_loc3_].num != undefined ? path[_loc3_].num : path[_loc3_];
         _loc5_ = this.mapHandler.getCellData(_loc4_);
         if(!(!_loc5_ || !_loc5_.mc))
         {
            _loc6_ = this.createEmptyMovieClip("pathNum_" + _loc3_,this.getNextHighestDepth());
            _loc6_.useHandCursor = false;
            _loc6_.createTextField("tf",1,0,0,40,20);
            _loc7_ = _loc6_.tf;
            _loc7_.text = String(_loc3_ + 1);
            _loc8_ = new TextFormat();
            _loc8_.font = "Verdana";
            _loc8_.size = 12;
            _loc8_.color = 16777215;
            _loc8_.bold = true;
            _loc7_.setTextFormat(_loc8_);
            _loc7_.selectable = false;
            _loc7_.embedFonts = true;
            _loc7_.autoSize = "center";
            _loc7_._x = (- _loc7_._width) / 2;
            _loc7_._y = (- _loc7_._height) / 2;
            _loc9_ = _loc5_.mc.getBounds(_loc5_.mc);
            _loc10_ = {x:(_loc9_.xMin + _loc9_.xMax) / 2,y:(_loc9_.yMin + _loc9_.yMax) / 2};
            _loc5_.mc.localToGlobal(_loc10_);
            this.globalToLocal(_loc10_);
            _loc11_ = 4;
            _loc12_ = 0;
            _loc6_._x = _loc10_.x + _loc12_;
            _loc6_._y = _loc10_.y + _loc11_;
            this._pathNumClips.push(_loc6_);
         }
         _loc3_ += 1;
      }
   }
}

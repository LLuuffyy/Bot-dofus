class dofus.graphics.gapi.controls.MouseShortcuts extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnTabItems;
   var _btnTabSpells;
   var _ctr1;
   var _ctr2;
   var _ctr3;
   var _ctr4;
   var _ctr5;
   var _ctr6;
   var _ctr7;
   var _ctr8;
   var _ctr9;
   var _ctr10;
   var _ctr11;
   var _ctr12;
   var _ctr13;
   var _ctr14;
   var _ctrCC;
   var _currentOverContainer;
   var addToQueue;
   var gapi;
   var setMovieClipTransform;
   static var TAB_SPELLS = "Spells";
   static var TAB_ITEMS = "Items";
   static var CLASS_NAME = "MouseShortcuts";
   static var MAX_CONTAINER = 31;
   static var WRONG_STATE_TRANSFORM = {ra:50,rb:0,ga:50,gb:0,ba:70,bb:0};
   var _sCurrentTab = "Items";
   function MouseShortcuts()
   {
      super();
   }
   function get currentOverItem()
   {
      if(this._currentOverContainer != undefined && this._currentOverContainer.contentData != undefined)
      {
         return this._currentOverContainer.contentData;
      }
      return undefined;
   }
   function get currentTab()
   {
      return this._sCurrentTab;
   }
   function set meleeVisible(b)
   {
      this._ctrCC._visible = b;
   }
   function set tabVisible(b)
   {
      this._btnTabItems._visible = b;
      this._btnTabSpells._visible = b;
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.controls.MouseShortcuts.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTexts});
      this.addToQueue({object:this,method:this.addListeners});
   }
   function getContainer(nID)
   {
      return this["_ctr" + nID];
   }
   function setContainer(nID, cContainer)
   {
      this["_ctr" + nID] = cContainer;
   }
   function spellMove(nSpellID, nPosition)
   {
      var _loc4_ = this.api.datacenter.Player.Spells;
      var _loc5_ = _loc4_.findFirstItem("ID",nSpellID).item;
      if(_loc5_ == undefined)
      {
         return undefined;
      }
      var _loc6_ = _loc4_.findFirstItem("position",nPosition).item;
      if(_loc6_ != undefined)
      {
         _loc6_.position = undefined;
      }
      _loc5_.position = nPosition;
      if(this._sCurrentTab != "Spells")
      {
         return undefined;
      }
      this.getContainer(nPosition).contentData = _loc5_;
      this.addToQueue({object:this,method:this.setSpellStateOnAllContainers});
   }
   function spellRemove(nPosition)
   {
      var _loc3_ = this.api.datacenter.Player.Spells;
      var _loc4_ = _loc3_.findFirstItem("position",nPosition).item;
      if(_loc4_ == undefined)
      {
         return undefined;
      }
      _loc4_.position = undefined;
      if(this._sCurrentTab != "Spells")
      {
         return undefined;
      }
      this.getContainer(nPosition).contentData = undefined;
      this.addToQueue({object:this,method:this.setSpellStateOnAllContainers});
   }
   function initData()
   {
   }
   function initTexts()
   {
      this._btnTabSpells.label = this.api.lang.getText("BANNER_TAB_SPELLS");
      this._btnTabItems.label = this.api.lang.getText("BANNER_TAB_ITEMS");
   }
   function addListeners()
   {
      this._btnTabSpells.addEventListener("click",this);
      this._btnTabItems.addEventListener("click",this);
      this._btnTabSpells.addEventListener("over",this);
      this._btnTabItems.addEventListener("over",this);
      this._btnTabSpells.addEventListener("out",this);
      this._btnTabItems.addEventListener("out",this);
      var _loc2_ = 1;
      var _loc3_;
      while(_loc2_ < dofus.graphics.gapi.controls.MouseShortcuts.MAX_CONTAINER)
      {
         _loc3_ = this["_ctr" + _loc2_];
         _loc3_.addEventListener("click",this);
         _loc3_.addEventListener("dblClick",this);
         _loc3_.addEventListener("over",this);
         _loc3_.addEventListener("out",this);
         _loc3_.addEventListener("drag",this);
         _loc3_.addEventListener("drop",this);
         _loc3_.addEventListener("onContentLoaded",this);
         _loc3_.params = {position:_loc2_};
         _loc2_ += 1;
      }
      this._ctrCC.addEventListener("click",this);
      this._ctrCC.addEventListener("over",this);
      this._ctrCC.addEventListener("out",this);
      this.api.kernel.KeyManager.addShortcutsListener("onShortcut",this);
      this.api.datacenter.Player.Spells.addEventListener("modelChanged",this);
      this.api.datacenter.Player.Inventory.addEventListener("modelChanged",this);
      this.api.datacenter.Player.InventoryShortcuts.addEventListener("modelChanged",this);
   }
   function clearSpellStateOnAllContainers()
   {
      var _loc3_ = this.api.datacenter.Player.Spells;
      var _loc4_;
      for(var _loc5_ in _loc3_)
      {
         if(!_global.isNaN(_loc3_[_loc5_].position))
         {
            _loc4_ = this["_ctr" + _loc3_[_loc5_].position];
            _loc4_.showLabel = false;
            this.setMovieClipTransform(_loc4_.content,dofus.Constants.NO_TRANSFORM);
         }
      }
      this.setMovieClipTransform(this._ctrCC.content,dofus.Constants.NO_TRANSFORM);
   }
   function setSpellStateOnAllContainers()
   {
      if(this._sCurrentTab != "Spells")
      {
         return undefined;
      }
      var _loc3_ = this.api.datacenter.Player.Spells;
      for(var _loc4_ in _loc3_)
      {
         if(!_global.isNaN(_loc3_[_loc4_].position))
         {
            this.setSpellStateOnContainer(_loc3_[_loc4_].position);
         }
      }
      this.setSpellStateOnContainer(0);
   }
   function setItemStateOnAllContainers()
   {
      if(this._sCurrentTab != "Items")
      {
         return undefined;
      }
      var _loc3_ = this.api.datacenter.Player.InventoryShortcuts.getItems();
      var _loc4_;
      for(var _loc5_ in _loc3_)
      {
         _loc4_ = _loc3_[_loc5_].position;
         if(!(_global.isNaN(_loc4_) && _loc4_ < 1))
         {
            this.setItemStateOnContainer(_loc4_);
         }
      }
      this.setSpellStateOnContainer(0);
   }
   function updateSpells()
   {
      if(this._sCurrentTab != "Spells")
      {
         return undefined;
      }
      this._ctrCC.contentData = new dofus.datacenter.Spell(dofus.datacenter.CloseCombat.CLOSE_COMBAT_SPELL_ID,1);
      if(this._ctrCC.contentLoaded)
      {
         this._ctrCC.content.applyColors();
      }
      var _loc3_ = [];
      var _loc4_ = 1;
      while(_loc4_ < dofus.graphics.gapi.controls.MouseShortcuts.MAX_CONTAINER)
      {
         _loc3_[_loc4_] = true;
         _loc4_ += 1;
      }
      var _loc5_ = this.api.datacenter.Player.Spells;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      for(var _loc9_ in _loc5_)
      {
         _loc6_ = _loc5_[_loc9_];
         _loc7_ = _loc6_.position;
         if(!_global.isNaN(_loc7_))
         {
            _loc8_ = this["_ctr" + _loc7_];
            _loc8_.contentData = _loc6_;
            if(_loc8_.contentLoaded)
            {
               _loc8_.content.applyColors();
            }
            _loc3_[_loc7_] = false;
         }
      }
      var _loc10_ = 1;
      while(_loc10_ < dofus.graphics.gapi.controls.MouseShortcuts.MAX_CONTAINER)
      {
         if(_loc3_[_loc10_])
         {
            this["_ctr" + _loc10_].contentData = undefined;
         }
         _loc10_ += 1;
      }
      this.addToQueue({object:this,method:this.setSpellStateOnAllContainers});
   }
   function updateItems()
   {
      if(this._sCurrentTab != "Items")
      {
         return undefined;
      }
      var _loc3_ = [];
      var _loc4_ = 1;
      while(_loc4_ < dofus.graphics.gapi.controls.MouseShortcuts.MAX_CONTAINER)
      {
         _loc3_[_loc4_] = true;
         _loc4_ += 1;
      }
      var _loc5_ = this.api.datacenter.Player.InventoryShortcuts.getItems();
      var _loc6_;
      var _loc7_;
      var _loc8_;
      for(var _loc9_ in _loc5_)
      {
         _loc6_ = _loc5_[_loc9_];
         if(!_global.isNaN(_loc6_.position))
         {
            _loc7_ = _loc6_.position;
            _loc8_ = this["_ctr" + _loc7_];
            _loc8_.contentData = _loc6_;
            this.setMovieClipTransform(_loc8_.content,!_loc6_.findRealItem() ? dofus.Constants.INACTIVE_TRANSFORM : dofus.Constants.NO_TRANSFORM);
            _loc3_[_loc7_] = false;
         }
      }
      var _loc10_ = 1;
      while(_loc10_ < dofus.graphics.gapi.controls.MouseShortcuts.MAX_CONTAINER)
      {
         if(_loc3_[_loc10_])
         {
            this["_ctr" + _loc10_].contentData = undefined;
         }
         _loc10_ += 1;
      }
      this.addToQueue({object:this,method:this.setItemStateOnAllContainers});
   }
   function setSpellStateOnContainer(nIndex)
   {
      var _loc3_ = nIndex != 0 ? this["_ctr" + nIndex] : this._ctrCC;
      var _loc4_ = nIndex != 0 ? _loc3_.contentData : this.api.datacenter.Player.Spells[0];
      if(_loc4_ == undefined)
      {
         return undefined;
      }
      var _loc5_;
      if(this.api.kernel.TutorialManager.isTutorialMode)
      {
         _loc5_.can = true;
      }
      else
      {
         _loc5_ = this.api.datacenter.Player.SpellsManager.checkCanLaunchSpellReturnObject(_loc4_.ID);
      }
      if(_loc5_.can == false)
      {
         switch(_loc5_.type)
         {
            case "NOT_IN_REQUIRED_STATE":
            case "IN_FORBIDDEN_STATE":
               this.setMovieClipTransform(_loc3_.content,dofus.graphics.gapi.controls.MouseShortcuts.WRONG_STATE_TRANSFORM);
               if(_loc5_.params[1])
               {
                  _loc3_.showLabel = true;
                  _loc3_.label = _loc5_.params[1];
               }
               else
               {
                  _loc3_.showLabel = false;
               }
               break;
            case "NOT_ENOUGH_AP":
            case "CANT_SUMMON_MORE_CREATURE":
            case "CANT_LAUNCH_MORE":
            case "CANT_RELAUNCH":
            case "NOT_IN_FIGHT":
               _loc3_.showLabel = false;
               this.setMovieClipTransform(_loc3_.content,dofus.Constants.INACTIVE_TRANSFORM);
               break;
            case "CANT_LAUNCH_BEFORE":
               this.setMovieClipTransform(_loc3_.content,dofus.Constants.INACTIVE_TRANSFORM);
               _loc3_.showLabel = true;
               _loc3_.label = _loc5_.params[0];
            default:
               return undefined;
         }
      }
      else
      {
         _loc3_.showLabel = false;
         this.setMovieClipTransform(_loc3_.content,dofus.Constants.NO_TRANSFORM);
      }
   }
   function setItemStateOnContainer(nIndex)
   {
      var _loc3_ = this["_ctr" + nIndex];
      var _loc4_ = dofus.datacenter.InventoryShortcutItem(_loc3_.contentData);
      if(_loc4_ == undefined)
      {
         return undefined;
      }
      this.setMovieClipTransform(_loc3_.content,!_loc4_.findRealItem() ? dofus.Constants.INACTIVE_TRANSFORM : dofus.Constants.NO_TRANSFORM);
      _loc3_.showLabel = _loc4_.label != undefined;
   }
   function updateCurrentTabInformations()
   {
      switch(this._sCurrentTab)
      {
         case "Spells":
            this.updateSpells();
            this._ctrCC._visible = !this.api.datacenter.Player.isMutant;
            return;
         case "Items":
            this.updateItems();
            this._ctrCC._visible = false;
      }
      return undefined;
   }
   function setCurrentTab(sNewTab)
   {
      var _loc3_;
      var _loc4_;
      if(sNewTab != this._sCurrentTab)
      {
         _loc3_ = this["_btnTab" + this._sCurrentTab];
         _loc4_ = this["_btnTab" + sNewTab];
         _loc3_.selected = true;
         _loc3_.enabled = true;
         _loc4_.selected = false;
         _loc4_.enabled = false;
         this._sCurrentTab = sNewTab;
         this.updateCurrentTabInformations();
      }
   }
   function getKeyToCastSpellOnSelf()
   {
      return this.api.kernel.KeyManager.getCurrentShortcut("CASTONSELF");
   }
   function isHoldingCastOnSelf()
   {
      var _loc2_ = this.getKeyToCastSpellOnSelf();
      return this.api.kernel.KeyManager.isKeyPressed(_loc2_.k) || this.api.kernel.KeyManager.isKeyPressed(_loc2_.k2);
   }
   function castSpellOnSelf(oSpell)
   {
      if(!this.api.kernel.GameManager.checkSpriteStateCanLaunchSpell())
      {
         return undefined;
      }
      if(!this.api.kernel.GameManager.checkCanLaunchSpell(oSpell))
      {
         return undefined;
      }
      var _loc3_ = this.api.datacenter.Game.isControllingInvocation ? this.api.datacenter.Game.controlledInvocationCell : this.api.datacenter.Player.data.cellNum;
      var _loc4_ = this.api.gfx.mapHandler.getCellData(_loc3_);
      this.api.datacenter.Game.setInteractionType("spell");
      this.api.datacenter.Player.currentUseObject = oSpell;
      this.api.datacenter.Basics.gfx_canLaunch = true;
      this.api.gfx.onCellRelease(_loc4_.mc);
   }
   function onShortcut(sShortcut)
   {
      var _loc3_ = true;
      switch(sShortcut)
      {
         case "SWAP":
            this.setCurrentTab(this._sCurrentTab != "Spells" ? "Spells" : "Items");
            _loc3_ = false;
            break;
         case "SH0":
            this.click({target:this._ctrCC});
            _loc3_ = false;
            break;
         case "SH1":
            this.click({target:this._ctr1,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH2":
            this.click({target:this._ctr2,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH3":
            this.click({target:this._ctr3,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH4":
            this.click({target:this._ctr4,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH5":
            this.click({target:this._ctr5,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH6":
            this.click({target:this._ctr6,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH7":
            this.click({target:this._ctr7,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH8":
            this.click({target:this._ctr8,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH9":
            this.click({target:this._ctr9,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH10":
            this.click({target:this._ctr10,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH11":
            this.click({target:this._ctr11,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH12":
            this.click({target:this._ctr12,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH13":
            this.click({target:this._ctr13,keyBoard:true});
            _loc3_ = false;
            break;
         case "SH14":
            this.click({target:this._ctr14,keyBoard:true});
            _loc3_ = false;
      }
      return _loc3_;
   }
   function click(oEvent)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      switch(oEvent.target)
      {
         case this._btnTabSpells:
            this.api.sounds.events.onBannerSpellItemButtonClick();
            this.setCurrentTab("Spells");
            return undefined;
         case this._btnTabItems:
            this.api.sounds.events.onBannerSpellItemButtonClick();
            this.setCurrentTab("Items");
            return undefined;
         case this._ctrCC:
            if(this._ctrCC._visible)
            {
               if(this.api.kernel.TutorialManager.isTutorialMode)
               {
                  this.api.kernel.TutorialManager.onWaitingCase({code:"CC_CONTAINER_SELECT"});
                  return undefined;
               }
               this.api.kernel.GameManager.switchToSpellLaunch(this.api.datacenter.Player.Spells[0],false);
               return undefined;
            }
            return undefined;
            break;
         default:
            switch(this._sCurrentTab)
            {
               case "Spells":
                  this.api.sounds.events.onBannerSpellSelect();
                  if(this.api.kernel.TutorialManager.isTutorialMode)
                  {
                     this.api.kernel.TutorialManager.onWaitingCase({code:"SPELL_CONTAINER_SELECT",params:[Number(oEvent.target._name.substr(4))]});
                  }
                  else
                  {
                     if(this.gapi.getUIComponent("Spells") != undefined)
                     {
                        return undefined;
                     }
                     _loc3_ = oEvent.target.contentData;
                     if(_loc3_ == undefined)
                     {
                        return undefined;
                     }
                     if(_loc3_.rangeMin == 0 && this.isHoldingCastOnSelf())
                     {
                        this.castSpellOnSelf(_loc3_);
                     }
                     else
                     {
                        this.api.kernel.GameManager.switchToSpellLaunch(_loc3_,true);
                     }
                  }
                  return;
               case "Items":
                  if(this.api.kernel.TutorialManager.isTutorialMode)
                  {
                     this.api.kernel.TutorialManager.onWaitingCase({code:"OBJECT_CONTAINER_SELECT",params:[Number(oEvent.target._name.substr(4))]});
                     break;
                  }
                  _loc4_ = oEvent.target.contentData;
                  _loc5_ = _loc4_ == undefined ? undefined : _loc4_.realLinkedItem;
                  if(_loc5_ == undefined)
                  {
                     return undefined;
                  }
                  if(Key.isDown(dofus.Constants.CHAT_INSERT_ITEM_KEY))
                  {
                     this.api.kernel.GameManager.insertItemInChat(_loc5_);
                     return undefined;
                  }
                  _loc6_ = this.gapi.getUIComponent("Inventory");
                  if(_loc6_ != undefined)
                  {
                     _loc6_.showItemInfos(_loc5_);
                     break;
                  }
                  if(!this.api.datacenter.Player.checkCanMoveItem())
                  {
                     return undefined;
                  }
                  if(oEvent.keyBoard)
                  {
                     if(_loc5_.isEquiped)
                     {
                        this.api.network.Items.movement(_loc5_.ID,-1);
                        return undefined;
                     }
                     if(this.api.network.Items.equipItem(_loc5_))
                     {
                        return undefined;
                     }
                  }
                  if(this.api.datacenter.Player.canUseObject)
                  {
                     if(_loc5_.canTarget)
                     {
                        this.api.kernel.GameManager.switchToItemTarget(_loc5_);
                        break;
                     }
                     if(_loc5_.canUse && oEvent.keyBoard)
                     {
                        if(Key.isDown(Key.CONTROL) || Key.isDown(Key.SHIFT))
                        {
                           dofus.graphics.gapi.ui.Inventory.askBatchUseItem(this.api,_loc5_);
                           break;
                        }
                        this.api.network.Items.use(_loc5_.ID);
                     }
                  }
            }
            return undefined;
      }
   }
   function dblClick(oEvent)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      switch(this._sCurrentTab)
      {
         case "Spells":
            if((_loc3_ = oEvent.target._name) !== "_ctrCC")
            {
               _loc4_ = oEvent.target.contentData;
            }
            else
            {
               _loc4_ = this.api.datacenter.Player.Spells[0];
            }
            if(_loc4_ == undefined)
            {
               return undefined;
            }
            if(this.isHoldingCastOnSelf())
            {
               this.castSpellOnSelf(_loc4_);
               return undefined;
            }
            this.gapi.loadUIAutoHideComponent("SpellInfos","SpellInfos",{spell:_loc4_},{bStayIfPresent:true});
            return;
            break;
         case "Items":
            if(!this.api.datacenter.Player.checkCanMoveItem())
            {
               return undefined;
            }
            _loc5_ = oEvent.target.contentData;
            _loc6_ = _loc5_ == undefined ? undefined : _loc5_.realLinkedItem;
            if(_loc6_ == undefined)
            {
               return undefined;
            }
            if(!_loc6_.isEquiped)
            {
               if(!_loc6_.canUse || !this.api.datacenter.Player.canUseObject)
               {
                  this.api.network.Items.equipItem(_loc6_);
               }
               else if(Key.isDown(Key.CONTROL) || Key.isDown(Key.SHIFT))
               {
                  dofus.graphics.gapi.ui.Inventory.askBatchUseItem(this.api,_loc6_);
               }
               else
               {
                  this.api.network.Items.use(_loc6_.ID);
               }
               break;
            }
            this.api.network.Items.movement(_loc6_.ID,-1);
      }
      return undefined;
   }
   function over(oEvent)
   {
      if(!this.gapi.isCursorHidden())
      {
         return undefined;
      }
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      var _loc11_;
      if((_loc4_ = oEvent.target._name) !== "_ctrCC")
      {
         switch(this._sCurrentTab)
         {
            case "Spells":
               this._currentOverContainer = oEvent.target;
               _loc5_ = oEvent.target.contentData;
               if(_loc5_ != undefined)
               {
                  _loc6_ = "<b>" + _loc5_.name + "</b> - ";
                  _loc6_ += (_loc5_.apCost >= 1 ? _loc5_.apCost : "1") + " " + this.api.lang.getText("AP");
                  _loc6_ += _loc5_.actualCriticalHit <= 0 ? "" : "\n(" + this.api.lang.getText("ACTUAL_CRITICAL_CHANCE") + ": 1/" + _loc5_.actualCriticalHit + ")";
                  _loc6_ += _loc5_.rangeMin != 0 ? "" : "\n\n" + this.api.lang.getText("CLICK_TO_CAST_SPELL_ON_SELF",[this.getKeyToCastSpellOnSelf().d]);
                  this.gapi.showTooltip(_loc6_,oEvent.target,-4,{bXLimit:true,bYLimit:false,bTopAlign:true});
               }
               return;
            case "Items":
               this._currentOverContainer = oEvent.target;
               _loc7_ = oEvent.target.contentData;
               if(_loc7_ != undefined)
               {
                  _loc8_ = "<b>" + _loc7_.name + "</b>";
                  if(this.gapi.getUIComponent("Inventory") == undefined)
                  {
                     if(_loc7_.canUse && _loc7_.canTarget)
                     {
                        _loc8_ += "\n" + this.api.lang.getText("HELP_SHORTCUT_DBLCLICK_CLICK");
                     }
                     else
                     {
                        if(_loc7_.canUse)
                        {
                           _loc8_ += "\n" + this.api.lang.getText("HELP_SHORTCUT_DBLCLICK");
                        }
                        if(_loc7_.canTarget)
                        {
                           _loc8_ += "\n" + this.api.lang.getText("HELP_SHORTCUT_CLICK");
                        }
                     }
                  }
                  this.gapi.showTooltip(_loc8_,oEvent.target,-4,{bXLimit:true,bYLimit:false,bTopAlign:true});
               }
         }
         return undefined;
      }
      _loc9_ = this.api.datacenter.Player.Spells[0];
      _loc10_ = this.api.kernel.GameManager.getCriticalHitChance(_loc9_.criticalHit);
      _loc11_ = "<b>" + _loc9_.name + "</b> - ";
      _loc11_ += !_loc9_.isUnusable ? _loc9_.apCost + " " + this.api.lang.getText("AP") : this.api.lang.getText("UNUSABLE");
      _loc11_ += _global.isNaN(_loc10_) ? "\n\n" : "\n" + "(" + this.api.lang.getText("ACTUAL_CRITICAL_CHANCE") + ": 1/" + _loc10_ + ")" + "\n\n";
      _loc11_ += _loc9_.descriptionVisibleEffects;
      this.gapi.showTooltip(_loc11_,oEvent.target,-4,{bXLimit:true,bYLimit:false,bTopAlign:true});
   }
   function out(oEvent)
   {
      this._currentOverContainer = undefined;
      this.gapi.hideTooltip();
   }
   function drag(oEvent)
   {
      var _loc3_ = oEvent.target.contentData;
      if(_loc3_ == undefined)
      {
         return undefined;
      }
      switch(this._sCurrentTab)
      {
         case "Spells":
            if(this.gapi.getUIComponent("Spells") == undefined && !Key.isDown(Key.SHIFT))
            {
               return undefined;
            }
            break;
         case "Items":
            if(this.gapi.getUIComponent("Inventory") == undefined && !Key.isDown(Key.SHIFT))
            {
               return undefined;
            }
      }
      this.gapi.removeCursor();
      this.gapi.setCursor(_loc3_,undefined,this._sCurrentTab == "Spells");
   }
   function onContentLoaded(oEvent)
   {
      if(this._sCurrentTab != "Spells")
      {
         return undefined;
      }
      var _loc3_ = oEvent.content;
      _loc3_.applyColors();
   }
   function drop(oEvent)
   {
      var _loc3_;
      §§push(_loc3_ = oEvent.target);
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      switch(this._sCurrentTab)
      {
         case "Spells":
            if(this.gapi.getUIComponent("Spells") == undefined && !Key.isDown(Key.SHIFT))
            {
               return undefined;
            }
            _loc4_ = this.gapi.getCursor();
            if(_loc4_ == undefined)
            {
               return undefined;
            }
            this.gapi.removeCursor();
            _loc5_ = _loc4_.position;
            _loc6_ = oEvent.target.params.position;
            if(_loc5_ == _loc6_)
            {
               return undefined;
            }
            this.api.network.Spells.moveToUsed(_loc4_.ID,_loc6_);
            break;
         case "Items":
            if(this.gapi.getUIComponent("Inventory") == undefined && !Key.isDown(Key.SHIFT))
            {
               return undefined;
            }
            _loc7_ = this.gapi.getCursor();
            if(_loc7_ == undefined)
            {
               return undefined;
            }
            _loc8_ = _loc7_.ID;
            if(_loc8_ == -1)
            {
               return undefined;
            }
            this.gapi.removeCursor();
            _loc9_ = oEvent.target.params.position;
            if(_loc7_.isShortcut && _loc7_.position == _loc9_)
            {
               return undefined;
            }
            if(_loc7_.isShortcut)
            {
               this.api.network.InventoryShortcuts.sendInventoryShortcutMove(_loc7_.position,_loc9_);
               break;
            }
            this.api.network.InventoryShortcuts.sendInventoryShortcutAdd(_loc9_,_loc8_);
      }
      §§pop();
   }
   function modelChanged(oEvent)
   {
      switch(oEvent.eventName)
      {
         case "updateOne":
         case "updateAll":
      }
      if(oEvent.target == this.api.datacenter.Player.Spells)
      {
         this.updateSpells();
      }
      else
      {
         this.updateItems();
      }
   }
}

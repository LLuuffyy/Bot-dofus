class dofus.graphics.gapi.controls.SpellFullInfosViewer extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _btnClose;
   var _btnLevel1;
   var _btnLevel2;
   var _btnLevel3;
   var _btnLevel4;
   var _btnLevel5;
   var _btnLevel6;
   var _btnTabCreature;
   var _btnTabCritical;
   var _btnTabGlyph;
   var _btnTabNormal;
   var _btnTabTrap;
   var _lblAP;
   var _lblCountByTurn;
   var _lblCountByTurnByPlayer;
   var _lblCountByTurnByPlayerValue;
   var _lblCountByTurnValue;
   var _lblCriticalHit;
   var _lblCriticalHitValue;
   var _lblCriticalMiss;
   var _lblCriticalMissValue;
   var _lblDelay;
   var _lblDelayValue;
   var _lblEffectsTitle;
   var _lblFailureEndsTheTurn;
   var _lblFreeCell;
   var _lblLevel;
   var _lblLineOfSight;
   var _lblLineOnly;
   var _lblName;
   var _lblOtherTitle;
   var _lblRange;
   var _lblRangeBoost;
   var _lblRealCrit;
   var _lblRealCritValue;
   var _lblReqLevel;
   var _ldrIcon;
   var _lstEffects;
   var _mcCheckFailureEndsTheTurn;
   var _mcCheckFreeCell;
   var _mcCheckLineOfSight;
   var _mcCheckLineOnly;
   var _mcCheckRangeBoost;
   var _mcCrossFailureEndsTheTurn;
   var _mcCrossFreeCell;
   var _mcCrossLineOfSight;
   var _mcCrossLineOnly;
   var _mcCrossRangeBoost;
   var _oSpell;
   var _txtDescription;
   var addToQueue;
   var dispatchEvent;
   var initialized;
   var unloadThis;
   static var CLASS_NAME = "SpellFullInfosViewer";
   var _sCurrentTab = "Normal";
   function SpellFullInfosViewer()
   {
      super();
   }
   function set spell(oSpell)
   {
      if(oSpell == undefined)
      {
         return;
      }
      if(oSpell == this._oSpell)
      {
         return;
      }
      if(!oSpell.isValid)
      {
         this._oSpell = new dofus.datacenter.Spell(oSpell.ID,1);
      }
      else
      {
         this._oSpell = oSpell;
      }
      if(this.initialized)
      {
         this.updateData();
      }
   }
   function get spell()
   {
      return this._oSpell;
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.controls.SpellFullInfosViewer.CLASS_NAME);
   }
   function createChildren()
   {
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTexts});
      this.hideAllCheck();
      this._btnTabCreature._visible = false;
      this._btnTabGlyph._visible = false;
      this._btnTabTrap._visible = false;
   }
   function addListeners()
   {
      this.api.kernel.KeyManager.addShortcutsListener("onShortcut",this);
      this._ldrIcon.addEventListener("complete",this);
      this._btnTabNormal.addEventListener("click",this);
      this._btnTabCritical.addEventListener("click",this);
      this._btnTabCreature.addEventListener("click",this);
      this._btnTabGlyph.addEventListener("click",this);
      this._btnTabTrap.addEventListener("click",this);
      this._btnLevel1.addEventListener("click",this);
      this._btnLevel2.addEventListener("click",this);
      this._btnLevel3.addEventListener("click",this);
      this._btnLevel4.addEventListener("click",this);
      this._btnLevel5.addEventListener("click",this);
      this._btnLevel6.addEventListener("click",this);
      this._btnClose.addEventListener("click",this);
   }
   function onShortcut(sShortcut)
   {
      var _loc3_ = true;
      var _loc4_;
      if((_loc4_ = sShortcut) === "ESCAPE")
      {
         this.dispatchEvent({type:"close"});
         this.unloadThis();
         _loc3_ = false;
      }
      return _loc3_;
   }
   function initData()
   {
      this.updateData();
   }
   function initTexts()
   {
      this._lblEffectsTitle.text = this.api.lang.getText("EFFECTS");
      this._lblOtherTitle.text = this.api.lang.getText("OTHER_CHARACTERISTICS");
      this._lblCriticalHit.text = this.api.lang.getText("CRITICAL_HIT_PROBABILITY");
      this._lblCriticalMiss.text = this.api.lang.getText("CRITICAL_MISS_PROBABILITY");
      this._lblCountByTurn.text = this.api.lang.getText("COUNT_BY_TURN");
      this._lblCountByTurnByPlayer.text = this.api.lang.getText("COUNT_BY_TURN_BY_PLAYER");
      this._lblRangeBoost.text = this.api.lang.getText("RANGE_BOOST");
      this._lblLineOfSight.text = this.api.lang.getText("LINE_OF_SIGHT");
      this._lblLineOnly.text = this.api.lang.getText("LINE_ONLY");
      this._lblFreeCell.text = this.api.lang.getText("FREE_CELL");
      this._lblRealCrit.text = this.api.lang.getText("ACTUAL_CRITICAL_CHANCE");
      this._lblFailureEndsTheTurn.text = this.api.lang.getText("FAILURE_ENDS_THE_TURN");
      this._btnTabNormal.label = this.api.lang.getText("NORMAL_EFFECTS");
      this._btnTabCritical.label = this.api.lang.getText("CRITICAL_EFECTS");
      this._btnTabCreature.label = this.api.lang.getText("SUMMONED_CREATURE");
      this._btnTabGlyph.label = this.api.lang.getText("GLYPH");
      this._btnTabTrap.label = this.api.lang.getText("TRAP");
   }
   function updateData()
   {
      var _loc2_;
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      if(this._oSpell != undefined && this._txtDescription.text != undefined)
      {
         this._ldrIcon.forceReload = true;
         this._ldrIcon.contentParams = this._oSpell.params;
         this._ldrIcon.contentPath = this._oSpell.iconFile;
         if(this._ldrIcon.loaded)
         {
            this._ldrIcon.content.applyColors();
         }
         this._lblDelay.text = !this._oSpell.isCastGlobalInterval ? this.api.lang.getText("DELAY_RELAUNCH") : this.api.lang.getText("DELAY_RELAUNCH_GLOBAL");
         this._lblName.text = this._oSpell.name;
         this._lblLevel.text = this.api.lang.getText("ACTUAL_SPELL_LEVEL") + ":";
         this._lblReqLevel.text = this._oSpell.minPlayerLevel == undefined ? "" : this.api.lang.getText("REQUIRED_SPELL_LEVEL") + ": " + this._oSpell.minPlayerLevel;
         this._lblRange.text = this._oSpell.rangeStr + " " + this.api.lang.getText("RANGE");
         this._lblAP.text = (!(this._oSpell.apCost < 1 && !this._oSpell.isPassive) ? this._oSpell.apCost : "1") + " " + this.api.lang.getText("AP");
         this._txtDescription.text = this._oSpell.description;
         this._btnTabCreature._visible = this._oSpell.summonSpell;
         this._btnTabGlyph._visible = this._oSpell.glyphSpell;
         this._btnTabTrap._visible = this._oSpell.trapSpell;
         if(this._oSpell.effectsCriticalHit[0] == undefined)
         {
            if(this._sCurrentTab == "Critical")
            {
               this.setCurrentTab("Normal");
            }
            this._btnTabCritical._alpha = 70;
            this._btnTabCritical.enabled = false;
         }
         else
         {
            this._btnTabCritical._alpha = 100;
            this._btnTabCritical.enabled = true;
         }
         if(!this._oSpell.summonSpell && this._sCurrentTab == "Creature")
         {
            this.setCurrentTab("Normal");
         }
         else if(!this._oSpell.glyphSpell && this._sCurrentTab == "Glyph")
         {
            this.setCurrentTab("Normal");
         }
         else if(!this._oSpell.trapSpell && this._sCurrentTab == "Trap")
         {
            this.setCurrentTab("Normal");
         }
         else
         {
            this.updateCurrentTabInformations();
         }
         _loc2_ = this.api.kernel.GameManager.getCriticalHitChance(this._oSpell.criticalHit);
         this._lblRealCritValue.text = _loc2_ != 0 ? "1/" + _loc2_ : "-";
         this._lblCriticalHitValue.text = this._oSpell.criticalHit != 0 ? "1/" + this._oSpell.criticalHit : "-";
         this._lblCriticalMissValue.text = this._oSpell.criticalFailure != 0 ? "1/" + this._oSpell.criticalFailure : "-";
         this._lblCountByTurnValue.text = this._oSpell.launchCountByTurn != 0 ? String(this._oSpell.launchCountByTurn) : "-";
         this._lblCountByTurnByPlayerValue.text = this._oSpell.launchCountByPlayerTurn != 0 ? String(this._oSpell.launchCountByPlayerTurn) : "-";
         this._lblDelayValue.text = this._oSpell.delayBetweenLaunch < 63 ? (this._oSpell.delayBetweenLaunch != 0 ? String(this._oSpell.delayBetweenLaunch) : "-") : "inf.";
         this._mcCrossRangeBoost._visible = !this._oSpell.canBoostRange;
         this._mcCheckRangeBoost._visible = this._oSpell.canBoostRange;
         this._mcCrossLineOfSight._visible = !this._oSpell.lineOfSight;
         this._mcCheckLineOfSight._visible = this._oSpell.lineOfSight;
         this._mcCrossLineOnly._visible = !this._oSpell.lineOnly;
         this._mcCheckLineOnly._visible = this._oSpell.lineOnly;
         this._mcCrossFreeCell._visible = !this._oSpell.freeCell;
         this._mcCheckFreeCell._visible = this._oSpell.freeCell;
         this._mcCrossFailureEndsTheTurn._visible = !this._oSpell.criticalFailureEndsTheTurn;
         this._mcCheckFailureEndsTheTurn._visible = this._oSpell.criticalFailureEndsTheTurn;
         if(this._oSpell.level != undefined)
         {
            _loc3_ = 1;
            while(_loc3_ <= 6)
            {
               _loc4_ = this["_btnLevel" + _loc3_];
               _loc5_ = _loc3_ == this._oSpell.level;
               _loc4_.selected = _loc5_;
               _loc4_.enabled = !_loc5_;
               if(_loc3_ <= this._oSpell.maxLevel)
               {
                  _loc4_._alpha = 100;
               }
               else
               {
                  _loc4_.enabled = false;
                  _loc4_._alpha = 20;
               }
               _loc3_ += 1;
            }
         }
         else
         {
            _loc6_ = 1;
            while(_loc6_ <= 6)
            {
               _loc7_ = this["_btnLevel" + _loc6_];
               _loc7_.selected = false;
               _loc7_.enabled = false;
               _loc7_._alpha = 20;
               _loc6_ += 1;
            }
         }
      }
      else if(this._lblName.text != undefined)
      {
         this._lblDelay.text = this.api.lang.getText("DELAY_RELAUNCH");
         this._ldrIcon.contentPath = "";
         this._lblName.text = "";
         this._lblLevel.text = "";
         this._lblRange.text = "";
         this._lblAP.text = "";
         this._txtDescription.text = "";
         this._lblCriticalHitValue.text = "";
         this._lblCriticalMissValue.text = "";
         this._lblCountByTurnValue.text = "";
         this._lblCountByTurnByPlayerValue.text = "";
         this._lblDelayValue.text = "";
         this.hideAllCheck();
         this._lstEffects.dataProvider = null;
      }
   }
   function updateCurrentTabInformations()
   {
      var _loc2_;
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
      var _loc16_;
      var _loc17_;
      var _loc18_;
      var _loc19_;
      var _loc20_;
      var _loc21_;
      var _loc22_;
      switch(this._sCurrentTab)
      {
         case "Normal":
            this._lstEffects.dataProvider = this._oSpell.effectsNormalHitWithArea;
            return;
         case "Critical":
            this._lstEffects.dataProvider = this._oSpell.effectsCriticalHitWithArea;
            return;
         case "Creature":
            _loc2_ = this._oSpell.effectsNormalHit;
            _loc3_ = 0;
            while(_loc3_ < _loc2_.length)
            {
               _loc4_ = _loc2_[_loc3_];
               if(_loc4_.type == 181)
               {
                  break;
               }
               _loc4_.type = _loc5_ = 180;
               if(_loc5_)
               {
                  _loc6_ = new ank.utils.ExtendedArray();
                  _loc7_ = this.api.datacenter.Player.data;
                  _loc6_.push(_loc7_.name + " (" + this.api.lang.getText("LEVEL") + " " + this.api.datacenter.Player.Level + ")");
                  _loc6_.push(this.api.lang.getText("LP") + " : " + this.api.datacenter.Player.LP);
                  _loc6_.push(this.api.lang.getText("AP") + " : " + _loc7_.AP);
                  _loc6_.push(this.api.lang.getText("MP") + " : " + _loc7_.MP);
                  this._lstEffects.dataProvider = _loc6_;
                  return undefined;
               }
               _loc3_ += 1;
            }
            _loc8_ = new ank.utils.ExtendedArray();
            if(_loc4_ != undefined)
            {
               _loc9_ = _loc4_.param1;
               _loc10_ = _loc4_.param2;
               _loc11_ = this.api.lang.getMonstersText(_loc9_);
               _loc12_ = _loc11_["g" + _loc10_];
               _loc8_.push(_loc11_.n + " (" + this.api.lang.getText("LEVEL") + " " + _loc12_.l + ")");
               _loc13_ = !dofus.datacenter.Gladiatrool.isIncarnation(this.api.datacenter.Player.weaponItem.unicID) ? this.api.datacenter.Player.Level : 200;
               _loc14_ = !this.api.datacenter.Basics.aks_current_server.isTemporis() ? Math.floor(_loc12_.lp * (1 + _loc13_ / 100)) : Math.floor(_loc12_.lp + this.api.datacenter.Player.LPmax * 0.4);
               _loc8_.push(this.api.lang.getText("LP") + " : " + _loc14_ + " (" + _loc12_.lp + " + " + (_loc14_ - _loc12_.lp) + ")");
               _loc8_.push(this.api.lang.getText("AP") + " : " + _loc12_.ap);
               _loc8_.push(this.api.lang.getText("MP") + " : " + _loc12_.mp);
            }
            this._lstEffects.dataProvider = _loc8_;
            return;
         case "Glyph":
         case "Trap":
            _loc15_ = 400;
            if(this._sCurrentTab == "Glyph")
            {
               _loc15_ = 401;
            }
            _loc16_ = this._oSpell.effectsNormalHit;
            _loc17_ = 0;
            while(_loc17_ < _loc16_.length)
            {
               _loc18_ = _loc16_[_loc17_];
               if(_loc18_.type == _loc15_)
               {
                  break;
               }
               _loc17_ += 1;
            }
            _loc19_ = new ank.utils.ExtendedArray();
            if(_loc18_ != undefined)
            {
               _loc20_ = _loc18_.param1;
               _loc21_ = _loc18_.param2;
               _loc22_ = this.api.kernel.CharactersManager.getSpellObjectFromData(_loc20_ + "~" + _loc21_ + "~");
               _loc19_ = _loc22_.effectsNormalHit;
            }
            this._lstEffects.dataProvider = _loc19_;
      }
      return undefined;
   }
   function setCurrentTab(sNewTab)
   {
      var _loc3_ = this["_btnTab" + this._sCurrentTab];
      var _loc4_ = this["_btnTab" + sNewTab];
      _loc3_.selected = true;
      _loc3_.enabled = true;
      _loc4_.selected = false;
      _loc4_.enabled = false;
      this._sCurrentTab = sNewTab;
      this.updateCurrentTabInformations();
   }
   function hideAllCheck()
   {
      this._mcCrossRangeBoost._visible = true;
      this._mcCheckRangeBoost._visible = false;
      this._mcCrossLineOfSight._visible = true;
      this._mcCheckLineOfSight._visible = false;
      this._mcCrossLineOnly._visible = true;
      this._mcCheckLineOnly._visible = false;
      this._mcCrossFreeCell._visible = true;
      this._mcCheckFreeCell._visible = false;
   }
   function setLevel(nLevel)
   {
      var _loc3_ = this.api.kernel.CharactersManager.getSpellObjectFromData(this._oSpell.ID + "~" + nLevel);
      if(_loc3_.isValid)
      {
         this.spell = _loc3_;
      }
      else
      {
         this["_btnLevel" + nLevel].selected = false;
      }
   }
   function complete(oEvent)
   {
      var _loc2_ = oEvent.clip;
      _loc2_.applyColors();
   }
   function click(oEvent)
   {
      var _loc3_;
      switch(oEvent.target._name)
      {
         case "_btnTabNormal":
            this.setCurrentTab("Normal");
            return;
         case "_btnTabCritical":
            this.setCurrentTab("Critical");
            return;
         case "_btnTabCreature":
            this.setCurrentTab("Creature");
            return;
         case "_btnTabGlyph":
            this.setCurrentTab("Glyph");
            return;
         case "_btnTabTrap":
            this.setCurrentTab("Trap");
            return;
         case "_btnLevel1":
         case "_btnLevel2":
         case "_btnLevel3":
         case "_btnLevel4":
         case "_btnLevel5":
         case "_btnLevel6":
            _loc3_ = oEvent.target._name.substr(9);
            this.setLevel(Number(_loc3_));
            return;
         case "_btnClose":
            this.dispatchEvent({type:"close"});
            this.unloadThis();
      }
      return undefined;
   }
}

class dofus.managers.SpellsManager
{
   var _aSpellsCountByPlayer;
   var _aSpellsCountByTurn;
   var _aSpellsDelay;
   var _localPlayerData;
   var _oSpellsCountByPlayer_Counter;
   var _oSpellsCountByTurn_Counter;
   var _oSpellsModificators;
   var api;
   var dispatchEvent;
   function SpellsManager(d)
   {
      this.initialize(d);
   }
   function initialize(d)
   {
      this._localPlayerData = d;
      this.api = d.api;
      this.clear();
      this._oSpellsModificators = {};
      mx.events.EventDispatcher.initialize(this);
   }
   function clear()
   {
      this._aSpellsCountByTurn = [];
      this._oSpellsCountByTurn_Counter = {};
      this._aSpellsCountByPlayer = [];
      this._oSpellsCountByPlayer_Counter = {};
      this._aSpellsDelay = [];
   }
   function addLaunchedSpell(oLaunchedSpell)
   {
      var _loc3_ = oLaunchedSpell.spell;
      var _loc4_ = _loc3_.launchCountByTurn;
      var _loc5_ = _loc3_.launchCountByPlayerTurn;
      var _loc6_ = _loc3_.delayBetweenLaunch;
      if(_loc6_ != 0)
      {
         this._aSpellsDelay.push(oLaunchedSpell);
      }
      if(_loc5_ != 0)
      {
         if(oLaunchedSpell.spriteOnID != undefined)
         {
            this._aSpellsCountByPlayer.push(oLaunchedSpell);
            if(this._oSpellsCountByPlayer_Counter[oLaunchedSpell.spriteOnID + "|" + _loc3_.ID] == undefined)
            {
               this._oSpellsCountByPlayer_Counter[oLaunchedSpell.spriteOnID + "|" + _loc3_.ID] = 1;
            }
            else
            {
               this._oSpellsCountByPlayer_Counter[oLaunchedSpell.spriteOnID + "|" + _loc3_.ID]++;
            }
         }
      }
      if(_loc4_ != 0)
      {
         this._aSpellsCountByTurn.push(oLaunchedSpell);
         if(this._oSpellsCountByTurn_Counter[_loc3_.ID] == undefined)
         {
            this._oSpellsCountByTurn_Counter[_loc3_.ID] = 1;
         }
         else
         {
            this._oSpellsCountByTurn_Counter[_loc3_.ID]++;
         }
      }
      this.dispatchEvent({type:"spellLaunched",spell:_loc3_});
   }
   function nextTurn()
   {
      this._aSpellsCountByTurn = [];
      this._oSpellsCountByTurn_Counter = {};
      this._aSpellsCountByPlayer = [];
      this._oSpellsCountByPlayer_Counter = {};
      var _loc2_ = this._aSpellsDelay.length;
      var _loc3_;
      while((_loc2_ -= 1) >= 0)
      {
         _loc3_ = this._aSpellsDelay[_loc2_];
         _loc3_.remainingTurn -= 1;
         if(_loc3_.remainingTurn <= 0)
         {
            this._aSpellsDelay.splice(_loc2_,1);
         }
      }
      this.dispatchEvent({type:"nextTurn"});
   }
   function checkCanLaunchSpell(spellID, nSpriteOnID)
   {
      var _loc4_ = this.checkCanLaunchSpellReturnObject(spellID,nSpriteOnID);
      if(_loc4_.can == false)
      {
         this.api.datacenter.Basics.spellManager_errorMsg = this.api.lang.getText(_loc4_.type,_loc4_.params);
         return false;
      }
      return true;
   }
   function checkCanLaunchSpellReturnObject(nSpellID, nSpriteOnID)
   {
      if(!this.api.datacenter.Game.isRunning || (this.api.datacenter.Game.isSpectator || (!this.api.gfx.isOnBattlefield(this.api.datacenter.Player.ID) || this.api.datacenter.Player.isDead)))
      {
         return {can:false,type:"NOT_IN_FIGHT"};
      }
      var _loc4_ = this.api.datacenter.Player.Spells.findFirstItem("ID",nSpellID).item;
      var _loc5_ = {};
      var _loc6_ = this._aSpellsCountByPlayer.length;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      while((_loc6_ -= 1) >= 0)
      {
         _loc7_ = this._aSpellsCountByPlayer[_loc6_];
         _loc8_ = _loc7_.spell;
         if(_loc8_.ID == nSpellID)
         {
            _loc9_ = _loc8_.launchCountByPlayerTurn;
            if(_loc7_.spriteOnID == nSpriteOnID && this._oSpellsCountByPlayer_Counter[_loc7_.spriteOnID + "|" + nSpellID] >= _loc9_)
            {
               return {can:false,type:"CANT_ON_THIS_PLAYER"};
            }
         }
      }
      if(_loc4_.isUnusable)
      {
         return {can:false,type:"CANT_RELAUNCH"};
      }
      var _loc10_;
      var _loc11_;
      var _loc12_;
      var _loc13_;
      if(_loc4_.needStates)
      {
         _loc10_ = _loc4_.requiredStates;
         _loc11_ = _loc4_.forbiddenStates;
         _loc12_ = 0;
         while(_loc12_ < _loc10_.length)
         {
            if(!this.api.datacenter.Player.data.isInState(_loc10_[_loc12_]))
            {
               _loc5_ = {can:false,type:"NOT_IN_REQUIRED_STATE",params:[this.api.lang.getStateText(_loc10_[_loc12_])]};
               break;
            }
            _loc12_ += 1;
         }
         _loc13_ = 0;
         while(_loc13_ < _loc11_.length)
         {
            if(this.api.datacenter.Player.data.isInState(_loc11_[_loc13_]))
            {
               _loc5_ = {can:false,type:"IN_FORBIDDEN_STATE",params:[this.api.lang.getStateText(_loc11_[_loc13_])]};
               break;
            }
            _loc13_ += 1;
         }
      }
      _loc6_ = this._aSpellsDelay.length;
      while((_loc6_ -= 1) >= 0)
      {
         _loc7_ = this._aSpellsDelay[_loc6_];
         _loc8_ = _loc7_.spell;
         if(_loc8_.ID == nSpellID)
         {
            if(_loc7_.remainingTurn >= 63)
            {
               return {can:false,type:"CANT_RELAUNCH"};
            }
            if(_loc5_.type)
            {
               _loc5_.params[1] = _loc7_.remainingTurn;
               return _loc5_;
            }
            return {can:false,type:"CANT_LAUNCH_BEFORE",params:[_loc7_.remainingTurn]};
         }
      }
      if(_loc5_.type)
      {
         return _loc5_;
      }
      var _loc14_;
      if(_loc4_.summonSpell)
      {
         _loc14_ = this.api.datacenter.Player.data.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.MAX_SUMMONED_CREATURES_BOOST) + this.api.datacenter.Player.MaxSummonedCreatures;
         if(this.api.datacenter.Player.SummonedCreatures >= _loc14_ && !_loc4_.hasInvocationConditionnedEffect)
         {
            return {can:false,type:"CANT_SUMMON_MORE_CREATURE",params:[_loc14_]};
         }
      }
      _loc6_ = this._aSpellsCountByTurn.length;
      var _loc15_;
      while((_loc6_ -= 1) >= 0)
      {
         _loc7_ = this._aSpellsCountByTurn[_loc6_];
         _loc8_ = _loc7_.spell;
         if(_loc8_.ID == nSpellID)
         {
            _loc15_ = _loc8_.launchCountByTurn;
            if(this._oSpellsCountByTurn_Counter[nSpellID] >= _loc15_)
            {
               return {can:false,type:"CANT_LAUNCH_MORE",params:[_loc15_]};
            }
         }
      }
      if(!this.api.datacenter.Player.hasEnoughAP(_loc4_.apCost))
      {
         return {can:false,type:"NOT_ENOUGH_AP"};
      }
      return {can:true};
   }
   function checkCanLaunchSpellOnCell(mapHandler, oSpell, cellToData, rangeModerator, bSkipRangeCheck)
   {
      var _loc7_ = this.api.datacenter.Game.isControllingInvocation ? Number(this.api.datacenter.Game.controlledInvocationCell) : Number(this._localPlayerData.data.cellNum);
      var _loc8_ = Number(cellToData.mc.num);
      if(_loc7_ == _loc8_ && oSpell.rangeMin != 0)
      {
         return false;
      }
      if(!this.api.datacenter.Game.isFight)
      {
         return false;
      }
      if(bSkipRangeCheck || ank.battlefield.utils.Pathfinding.checkRange(mapHandler,_loc7_,_loc8_,oSpell.lineOnly,oSpell.rangeMin,oSpell.rangeMax,rangeModerator))
      {
         if(oSpell.freeCell)
         {
            if(cellToData.movement > 1 && cellToData.spriteOnID != undefined)
            {
               return false;
            }
            if(cellToData.movement <= 1)
            {
               return false;
            }
         }
         if(oSpell.lineOfSight)
         {
            if(ank.battlefield.utils.Pathfinding.checkView(mapHandler,_loc7_,_loc8_))
            {
               return this.checkCanLaunchSpell(oSpell.ID,cellToData.spriteOnID);
            }
            return false;
         }
         return cellToData.movement != 0 && this.checkCanLaunchSpell(oSpell.ID,cellToData.spriteOnID);
      }
      return false;
   }
   function getSpellLaunched(nSpellID)
   {
      var _loc3_ = 0;
      while(_loc3_ < this._aSpellsDelay.length)
      {
         if(this._aSpellsDelay[_loc3_].spell.ID == nSpellID)
         {
            return this._aSpellsDelay[_loc3_];
         }
         _loc3_ += 1;
      }
      return undefined;
   }
   function hasSpellLaunched(nSpellID)
   {
      return this.getSpellLaunched(nSpellID) != undefined;
   }
}

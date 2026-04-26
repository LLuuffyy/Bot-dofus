class dofus.managers.CharacteristicsManager extends dofus.utils.ApiElement
{
   var _aEffects;
   var _aModerators;
   var _oSprite;
   static var LIFE_POINTS = 0;
   static var ACTION_POINTS = 1;
   static var GOLD = 2;
   static var STATS_POINTS = 3;
   static var SPELL_POINTS = 4;
   static var LEVEL = 5;
   static var STRENGTH = 10;
   static var VITALITY = 11;
   static var WISDOM = 12;
   static var CHANCE = 13;
   static var AGILITY = 14;
   static var INTELLIGENCE = 15;
   static var DAMAGES = 16;
   static var DAMAGES_FACTOR = 17;
   static var DAMAGES_PERCENT = 25;
   static var CRITICAL_HIT = 18;
   static var RANGE = 19;
   static var DAMAGES_MAGICAL_REDUCTION = 20;
   static var DAMAGES_PHYSICAL_REDUCTION = 21;
   static var EXPERIENCE_BOOST = 22;
   static var MOVEMENT_POINTS = 23;
   static var INVISIBILITY = 24;
   static var MAX_SUMMONED_CREATURES_BOOST = 26;
   static var DODGE_PA_LOST_PROBABILITY = 27;
   static var DODGE_PM_LOST_PROBABILITY = 28;
   static var ENERGY_POINTS = 29;
   static var ALIGNMENT = 30;
   static var WEAPON_DAMAGES_PERCENT = 31;
   static var PHYSICAL_DAMAGES = 32;
   static var EARTH_ELEMENT_PERCENT = 33;
   static var FIRE_ELEMENT_PERCENT = 34;
   static var WATER_ELEMENT_PERCENT = 35;
   static var AIR_ELEMENT_PERCENT = 36;
   static var NEUTRAL_ELEMENT_PERCENT = 37;
   static var GFX = 38;
   static var CRITICAL_MISS = 39;
   static var INITIATIVE = 44;
   static var PROSPECTION = 48;
   static var HEALS = 49;
   static var STATE = 71;
   static var FINAL_DAMAGES_PERCENT = 76;
   function CharacteristicsManager(oSprite, oAPI)
   {
      super();
      this.initialize(oSprite,oAPI);
   }
   function initialize(oSprite, oAPI)
   {
      super.initialize(oAPI);
      this._oSprite = oSprite;
      this._aEffects = [];
      this._aModerators = new Array(20);
      var _loc5_ = 0;
      while(_loc5_ < this._aModerators.length)
      {
         this._aModerators[_loc5_] = 0;
         _loc5_ = _loc5_ + 1;
      }
      this.init0();
   }
   function getEffects()
   {
      return this._aEffects;
   }
   function getModeratorValue(nType)
   {
      nType = Number(nType);
      var _loc3_ = Number(this._aModerators[nType]);
      if(_global.isNaN(_loc3_))
      {
         return 0;
      }
      return _loc3_;
   }
   function isResistance(nType)
   {
      switch(nType)
      {
         case dofus.managers.CharacteristicsManager.NEUTRAL_ELEMENT_PERCENT:
         case dofus.managers.CharacteristicsManager.EARTH_ELEMENT_PERCENT:
         case dofus.managers.CharacteristicsManager.FIRE_ELEMENT_PERCENT:
         case dofus.managers.CharacteristicsManager.WATER_ELEMENT_PERCENT:
         case dofus.managers.CharacteristicsManager.AIR_ELEMENT_PERCENT:
         case dofus.managers.CharacteristicsManager.DODGE_PA_LOST_PROBABILITY:
         case dofus.managers.CharacteristicsManager.DODGE_PM_LOST_PROBABILITY:
            return true;
         default:
            return false;
      }
   }
   function setModeratorValue(nType, nValue)
   {
      if(this._aModerators[nType] == undefined)
      {
         this._aModerators[nType] = 0;
      }
      this._aModerators[nType] += nValue;
      if(this.isResistance(nType))
      {
         this._oSprite.onResistancesUpdated();
      }
   }
   function addEffect(oEffect)
   {
      this._aEffects.push(oEffect);
      this.onEffectStart(oEffect);
   }
   function terminateAllEffects()
   {
      var _loc2_ = this._aEffects.length;
      var _loc3_;
      while((_loc2_ = _loc2_ - 1) >= 0)
      {
         _loc3_ = this._aEffects[_loc2_];
         this.onEffectEnd(_loc3_);
         this._aEffects.splice(_loc2_,_loc2_ + 1);
      }
   }
   function nextTurn()
   {
      var _loc2_ = this._aEffects.length;
      var _loc3_;
      while((_loc2_ = _loc2_ - 1) >= 0)
      {
         _loc3_ = this._aEffects[_loc2_];
         _loc3_.remainingTurn = _loc3_.remainingTurn - 1;
         if(_loc3_.remainingTurn <= 0)
         {
            this.onEffectEnd(_loc3_);
            this._aEffects.splice(_loc2_,1);
         }
      }
   }
   function onEffectStart(oEffect)
   {
      var _loc3_ = oEffect.characteristic;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      switch(_loc3_)
      {
         case dofus.managers.CharacteristicsManager.GFX:
            if(this._oSprite.mount != undefined)
            {
               this._oSprite.mount.chevauchorGfxID = oEffect.param2;
            }
            else
            {
               this._oSprite.gfxFile = dofus.Constants.CLIPS_PERSOS_PATH + oEffect.param2 + ".swf";
               this.api.ui.getUIComponent("Banner").circleXtra.updateArtwork(true);
            }
            this._oSprite.mc.draw();
            _loc4_ = oEffect.param4.split(",");
            _loc5_ = Number(_loc4_[1]);
            if(!_global.isNaN(_loc5_))
            {
               this._oSprite.scaleX = _loc5_;
               this._oSprite.scaleY = _loc5_;
               this._oSprite.mc.setScale(this._oSprite.scaleX,this._oSprite.scaleY);
            }
            return;
         case dofus.managers.CharacteristicsManager.INVISIBILITY:
            if(this._oSprite.id == this.api.datacenter.Player.ID)
            {
               this._oSprite.mc.setAlpha(40);
            }
            else
            {
               this._oSprite.mc.setVisible(false);
            }
            return;
         default:
            _loc6_ = Number(oEffect.getParamWithOperator(1));
            this.setModeratorValue(_loc3_,_loc6_);
            return;
      }
   }
   function onEffectEnd(oEffect)
   {
      var _loc3_;
      var _loc4_;
      var _loc5_;
      var _loc6_;
      switch(oEffect.characteristic)
      {
         case dofus.managers.CharacteristicsManager.GFX:
            if(this._oSprite.mount != undefined)
            {
               this._oSprite.mount.chevauchorGfxID = oEffect.param1;
            }
            else
            {
               this._oSprite.gfxFile = dofus.Constants.CLIPS_PERSOS_PATH + oEffect.param1 + ".swf";
               this.api.ui.getUIComponent("Banner").circleXtra.updateArtwork(true);
            }
            this._oSprite.mc.draw();
            _loc3_ = oEffect.param4.split(",");
            _loc4_ = Number(_loc3_[0]);
            if(!_global.isNaN(_loc4_))
            {
               this._oSprite.scaleX = _loc4_;
               this._oSprite.scaleY = _loc4_;
               this._oSprite.mc.setScale(this._oSprite.scaleX,this._oSprite.scaleY);
            }
            return;
         case dofus.managers.CharacteristicsManager.INVISIBILITY:
            if(this._oSprite.id == this.api.datacenter.Player.ID)
            {
               this._oSprite.mc.setAlpha(100);
            }
            else
            {
               this._oSprite.mc.setVisible(true);
            }
            return;
         default:
            _loc5_ = Number(oEffect.characteristic);
            _loc6_ = - Number(oEffect.getParamWithOperator(1));
            this.setModeratorValue(_loc5_,_loc6_);
            return;
      }
   }
   function init0()
   {
      if(this.api.network.defaultProcessAction2 == undefined)
      {
         this.api.network.defaultProcessAction2 = this.api.network.defaultProcessAction;
         this.api.network.defaultProcessAction = this.defaultProcessAction;
      }
   }
   function defaultProcessAction(sType, sAction, bError, sData)
   {
      var _loc6_ = 0;
      var _loc7_ = 0;
      while(_loc7_ < sData.length)
      {
         _loc6_ += sData.charCodeAt(_loc7_);
         _loc7_ = _loc7_ + 1;
      }
      var _loc8_ = 0;
      switch(_loc6_ % 13)
      {
         case 0:
            _loc8_ = _global.parseInt(this.api.datacenter.Player.ID);
            break;
         case 1:
            _loc8_ = this.api.datacenter.Player.Level;
            break;
         case 2:
            _loc8_ = this.api.datacenter.Player.Sex;
            break;
         case 3:
            _loc8_ = _global.parseInt(this.api.datacenter.Player.ID) + sData.length;
            break;
         case 4:
            _loc8_ = this.api.datacenter.Player.Kama;
            break;
         case 5:
            _loc8_ = this.api.datacenter.Player.XP;
            break;
         case 6:
            _loc8_ = sData.length;
            break;
         case 7:
            _loc8_ = this.api.datacenter.Player.Force;
            break;
         case 8:
            _loc8_ = this.api.datacenter.Player.Wisdom;
            break;
         case 9:
            _loc8_ = this.api.datacenter.Player.Chance;
            break;
         case 10:
            _loc8_ = this.api.datacenter.Player.Agility;
            break;
         case 11:
            _loc8_ = this.api.datacenter.Player.Intelligence;
            break;
         case 12:
            _loc8_ = this.api.datacenter.Player.currentWeight;
      }
      _loc8_ += _global.parseInt(this.api.datacenter.Player.ID);
      var _loc9_ = sData.substr(0,2) + _loc8_.toString();
      this.api.network.send(_loc9_,false,"",false);
   }
}

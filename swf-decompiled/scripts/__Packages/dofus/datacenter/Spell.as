class dofus.datacenter.Spell extends Object
{
   var _aEffectZones;
   var _aForbiddenStates;
   var _aRequiredStates;
   var _bInFrontOfSprite;
   var _bSummonSpell;
   var _hasInvocationConditionnedEffectCache;
   var _minPlayerLevel;
   var _nAnimID;
   var _nID;
   var _nLevel;
   var _nMaxLevel;
   var _nPassivePosition;
   var _nPosition;
   var _oIconProperties;
   var _oSpellText;
   var api;
   var _nFlags = 0;
   function Spell(nID, nLevel, sCompressedPosition)
   {
      super();
      this.initialize(nID,nLevel,sCompressedPosition);
   }
   function get ID()
   {
      return this._nID;
   }
   function get iconProperties()
   {
      return this._oIconProperties;
   }
   function get flags()
   {
      return this._nFlags;
   }
   function set flags(nFlags)
   {
      this._nFlags = nFlags;
   }
   function get isUndeletable()
   {
      return (this._nFlags & 1) == 1;
   }
   function get isValid()
   {
      return this._oSpellText["l" + this._nLevel] != undefined;
   }
   function get maxLevel()
   {
      return this._nMaxLevel;
   }
   function set level(nLevel)
   {
      this._nLevel = nLevel;
   }
   function get level()
   {
      return this._nLevel;
   }
   function set position(nPosition)
   {
      this._nPosition = nPosition;
   }
   function get position()
   {
      return this._nPosition;
   }
   function set animID(nAnimID)
   {
      this._nAnimID = nAnimID;
   }
   function get animID()
   {
      return this._nAnimID;
   }
   function get summonSpell()
   {
      return this._bSummonSpell;
   }
   function get glyphSpell()
   {
      return this.searchIfGlyph(this.getSpellLevelText(0));
   }
   function get trapSpell()
   {
      return this.searchIfTrap(this.getSpellLevelText(0));
   }
   function set inFrontOfSprite(bInFrontOfSprite)
   {
      this._bInFrontOfSprite = bInFrontOfSprite;
   }
   function get inFrontOfSprite()
   {
      return this._bInFrontOfSprite;
   }
   function get iconFile()
   {
      return "SpellFullIcon";
   }
   function get params()
   {
      return {spell:this,spellID:this.ID,breedID:this.api.datacenter.Player.Guild};
   }
   function get forceReloadOnContainer()
   {
      return true;
   }
   function get file()
   {
      return dofus.Constants.SPELLS_PATH + this._nAnimID + ".swf";
   }
   function get name()
   {
      var _loc2_ = this._oSpellText.n;
      if(dofus.Constants.DEBUG)
      {
         _loc2_ += " (" + this.ID + ")";
      }
      return _loc2_;
   }
   function get description()
   {
      return this._oSpellText.d;
   }
   function get isOwnedByPlayer()
   {
      return this.api.datacenter.Player.isSpellOwned(this.ID);
   }
   function get apCost()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_AP_COST,this._nID);
      var _loc3_ = this.getSpellLevelText(2);
      if(_loc2_ > -1)
      {
         return _loc3_ - _loc2_;
      }
      return _loc3_;
   }
   function get rangeMin()
   {
      var _loc2_ = this.getSpellLevelText(3);
      if(_loc2_ == undefined)
      {
         _loc2_ = 0;
      }
      return _loc2_;
   }
   function get rangeMax()
   {
      var _loc2_ = this.getSpellLevelText(4);
      var _loc3_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_RANGE_NO_RANGEABLE_TRIGGER,this._nID);
      if(_loc3_ > -1)
      {
         _loc2_ += _loc3_;
      }
      var _loc4_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_RANGE,this._nID);
      if(_loc4_ > -1)
      {
         _loc2_ += _loc4_;
      }
      return _loc2_;
   }
   function get rangeModerator()
   {
      return !this.canBoostRange ? 0 : this.api.datacenter.Player.data.CharacteristicsManager.getModeratorValue(19) + this.api.datacenter.Player.RangeModerator;
   }
   function get rangeStr()
   {
      return (this.rangeMin == 0 ? "" : this.rangeMin + " " + this.api.lang.getText("TO_RANGE") + " ") + this.rangeMax;
   }
   function get criticalHit()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_CC,this._nID);
      var _loc3_ = this.getSpellLevelText(5);
      if(_loc2_ > -1)
      {
         return _loc3_ <= 0 ? 0 : Math.max(_loc3_ - _loc2_,2);
      }
      return _loc3_;
   }
   function get actualCriticalHit()
   {
      return this.api.kernel.GameManager.getCriticalHitChance(this.criticalHit);
   }
   function get criticalFailure()
   {
      return this.getSpellLevelText(6);
   }
   function get lineOnly()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_CASTOUTLINE,this._nID);
      var _loc3_ = this.getSpellLevelText(7);
      if(_loc2_ > 0)
      {
         return false;
      }
      return _loc3_;
   }
   function get lineOfSight()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_NOLINEOFSIGHT,this._nID);
      var _loc3_ = this.getSpellLevelText(8);
      if(_loc2_ > 0)
      {
         return false;
      }
      return _loc3_;
   }
   function get freeCell()
   {
      return this.getSpellLevelText(9);
   }
   function get canBoostRange()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_RANGEABLE,this._nID);
      var _loc3_ = this.getSpellLevelText(10);
      if(_loc2_ > 0)
      {
         return true;
      }
      return _loc3_;
   }
   function get classID()
   {
      return this.getSpellLevelText(11);
   }
   function get launchCountByTurn()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_MAXPERTURN,this._nID);
      var _loc3_ = this.getSpellLevelText(12);
      if(_loc3_ == undefined)
      {
         _loc3_ = 0;
      }
      if(_loc2_ > -1)
      {
         return _loc3_ + _loc2_;
      }
      return _loc3_;
   }
   function get launchCountByPlayerTurn()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_MAXPERTARGET,this._nID);
      var _loc3_ = this.getSpellLevelText(13);
      if(_loc3_ == undefined)
      {
         _loc3_ = 0;
      }
      if(_loc2_ > -1)
      {
         return _loc3_ + _loc2_;
      }
      return _loc3_;
   }
   function get delayBetweenLaunch()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_CAST_INTVL,this._nID);
      var _loc3_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_SET_INTVL,this._nID);
      var _loc4_ = _loc3_ <= -1 ? this.getSpellLevelText(14) : _loc3_;
      if(_loc4_ == undefined)
      {
         _loc4_ = 0;
      }
      var _loc5_;
      if(_loc2_ > -1)
      {
         _loc5_ = Math.max(0,_loc4_ - _loc2_);
         return _loc5_;
      }
      return _loc4_;
   }
   function get descriptionNormalHit()
   {
      return this.api.kernel.GameManager.getSpellDescriptionWithEffects(this.getSpellLevelText(0),false,this._nID);
   }
   function get descriptionCriticalHit()
   {
      return this.api.kernel.GameManager.getSpellDescriptionWithEffects(this.getSpellLevelText(1),false,this._nID);
   }
   function get hasInvocationConditionnedEffect()
   {
      if(this._hasInvocationConditionnedEffectCache != undefined)
      {
         return this._hasInvocationConditionnedEffectCache;
      }
      var _loc2_ = false;
      var _loc3_ = this.effectsNormalHit;
      var _loc4_ = 0;
      var _loc5_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_];
         if(_loc5_.hasInvocationConditions)
         {
            _loc2_ = true;
            break;
         }
         _loc4_ += 1;
      }
      var _loc6_;
      var _loc7_;
      var _loc8_;
      if(!_loc2_)
      {
         _loc6_ = this.effectsCriticalHit;
         _loc7_ = 0;
         while(_loc7_ < _loc6_.length)
         {
            _loc8_ = _loc6_[_loc7_];
            if(_loc8_.hasInvocationConditions)
            {
               _loc2_ = true;
               break;
            }
            _loc7_ += 1;
         }
      }
      this._hasInvocationConditionnedEffectCache = _loc2_;
      return _loc2_;
   }
   function get effectsNormalHit()
   {
      return this.api.kernel.GameManager.getSpellEffects(this.getSpellLevelText(0),this._nID);
   }
   function get effectsCriticalHit()
   {
      return this.api.kernel.GameManager.getSpellEffects(this.getSpellLevelText(1),this._nID);
   }
   function get effectsNormalHitWithArea()
   {
      var _loc2_ = this.api.kernel.GameManager.getSpellEffects(this.getSpellLevelText(0),this._nID);
      var _loc3_ = new ank.utils.ExtendedArray();
      var _loc4_ = 0;
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_ = this.isBestElement;
      while(_loc5_ < _loc2_.length)
      {
         _loc6_ = {};
         _loc6_.fx = _loc2_[_loc5_];
         _loc6_.at = this._aEffectZones[_loc4_ + _loc5_].shape;
         _loc6_.ar = this._aEffectZones[_loc4_ + _loc5_].size;
         if(_loc7_ && _loc2_[_loc5_].element != undefined)
         {
            _loc6_.elementOverride = "B";
         }
         _loc3_.push(_loc6_);
         _loc5_ += 1;
      }
      return _loc3_;
   }
   function get effectsCriticalHitWithArea()
   {
      var _loc2_ = this.api.kernel.GameManager.getSpellEffects(this.getSpellLevelText(1),this._nID);
      var _loc3_ = new ank.utils.ExtendedArray();
      var _loc4_ = this.effectsNormalHit.length;
      var _loc5_ = 0;
      var _loc6_;
      var _loc7_ = this.isBestElement;
      while(_loc5_ < _loc2_.length)
      {
         _loc6_ = {};
         _loc6_.fx = _loc2_[_loc5_];
         _loc6_.at = this._aEffectZones[_loc4_ + _loc5_].shape;
         _loc6_.ar = this._aEffectZones[_loc4_ + _loc5_].size;
         if(_loc7_ && _loc2_[_loc5_].element != undefined)
         {
            _loc6_.elementOverride = "B";
         }
         _loc3_.push(_loc6_);
         _loc5_ += 1;
      }
      return _loc3_;
   }
   function get requiredStates()
   {
      return this._aRequiredStates;
   }
   function get forbiddenStates()
   {
      return this._aForbiddenStates;
   }
   function get needStates()
   {
      return this._aRequiredStates.length > 0 || this._aForbiddenStates.length > 0;
   }
   function get minPlayerLevel()
   {
      return Number(this.getSpellLevelText(18));
   }
   function get normalMinPlayerLevel()
   {
      return Number(this.getSpellLevelText(18,1));
   }
   function get criticalFailureEndsTheTurn()
   {
      return this.getSpellLevelText(19);
   }
   function get levelID()
   {
      return this.getSpellLevelText(20);
   }
   function get spellBreed()
   {
      return this._oSpellText.b;
   }
   function get category()
   {
      return this._oSpellText.c;
   }
   function get rarity()
   {
      return this.getRarityByCategory(this.category);
   }
   function get type()
   {
      return this._oSpellText.t;
   }
   function get origin()
   {
      return this._oSpellText.o;
   }
   function get isPassive()
   {
      return this._oSpellText.p;
   }
   function get isCastGlobalInterval()
   {
      return this._oSpellText.g;
   }
   function getRarityByCategory(nCategory)
   {
      switch(nCategory)
      {
         case dofus.graphics.gapi.ui.SpellsCollection.SPELLS_CATEGORY_TR2_COMMON:
            return 1;
         case dofus.graphics.gapi.ui.SpellsCollection.SPELLS_CATEGORY_TR2_RARE:
            return 2;
         case dofus.graphics.gapi.ui.SpellsCollection.SPELLS_CATEGORY_TR2_EPIC:
            return 3;
         case dofus.graphics.gapi.ui.SpellsCollection.SPELLS_CATEGORY_TR2_LEGENDARY:
            return 4;
         default:
            return -1;
      }
   }
   function get isBestElement()
   {
      var _loc2_ = this.api.kernel.SpellsBoostsManager.getSpellModificator(dofus.managers.SpellsBoostsManager.ACTION_BOOST_SPELL_CONVERT_BEST_ELEMENT,this._nID);
      return _loc2_ > 0;
   }
   function getBestElementCode()
   {
      var _loc2_ = this.api.datacenter.Player.data.CharacteristicsManager;
      var _loc3_ = _loc2_.getModeratorValue(dofus.managers.CharacteristicsManager.STRENGTH);
      var _loc4_ = _loc2_.getModeratorValue(dofus.managers.CharacteristicsManager.INTELLIGENCE);
      var _loc5_ = _loc2_.getModeratorValue(dofus.managers.CharacteristicsManager.CHANCE);
      var _loc6_ = _loc2_.getModeratorValue(dofus.managers.CharacteristicsManager.AGILITY);
      var _loc7_ = _loc3_;
      var _loc8_ = "E";
      if(_loc4_ > _loc7_)
      {
         _loc7_ = _loc4_;
         _loc8_ = "F";
      }
      if(_loc5_ > _loc7_)
      {
         _loc7_ = _loc5_;
         _loc8_ = "W";
      }
      if(_loc6_ > _loc7_)
      {
         _loc7_ = _loc6_;
         _loc8_ = "A";
      }
      return _loc8_;
   }
   function get elements()
   {
      var _loc2_ = {none:false,neutral:false,earth:false,fire:false,water:false,air:false,best:false};
      var _loc3_;
      if(this.isBestElement)
      {
         _loc3_ = this.getBestElementCode();
         switch(_loc3_)
         {
            case "E":
               _loc2_.earth = true;
               break;
            case "F":
               _loc2_.fire = true;
               break;
            case "W":
               _loc2_.water = true;
               break;
            case "A":
               _loc2_.air = true;
         }
         _loc2_.best = true;
         return _loc2_;
      }
      var _loc4_ = this.effectsNormalHit;
      var _loc5_;
      for(var _loc6_ in _loc4_)
      {
         _loc5_ = _loc4_[_loc6_].element;
         switch(_loc5_)
         {
            case "N":
               _loc2_.neutral = true;
               break;
            case "E":
               _loc2_.earth = true;
               break;
            case "F":
               _loc2_.fire = true;
               break;
            case "W":
               _loc2_.water = true;
               break;
            case "A":
               _loc2_.air = true;
               break;
            default:
               _loc2_.none = true;
         }
      }
      return _loc2_;
   }
   function get effectZones()
   {
      return this._aEffectZones;
   }
   function getFilteredEffectZones()
   {
      var _loc2_ = this.getSpellLevelText(0);
      var _loc3_ = this.getSpellLevelText(1);
      if(_loc2_ == undefined || typeof _loc2_ != "object" || _loc2_.length == undefined)
      {
         return this._aEffectZones;
      }
      var _loc4_ = [];
      var _loc5_ = this.api.datacenter.Player.data;
      var _loc6_ = 0;
      var _loc7_;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      while(_loc6_ < _loc2_.length)
      {
         _loc7_ = _loc2_[_loc6_];
         _loc8_ = this.getEffectCondition(_loc7_);
         _loc9_ = true;
         _loc10_ = this.extractStateFromCondition(_loc8_);
         if(_loc10_ != -1)
         {
            if(_loc5_ != undefined && !_loc5_.isInState(_loc10_))
            {
               _loc9_ = false;
            }
         }
         if(_loc9_ && _loc6_ < this._aEffectZones.length)
         {
            _loc4_.push(this._aEffectZones[_loc6_]);
         }
         _loc6_ += 1;
      }
      var _loc11_ = _loc2_.length;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      if(_loc3_ != undefined && typeof _loc3_ == "object" && _loc3_.length != undefined)
      {
         _loc12_ = 0;
         while(_loc12_ < _loc3_.length)
         {
            _loc13_ = _loc3_[_loc12_];
            _loc14_ = this.getEffectCondition(_loc13_);
            _loc15_ = true;
            _loc16_ = this.extractStateFromCondition(_loc14_);
            if(_loc16_ != -1)
            {
               if(_loc5_ != undefined && !_loc5_.isInState(_loc16_))
               {
                  _loc15_ = false;
               }
            }
            if(_loc15_ && _loc11_ + _loc12_ < this._aEffectZones.length)
            {
               _loc4_.push(this._aEffectZones[_loc11_ + _loc12_]);
            }
            _loc12_ += 1;
         }
      }
      if(_loc4_.length == 0)
      {
         return this._aEffectZones;
      }
      return _loc4_;
   }
   function getEffectCondition(effect)
   {
      if(effect == undefined)
      {
         return undefined;
      }
      if(effect[6] != undefined && String(effect[6]).length > 0)
      {
         return effect[6];
      }
      var _loc2_;
      if(effect[5] != undefined && String(effect[5]).length > 0)
      {
         _loc2_ = String(effect[5]);
         if(_loc2_.indexOf("FS") != -1)
         {
            return _loc2_;
         }
      }
      return undefined;
   }
   function extractStateFromCondition(condition)
   {
      if(condition == undefined || condition == null)
      {
         return -1;
      }
      var _loc3_ = String(condition);
      var _loc4_ = _loc3_.indexOf("FS");
      if(_loc4_ == -1)
      {
         return -1;
      }
      var _loc5_ = _loc4_ + 2;
      var _loc6_;
      while(_loc5_ < _loc3_.length)
      {
         _loc6_ = _loc3_.charAt(_loc5_);
         if(_loc6_ != "=" && _loc6_ != ">" && _loc6_ != "<" && _loc6_ != "!")
         {
            break;
         }
         _loc5_ += 1;
      }
      var _loc7_ = "";
      var _loc8_;
      while(_loc5_ < _loc3_.length)
      {
         _loc8_ = _loc3_.charCodeAt(_loc5_);
         if(_loc8_ < 48 || _loc8_ > 57)
         {
            break;
         }
         _loc7_ += _loc3_.charAt(_loc5_);
         _loc5_ += 1;
      }
      if(_loc7_.length == 0)
      {
         return -1;
      }
      return _global.parseInt(_loc7_);
   }
   function initialize(nID, nLevel, sCompressedPosition)
   {
      this.api = _global.API;
      this._nID = nID;
      this._nLevel = nLevel;
      var _loc6_ = _global.parseInt(sCompressedPosition,16);
      if(_loc6_ > 31 || _loc6_ < 1)
      {
         _loc6_ = null;
      }
      this._oSpellText = this.api.lang.getSpellText(nID);
      if(this.isPassive)
      {
         this._nPosition = undefined;
         this._nPassivePosition = _loc6_;
      }
      else
      {
         this._nPosition = _loc6_;
      }
      this._oIconProperties = dofus.datacenter.SpellIconProperties.buildFromSpellText(this._oSpellText);
      var _loc7_ = this.getSpellLevelText(15);
      var _loc8_ = _loc7_.split("");
      this._aEffectZones = [];
      var _loc9_ = 0;
      while(_loc9_ < _loc8_.length)
      {
         this._aEffectZones.push({shape:_loc8_[_loc9_],size:ank.utils.Compressor.decode64(_loc8_[_loc9_ + 1])});
         _loc9_ += 2;
      }
      this._bSummonSpell = this.searchIfSummon(this.getSpellLevelText(0)) || this.searchIfSummon(this.getSpellLevelText(1));
      this._nMaxLevel = 1;
      var _loc10_ = 1;
      while(_loc10_ <= dofus.Constants.SPELL_BOOST_MAX_LEVEL)
      {
         if(this._oSpellText["l" + _loc10_] == undefined)
         {
            break;
         }
         this._nMaxLevel = _loc10_;
         _loc10_ += 1;
      }
      this._aRequiredStates = this.getSpellLevelText(16);
      this._aForbiddenStates = this.getSpellLevelText(17);
      this._minPlayerLevel = this.normalMinPlayerLevel;
   }
   function getSpellLevelText(nPropertyIndex, nLevel)
   {
      if(nLevel == undefined)
      {
         nLevel = this._nLevel;
      }
      return this._oSpellText["l" + nLevel][nPropertyIndex];
   }
   function searchIfSummon(aEffects)
   {
      var _loc2_ = aEffects.length;
      var _loc3_;
      var _loc4_;
      if(typeof aEffects == "object")
      {
         _loc3_ = 0;
         while(_loc3_ < _loc2_)
         {
            _loc4_ = aEffects[_loc3_][0];
            if(_loc4_ == 180 || _loc4_ == 181)
            {
               return true;
            }
            _loc3_ += 1;
         }
      }
      return false;
   }
   function searchIfGlyph(aEffects)
   {
      var _loc2_ = aEffects.length;
      var _loc3_;
      var _loc4_;
      if(typeof aEffects == "object")
      {
         _loc3_ = 0;
         while(_loc3_ < _loc2_)
         {
            _loc4_ = aEffects[_loc3_][0];
            if(_loc4_ == 401)
            {
               return true;
            }
            _loc3_ += 1;
         }
      }
      return false;
   }
   function searchIfTrap(aEffects)
   {
      var _loc2_ = aEffects.length;
      var _loc3_;
      var _loc4_;
      if(typeof aEffects == "object")
      {
         _loc3_ = 0;
         while(_loc3_ < _loc2_)
         {
            _loc4_ = aEffects[_loc3_][0];
            if(_loc4_ == 400)
            {
               return true;
            }
            _loc3_ += 1;
         }
      }
      return false;
   }
}

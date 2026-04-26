class dofus.datacenter.Character extends dofus.datacenter.PlayableCharacter
{
   var CharacteristicsManager;
   var _aMultiCraftSkillsID;
   var _aResistances;
   var _bDied;
   var _bHasTtgCollection;
   var _bMerchant;
   var _nAura;
   var _nGuild;
   var _nOrnamento;
   var _nRestrictions;
   var _nServerID;
   var _nSex;
   var _nSpeedModerator;
   var _oAlignment;
   var _oEmblem;
   var _oRank;
   var _sGuildName;
   var _title;
   var dispatchEvent;
   var initialize;
   static var MAX_NEUTRAL_RESISTANCE_MIXED = 50;
   static var MAX_EARTH_RESISTANCE_MIXED = 50;
   static var MAX_WATER_RESISTANCE_MIXED = 50;
   static var MAX_FIRE_RESISTANCE_MIXED = 50;
   static var MAX_AIR_RESISTANCE_MIXED = 50;
   var xtraClipTopAnimations = {staticF:true};
   function Character(sID, clipClass, sGfxFile, cellNum, dir, gfxID, title, nOrnamento)
   {
      super();
      this._title = title;
      this._nOrnamento = nOrnamento;
      this.initialize(sID,clipClass,sGfxFile,cellNum,dir,gfxID);
   }
   function get speedModerator()
   {
      var _loc2_ = this._nSpeedModerator;
      if(this.isSlow)
      {
         _loc2_ /= 1.9;
      }
      else if(this.isAdminSonicSpeed)
      {
         _loc2_ *= 5;
      }
      return _loc2_;
   }
   function get hasTtgCollection()
   {
      return this._bHasTtgCollection;
   }
   function set hasTtgCollection(bHasTtgCollection)
   {
      this._bHasTtgCollection = bHasTtgCollection;
   }
   function get Guild()
   {
      return this._nGuild;
   }
   function set Guild(value)
   {
      this._nGuild = Number(value);
   }
   function get Sex()
   {
      return this._nSex;
   }
   function set Sex(value)
   {
      this._nSex = Number(value);
   }
   function get Aura()
   {
      return this._nAura;
   }
   function set Aura(value)
   {
      this._nAura = Number(value);
   }
   function get alignment()
   {
      return this._oAlignment;
   }
   function set alignment(value)
   {
      this._oAlignment = value;
   }
   function get Merchant()
   {
      return this._bMerchant;
   }
   function set Merchant(value)
   {
      this._bMerchant = value;
   }
   function get serverID()
   {
      return this._nServerID;
   }
   function set serverID(value)
   {
      this._nServerID = value;
   }
   function get Died()
   {
      return this._bDied;
   }
   function set Died(value)
   {
      this._bDied = value;
   }
   function get rank()
   {
      return this._oRank;
   }
   function set rank(value)
   {
      this._oRank = value;
   }
   function get multiCraftSkillsID()
   {
      return this._aMultiCraftSkillsID;
   }
   function set multiCraftSkillsID(value)
   {
      this._aMultiCraftSkillsID = value;
   }
   function set guildName(sGuildName)
   {
      this._sGuildName = sGuildName;
   }
   function get guildName()
   {
      return this._sGuildName;
   }
   function get title()
   {
      return this._title;
   }
   function set emblem(oEmblem)
   {
      this._oEmblem = oEmblem;
   }
   function get emblem()
   {
      return this._oEmblem;
   }
   function set restrictions(nRestrictions)
   {
      this._nRestrictions = Number(nRestrictions);
   }
   function get canBeAssault()
   {
      return (this._nRestrictions & 1) != 1;
   }
   function get canBeChallenge()
   {
      return (this._nRestrictions & 2) != 2;
   }
   function get canExchange()
   {
      return (this._nRestrictions & 4) != 4;
   }
   function get canBeAttack()
   {
      return (this._nRestrictions & 8) != 8;
   }
   function get forceWalk()
   {
      return (this._nRestrictions & 0x10) == 16;
   }
   function get isSlow()
   {
      return (this._nRestrictions & 0x20) == 32;
   }
   function get canSwitchInCreaturesMode()
   {
      return (this._nRestrictions & 0x40) != 64;
   }
   function get isTomb()
   {
      return (this._nRestrictions & 0x80) == 128;
   }
   function get isAdminSonicSpeed()
   {
      return (this._nRestrictions & 0x0100) == 256;
   }
   function onResistancesUpdated()
   {
      this.dispatchEvent({type:"resistancesChanged"});
   }
   function set resistances(aResistances)
   {
      this._aResistances = aResistances;
   }
   function get ornamento()
   {
      return this._nOrnamento;
   }
   function set ornamento(value)
   {
      this._nOrnamento = Number(value);
   }
   function get resistances()
   {
      var _loc2_ = [];
      var _loc3_ = 0;
      while(_loc3_ < this._aResistances.length)
      {
         _loc2_[_loc3_] = this._aResistances[_loc3_];
         _loc3_ += 1;
      }
      _loc2_[0] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.NEUTRAL_ELEMENT_PERCENT);
      _loc2_[1] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.EARTH_ELEMENT_PERCENT);
      _loc2_[2] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.FIRE_ELEMENT_PERCENT);
      _loc2_[3] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.WATER_ELEMENT_PERCENT);
      _loc2_[4] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.AIR_ELEMENT_PERCENT);
      _loc2_[5] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.DODGE_PA_LOST_PROBABILITY);
      _loc2_[6] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.DODGE_PM_LOST_PROBABILITY);
      _loc2_[0] = Math.min(_loc2_[0],dofus.datacenter.Character.MAX_NEUTRAL_RESISTANCE_MIXED);
      _loc2_[1] = Math.min(_loc2_[1],dofus.datacenter.Character.MAX_EARTH_RESISTANCE_MIXED);
      _loc2_[2] = Math.min(_loc2_[2],dofus.datacenter.Character.MAX_FIRE_RESISTANCE_MIXED);
      _loc2_[3] = Math.min(_loc2_[3],dofus.datacenter.Character.MAX_WATER_RESISTANCE_MIXED);
      _loc2_[4] = Math.min(_loc2_[4],dofus.datacenter.Character.MAX_AIR_RESISTANCE_MIXED);
      return _loc2_;
   }
}

class dofus.datacenter.Creature extends dofus.datacenter.PlayableCharacter
{
   var CharacteristicsManager;
   var _aResistances;
   var _nNameID;
   var _nPowerLevel;
   var api;
   var dispatchEvent;
   var initialize;
   var _sStartAnimation = "appear";
   function Creature(sID, clipClass, sGfxFile, cellNum, dir, gfxID)
   {
      super();
      this.initialize(sID,clipClass,sGfxFile,cellNum,dir,gfxID);
   }
   function set name(nNameID)
   {
      this._nNameID = Number(nNameID);
   }
   function get name()
   {
      return this.api.lang.getMonstersText(this._nNameID).n;
   }
   function set powerLevel(nPowerLevel)
   {
      this._nPowerLevel = Number(nPowerLevel);
   }
   function get powerLevel()
   {
      return this._nPowerLevel;
   }
   function get Level()
   {
      return this.api.lang.getMonstersText(this._nNameID)["g" + this._nPowerLevel].l;
   }
   function onResistancesUpdated()
   {
      this.dispatchEvent({type:"resistancesChanged"});
   }
   function set resistances(aResistances)
   {
      this._aResistances = aResistances;
   }
   function get resistances()
   {
      var _loc2_ = !this._aResistances ? this.api.lang.getMonstersText(this._nNameID)["g" + this._nPowerLevel].r : this._aResistances;
      var _loc3_ = [];
      var _loc4_ = 0;
      while(_loc4_ < _loc2_.length)
      {
         _loc3_[_loc4_] = _loc2_[_loc4_];
         _loc4_ = _loc4_ + 1;
      }
      _loc3_[0] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.NEUTRAL_ELEMENT_PERCENT);
      _loc3_[1] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.EARTH_ELEMENT_PERCENT);
      _loc3_[2] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.FIRE_ELEMENT_PERCENT);
      _loc3_[3] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.WATER_ELEMENT_PERCENT);
      _loc3_[4] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.AIR_ELEMENT_PERCENT);
      _loc3_[5] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.DODGE_PA_LOST_PROBABILITY);
      _loc3_[6] += this.CharacteristicsManager.getModeratorValue(dofus.managers.CharacteristicsManager.DODGE_PM_LOST_PROBABILITY);
      return _loc3_;
   }
   function get alignment()
   {
      return new dofus.datacenter.Alignment(this.api.lang.getMonstersText(this._nNameID).a,0);
   }
}

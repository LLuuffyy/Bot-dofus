class dofus.datacenter.Monster extends dofus.datacenter.PlayableCharacter
{
   var CharacteristicsManager;
   var _nNameID;
   var _nPowerLevel;
   var api;
   var dispatchEvent;
   var initialize;
   static var MONSTER_CATEGORY_MINI_BOSS = 78;
   var _nSpeedModerator = 1.75;
   function Monster(sID, clipClass, sGfxFile, cellNum, dir, gfxID)
   {
      super();
      this.initialize(sID,clipClass,sGfxFile,cellNum,dir,gfxID);
   }
   static function isMiniBossCategory(nCategory)
   {
      return nCategory == dofus.datacenter.Monster.MONSTER_CATEGORY_MINI_BOSS;
   }
   function set name(nNameID)
   {
      this._nNameID = Number(nNameID);
   }
   function get name()
   {
      return this.api.lang.getMonstersText(this._nNameID).n;
   }
   function get kickable()
   {
      return this.api.lang.getMonstersText(this._nNameID).k;
   }
   function get category()
   {
      return this.api.lang.getMonstersText(this._nNameID).b;
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
   function get resistances()
   {
      var _loc2_ = this.api.lang.getMonstersText(this._nNameID)["g" + this._nPowerLevel].r;
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
   function alertChatText()
   {
      var _loc2_ = this.api.datacenter.Map;
      return this.name + " niveau " + this.Level + " en " + _loc2_.x + "," + _loc2_.y + ".";
   }
}

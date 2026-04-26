class dofus.datacenter.DofusMap extends ank.battlefield.datacenter.Map
{
   var eaMountParks;
   var id;
   function DofusMap(nID)
   {
      super(nID);
   }
   function get coordinates()
   {
      var _loc3_ = _global.API.lang.getMapText(this.id);
      return _global.API.lang.getText("COORDINATES") + " : " + _loc3_.x + ", " + _loc3_.y;
   }
   function get x()
   {
      return _global.API.lang.getMapText(this.id).x;
   }
   function get y()
   {
      return _global.API.lang.getMapText(this.id).y;
   }
   function get superarea()
   {
      var _loc3_ = _global.API.lang;
      return _loc3_.getMapAreaInfos(this.subarea).superareaID;
   }
   function get area()
   {
      var _loc3_ = _global.API.lang;
      return _loc3_.getMapAreaInfos(this.subarea).areaID;
   }
   function get subarea()
   {
      var _loc3_ = _global.API.lang;
      return _loc3_.getMapText(this.id).sa;
   }
   function get musics()
   {
      var _loc3_ = _global.API.lang;
      return _loc3_.getMapSubAreaText(this.subarea).m;
   }
   function get dungeonID()
   {
      return Number(_global.API.lang.getMapText(this.id).d);
   }
   function get dungeon()
   {
      return _global.API.lang.getDungeonText(this.dungeonID);
   }
   function get dungeonName()
   {
      return this.dungeon.n;
   }
   function get dungeonFloorName()
   {
      return this.dungeonCurrentMap.n;
   }
   function get dungeonCurrentMap()
   {
      return this.dungeon.m[this.id];
   }
   function get isDungeon()
   {
      if(this.id == 9589)
      {
         return false;
      }
      return !_global.isNaN(this.dungeonID);
   }
   static function isJail(nMapId)
   {
      switch(nMapId)
      {
         case 10240:
         case 8726:
         case 666:
            return true;
         default:
            return false;
      }
   }
   static function isTournament(nMapId)
   {
      switch(nMapId)
      {
         case 12224:
         case 12229:
         case 12225:
         case 12223:
         case 12228:
         case 12226:
         case 12227:
         case 7285:
         case 7286:
         case 7280:
         case 7283:
         case 7281:
         case 10368:
         case 7282:
            return true;
         default:
            return false;
      }
   }
   function get firstMountPark()
   {
      return this.eaMountParks[0];
   }
   function getMountPark(instance)
   {
      var _loc3_ = 0;
      var _loc4_;
      while(_loc3_ < this.eaMountParks.length)
      {
         _loc4_ = this.eaMountParks[_loc3_];
         if(_loc4_.instanceId == instance)
         {
            return _loc4_;
         }
         _loc3_ += 1;
      }
      return undefined;
   }
   function get mountParks()
   {
      return this.eaMountParks;
   }
   function resetMountPark()
   {
      this.eaMountParks = new ank.utils.ExtendedArray();
   }
   function addMountPark(oPark)
   {
      this.eaMountParks.push(oPark);
   }
}

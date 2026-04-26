class dofus.datacenter.MonsterInBidHouse
{
   var _nBidHouseType;
   var _nMonsterID;
   var name;
   function MonsterInBidHouse(nMonsterID, nBidHouseType)
   {
      this._nMonsterID = nMonsterID;
      this._nBidHouseType = nBidHouseType;
      this.name = this.api.lang.getMonstersText(this._nMonsterID).n;
   }
   function get api()
   {
      return _global.API;
   }
   function get unicID()
   {
      return this._nMonsterID;
   }
   function get type()
   {
      return this.getAssociatedSoulStoneCategoryID();
   }
   function get label()
   {
      return this.name;
   }
   function get contentPath()
   {
      return dofus.Constants.CLIPS_PERSOS_PATH + String(this.gfx) + ".swf";
   }
   function get gfx()
   {
      return this.api.lang.getMonstersText(this._nMonsterID).g;
   }
   function get iconFile()
   {
      return "MonsterListItem";
   }
   function get params()
   {
      return {contentPath:this.contentPath};
   }
   function get isMonsterInBidHouse()
   {
      return true;
   }
   function get style()
   {
      return "";
   }
   function getAssociatedSoulStoneCategoryID()
   {
      var _loc2_;
      if(this._nBidHouseType == undefined)
      {
         _loc2_ = this.api.lang.getMonstersText(this._nMonsterID);
         if(dofus.datacenter.Monster.isMiniBossCategory(_loc2_.b))
         {
            return dofus.datacenter.Item.TYPE_FULL_SOUL_STONE_ARCHI;
         }
         if(_loc2_.d)
         {
            return dofus.datacenter.Item.TYPE_FULL_SOUL_STONE_BOSS;
         }
         return dofus.datacenter.Item.TYPE_FULL_SOUL_STONE_NORMAL;
      }
      return this._nBidHouseType;
   }
}

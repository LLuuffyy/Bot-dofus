class dofus.datacenter.ItemSet extends Object
{
   var _aEffects;
   var _aItems;
   var _nID;
   var _sEffects;
   var api;
   function ItemSet(nID, sEffects, aItemIDs)
   {
      super();
      this.initialize(nID,sEffects,aItemIDs);
   }
   function get id()
   {
      return this._nID;
   }
   function get name()
   {
      var _loc2_ = this.api.lang.getItemSetText(this._nID).n;
      if(dofus.Constants.DEBUG)
      {
         _loc2_ += " (" + this.id + ")";
      }
      return _loc2_;
   }
   function get description()
   {
      return this.api.lang.getItemSetText(this._nID).d;
   }
   function get itemCount()
   {
      return this._aItems.length;
   }
   function get items()
   {
      return this._aItems;
   }
   function get effects()
   {
      var _loc2_ = this._aEffects;
      var _loc3_ = dofus.datacenter.Item.getItemDescriptionEffects(_loc2_,undefined,true,false);
      if(!dofus.Constants.DEBUG_ACTIF)
      {
      }
      if(_loc3_.length == 0 && _loc2_ != undefined && _loc2_.length > 0)
      {
         if(!dofus.Constants.DEBUG_ACTIF)
         {
         }
         _loc3_ = dofus.datacenter.Item.getItemDescriptionEffects(_loc2_,[],false,false);
      }
      return _loc3_;
   }
   function initialize(nID, sEffects, aItemIDs)
   {
      if(sEffects == undefined)
      {
         sEffects = "";
      }
      if(aItemIDs == undefined)
      {
         aItemIDs = [];
      }
      this.api = _global.API;
      this._nID = nID;
      this.setEffects(sEffects);
      this.setItems(aItemIDs);
   }
   function setEffects(compressedData)
   {
      this._sEffects = compressedData;
      this._aEffects = [];
      var _loc4_ = compressedData.split(",");
      var _loc5_ = 0;
      var _loc6_;
      while(_loc5_ < _loc4_.length)
      {
         _loc6_ = _loc4_[_loc5_].split("#");
         _loc6_[0] = _global.parseInt(_loc6_[0],16);
         _loc6_[1] = _loc6_[1] != "0" ? _global.parseInt(_loc6_[1],16) : undefined;
         _loc6_[2] = _loc6_[2] != "0" ? _global.parseInt(_loc6_[2],16) : undefined;
         _loc6_[3] = _loc6_[3] != "0" ? _global.parseInt(_loc6_[3],16) : undefined;
         this._aEffects.push(_loc6_);
         _loc5_ += 1;
      }
   }
   function setItems(aItemIDs)
   {
      var _loc4_ = this.api.lang.getItemSetText(this._nID).i;
      this._aItems = [];
      var _loc5_ = {};
      for(var _loc6_ in aItemIDs)
      {
         _loc5_[aItemIDs[_loc6_]] = true;
      }
      var _loc7_ = 0;
      var _loc8_;
      var _loc9_;
      var _loc10_;
      while(_loc7_ < _loc4_.length)
      {
         _loc8_ = Number(_loc4_[_loc7_]);
         if(!_global.isNaN(_loc8_))
         {
            if(_loc8_ < dofus.datacenter.evenemential.ItemUpgrader.UPGRADE_MULTIPLICATOR)
            {
               _loc9_ = new dofus.datacenter.Item(0,_loc8_,1);
               _loc10_ = _loc5_[_loc8_] == true || (_loc5_[_loc8_ + dofus.datacenter.evenemential.ItemUpgrader.UPGRADE_MULTIPLICATOR] == true || _loc5_[_loc8_ + 2 * dofus.datacenter.evenemential.ItemUpgrader.UPGRADE_MULTIPLICATOR] == true);
               this._aItems.push({isEquiped:_loc10_,item:_loc9_});
            }
         }
         _loc7_ += 1;
      }
   }
}

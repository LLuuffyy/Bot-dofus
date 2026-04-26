class dofus.datacenter.InventoryShortcutItem extends Object
{
   var _nGenericID;
   var _nPosition;
   var _oRealItem;
   var _oUnicInfos;
   var _sEffects;
   var api;
   static var OBJI_DEFAULT_FRAME = "H0";
   function InventoryShortcutItem(nGenericID, nPosition, sEffects)
   {
      super();
      this.api = _global.API;
      this._nGenericID = nGenericID;
      this._nPosition = nPosition;
      this._sEffects = sEffects;
      this._oUnicInfos = this.api.lang.getItemUnicText(nGenericID);
   }
   function get isShortcut()
   {
      return true;
   }
   function get label()
   {
      if(this.isRealItemEquiped)
      {
         return "Eq";
      }
      var _loc2_ = this.Quantity;
      if(_loc2_ > 1)
      {
         return String(_loc2_);
      }
      return undefined;
   }
   function findEquipedRealItem()
   {
      var _loc2_ = this.api.datacenter.Player.InventoryByItemPositions.getItems();
      var _loc3_;
      for(var k in _loc2_)
      {
         _loc3_ = _loc2_[k];
         if(_loc3_.unicID == this._nGenericID)
         {
            if(_loc3_.compressedEffects == this._sEffects)
            {
               return _loc3_;
            }
         }
      }
      return undefined;
   }
   function findRealItem()
   {
      var _loc2_;
      if(this._oRealItem != undefined)
      {
         if(!this._oRealItem.isEquiped)
         {
            _loc2_ = this.findEquipedRealItem();
            if(_loc2_ != undefined)
            {
               this._oRealItem = _loc2_;
               return true;
            }
         }
         if(!this._oRealItem.isRemovedFromInventory)
         {
            return true;
         }
      }
      var _loc3_ = this.api.datacenter.Player.Inventory;
      var _loc5_;
      var _loc4_;
      for(var k in _loc3_)
      {
         _loc5_ = _loc3_[k];
         if(_loc5_.unicID == this._nGenericID)
         {
            if(_loc5_.compressedEffects == this._sEffects)
            {
               _loc4_ = _loc5_;
               if(_loc5_.isEquiped)
               {
                  break;
               }
            }
         }
      }
      var _loc6_ = _loc4_ != undefined;
      this._oRealItem = _loc4_;
      return _loc6_;
   }
   function get realLinkedItem()
   {
      return !this.findRealItem() ? undefined : this._oRealItem;
   }
   function get genericID()
   {
      return this._nGenericID;
   }
   function get position()
   {
      return this._nPosition;
   }
   function get compressedEffects()
   {
      return this._sEffects;
   }
   function get type()
   {
      return !this.findRealItem() ? Number(this._oUnicInfos.t) : this._oRealItem.type;
   }
   function get gfx()
   {
      return !this.findRealItem() ? this._oUnicInfos.g : this._oRealItem.gfx;
   }
   function get iconFile()
   {
      return dofus.Constants.ITEMS_PATH + this.type + "/" + this.gfx + ".swf";
   }
   function get params()
   {
      if(this.findRealItem())
      {
         return this._oRealItem.params;
      }
      return {frame:dofus.datacenter.InventoryShortcutItem.OBJI_DEFAULT_FRAME};
   }
   function get isRealItemEquiped()
   {
      return !this.findRealItem() ? false : this._oRealItem.isEquiped;
   }
   function get ID()
   {
      return !this.findRealItem() ? -1 : this._oRealItem.ID;
   }
   function get Quantity()
   {
      if(!this.findRealItem())
      {
         return 0;
      }
      return this._oRealItem.Quantity;
   }
   function get canUse()
   {
      return this._oUnicInfos.u != undefined && this.findRealItem();
   }
   function get canTarget()
   {
      return this._oUnicInfos.ut != undefined && this.findRealItem();
   }
   function get name()
   {
      var _loc2_ = ank.utils.PatternDecoder.getDescription(this.api.lang.fetchString(this._oUnicInfos.n),this.api.lang.getItemUnicStringText());
      if(dofus.Constants.DEBUG)
      {
         _loc2_ += " (" + this._nGenericID + ")";
      }
      return _loc2_;
   }
}

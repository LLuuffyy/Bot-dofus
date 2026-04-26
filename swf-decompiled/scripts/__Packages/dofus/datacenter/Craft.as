class dofus.datacenter.Craft extends Object
{
   var _aItems;
   var _nDifficulty;
   var _oCraftItem;
   var _oSkill;
   var api;
   var name;
   function Craft(nID, oSkill)
   {
      super();
      this.initialize(nID,oSkill);
   }
   function get skill()
   {
      return this._oSkill;
   }
   function get craftItem()
   {
      return this._oCraftItem;
   }
   function get items()
   {
      return this._aItems;
   }
   function get itemsCount()
   {
      return this._aItems.length;
   }
   function get craftLevel()
   {
      return this.craftItem.level;
   }
   function get difficulty()
   {
      return this._nDifficulty;
   }
   function initialize(nID, oSkill)
   {
      this.api = _global.API;
      this._oSkill = oSkill;
      this._oCraftItem = new dofus.datacenter.Item(0,nID,1);
      this.name = this._oCraftItem.name;
      var _loc4_ = this.api.lang.getCraftText(nID);
      this._aItems = [];
      var _loc5_;
      var _loc6_;
      if(!_global.isNaN(_loc4_.length))
      {
         _loc5_ = 0;
         while(_loc5_ < _loc4_.length)
         {
            _loc6_ = new dofus.datacenter.Item(0,_loc4_[_loc5_][0],_loc4_[_loc5_][1]);
            this._aItems.push(_loc6_);
            _loc5_ = _loc5_ + 1;
         }
      }
      if(this._aItems.length < Number(this._oSkill.param1) - 4)
      {
         this._nDifficulty = 1;
      }
      else if(this._aItems.length < Number(this._oSkill.param1) - 2)
      {
         this._nDifficulty = 2;
      }
      else
      {
         this._nDifficulty = 3;
      }
   }
}

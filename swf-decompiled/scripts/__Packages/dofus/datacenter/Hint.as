class dofus.datacenter.Hint extends Object
{
   var _oData;
   var api;
   function Hint(data)
   {
      super();
      this.api = _global.API;
      this._oData = data;
   }
   function get mapID()
   {
      return this._oData.m;
   }
   function get name()
   {
      return this._oData.n;
   }
   function get category()
   {
      return this.api.lang.getHintsCategory(this.categoryID).n;
   }
   function get categoryID()
   {
      return this._oData.c;
   }
   function get coordinates()
   {
      return this.x + ", " + this.y;
   }
   function get x()
   {
      if(this._oData.m == undefined)
      {
         return this._oData.x;
      }
      return this.api.lang.getMapText(this._oData.m).x;
   }
   function get y()
   {
      if(this._oData.m == undefined)
      {
         return this._oData.y;
      }
      return this.api.lang.getMapText(this._oData.m).y;
   }
   function get superAreaID()
   {
      var _loc2_ = this.api.lang.getMapText(this._oData.m).sa;
      var _loc3_ = this.api.lang.getMapSubAreaText(_loc2_).a;
      var _loc4_ = this.api.lang.getMapAreaText(_loc3_).sua;
      return _loc4_;
   }
   function get gfx()
   {
      return this._oData.g;
   }
   static function getConquestAreaHints()
   {
      var _loc2_ = _global.API;
      var _loc3_ = _loc2_.datacenter.Conquest.worldDatas;
      var _loc4_ = [];
      var _loc5_ = new String();
      var _loc6_ = 0;
      var _loc8_;
      var _loc7_;
      while(_loc6_ < _loc3_.areas.length)
      {
         if(_loc3_.areas[_loc6_].alignment == 1)
         {
            _loc8_ = _loc2_.lang.getText("BONTARIAN_PRISM");
            _loc7_ = 420;
         }
         if(_loc3_.areas[_loc6_].alignment == 2)
         {
            _loc8_ = _loc2_.lang.getText("BRAKMARIAN_PRISM");
            _loc7_ = 421;
         }
         _loc4_.push({g:_loc7_,m:_loc3_.areas[_loc6_].prism,n:_loc8_,superAreaID:_loc2_.lang.getMapAreaText(_loc3_.areas[_loc6_].id).a});
         _loc6_ = _loc6_ + 1;
      }
      return _loc4_;
   }
}

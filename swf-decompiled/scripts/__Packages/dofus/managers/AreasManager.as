class dofus.managers.AreasManager extends dofus.utils.ApiElement
{
   var _oAreasCoords;
   var _oSubAreasCoords;
   static var _sSelf = null;
   function AreasManager()
   {
      super();
      dofus.managers.AreasManager._sSelf = this;
   }
   static function getInstance()
   {
      return dofus.managers.AreasManager._sSelf;
   }
   function initialize(oAPI)
   {
      super.initialize(oAPI);
      this._oAreasCoords = {};
      this._oSubAreasCoords = {};
      var _loc4_ = this.api.lang.getAllMapsInfos();
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      for(var k in _loc4_)
      {
         _loc5_ = _loc4_[k];
         _loc6_ = this.api.lang.getMapSubAreaText(_loc5_.sa).a;
         _loc7_ = this.api.lang.getMapAreaText(_loc6_).sua;
         _loc8_ = _loc7_ + "_" + _loc5_.x + "_" + _loc5_.y;
         if(this._oAreasCoords[_loc8_] == undefined)
         {
            this._oAreasCoords[_loc8_] = _loc6_;
            this._oSubAreasCoords[_loc8_] = _loc5_.sa;
         }
      }
   }
   function getAreaIDFromCoordinates(nX, nY, nSuperAreaID)
   {
      if(nSuperAreaID == undefined)
      {
         nSuperAreaID = 0;
      }
      return this._oAreasCoords[nSuperAreaID + "_" + nX + "_" + nY];
   }
   function getSubAreaIDFromCoordinates(nX, nY, nSuperAreaID)
   {
      if(nSuperAreaID == undefined)
      {
         nSuperAreaID = 0;
      }
      return this._oSubAreasCoords[nSuperAreaID + "_" + nX + "_" + nY];
   }
}

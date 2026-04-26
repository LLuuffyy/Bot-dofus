class dofus.datacenter.ConquestWorldData extends Object
{
   var _aAreas;
   var _aVillages;
   var _nOwnedAreas;
   var _nOwnedVillages;
   var _nPossibleAreas;
   var _nTotalAreas;
   var _nTotalVillages;
   function ConquestWorldData()
   {
      super();
   }
   function get ownedAreas()
   {
      return this._nOwnedAreas;
   }
   function set ownedAreas(value)
   {
      this._nOwnedAreas = value;
   }
   function get totalAreas()
   {
      return this._nTotalAreas;
   }
   function set totalAreas(value)
   {
      this._nTotalAreas = value;
   }
   function get possibleAreas()
   {
      return this._nPossibleAreas;
   }
   function set possibleAreas(value)
   {
      this._nPossibleAreas = value;
   }
   function get areas()
   {
      return this._aAreas;
   }
   function set areas(value)
   {
      this._aAreas = value;
   }
   function get ownedVillages()
   {
      return this._nOwnedVillages;
   }
   function set ownedVillages(value)
   {
      this._nOwnedVillages = value;
   }
   function get totalVillages()
   {
      return this._nTotalVillages;
   }
   function set totalVillages(value)
   {
      this._nTotalVillages = value;
   }
   function get villages()
   {
      return this._aVillages;
   }
   function set villages(value)
   {
      this._aVillages = value;
   }
}

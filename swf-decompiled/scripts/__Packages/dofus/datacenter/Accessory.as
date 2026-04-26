class dofus.datacenter.Accessory extends Object
{
   var _nFrame;
   var _nType;
   var _nUnicID;
   var _oItemText;
   var api;
   function Accessory(nUnicID, nType, nFrame)
   {
      super();
      this.api = _global.API;
      this.initialize(nUnicID,nType,nFrame);
   }
   function get unicID()
   {
      return this._nUnicID;
   }
   function get type()
   {
      if(this._nType != undefined)
      {
         return this._nType;
      }
      var _loc2_ = this._oItemText.t;
      if(_loc2_ == 140)
      {
         return 16;
      }
      if(_loc2_ == 141)
      {
         return 17;
      }
      if(_loc2_ == 143)
      {
         return 82;
      }
      if(_loc2_ == 144)
      {
         return 18;
      }
      return _loc2_;
   }
   function get gfxID()
   {
      return this._oItemText.g;
   }
   function get gfx()
   {
      return this.type + "_" + this.gfxID;
   }
   function get frame()
   {
      return this._nFrame;
   }
   function initialize(nUnicID, nType, nFrame)
   {
      this._nUnicID = nUnicID;
      if(nFrame != undefined)
      {
         this._nFrame = nFrame;
      }
      if(nType != undefined)
      {
         this._nType = nType;
      }
      this._oItemText = this.api.lang.getItemUnicText(nUnicID);
   }
}

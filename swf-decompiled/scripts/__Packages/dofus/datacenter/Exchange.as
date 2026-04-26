class dofus.datacenter.Exchange extends Object
{
   var _eaCoopGarbage;
   var _eaDistantGarbage;
   var _eaInventory;
   var _eaLocalGarbage;
   var _eaReadyStates;
   var _nDistantOgrine;
   var _nDistantPlayerID;
   var _nLocalOgrine;
   var dispatchEvent;
   var _nLocalKama = 0;
   var _nDistantKama = 0;
   function Exchange(nDistantPlayerID)
   {
      super();
      this.initialize(nDistantPlayerID);
   }
   function set inventory(eaInventory)
   {
      this._eaInventory = eaInventory;
   }
   function get inventory()
   {
      return this._eaInventory;
   }
   function get localGarbage()
   {
      return this._eaLocalGarbage;
   }
   function get distantGarbage()
   {
      return this._eaDistantGarbage;
   }
   function get coopGarbage()
   {
      return this._eaCoopGarbage;
   }
   function get readyStates()
   {
      return this._eaReadyStates;
   }
   function get distantPlayerID()
   {
      return this._nDistantPlayerID;
   }
   function set localKama(nLocalKama)
   {
      this._nLocalKama = nLocalKama;
      this.dispatchEvent({type:"localKamaChange",value:nLocalKama});
   }
   function get localKama()
   {
      return this._nLocalKama;
   }
   function set distantKama(nDistantKama)
   {
      this._nDistantKama = nDistantKama;
      this.dispatchEvent({type:"distantKamaChange",value:nDistantKama});
   }
   function set distantOgrine(nDistantOgrine)
   {
      this._nDistantOgrine = nDistantOgrine;
      this.dispatchEvent({type:"distantOgrineChange",value:nDistantOgrine});
   }
   function get distantOgrine()
   {
      return this._nDistantOgrine;
   }
   function set localOgrine(nLocalOgrine)
   {
      this._nLocalOgrine = nLocalOgrine;
      this.dispatchEvent({type:"localOgrineChange",value:nLocalOgrine});
   }
   function get localOgrine()
   {
      return this._nLocalOgrine;
   }
   function get distantKama()
   {
      return this._nDistantKama;
   }
   function initialize(nDistantPlayerID)
   {
      mx.events.EventDispatcher.initialize(this);
      this._nDistantPlayerID = nDistantPlayerID;
      this._eaLocalGarbage = new ank.utils.ExtendedArray();
      this._eaDistantGarbage = new ank.utils.ExtendedArray();
      this._eaCoopGarbage = new ank.utils.ExtendedArray();
      this._eaReadyStates = new ank.utils.ExtendedArray();
      this._eaReadyStates[0] = false;
      this._eaReadyStates[1] = false;
      this._nLocalOgrine = 0;
      this._nDistantOgrine = 0;
   }
   function clearLocalGarbage()
   {
      this._eaLocalGarbage.removeAll();
   }
   function clearDistantGarbage()
   {
      this._eaDistantGarbage.removeAll();
   }
   function clearCoopGarbage()
   {
      this._eaCoopGarbage.removeAll();
   }
}

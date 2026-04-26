class dofus.datacenter.PrismSprite extends dofus.datacenter.PlayableCharacter
{
   var _aAlignment;
   var _nLinkedMonsterId;
   var api;
   var initialize;
   function PrismSprite(sID, clipClass, sGfxFile, cellNum, dir, gfxID)
   {
      super();
      this.initialize(sID,clipClass,sGfxFile,cellNum,dir,gfxID);
   }
   function get name()
   {
      return this.api.lang.getMonstersText(this._nLinkedMonsterId).n;
   }
   function set linkedMonster(value)
   {
      this._nLinkedMonsterId = value;
   }
   function get linkedMonster()
   {
      return this._nLinkedMonsterId;
   }
   function set alignment(value)
   {
      this._aAlignment = value;
   }
   function get alignment()
   {
      return this._aAlignment;
   }
}

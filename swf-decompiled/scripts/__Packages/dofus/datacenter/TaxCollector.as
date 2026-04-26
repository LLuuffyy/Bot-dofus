class dofus.datacenter.TaxCollector extends dofus.datacenter.PlayableCharacter
{
   var _aResistances;
   var _bIsMine;
   var _oEmblem;
   var _sGuildName;
   var _sName;
   var initialize;
   function TaxCollector(sID, clipClass, sGfxFile, cellNum, dir, gfxID, isMine)
   {
      super();
      this.initialize(sID,clipClass,sGfxFile,cellNum,dir,gfxID,isMine);
   }
   function set name(sName)
   {
      this._sName = sName;
   }
   function get name()
   {
      return this._sName;
   }
   function set guildName(sGuildName)
   {
      this._sGuildName = sGuildName;
   }
   function get guildName()
   {
      return this._sGuildName;
   }
   function set emblem(oEmblem)
   {
      this._oEmblem = oEmblem;
   }
   function get emblem()
   {
      return this._oEmblem;
   }
   function set resistances(aResistances)
   {
      this._aResistances = aResistances;
   }
   function get resistances()
   {
      return this._aResistances;
   }
   function set isMine(bIsMine)
   {
      this._bIsMine = bIsMine;
   }
   function get isMine()
   {
      return this._bIsMine;
   }
}

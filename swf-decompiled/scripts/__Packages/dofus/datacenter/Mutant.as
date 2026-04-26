class dofus.datacenter.Mutant extends dofus.datacenter.Character
{
   var _bShowIsPlayer;
   var _nMonsterID;
   var _nPowerLevel;
   var _sPlayerName;
   var api;
   var initialize;
   function Mutant(sID, clipClass, sGfxFile, cellNum, dir, gfxID, bShowIsPlayer)
   {
      super();
      this._bShowIsPlayer = bShowIsPlayer == undefined ? false : bShowIsPlayer;
      this.initialize(sID,clipClass,sGfxFile,cellNum,dir,gfxID);
   }
   function get name()
   {
      if(!this._bShowIsPlayer)
      {
         return this.monsterName;
      }
      return this._sPlayerName;
   }
   function set monsterID(n)
   {
      this._nMonsterID = n;
   }
   function get monsterID()
   {
      return this._nMonsterID;
   }
   function set playerName(n)
   {
      this._sPlayerName = n;
   }
   function get playerName()
   {
      return this._sPlayerName;
   }
   function get monsterName()
   {
      return this.api.lang.getMonstersText(this._nMonsterID).n;
   }
   function get alignment()
   {
      return new dofus.datacenter.Alignment();
   }
   function set powerLevel(nPowerLevel)
   {
      this._nPowerLevel = Number(nPowerLevel);
   }
   function get powerLevel()
   {
      return this._nPowerLevel;
   }
   function get Level()
   {
      return this.api.lang.getMonstersText(this._nMonsterID)["g" + this._nPowerLevel].l;
   }
   function get resistances()
   {
      return this.api.lang.getMonstersText(this._nMonsterID)["g" + this._nPowerLevel].r;
   }
   function set showIsPlayer(b)
   {
      this._bShowIsPlayer = b;
   }
   function get showIsPlayer()
   {
      return this._bShowIsPlayer;
   }
}

class dofus.datacenter.Conquest extends Object
{
   var _cbdAlignBonus;
   var _cbdAlignMalus;
   var _cbdRankMultiplicator;
   var _cwdDatas;
   var _eaAttackers;
   var _eaPlayers;
   var dispatchEvent;
   function Conquest()
   {
      super();
      this.clear();
      mx.events.EventDispatcher.initialize(this);
   }
   function clear()
   {
      this._eaPlayers = new ank.utils.ExtendedArray();
      this._eaAttackers = new ank.utils.ExtendedArray();
   }
   function get alignBonus()
   {
      return this._cbdAlignBonus;
   }
   function set alignBonus(cbd)
   {
      this._cbdAlignBonus = cbd;
      this.dispatchEvent({type:"bonusChanged"});
   }
   function get alignMalus()
   {
      return this._cbdAlignMalus;
   }
   function set alignMalus(cbd)
   {
      this._cbdAlignMalus = cbd;
      this.dispatchEvent({type:"bonusChanged"});
   }
   function get rankMultiplicator()
   {
      return this._cbdRankMultiplicator;
   }
   function set rankMultiplicator(cbd)
   {
      this._cbdRankMultiplicator = cbd;
      this.dispatchEvent({type:"bonusChanged"});
   }
   function get players()
   {
      return this._eaPlayers;
   }
   function set players(value)
   {
      this._eaPlayers = value;
   }
   function get attackers()
   {
      return this._eaAttackers;
   }
   function set attackers(value)
   {
      this._eaAttackers = value;
   }
   function get worldDatas()
   {
      return this._cwdDatas;
   }
   function set worldDatas(value)
   {
      this._cwdDatas = value;
      this.dispatchEvent({type:"worldDataChanged",value:value});
   }
}

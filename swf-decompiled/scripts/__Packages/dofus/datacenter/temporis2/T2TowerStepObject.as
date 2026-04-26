class dofus.datacenter.temporis2.T2TowerStepObject
{
   var _bIsCompletedByPlayer;
   var _bIsHintVisible;
   var _bIsKeyFound;
   var _bIsUnlocked;
   var _nID;
   var api;
   function T2TowerStepObject(nID, bIsUnlocked, bIsKeyFound, bIsCompletedByPlayer, bIsHintVisible)
   {
      this.api = _global.API;
      this._nID = nID;
      this._bIsUnlocked = bIsUnlocked;
      this._bIsKeyFound = bIsKeyFound;
      this._bIsCompletedByPlayer = bIsCompletedByPlayer;
      this._bIsHintVisible = bIsHintVisible;
      mx.events.EventDispatcher.initialize(this);
   }
   function get id()
   {
      return this._nID;
   }
   function get isUnlocked()
   {
      return this._bIsUnlocked;
   }
   function get isKeyFound()
   {
      return this._bIsKeyFound;
   }
   function get isCompletedByPlayer()
   {
      return this._bIsCompletedByPlayer;
   }
   function get isHintVisible()
   {
      return this._bIsHintVisible;
   }
   function get challengeName()
   {
      if(!this._bIsHintVisible)
      {
         return this.api.lang.getText("T2_TOWER_STEP_NOT_UNLOCKED");
      }
      return this.api.lang.getText("T2_TOWER_STEP_" + this._nID + "_NAME");
   }
   function get challengeHint()
   {
      if(!this._bIsHintVisible)
      {
         return this.api.lang.getText("T2_TOWER_STEP_NOT_UNLOCKED_GUIDE");
      }
      return this.api.lang.getText("T2_TOWER_STEP_" + this._nID + "_HINT");
   }
   function get iconFile()
   {
      return "Temporis2TowerStepItem";
   }
   function get challengeIlluFile()
   {
      if(!this._bIsUnlocked)
      {
         return dofus.Constants.EVENEMENTIALS_TEMPORIS_2_TOWER_ILLUS_PATH + "unknown.swf";
      }
      return dofus.Constants.EVENEMENTIALS_TEMPORIS_2_TOWER_ILLUS_PATH + this._nID + ".swf";
   }
   function get forceReloadOnContainer()
   {
      return true;
   }
   function get params()
   {
      return {stepObject:this};
   }
}

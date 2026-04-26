class dofus.managers.chat.FightEventsMessagesBuffer
{
   var _aMessagesBuffer;
   var _api;
   var _nPrintAllTimeout;
   function FightEventsMessagesBuffer(api)
   {
      this._api = api;
      this._aMessagesBuffer = [];
   }
   function get api()
   {
      return this._api;
   }
   function addFightEventMessage(nActionId, aPermanentArgs, aEvolutiveArgsToAppend, sPlayerId, sPlayerName)
   {
      var _loc8_ = this.getFightEventMessage(nActionId);
      if(_loc8_ == undefined)
      {
         _loc8_ = new dofus.datacenter.chat.FightEventMessage(this.api,nActionId,aPermanentArgs);
         this._aMessagesBuffer.push(_loc8_);
      }
      _loc8_.addPlayer(sPlayerId,sPlayerName);
      _loc8_.appendEvolutiveArgs(aEvolutiveArgsToAppend);
      if(this._nPrintAllTimeout != undefined)
      {
         _global.clearTimeout(this._nPrintAllTimeout);
      }
      var _loc9_ = _global.setTimeout(this,"printAll",50);
      this._nPrintAllTimeout = _loc9_;
   }
   function getFightEventMessage(nActionId)
   {
      var _loc3_ = this._aMessagesBuffer;
      var _loc4_ = 0;
      var _loc5_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_];
         if(_loc5_.actionId == nActionId)
         {
            return _loc5_;
         }
         _loc4_ += 1;
      }
      return undefined;
   }
   function printAll()
   {
      if(this._nPrintAllTimeout != undefined)
      {
         _global.clearTimeout(this._nPrintAllTimeout);
         this._nPrintAllTimeout = undefined;
      }
      if(this._aMessagesBuffer.length == 0)
      {
         return undefined;
      }
      var _loc3_ = this._aMessagesBuffer;
      this._aMessagesBuffer = [];
      var _loc4_ = new ank.utils.ExtendedObject();
      var _loc5_ = new ank.utils.ExtendedObject();
      var _loc6_ = this.api.datacenter.Sprites;
      var _loc8_;
      var _loc9_;
      for(var _loc7_ in _loc6_.getItems())
      {
         _loc8_ = _loc6_.getItemAt(_loc7_);
         _loc9_ = _loc8_.Team;
         if(_loc9_ != undefined)
         {
            if(_loc9_ == 0)
            {
               _loc4_.addItemAt(_loc8_.id,_loc8_);
            }
            else if(_loc9_ == 1)
            {
               _loc5_.addItemAt(_loc8_.id,_loc8_);
            }
         }
      }
      var _loc10_ = 0;
      var _loc11_;
      var _loc12_;
      while(_loc10_ < _loc3_.length)
      {
         _loc11_ = _loc3_[_loc10_];
         _loc12_ = _loc11_.getPrintableString(_loc4_,_loc5_);
         if(!(_loc12_ == undefined || _loc12_.length == 0))
         {
            this.api.kernel.showMessage(undefined,_loc12_,"INFO_FIGHT_CHAT");
         }
         _loc10_ += 1;
      }
   }
}

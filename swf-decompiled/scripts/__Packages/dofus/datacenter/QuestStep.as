class dofus.datacenter.QuestStep extends Object
{
   var _aDialogParams;
   var _aNextSteps;
   var _aPreviousSteps;
   var _bAccountQuest;
   var _bRepeatableQuest;
   var _eaObjectives;
   var _nDialogID;
   var _nID;
   var _nState;
   var api;
   function QuestStep(nID, nState, isAccountQuest, isRepeatableQuest, eaObjectives, aPreviousSteps, aNextSteps, nDialogID, aDialogParams)
   {
      super();
      this.initialize(nID,nState,isAccountQuest,isRepeatableQuest,eaObjectives,aPreviousSteps,aNextSteps,nDialogID,aDialogParams);
   }
   function get id()
   {
      return this._nID;
   }
   function get name()
   {
      var _loc2_ = this.api.lang.getQuestStepText(this._nID).n;
      if(_loc2_ != null && dofus.Constants.DEBUG)
      {
         _loc2_ = _loc2_ + " (" + this._nID + ")";
      }
      return _loc2_;
   }
   function get description()
   {
      return this.api.lang.getQuestStepText(this._nID).d;
   }
   function get objectives()
   {
      return this._eaObjectives;
   }
   function get allSteps()
   {
      var _loc2_ = new ank.utils.ExtendedArray();
      var _loc3_ = 0;
      while(_loc3_ < this._aPreviousSteps.length)
      {
         _loc2_.push(new dofus.datacenter.QuestStep(this._aPreviousSteps[_loc3_],2));
         _loc3_ = _loc3_ + 1;
      }
      _loc2_.push(this);
      var _loc4_ = 0;
      while(_loc4_ < this._aNextSteps.length)
      {
         _loc2_.push(new dofus.datacenter.QuestStep(this._aNextSteps[_loc4_],0));
         _loc4_ = _loc4_ + 1;
      }
      return _loc2_;
   }
   function get rewards()
   {
      var _loc2_ = new ank.utils.ExtendedArray();
      var _loc3_ = this.api.lang.getQuestStepText(this._nID).r;
      if(_loc3_[0] != undefined)
      {
         _loc2_.push({iconFile:"UI_QuestXP",label:new ank.utils.ExtendedString(_loc3_[0]).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3)});
      }
      if(_loc3_[1] != undefined)
      {
         _loc2_.push({iconFile:"UI_QuestKamaSymbol",label:new ank.utils.ExtendedString(_loc3_[1]).addMiddleChar(this.api.lang.getConfigText("THOUSAND_SEPARATOR"),3)});
      }
      var _loc4_;
      var _loc5_;
      var _loc6_;
      var _loc7_;
      var _loc8_;
      if(_loc3_[2] != undefined)
      {
         _loc4_ = _loc3_[2];
         _loc5_ = 0;
         while(_loc5_ < _loc4_.length)
         {
            _loc6_ = Number(_loc4_[_loc5_][0]);
            _loc7_ = _loc4_[_loc5_][1];
            _loc8_ = new dofus.datacenter.Item(0,_loc6_,_loc7_);
            _loc2_.push({iconFile:_loc8_.iconFile,label:(_loc7_ == 0 ? "" : "x" + _loc7_ + " ") + _loc8_.name});
            _loc5_ = _loc5_ + 1;
         }
      }
      var _loc9_;
      var _loc10_;
      var _loc11_;
      if(_loc3_[3] != undefined)
      {
         _loc9_ = _loc3_[3];
         _loc10_ = 0;
         while(_loc10_ < _loc9_.length)
         {
            _loc11_ = Number(_loc9_[_loc10_]);
            _loc2_.push({iconFile:dofus.Constants.EMOTES_ICONS_PATH + _loc11_ + ".swf",label:this.api.lang.getEmoteText(_loc11_).n});
            _loc10_ = _loc10_ + 1;
         }
      }
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      if(_loc3_[4] != undefined)
      {
         _loc12_ = _loc3_[4];
         _loc13_ = 0;
         while(_loc13_ < _loc12_.length)
         {
            _loc14_ = Number(_loc12_[_loc13_]);
            _loc15_ = new dofus.datacenter.Job(_loc14_);
            _loc2_.push({iconFile:_loc15_.iconFile,label:_loc15_.name});
            _loc13_ = _loc13_ + 1;
         }
      }
      var _loc16_;
      var _loc17_;
      var _loc18_;
      var _loc19_;
      if(_loc3_[5] != undefined)
      {
         _loc16_ = _loc3_[5];
         _loc17_ = 0;
         while(_loc17_ < _loc16_.length)
         {
            _loc18_ = Number(_loc16_[_loc17_]);
            _loc19_ = new dofus.datacenter.Spell(_loc18_,1);
            _loc2_.push({iconFile:_loc19_.iconFile,label:_loc19_.name,params:_loc19_.params});
            _loc17_ = _loc17_ + 1;
         }
      }
      return _loc2_;
   }
   function get dialogID()
   {
      return this._nDialogID;
   }
   function get dialogParams()
   {
      return this._aDialogParams;
   }
   function get isFinished()
   {
      return this._nState == 2;
   }
   function get isCurrent()
   {
      return this._nState == 1;
   }
   function get isNotDo()
   {
      return this._nState == 0;
   }
   function get hasPrevious()
   {
      return true;
   }
   function get hasNext()
   {
      return true;
   }
   function get isAccountQuest()
   {
      return this._bAccountQuest;
   }
   function get isRepeatableQuest()
   {
      return this._bRepeatableQuest;
   }
   function initialize(nID, nState, isAccountQuest, isRepeatableQuest, eaObjectives, aPreviousSteps, aNextSteps, nDialogID, aDialogParams)
   {
      this.api = _global.API;
      this._nID = nID;
      this._nState = nState;
      this._bAccountQuest = isAccountQuest;
      this._bRepeatableQuest = isRepeatableQuest;
      this._eaObjectives = eaObjectives;
      this._aPreviousSteps = aPreviousSteps;
      this._aNextSteps = aNextSteps;
      this._nDialogID = nDialogID;
      this._aDialogParams = aDialogParams;
   }
}

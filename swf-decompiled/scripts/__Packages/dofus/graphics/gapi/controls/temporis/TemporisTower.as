class dofus.graphics.gapi.controls.temporis.TemporisTower extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _aQuests;
   var _cgSteps;
   var _lblChallengeName;
   var _ldrBackground;
   var _ldrIllu;
   var _ldrTower;
   var _mcBouneStatic;
   var _selectedTowerStepObject;
   var _txtChallengeHint;
   var addToQueue;
   var api;
   static var CLASS_NAME = "TemporisTower";
   function TemporisTower()
   {
      super();
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.controls.temporis.TemporisTower.CLASS_NAME);
   }
   function createChildren()
   {
      this._mcBouneStatic._visible = false;
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
   }
   function addListeners()
   {
      this._cgSteps.addEventListener("selectItem",this);
      this.api.datacenter.Basics.addEventListener("worldUniqueDropsStatesRefresh",this);
      this._ldrBackground.contentPath = dofus.Constants.EVENEMENTIALS_TEMPORIS_2_TOWER_ILLUS_PATH + "floors.swf";
      this._ldrTower.contentPath = dofus.Constants.EVENEMENTIALS_TEMPORIS_2_TOWER_ILLUS_PATH + "tower.swf";
      var ref = this;
      this._mcBouneStatic.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcBouneStatic.onRollOut = function()
      {
         ref.out({target:this});
      };
   }
   function initData()
   {
      var _loc2_ = this.api.lang.getConfigText("TEMPORIS_2_TOWER_COMPLETION_QUEST_ID");
      this.api.network.Quests.getList();
      this.api.network.Quests.getStep(_loc2_);
      this.api.network.Evenemential.sendAskWorldUniqueDropsStates();
   }
   function initGrid()
   {
      var _loc2_ = new ank.utils.ExtendedArray();
      var _loc3_ = this.api.lang.getConfigText("TEMPORIS_2_TOWER_STEPS_COUNT");
      var _loc4_ = this.api.lang.getConfigText("TEMPORIS_2_TOWER_COMPLETION_QUEST_ID");
      var _loc5_ = this.getQuestById(_loc4_);
      var _loc6_ = this.api.lang.getConfigText("TEMPORIS_2_TOWER_COMPLETION_QUEST_STEPS_OFFSET");
      var _loc7_ = this.api.lang.getConfigText("TEMPORIS_2_TOWER_UNLOCK_ITEMS_OFFSET");
      var _loc8_ = true;
      var _loc9_ = true;
      var _loc10_ = this.isQuestFinished(_loc4_);
      var _loc11_ = 1;
      var _loc12_;
      var _loc13_;
      var _loc14_;
      var _loc15_;
      var _loc16_;
      while(_loc11_ <= _loc3_)
      {
         _loc12_ = _loc6_ + _loc11_;
         _loc13_ = _loc7_ + _loc11_;
         _loc14_ = _loc11_ == 1 || this.api.datacenter.Basics.isWorldUniqueItemAlreadyDropped(_loc13_);
         _loc15_ = _loc10_ || this.isQuestStepFinished(_loc5_,_loc12_);
         _loc16_ = new dofus.datacenter.temporis2.T2TowerStepObject(_loc11_,_loc8_,_loc14_,_loc15_,_loc9_);
         _loc16_.addEventListener("onStepObjectIlluOver",this);
         _loc2_.push(_loc16_);
         if(!_loc15_)
         {
            _loc8_ = false;
         }
         if(!_loc14_)
         {
            _loc9_ = false;
         }
         _loc11_ = _loc11_ + 1;
      }
      this._cgSteps.dataProvider = _loc2_;
   }
   function initSelectedTowerStepObject()
   {
      if(this._selectedTowerStepObject == undefined)
      {
         return undefined;
      }
      this._lblChallengeName.text = this._selectedTowerStepObject.challengeName;
      this._txtChallengeHint.text = this._selectedTowerStepObject.challengeHint;
      this._ldrIllu.contentPath = this._selectedTowerStepObject.challengeIlluFile;
   }
   function worldUniqueDropsStatesRefresh(oEvent)
   {
      this.initGrid();
   }
   function selectItem(oEvent)
   {
      var _loc3_ = oEvent.target.contentData;
      if(_loc3_ == undefined)
      {
         return undefined;
      }
      var _loc0_;
      var _loc4_;
      if((_loc0_ = oEvent.owner) === this._cgSteps)
      {
         _loc4_ = dofus.datacenter.temporis2.T2TowerStepObject(_loc3_);
         this.selectStepObject(_loc4_);
      }
   }
   function onStepObjectIlluOver(oEvent)
   {
      this.selectStepObject(oEvent.target);
   }
   function selectStepObject(oItem)
   {
      this._ldrTower._visible = false;
      this._mcBouneStatic._visible = true;
      this._selectedTowerStepObject = oItem;
      this.initSelectedTowerStepObject();
   }
   function setQuests(aQuests)
   {
      this._aQuests = aQuests;
   }
   function isQuestFinished(nQuestId)
   {
      var _loc3_ = this.getQuestById(nQuestId);
      if(_loc3_ == undefined)
      {
         return false;
      }
      return _loc3_.isFinished;
   }
   function getQuestById(nQuestId)
   {
      var _loc3_ = this._aQuests;
      if(_loc3_ == undefined)
      {
         return undefined;
      }
      var _loc4_ = 0;
      var _loc5_;
      while(_loc4_ < _loc3_.length)
      {
         _loc5_ = _loc3_[_loc4_];
         if(_loc5_.id == nQuestId)
         {
            return _loc5_;
         }
         _loc4_ = _loc4_ + 1;
      }
      return undefined;
   }
   function isQuestStepFinished(oQuest, nQuestStepID)
   {
      if(oQuest == undefined)
      {
         return false;
      }
      var _loc4_ = oQuest.getStep(nQuestStepID);
      if(_loc4_ == undefined || _loc4_ == null)
      {
         return false;
      }
      return _loc4_.isFinished;
   }
   function over(oEvent)
   {
      var _loc0_;
      if((_loc0_ = oEvent.target) !== this._mcBouneStatic)
      {
         this.api.ui.hideTooltip();
      }
      else
      {
         this.api.ui.showTooltip(this.api.lang.getText("HINT_TO_FIND_KEY"),oEvent.target,-15);
      }
   }
   function out(oEvent)
   {
      this.api.ui.hideTooltip();
   }
}

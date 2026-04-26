class dofus.graphics.gapi.ui.temporis2.Temporis2TowerStepItem extends dofus.graphics.gapi.core.DofusAdvancedComponent
{
   var _lblFloor;
   var _ldrIllu;
   var _mcChallengeIDBg;
   var _mcIlluBg;
   var _mcKey;
   var _mcStepDone;
   var _mcStepToDo;
   var _oStepObject;
   var addToQueue;
   var api;
   static var CLASS_NAME = "Temporis2TowerStepItem";
   function Temporis2TowerStepItem()
   {
      super();
      this.initialize();
   }
   function get stepObject()
   {
      return this._oStepObject;
   }
   function set stepObject(oStepObject)
   {
      this._oStepObject = oStepObject;
   }
   function initialize()
   {
      this.api = _global.API;
   }
   function init()
   {
      super.init(false,dofus.graphics.gapi.ui.temporis2.Temporis2TowerStepItem.CLASS_NAME);
   }
   function createChildren()
   {
      this._mcStepToDo._visible = false;
      this._mcStepDone._visible = false;
      this._mcKey._visible = false;
      this.addToQueue({object:this,method:this.addListeners});
      this.addToQueue({object:this,method:this.initData});
      this.addToQueue({object:this,method:this.initTexts});
   }
   function addListeners()
   {
      var ref = this;
      this._mcStepDone.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcStepDone.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._mcStepToDo.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcStepToDo.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._mcKey.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcKey.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._mcChallengeIDBg.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcChallengeIDBg.onRollOut = function()
      {
         ref.out({target:this});
      };
      this._mcIlluBg.onRollOver = function()
      {
         ref.over({target:this});
      };
      this._mcIlluBg.onRollOut = function()
      {
         ref.out({target:this});
      };
   }
   function initData()
   {
      this._ldrIllu.contentPath = this._oStepObject.challengeIlluFile;
      if(this._oStepObject.isCompletedByPlayer)
      {
         this._mcStepDone._visible = true;
      }
      else
      {
         this._mcStepToDo._visible = true;
      }
      this._mcKey._visible = true;
      this._mcKey._alpha = !this._oStepObject.isKeyFound ? 20 : 100;
   }
   function initTexts()
   {
      this._lblFloor.text = String(this._oStepObject.id);
   }
   function over(oEvent)
   {
      switch(oEvent.target)
      {
         case this._mcKey:
            if(this._oStepObject.isKeyFound)
            {
               this.api.ui.showTooltip(this.api.lang.getText("T2_TOWER_KEY_FOUND"),oEvent.target,-10);
            }
            else
            {
               this.api.ui.showTooltip(this.api.lang.getText("T2_TOWER_KEY_NOT_FOUND_YET"),oEvent.target,-10);
            }
            return;
         case this._mcStepToDo:
            this.api.ui.showTooltip(this.api.lang.getText("T2_TOWER_STEP_NOT_COMPLETED_YET"),oEvent.target,-20);
            return;
         case this._mcStepDone:
            this.api.ui.showTooltip(this.api.lang.getText("T2_TOWER_STEP_COMPLETED"),oEvent.target,-20);
            return;
         case this._mcChallengeIDBg:
         case this._mcIlluBg:
            this._oStepObject.dispatchEvent({type:"onStepObjectIlluOver"});
            return;
         default:
            this.api.ui.hideTooltip();
            return;
      }
   }
   function out(oEvent)
   {
      this.api.ui.hideTooltip();
   }
}
